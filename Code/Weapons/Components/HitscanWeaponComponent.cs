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

	[Property, Group("HitScan")] private EHitscanFireType FireType
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

	protected override void OnInputUpdate()
	{
		bool IsShooting = IsDown() && CanShoot();

		// TODO : should this be a host/server rpc?
		if (IsProxy)
		{
			return;
		}

		if (IsShooting)
		{
			Shoot();
		}
	}

	protected virtual void Shoot()
	{
		var LocalPlayerPawn = Equipment.Owner;
		if (!LocalPlayerPawn.IsValid())
		{
			return;
		}

		TimeSinceShot = 0;
		Ammo--;

		var TraceStart = WeaponRay.Position;
		var StartRotation = Rotation.LookAt(WeaponRay.Forward);
		var TraceForward = StartRotation.Forward.Normal;

		var ShotTraces = ShootHelper.GetShootTraceElements(Scene.Trace, GameObject, TraceStart, WeaponRay.Position + TraceForward * 9999f, DebugOverlay);
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
					AttackerPlayerPawn = LocalPlayerPawn,
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
