using Sandbox.Events;
using System;

namespace KOTH;

public sealed class Sticky : Projectile, IGameEventHandler<ProjectileCollideEvent>
{
	[Property, Group("Explosion")] public float Damage { get; set; } = 90f;
	[Property, Group("Explosion")] public float KnockbackStrength { get; set; } = 300f;
	[Property, Group("Explosion")] public float MinDetTime { get; set; } = 0.66f;
	[Property, Group("Explosion")] public GameObject ExplosionPrefab { get; set; }
	[Property, Group("Explosion")] public SoundEvent ExplodeSound { get; set; }

	/////////////////////////////////////////////////////////////////////////////////////

	public TimeSince AliveTime { get; private set; } = new();

	public void SetSpin()
	{

	}

	protected override void OnStart()
	{
		base.OnStart();

		AliveTime = 0;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

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

		if (!EventArgs.ProjectileCollision.HitObject.IsValid())
		{
			return;
		}

		if (!EventArgs.ProjectileCollision.HitObject.Tags.Contains("player"))
		{
			Rigidbody.Velocity = Vector3.Zero;
			Rigidbody.MotionEnabled = false;
		}
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
				BaseDamage = Damage,
				BaseKnockbackStrength = KnockbackStrength,
				DamageType = EDamageType.Projectile,
				DamageFalloffType = EDamageFalloffType.Falloff,
				MaxFalloffDistance = ExplosionRadius,
			};
			Scene.Dispatch(new DamageRequestEvent(DamageRequest));
		}

		GameObject.Root.Destroy();
	}
}

