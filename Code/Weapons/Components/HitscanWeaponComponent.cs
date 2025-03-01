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
	[Property, Group("Recoil")] public int BulletsPerSecondBeforeMax { get; set; } = 0;
	[Property, Group("Recoil")] public float MaxOutwardDistance { get; set; } = 0f;
	[Property, Group("Recoil")] public float MaxUpwardDistance { get; set; } = 0f;

	[Property, Group("Spread")] public int Sides { get; set; } = 1;
	[Property, Group("Spread")] public float Radius { get; set; } = 4f;
	[Property, Group("Spread")] public float OutwardFactor { get; set; } = .77f;

	[Property, Group("VFX")] public DecalDefinition DecalDefinition { get; set; }
	[Property, Group("VFX")] public GameObject TrailPrefab { get; set; }
	[Property, Group("VFX")] public float TrialAmount { get; set; } = 10f;

	/////////////////////////////////////////////////////////////

	List<RealTimeSince> BulletsShot = new(); // appended to in Shoot()

	protected override void OnInputUpdate()
	{
		Assert.IsValid(Equipment);
		Assert.IsValid(Equipment.Owner);

		bool IsShooting = IsDown() && CanShoot();
		WorldShotVFX();

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
					var RadiusIn = Radius + (BulletsShot.Count / 2f); 

					double Angle = 2 * Math.PI * SideIndex / Sides;
					float X = (float)(RadiusIn * Math.Cos(Angle));
					float Y = (float)(RadiusIn * Math.Sin(Angle));

					var ShootVector = Forward.RotateAround(Vector3.Zero, Rotation.From(X * OutwardFactor, Y * OutwardFactor, 0));
					ShootVector = ShootVector.RotateAround(Vector3.Zero, Boom.WorldRotation);

					Ray ShapeRay = new(Boom.WorldPosition + ((Boom.WorldRotation.Up * Y) + (Boom.WorldRotation.Right * X)), ShootVector);

					Shoot(ShapeRay);
				}
			}

			TimeSinceShot = 0;
			Ammo--;
		}
		Equipment.ViewModel?.ModelRenderer?.Set("b_attack", IsShooting);
	}

	const float RecoilBulletDecayTime = 1.33f;
	private Rotation GetNextShotOffset()
	{
		BulletsShot.RemoveAll(Bullet => Bullet > RecoilBulletDecayTime);
		int BulletCount = BulletsShot.Count;

		float RecoilBulletsLerp = (float)BulletCount / (float)BulletsPerSecondBeforeMax;
		if (RecoilBulletsLerp == 0)
		{
			return Rotation.Identity;
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

		Log.Info($"count:{BulletCount}, angle:{Side}, distance:{Distance}");
		Log.Info($"{Yaw}:{Pitch}");

		return ShotVectorAngleOffset;
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
		BulletsShot.Add(0);

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

		if (IsReloading && Ammo > 0)
		{
			TryCancelReload();
		}

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

	List<FFXHitscanShotTrail> FXTrails = new();
	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();


	}

	[Rpc.Broadcast]
	public void WorldShotVFX()
	{
	}

	const int MaxParticlesPerShot = 250;
	int ShotParticles = 0;

	[Rpc.Broadcast]
	public void BulletHitVFX(Vector3 HitObjectPosition)
	{
		if (!Equipment.Owner.IsValid())
		{
			Log.Warning($"owner not valid on hitscan comp {this}");
			return;
		}

		if (TrailPrefab.IsValid())
		{
			var EstimatedStartPositionWorld = Equipment.Owner.CenterPosition;
			var LerpFactor = TrialAmount / EstimatedStartPositionWorld.Distance(HitObjectPosition);

			var Lerp = 0.05f;
			while (Lerp < 1f)
			{
				var Position = Vector3.Lerp(EstimatedStartPositionWorld, HitObjectPosition, Lerp);
				TrailPrefab.Clone(Position, Equipment.Owner.Boom.WorldRotation);
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
			var Decal = Game.Random.FromList(DecalDefinition.Decals);

			var DecalObject = Scene.CreateObject();
			DecalObject.NetworkMode = NetworkMode.Never;
			DecalObject.WorldPosition = HitObjectPosition;

			var DecalRenderer = DecalObject.AddComponent<DecalRenderer>();
			DecalRenderer.Material = Decal.Material;
			DecalRenderer.Size = new(Decal.Width.GetValue(), Decal.Height.GetValue(), Decal.Depth.GetValue());

			var Destroy = DecalObject.AddComponent<TimedDestroyComponent>();
			Destroy.Time = 15f;

			DecalObject.NetworkSpawn();
		}
	}
}
