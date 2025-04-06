using Sandbox.Diagnostics;
using Sandbox.Events;
using System;

namespace KOTH;

public sealed class Rkt : Projectile, IGameEventHandler<ProjectileCollideEvent>
{
	[Property, Group("VFX")] public GameObject AuraPrefab { get; set; }
	[Property, Group("VFX")] public GameObject TrailPrefab { get; set; }
	[Property, Group("VFX")] public GameObject ExplosionPrefab { get; set; }
	[Property, Group("VFX")] public SoundEvent ExplosionSound { get; set; }

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

		if (!IsProxy && TimeSinceSpawn < LocalInvisableTime)
		{
			return;
		}

		if (TrailPrefab.IsValid())
		{
			TrailPrefab.Clone(WorldPosition);
		}

		if (AuraPrefab.IsValid())
		{
			var TeamAura = AuraPrefab;
			if (TeamAura.GetComponent<ParticleSpriteRenderer>() is { } Sprite)
			{
				Sprite.Texture = Texture.Create(1, 1).WithData(new byte[4] { 0, 0, 0, 255 }).Finish();
			}

			TeamAura.Clone(WorldPosition, WorldRotation);
		}
	}

	[Rpc.Broadcast]
	public void DoExplosionVfx(Vector3 EndLocation)
	{
		if (ExplosionSound.IsValid())
		{
			if (Sound.Play(ExplosionSound, EndLocation) is { } SoundHandel) { }
		}
		if (ExplosionPrefab.IsValid())
		{
			ExplosionPrefab.Clone(EndLocation);
		}

	}

	public void OnGameEvent(ProjectileCollideEvent EventArgs)
	{
		var Collision = EventArgs.ProjectileCollision;

		foreach (var DamageComponent in Collision.TracedDamageComponents)
		{
			if (!DamageComponent.IsValid())
			{
				continue;
			}

			FDamageRequest DamageRequest = new()
			{
				TargetDamageComponent = DamageComponent,
				AttackerPlayerPawn = OwnerPlayerPawn,
				DamageOrigin = Collision.HitLocation,
				TargetOrigin = DamageComponent.WorldPosition,
				BaseDamage = BaseDamage,
				BaseKnockbackStrength = BaseKnockbackStrength,
				DirectImpact = DamageComponent.GameObject.Root == Collision.HitObject?.Root,
				DamageType = EDamageType.Projectile,
				DamageFalloffType = EDamageFalloffType.Falloff,
				DoesLessSelfDamage = true,
				MaxDamageImpactDistance = ExplosionRadius,
			};

			if (DamageComponent.GameObject.GetComponent<PlayerPawn>() is { } PlayerPawn)
			{
				DamageRequest.TargetPlayerPawn = PlayerPawn;
				DamageRequest.TargetOrigin = PlayerPawn.CenterPosition;
			}

			Scene.Dispatch(new DamageRequestEvent(DamageRequest));
		}

		DoExplosionVfx(Collision.HitLocation);

		GameObject.Root.Destroy();
	}
}
