using Sandbox.Diagnostics;
using Sandbox.Events;
using System;

namespace KOTH;

public sealed class Rkt : Projectile, IGameEventHandler<ProjectileCollideEvent>
{
	[Property, Group("VFX")] public GameObject TrailPrefab { get; set; }
	[Property, Group("VFX")] public GameObject ExplosionPrefab { get; set; }

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

		Assert.IsValid(TrailPrefab);
		TrailPrefab.Clone(WorldPosition);
	}

	[Rpc.Broadcast]
	public void DoExplosionVfx()
	{
		Assert.IsValid(ExplosionPrefab);
		ExplosionPrefab.Clone(WorldPosition);
	}

	public void OnGameEvent(ProjectileCollideEvent EventArgs)
	{
		Log.Info("collide!");
		var Collision = EventArgs.ProjectileCollision;

		Log.Info(Collision.TracedPlayers.Count);
		foreach (var PlayerPawn in Collision.TracedPlayers)
		{
			if (!PlayerPawn.IsValid())
			{
				Log.Warning("no");
				continue;
			}

			Log.Info("fire");
			FDamageRequest DamageRequest = new()
			{
				TargetPlayerPawn = PlayerPawn,
				AttackerPlayerPawn = OwnerPlayerPawn,
				DamageOrigin = Collision.HitLocation,
				BaseDamage = BaseDamage,
				BaseKnockbackStrength = BaseKnockbackStrength,
				DirectImpact = PlayerPawn.GameObject.Root == Collision.HitObject?.Root,
				DamageType = EDamageType.Projectile,
				DamageFalloffType = EDamageFalloffType.Falloff,
				DoesLessSelfDamage = true,
				MaxFalloffDistance = ExplosionRadius,
			};
			Scene.Dispatch(new DamageRequestEvent(DamageRequest));
		}

		DoExplosionVfx();

		GameObject.Root.Destroy();
	}
}
