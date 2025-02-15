using Sandbox.Events;
using System;

namespace KOTH;

public sealed class Sticky : Projectile, IGameEventHandler<ProjectileCollideEvent>
{
	[Property, Group("Explosion")] public float MinDetTime { get; set; } = 0.66f;
	[Property, Group("Explosion")] public GameObject ExplosionPrefab { get; set; }
	[Property, Group("Explosion")] public SoundEvent ExplodeSound { get; set; }

	/////////////////////////////////////////////////////////////////////////////////////

	private GameObject AttachedGameObject = null;
	private Transform InitArmedWorldTransformSticky;
	private Transform InitArmedWorldTransformAttachedObject;

	/////////////////////////////////////////////////////////////////////////////////////

	public TimeSince AliveTime { get; private set; } = new();

	public void SetSpin()
	{
		// TODO : imp
	}

	protected override void OnStart()
	{
		base.OnStart();

		AliveTime = 0;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		// TODO : make logic seperate 
		if (AttachedGameObject != null)
		{
			Vector3 GameObjectOffset = AttachedGameObject.WorldPosition - InitArmedWorldTransformAttachedObject.Position;
			GameObject.WorldPosition = InitArmedWorldTransformSticky.Position + GameObjectOffset;
			// GameObject.WorldPosition = AttachedGameObject.WorldPosition;
		}

		// TODO : this is expensive, make a timed lambda
		if (AliveTime >= MinDetTime)
		{
			foreach (var Child in GameObject.Root.Children)
			{
				var Model = Child.Components.Get<ModelRenderer>();
				if (Model.IsValid())
				{
					Model.Tint = Color.Yellow;
				}
			}
		}
	}

	public void OnGameEvent(ProjectileCollideEvent EventArgs)
	{
		var Rigidbody = GameObject.Root.Components.Get<Rigidbody>();
		if (!Rigidbody.IsValid())
		{
			Log.Warning($"cannot find rigidboy comp on sticky {this}");
		}

		if (!Rigidbody.MotionEnabled)
		{
			return;
		}

		var HitObject = EventArgs.ProjectileCollision.HitObject;

		if (!HitObject.IsValid())
		{
			return;
		}

		Rigidbody.Velocity = Vector3.Zero;
		Rigidbody.MotionEnabled = false;
		AttachedGameObject = HitObject;
		InitArmedWorldTransformAttachedObject = HitObject.WorldTransform;
		InitArmedWorldTransformSticky = GameObject.Root.WorldTransform;
	}

	[Obsolete]
	public void Explode()
	{
		// TODO : this is likely because the object is destroyed on clients before this
		// code can run (race condition). Maybe somehow call async? 
		if (Transform == null)
		{
			Log.Warning("transform null on sticky");
			return;
		}

		if (ExplosionPrefab.IsValid())
		{
			var Explosion = ExplosionPrefab.Clone(WorldPosition);
			if (Explosion.IsValid())
			{
				Explosion.NetworkSpawn();
			}

			if (ExplodeSound != null)
			{
				GameObject.PlaySound(ExplodeSound, false);
			}
		}

		SimulateExplode(out FProjectileCollision ProjectileCollision, Transform.Position);

		foreach (var PlayerPawn in ProjectileCollision.TracedPlayers)
		{
			if (!PlayerPawn.IsValid())
			{
				continue;
			}

			FDamageRequest DamageRequest = new()
			{
				TargetPlayerPawn = PlayerPawn,
				AttackerPlayerPawn = OwnerPlayerPawn,
				DamageOrigin = ProjectileCollision.HitLocation,
				BaseDamage = BaseDamage,
				BaseKnockbackStrength = BaseKnockbackStrength,
				DamageType = EDamageType.Projectile,
				DamageFalloffType = EDamageFalloffType.Falloff,
				MaxFalloffDistance = ExplosionRadius,
			};
			Scene.Dispatch(new DamageRequestEvent(DamageRequest));
		}

		GameObject.Root.Destroy();
	}
}

