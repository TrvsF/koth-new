using Sandbox.Events;
using Sandbox.Utility;
using System;

namespace KOTH;

public sealed class Needle : Projectile, IGameEventHandler<ProjectileCollideEvent>
{
	[Property, Group("Healing")] public float MinHealing { get; set; } = 50f;
	[Property, Group("Healing")] public float MaxHealing { get; set; } = 100f;
	[Property, Group("Damage")] public float MinDamage { get; set; } = 40f;
	[Property, Group("Damage")] public float MaxDamage { get; set; } = 90f;

	/////////////////////////////////////////////////////////////////////////////////////

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

		if (AttachedGameObject != null)
		{
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
				var Healing = MathX.Lerp(MinHealing, MaxHealing, InterpFactor);

				Log.Info($"doing {Healing} healing");

				FHealingRequest HealingRequest = new()
				{
					TargetPlayerPawn = CollidePlayerPawn,
					AttackerPlayerPawn = OwnerPlayerPawn,
					BaseHealing = Healing,
					AllowOverheal = false,
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
		AttachedGameObject = Collision.HitObject;
		InitArmedWorldTransformAttachedObject = Collision.HitObject.WorldTransform;
		HitLocation = Collision.HitLocation;
	}
}
