using Sandbox.Events;
using System;

namespace KOTH;

public sealed class Rkt : Projectile, IGameEventHandler<ProjectileCollideEvent>
{
	[Property, Group("Explosion")] public float Damage { get; set; } = 50f;
	[Property, Group("Explosion")] public float KnockbackStrength { get; set; } = 300f;

	/////////////////////////////////////////////////////////////////////////////////////

	private TimedDestroyComponent DestroyComponent;
	private readonly float MaxAliveTime = 30; // seconds
	protected override void OnStart()
	{
		DestroyComponent = Components.Create<TimedDestroyComponent>();
		DestroyComponent.Time = MaxAliveTime;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		// WorldRotation = WorldRotation.RotateAroundAxis(WorldTransform.Forward, 1f);
	}

	public void OnGameEvent(ProjectileCollideEvent EventArgs)
	{
		var Collision = EventArgs.ProjectileCollision;

		foreach (var PlayerPawn in Collision.TracedPlayers)
		{
			if (!PlayerPawn.IsValid())
			{
				continue;
			}

			FDamageRequest DamageRequest = new()
			{
				TargetPlayerPawn = PlayerPawn,
				AttackerPlayerPawn = OwnerPlayerPawn,
				DamageOrigin = Collision.HitLocation,
				BaseDamage = Damage,
				BaseKnockbackStrength = KnockbackStrength,
				DirectImpact = PlayerPawn.GameObject.Root == Collision.HitObject?.Root,
				DamageType = EDamageType.Projectile,
				DamageFalloffType = EDamageFalloffType.Falloff,
				DoesLessSelfDamage = true,
				MaxFalloffDistance = ExplosionRadius,
			};
			Scene.Dispatch(new DamageRequestEvent(DamageRequest));
		}

		GameObject.Root.Destroy();
	}
}
