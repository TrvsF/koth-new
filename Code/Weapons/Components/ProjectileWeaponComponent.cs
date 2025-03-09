using KOTH.UI;
using Sandbox;
using Sandbox.Events;
using System.Net.Http;
using System;
using System.Text;
using Sandbox.Diagnostics;
using System.Data.Common;

namespace KOTH;

// TODO : this piece of shit needs a proper refactor

[Title("Projectile Shooter"), Group("Weapon Components")]
public class ProjectileWeaponComponent : InputWeaponComponent
{
	[Property, Group("Projectile")] public GameObject ProjectilePrefab { get; set; }
	[Property, Group("Projectile")] public float ProjectileHorizontalSpeed { get; set; } = 600.0f;
	[Property, Group("Projectile")] public float ProjectileVerticalSpeed { get; set; } = 0f;

	[Property, Group("VFX")] public SoundEvent ShootSound { get; set; }

	////////////////////////////////////////////////////////////////////////

	// TODO : sticky only, look into doing a better way
	public bool IsStickyLauncher { get => StickyTrackerComponent.IsValid(); }
	public int ActiveStickies { get => StickyTrackerComponent.IsValid() ? StickyTrackerComponent.StickyCount : 0; }
	protected StickyTrackerComponent StickyTrackerComponent { get; set; }
	
	////////////////////////////////////////////////////////////////////////

	protected override void OnAwake()
	{
		base.OnAwake();

		// TODO : revisit, sticky only
		StickyTrackerComponent = Components.Get<StickyTrackerComponent>();
	}

	protected override void OnInputUpdate()
	{
		if (!Equipment.Owner.IsValid())
		{
			return;
		}

		if (!Equipment.Owner.IsLocallyControlled)
		{
			return;
		}

		bool IsShooting = IsDown() && CanShoot();
		if (IsShooting)
		{
			Shoot();
		}
		Equipment.ViewModel?.ModelRenderer?.Set("b_attack", IsShooting);
	}

	protected virtual GameObject Shoot() // TODO : revisit the return
	{
		var PlayerPawn = Equipment.Owner;
		if (!PlayerPawn.IsValid())
		{
			return null;
		}

		TimeSinceShot = 0;
		Ammo--;

		var AimForward = PlayerPawn.AimRay.Forward;

		// create projectile object from prefab
		var ProjectilePosition = PlayerPawn.AimRay.Position + (Vector3.Down * 3.33f); // magic
		var ProjectileRotation = Rotation.LookAt(AimForward);
		var Projectile = ProjectilePrefab.Clone(ProjectilePosition, ProjectileRotation);

		// give it the player
		var ProjectileComponent = Projectile.Components.Get<Projectile>();
		ProjectileComponent.OwnerPlayerPawn = PlayerPawn;

		if (IsStickyLauncher)
		{
			StickyTrackerComponent.AddSticky(Projectile.Components.Get<Sticky>());
		}

		var Rigidbody = Projectile.Components.Get<Rigidbody>();
		SetProjectileVelocity(Rigidbody, AimForward);

		Projectile.Root.Tags.Add("self");
		Projectile.NetworkSpawn();
		return Projectile;
	}

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

	protected virtual void SetProjectileVelocity(Rigidbody ProjectileRigidbody, Vector3 AimForward)
	{
		ProjectileRigidbody.Velocity = AimForward * ProjectileHorizontalSpeed;
		ProjectileRigidbody.Velocity += Vector3.Up * ProjectileVerticalSpeed;
	}

	protected TimeSince TimeSinceShot = new();
	protected virtual bool CanShoot()
	{
		// these 2 should be ensures?
		if (!Equipment.IsValid()) return false;
		if (!Equipment.Owner.IsValid()) return false;

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

////////////////////////////////////////////////////////////////////////

// NOTE : i think this should now just be a 'mode' within the base projectile comp

[Title("Sticky Shooter"), Group("Weapon Components")]
public class StickyWeaponComponent : ProjectileWeaponComponent
{
	[Property, Category("Sticky")] public float MaxChargeTime { get; private set; } = 1.6f;

	// TODO : THIS IS QUICKLY BECOMING A MESS!

	public TimeSince TimeSinceInputFirstDown { get; private set; } = new();
	public bool WasInputDownLastTick { get; private set; } = false;
	protected override void OnInputUpdate() // TODO : OnInput()?
	{
		if (!Equipment.Owner.IsValid())
		{
			return;
		}

		if (!Equipment.Owner.IsLocallyControlled)
		{
			return;
		}

		bool KeyDown = IsDown();

		if (WasInputDownLastTick)
		{
			if (!KeyDown && CanShoot())
			{
				GameObject Projectile = Shoot();
				if (Projectile.Components.Get<Sticky>() is Sticky Sticky)
				{
					Equipment.Owner.OnDeath += Sticky.Destroy;
				}
			}
		}
		else
		{
			if (KeyDown)
			{
				TimeSinceInputFirstDown = 0;
			}
		}

		WasInputDownLastTick = KeyDown;
	}

	protected override void SetProjectileVelocity(Rigidbody ProjectileRigidbody, Vector3 AimForward)
	{
		const float Factor = .66f;
		var SpeedFactor = 1 + (Math.Min(TimeSinceInputFirstDown, MaxChargeTime) * Factor);
		ProjectileRigidbody.Velocity = AimForward * ProjectileHorizontalSpeed * SpeedFactor;
		ProjectileRigidbody.Velocity += Vector3.Up * ProjectileVerticalSpeed;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		bool IsAltFire = Input.Down("attack2");
		if (IsAltFire && StickyTrackerComponent.IsValid())
		{
			StickyTrackerComponent.Detonate();
		}
	}
}
