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
	private enum EHitscanFireType
	{
		SingleShot,
		Continuous,
		Infinite,
	}

	[Property, Group("HitScan")] private GameObject TrailPrefab { get; set; }
	[Property, Group("HitScan")]
	private EHitscanFireType FireType
	{
		get => GetFireType();
	}

	private EHitscanFireType GetFireType()
	{
		if (MaxAmmo == -1)
		{
			return EHitscanFireType.Infinite;
		}
		else if (MaxAmmo == 1)
		{
			return EHitscanFireType.SingleShot;
		}
		else
		{
			return EHitscanFireType.Continuous;
		}
	}

	////////////////////////////////////////////////////////////////////////

	[Property] public int Sides { get; set; } = 1;

	protected override void OnInputUpdate()
	{
		Assert.IsValid(Equipment);
		Assert.IsValid(Equipment.Owner);

		bool IsShooting = IsDown() && CanShoot();

		// TODO : should this be a host/server rpc?
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
				const float Radius = 8f;
				for (int SideIndex = 0; SideIndex < Sides; ++SideIndex)
				{
					double angle = 2 * Math.PI * SideIndex / Sides;
					float x = (float)(Radius * Math.Cos(angle));
					float y = (float)(Radius * Math.Sin(angle));
					Ray ShapeRay = new(Boom.WorldPosition + ((Boom.WorldRotation.Up * y) + (Boom.WorldRotation.Right * x)), Boom.WorldRotation.Forward);
					Shoot(ShapeRay);
				}
			}
			
			TimeSinceShot = 0;
			Ammo--;
		}
		Equipment.ViewModel?.ModelRenderer?.Set("b_attack", IsShooting);
	}

	[Rpc.Broadcast]
	public void TrailFx(Vector3 TraceStart, Vector3 TraceEnd)
	{
		if (TrailPrefab.IsValid())
		{
			var Lerp = 0f;
			while (Lerp < 1f)
			{
				var Position = Vector3.Lerp(TraceStart, TraceEnd, Lerp);
				TrailPrefab.Clone(Position);
				Lerp += 0.025f;
			}
		}
	}

	protected virtual void Shoot(Ray WeaponRay)
	{
		var TraceStart = WeaponRay.Position;
		var StartRotation = Rotation.LookAt(WeaponRay.Forward);
		var TraceForward = StartRotation.Forward.Normal;
		var TraceEnd = WeaponRay.Position + TraceForward * 1600f;

		TrailFx(TraceStart, TraceEnd);

		var ShotTraces = ShootHelper.GetShootTraceElements(Scene.Trace, GameObject, TraceStart, TraceEnd);
		foreach (var TraceElement in ShotTraces)
		{
			if (!TraceElement.Hit)
			{
				continue;
			}

			// HACK
			if (!TraceElement.Tags.Contains("player_collider"))
			{
				return;
			}

			if (TraceElement.GameObject.Root.Components.Get<PlayerPawn>(FindMode.EnabledInSelfAndDescendants) is { } HitPlayerPawn)
			{
				if (!Network.IsOwner)
				{
					return;
				}

				FDamageRequest DamageRequest = new()
				{
					TargetPlayerPawn = HitPlayerPawn,
					AttackerPlayerPawn = Equipment.Owner,
					DamageOrigin = TraceElement.HitPosition,
					BaseDamage = BaseDamage,
					BaseKnockbackStrength = KnockbackStrength,
					DamageType = EDamageType.HitScan,
					DamageFalloffType = EDamageFalloffType.Falloff,
					DoesLessSelfDamage = true,
					MaxFalloffDistance = 5000,
				};

				Scene.Dispatch(new DamageRequestEvent(DamageRequest));
			}
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

	//////////////////////////////////////////////////////////////

	protected Ray WeaponRay => Equipment.Owner.AimRay;
}
