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

[Title("Hitscan Shooter"), Group("Weapon Components")]
public class HitscanWeaponComponent : InputWeaponComponent
{
	[Property, Group("Spread")] public int Sides { get; set; } = 1;
	[Property, Group("VFX")] public DecalDefinition DecalDefinition { get; set; }
	[Property, Group("VFX")] public GameObject TrailPrefab { get; set; }
	[Property, Group("VFX")] public float TrialAmount { get; set; } = 50f;

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

			Shoot(Equipment.Owner.AimRay);

			if (Sides >= 3)
			{
				const float Radius = 4f;
				for (int SideIndex = 0; SideIndex < Sides; ++SideIndex)
				{
					double Angle = 2 * Math.PI * SideIndex / Sides;
					float X = (float)(Radius * Math.Cos(Angle));
					float Y = (float)(Radius * Math.Sin(Angle));

					var Forward = Vector3.Forward;
					var ShootVector = Forward.RotateAround(Vector3.Zero, Rotation.From(X * .5f, Y * .5f, 0));
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

	[Rpc.Broadcast]
	public void WorldShotVFX()
	{
	}

	[Rpc.Broadcast]
	public void BulletHitVFX(Vector3 HitObjectPosition)
	{
		if (TrailPrefab.IsValid())
		{
			var EstimatedStartPositionWorld = Equipment.Muzzle.WorldPosition;

			var LerpFactor = TrialAmount / EstimatedStartPositionWorld.Distance(HitObjectPosition);

			bool HACKskipfirstone = true;
			var Lerp = 0f;
			while (Lerp < 1f)
			{
				if (HACKskipfirstone)
				{
					HACKskipfirstone = false;
					continue;
				}

				var Position = Vector3.Lerp(EstimatedStartPositionWorld, HitObjectPosition, Lerp);
				TrailPrefab.Clone(Position);
				Lerp += LerpFactor;
			}
		}

		if (DecalDefinition.IsValid())
		{
			var Decal = Game.Random.FromList(DecalDefinition.Decals);

			var DecalObject = Scene.CreateObject();
			DecalObject.WorldPosition = HitObjectPosition;

			var DecalRenderer = DecalObject.AddComponent<DecalRenderer>();
			DecalRenderer.Material = Decal.Material;
			DecalRenderer.Size = new(Decal.Width.GetValue(), Decal.Height.GetValue(), Decal.Depth.GetValue());

			var Destroy = DecalObject.AddComponent<TimedDestroyComponent>();
			Destroy.Time = 15f;

			DecalObject.NetworkSpawn();
		}
	}

	////////////////////////////////////////////////////////////////////////
	
	protected Ray WeaponRay => Equipment.Owner.AimRay;

	protected virtual void Shoot(Ray WeaponRay)
	{
		var TraceStart = WeaponRay.Position;
		var StartRotation = Rotation.LookAt(WeaponRay.Forward);
		var TraceForward = StartRotation.Forward.Normal;
		var TraceEnd = WeaponRay.Position + TraceForward * 80000f; // TODO : silly number, but if this doesn't hit we shoot at world.forward

		var DamageComponentsHit = ShootHelper.GetDamageComponentsFromTrace(Scene.Trace, GameObject, TraceStart, TraceEnd, out var FirstImpactLocation);

		if (Network.IsOwner)
		{
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
					MaxFalloffDistance = 600,
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

		BulletHitVFX(FirstImpactLocation);
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
}
