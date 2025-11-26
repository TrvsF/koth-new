using KOTH.UI;
using Sandbox;
using Sandbox.Events;
using System.Net.Http;
using System;
using System.Text;
using Sandbox.Diagnostics;
using static Sandbox.VertexLayout;
using System.Numerics;

namespace KOTH;

struct FFXHitscanShotTrail // TODO : visit
{
	public GameObject TrialObject { get; init; }
	public Vector3 EndPosition { get; init; }
	public float LerpDistance { get; init; }

	public bool IsValid()
	{
		return TrialObject.IsValid();
	}
};

[Title("Hitscan Shooter"), Group("Weapon Components")]
public class HitscanWeaponComponent : InputWeaponComponent
{
	[Property, Group("Recoil")] public float RecoilAddFactor { get; set; } = 0.2f;
	[Property, Group("Recoil")] public float MaxOutwardDistance { get; set; } = 0f;
	[Property, Group("Recoil")] public float MaxUpwardDistance { get; set; } = 0f;

	[Property, Group("Spread")] public int Sides { get; set; } = 1;
	[Property, Group("Spread")] public float Radius { get; set; } = 4f;
	[Property, Group("Spread")] public float OutwardFactor { get; set; } = .77f;

	[Property, Group("VFX")] public SoundEvent ShootSound { get; set; }
	[Property, Group("VFX")] public DecalDefinition DecalDefinition { get; set; }
	[Property, Group("VFX")] public GameObject TrailPrefab { get; set; }
	[Property, Group("VFX")] public float TrialAmount { get; set; } = 10f;

	/////////////////////////////////////////////////////////////

	protected override void OnInputUpdate()
	{
		Assert.IsValid(Equipment);
		Assert.IsValid(Equipment.Owner);

		bool IsShooting = IsDown() && CanShoot();

		if (IsShooting)
		{
			Equipment.Owner.VFXOnShoot();
			if (IsReloading && Ammo > 0)
			{
				CancelReload();
			}
		}

		if (IsProxy)
		{
			return;
		}

		if (IsShooting)
		{
			var Boom = Equipment.Owner.Boom;
			if (!Equipment.Owner.Boom.IsValid())
			{
				Log.Warning($"shooting without a boom {this}");
			}

			ShotParticles = 0; // !

			var Forward = Vector3.Forward;
			var OffsetVector = Forward.RotateAround(Vector3.Zero, GetNextShotOffset());
			OffsetVector = OffsetVector.RotateAround(Vector3.Zero, Boom.WorldRotation);

			Shoot(new(Boom.WorldPosition + OffsetVector, OffsetVector));

			if (Sides >= 3)
			{
				for (int SideIndex = 0; SideIndex < Sides; ++SideIndex)
				{
					var RadiusIn = Radius + (BulletSpreadFactorer * 10f); 

					double Angle = 2 * Math.PI * SideIndex / Sides;
					float X = (float)(RadiusIn * Math.Cos(Angle));
					float Y = (float)(RadiusIn * Math.Sin(Angle));

					var ShootVector = Forward.RotateAround(Vector3.Zero, Rotation.From(X * OutwardFactor, Y * OutwardFactor, 0));
					ShootVector = ShootVector.RotateAround(Vector3.Zero, Boom.WorldRotation);

					Ray ShapeRay = new(Boom.WorldPosition + ((Boom.WorldRotation.Up * Y) + (Boom.WorldRotation.Right * X)), ShootVector);

					Shoot(ShapeRay);
				}
			}

			BulletSpreadFactorer += 0.5f; // !
			TimeSinceShot = 0;
			Ammo--;
			
			WorldShotVFX();
		}
	}


	float BulletRecoilFactorer = 0;
	float BulletSpreadFactorer = 0;

	private Rotation GetNextShotOffset()
	{
		float RecoilBulletsLerp = 0;
		if (BulletRecoilFactorer > 0)
		{
			RecoilBulletsLerp = BulletRecoilFactorer / 1;
		}

		Random Random = new();
		float Side = MathX.Lerp(0, MaxOutwardDistance, RecoilBulletsLerp);
		float Distance = MathX.Lerp(0, MaxUpwardDistance, RecoilBulletsLerp);

		const double YawMaxOffset = 0.2;
		const double PitchMaxOffset = 0.05;

		Side *= Random.Next(-1, 2);
		Distance *= -1;

		float Yaw = (float)(Random.NextDouble() * (YawMaxOffset * 2) - YawMaxOffset) + Side;
		float Pitch = (float)(Random.NextDouble() * (PitchMaxOffset * 2) - PitchMaxOffset) + Distance;

		Rotation ShotVectorAngleOffset = Rotation.From(Pitch, Yaw, 0);

		BulletRecoilFactorer += RecoilAddFactor;

		return ShotVectorAngleOffset;
	}

