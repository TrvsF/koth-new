using KOTH.UI;
using Sandbox;
using Sandbox.Events;
using System.Net.Http;
using System;
using System.Text;
using Sandbox.Diagnostics;

namespace KOTH;

[Title("Hitscan Shooter"), Group("Weapon Components")]
public class HitscanWeaponComponent : InputWeaponComponent
{
	[Property, Group("HitScan")] private GameObject TrailPrefab { get; set; }
	[Property, Group("Spread")] public int Sides { get; set; } = 1;

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
				const float Radius = 6f;
				for (int SideIndex = 0; SideIndex < Sides; ++SideIndex)
				{
					double Angle = 2 * Math.PI * SideIndex / Sides;
					float X = (float)(Radius * Math.Cos(Angle));
					float Y = (float)(Radius * Math.Sin(Angle));

					var RotateVectorOffset = new Vector3(X * 0.01f, 0, Y * 0.01f);
					Ray ShapeRay = new(Boom.WorldPosition + ((Boom.WorldRotation.Up * Y) + (Boom.WorldRotation.Right * X)), Boom.WorldRotation.Forward + RotateVectorOffset);
					
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
	public void BulletTrialVFX(Vector3 HitObjectPosition)
	{
		if (TrailPrefab.IsValid())
		{
			var EstimatedStartPositionWorld = Equipment.Muzzle.WorldPosition;
			EstimatedStartPositionWorld.z += 12f;

			var Lerp = 0f;
			while (Lerp < 1f)
			{
				var Position = Vector3.Lerp(EstimatedStartPositionWorld, HitObjectPosition, Lerp);
				TrailPrefab.Clone(Position);
				Lerp += 0.033f;
			}
		}
	}

	////////////////////////////////////////////////////////////////////////
	
	protected Ray WeaponRay => Equipment.Owner.AimRay;

	protected virtual void Shoot(Ray WeaponRay)
	{
		var TraceStart = WeaponRay.Position;
		var StartRotation = Rotation.LookAt(WeaponRay.Forward);
		var TraceForward = StartRotation.Forward.Normal;
		var TraceEnd = WeaponRay.Position + TraceForward * 1600f;

		var DamageComponentsHit = ShootHelper.GetDamageComponentsFromTrace(Scene.Trace, GameObject, TraceStart, TraceEnd, out var FirstImpactLocation);

		var TotalBaseDamage = 0f;
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

				TotalBaseDamage += BaseDamage;

				Scene.Dispatch(new DamageRequestEvent(DamageRequest));
			}
		}

		BulletTrialVFX(FirstImpactLocation);
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
