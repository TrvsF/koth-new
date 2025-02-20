using Sandbox.Events;
using System;

namespace KOTH;

public sealed class Grenade : Projectile, IGameEventHandler<ProjectileCollideEvent>
{
	[Property, Group("Explosion")] public float ExplosionFuse { get; set; } = 3f;
	[Property, Group("Explosion")] public GameObject ExplosionPrefab { get; set; }

	private TimeSince AliveTime = new();

	protected override void OnStart()
	{
		base.OnStart();

		AliveTime = 0;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		// -------------------- 
		// explode on our own accord
		if (AliveTime >= ExplosionFuse)
		{
			FProjectileCollision ProjectileCollision;
			SimulateExplode(out ProjectileCollision, WorldPosition);

			foreach (var PlayerPawn in ProjectileCollision.TracedPlayers)
			{
				if (!PlayerPawn.IsValid())
				{
					continue;
				}

				FDamageRequest DamageRequest = new()
				{
					TargetDamageComponent = PlayerPawn.DamageComponent,
					AttackerPlayerPawn = OwnerPlayerPawn,
					TargetPlayerPawn = PlayerPawn,
					DamageOrigin = ProjectileCollision.HitLocation,
					TargetOrigin = PlayerPawn.CenterPosition,
					BaseDamage = BaseDamage * .33f,
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

	public void OnGameEvent(ProjectileCollideEvent EventArgs)
	{
		var Collision = EventArgs.ProjectileCollision;

		// -------------------- the guy we hit
		var HitPlayerPawn = Collision.HitObject.Root.Components.Get<PlayerPawn>();
		if (HitPlayerPawn.IsValid())
		{
			FDamageRequest DirectDamageRequest = new()
			{
				TargetDamageComponent = HitPlayerPawn.DamageComponent,
				AttackerPlayerPawn = OwnerPlayerPawn,
				TargetPlayerPawn = HitPlayerPawn,
				DamageOrigin = Collision.HitLocation,
				TargetOrigin = HitPlayerPawn.CenterPosition,
				BaseDamage = BaseDamage,
				BaseKnockbackStrength = BaseKnockbackStrength,
				DirectImpact = true,
				DamageType = EDamageType.Projectile,
				DamageFalloffType = EDamageFalloffType.None,
				MaxFalloffDistance = ExplosionRadius,
			};
			Scene.Dispatch(new DamageRequestEvent(DirectDamageRequest));

			// -------------------- explode on them
			FProjectileCollision ProjectileCollision;
			SimulateExplode(out ProjectileCollision, Collision.HitLocation);

			foreach (var PlayerPawn in ProjectileCollision.TracedPlayers)
			{
				if (!PlayerPawn.IsValid() || PlayerPawn == HitPlayerPawn)
				{
					continue;
				}

				FDamageRequest DamageRequest = new()
				{
					TargetDamageComponent = PlayerPawn.DamageComponent,
					AttackerPlayerPawn = OwnerPlayerPawn,
					TargetPlayerPawn = PlayerPawn,
					DamageOrigin = ProjectileCollision.HitLocation,
					TargetOrigin = PlayerPawn.CenterPosition,
					BaseDamage = BaseDamage * .66f,
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
}

