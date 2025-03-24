using Sandbox.Events;
using Sandbox.Utility;
using System;

namespace KOTH;

public sealed class Needle : Projectile, IGameEventHandler<ProjectileCollideEvent>
{
	[Property, Group("Healing")] public int MinHealing { get; set; } = 50;
	[Property, Group("Healing")] public int MaxHealing { get; set; } = 100;
	[Property, Group("Damage")] public int MinDamage { get; set; } = 40;
	[Property, Group("Damage")] public int MaxDamage { get; set; } = 90;

	/////////////////////////////////////////////////////////////////////////////////////

	private bool IsAttached = false;
	private GameObject AttachedGameObject = null;
	private Vector3 HitLocation;
	private Transform InitArmedWorldTransformAttachedObject;

	/////////////////////////////////////////////////////////////////////////////////////

	public TimedDestroyComponent DestroyComponent { get; private set; }

	private readonly float MaxAliveTime = 30; // seconds
	protected override void OnStart()
	{
		DestroyComponent = Components.Create<TimedDestroyComponent>();
		DestroyComponent.Time = MaxAliveTime;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if (IsAttached)
		{
			if (!AttachedGameObject.IsValid())
			{
				GameObject.Root.Destroy();
				return;
			}
			
			Vector3 GameObjectOffset = AttachedGameObject.WorldPosition - InitArmedWorldTransformAttachedObject.Position;
			GameObject.WorldPosition = HitLocation + GameObjectOffset;
		}
	}

	private float MaxDistance = 1800f;
	public void OnGameEvent(ProjectileCollideEvent EventArgs)
	{
		var Collision = EventArgs.ProjectileCollision;
		var CollidePlayerPawn = Collision.HitObject.Root.Components.Get<PlayerPawn>();
		if (CollidePlayerPawn.IsValid())
		{
			if (!OwnerPlayerPawn.IsValid())
			{
				Log.Warning($"owner pawn not valid on needle collision {this}");
				return;
			}

			var Distance = OwnerPlayerPawn.WorldPosition.Distance(CollidePlayerPawn.WorldPosition);
			float InterpFactor = Distance / MaxDistance;

			if (CollidePlayerPawn.Team == OwnerPlayerPawn.Team)
			{
				var Healing = MathX.Lerp(MinHealing, MaxHealing, InterpFactor).CeilToInt();

				FHealingRequest HealingRequest = new()
				{
					TargetDamageComponent = CollidePlayerPawn.DamageComponent,
					TargetPlayerPawn = CollidePlayerPawn,
					AttackerPlayerPawn = OwnerPlayerPawn,
					BaseHealing = Healing,
					AllowOverheal = false,
					HealingType = EHealingType.OneOff,
				};
				Scene.Dispatch(new HealingRequestEvent(HealingRequest));
			}
			else
			{
				var Damage = MathX.Lerp(MinDamage, MaxDamage, InterpFactor);
				FDamageRequest DamageRequest = new()
				{
					TargetDamageComponent = CollidePlayerPawn.DamageComponent,
					AttackerPlayerPawn = OwnerPlayerPawn,
					TargetPlayerPawn = CollidePlayerPawn,
					DamageOrigin = Collision.HitLocation,
					TargetOrigin = CollidePlayerPawn.CenterPosition,
					BaseDamage = MaxDamage,
					BaseKnockbackStrength = BaseKnockbackStrength,
					DirectImpact = true,
					DamageType = EDamageType.Projectile,
					DamageFalloffType = EDamageFalloffType.Rampup,
					MaxDamageImpactDistance = ExplosionRadius,
				};
				Scene.Dispatch(new DamageRequestEvent(DamageRequest));
			}
		}

		var Rigidbody = GameObject.Root.Components.Get<Rigidbody>();
		if (!Rigidbody.IsValid())
		{
			Log.Warning($"cannot find rigidboy comp on needle {this}");
		}

		Rigidbody.Velocity = Vector3.Zero;
		Rigidbody.MotionEnabled = false;

		IsAttached = true;
		AttachedGameObject = Collision.HitObject;
		InitArmedWorldTransformAttachedObject = Collision.HitObject.WorldTransform;
		HitLocation = Collision.HitLocation;
	}
}