	protected override void OnFixedUpdate()
	{
		BulletRecoilFactorer -= 0.015f;
		BulletRecoilFactorer = Math.Clamp(BulletRecoilFactorer, 0, 1);
		
		BulletSpreadFactorer -= 0.015f;
		BulletSpreadFactorer = Math.Clamp(BulletSpreadFactorer, 0, 1);

		base.OnFixedUpdate();
	}
	
	/////////////////////////////////////////////////////////////

	protected virtual void Shoot(Ray WeaponRay)
	{
		var TraceStart = WeaponRay.Position;
		var StartRotation = Rotation.LookAt(WeaponRay.Forward);
		var TraceForward = StartRotation.Forward.Normal;
		var TraceEnd = WeaponRay.Position + TraceForward * 80000f; // TODO : silly number, but if this doesn't hit we shoot at world.forward

		var DamageComponentsHit = ShootHelper.GetDamageComponentsFromTrace(Scene.Trace, GameObject, TraceStart, TraceEnd, out var FirstImpactLocation);
		BulletHitVFX(FirstImpactLocation);

		if (!Network.IsOwner)
		{
			return;
		}

		// request the damage /\/\/\/\/\/\/\///////////////////////////////
		foreach (var (DamageComponent, HitLocation) in DamageComponentsHit)
		{
			FDamageRequest DamageRequest = new()
			{
				TargetDamageComponent = DamageComponent,
				AttackerPlayerPawn = Equipment.Owner,
				DamageOrigin = HitLocation,
				BaseDamage = BaseDamage,
				TargetOrigin = DamageComponent.GameObject.WorldPosition,
				BaseKnockbackStrength = KnockbackStrength,
				DamageType = EDamageType.HitScan,
				DamageFalloffType = EDamageFalloffType.Falloff,
				DoesLessSelfDamage = true,
				DirectImpact = true,
			};

			if (DamageComponent.GameObject.GetComponent<PlayerPawn>() is { } PlayerPawn)
			{
				DamageRequest.TargetPlayerPawn = PlayerPawn;
				DamageRequest.TargetOrigin = PlayerPawn.CenterPosition;
			}

			Scene.Dispatch(new DamageRequestEvent(DamageRequest));
		}
	}

	protected TimeSince TimeSinceShot = new();
	protected virtual bool CanShoot()
	{
		Assert.IsValid(Equipment);
		Assert.IsValid(Equipment.Owner);

		if (Equipment.Owner.IsFrozen)
			return false;

		if (Equipment.Tags.Has("equipping"))
			return false;

		if (TimeSinceShot < FireRate)
			return false;

		if (!HasAmmo)
			return false;

		return true;
	}

	/////////////////////////////////////////////////////////////


	[Rpc.Broadcast]
	public void WorldShotVFX()
	{
		if (ShootSound.IsValid())
		{
			if (Sound.Play(ShootSound, Equipment.WorldPosition) is { } SoundHandel)
			{

			}
		}
	}

	const int MaxParticlesPerShot = 250;
	int ShotParticles = 0;

	[Rpc.Broadcast]
	public void BulletHitVFX(Transform HitObjectTransform)
	{
		if (!Equipment.IsValid() || !Equipment.Owner.IsValid() || !Equipment.ViewModel.IsValid())
		{
			return;
		}

		if (TrailPrefab.IsValid())
		{
			var EstimatedStartPositionWorld = IsProxy ? Equipment.Muzzle.WorldPosition : Equipment.ViewModel.Muzzle.WorldPosition;
			var LerpFactor = TrialAmount / EstimatedStartPositionWorld.Distance(HitObjectTransform.Position);

			var Lerp = IsProxy ? 0 : 0.02f;
			while (Lerp < 1f)
			{
				var Position = Vector3.Lerp(EstimatedStartPositionWorld, HitObjectTransform.Position, Lerp);
				var Trail = TrailPrefab.Clone(Position, Equipment.Owner.Boom.WorldRotation);
				Trail.NetworkMode = NetworkMode.Never;

				Lerp += LerpFactor;
				++ShotParticles;

				if (ShotParticles > MaxParticlesPerShot)
				{
					break;
				}
			}
		}

		if (DecalDefinition.IsValid())
		{
			//var Decal = Game.Random.FromList(DecalDefinition.Decals);

			//var DecalObject = Scene.CreateObject();
			//DecalObject.NetworkMode = NetworkMode.Never;
			//DecalObject.WorldPosition = HitObjectTransform.Position;
			//DecalObject.WorldRotation = HitObjectTransform.Rotation;

			//var DecalRenderer = DecalObject.AddComponent<DecalRenderer>();
			//DecalRenderer.Material = Decal.Material;
			//DecalRenderer.Size = new(Decal.Width.GetValue(), Decal.Height.GetValue(), Decal.Depth.GetValue());

			//var Destroy = DecalObject.AddComponent<TimedDestroyComponent>();
			//Destroy.Time = 15f;

			//DecalObject.NetworkSpawn();
		}
	}
}
