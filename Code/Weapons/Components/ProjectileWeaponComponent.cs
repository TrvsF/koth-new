using KOTH.UI;
using Sandbox;
using Sandbox.Events;
using System.Net.Http;
using System;
using System.Text;

namespace KOTH;

[Title("Projectile Shooter"), Group("Weapon Components")]
public class ProjectileWeaponComponent : InputWeaponComponent,
	IGameEventHandler<EquipmentHolsteredEvent>
{
	[Property, Group("Projectile"), EquipmentResourceProperty] public GameObject ProjectilePrefab { get; set; }
	[Property, Group("Projectile"), EquipmentResourceProperty] public float ProjectileHorizontalSpeed { get; set; } = 600.0f;
	[Property, Group("Projectile"), EquipmentResourceProperty] public float ProjectileVerticalSpeed { get; set; } = 0f;
	[Property, Group("Projectile"), EquipmentResourceProperty] public float FireRate { get; set; } = 0.2f;

	////////////////////////////////////////////////////////////////////////

	protected AmmoComponent AmmoComponent { get; set; }
	protected ReloadWeaponComponent ReloadComponent { get; set; }

	// TODO : sticky only, look into doing a better way
	public bool IsStickyLauncher { get => StickyTrackerComponent.IsValid(); }
	public int ActiveStickies { get => StickyTrackerComponent.IsValid() ? StickyTrackerComponent.StickyCount : 0; }
	protected StickyTrackerComponent StickyTrackerComponent { get; set; }

	public void OnGameEvent(EquipmentHolsteredEvent eventArgs)
	{ }

	protected override void OnAwake()
	{
		base.OnAwake();

		AmmoComponent = Components.Get<AmmoComponent>();
		if (!AmmoComponent.IsValid())
		{
			Log.Warning($"{this} does not have a valid ammo component");
		}

		ReloadComponent = Components.Get<ReloadWeaponComponent>();
		if (!ReloadComponent.IsValid())
		{
			Log.Warning($"{this} does not have a reload component");
		}

		// this one's specualtive
		StickyTrackerComponent = Components.Get<StickyTrackerComponent>();
	}

	protected override void OnInputUpdate()
	{
		bool IsShooting = IsDown() && CanShoot();

		// we only care about projectiles spawned directly by a client
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
		var PlayerPawn = Equipment.Owner;
		if (!PlayerPawn.IsValid())
		{
			return;
		}

		TimeSinceShot = 0;
		AmmoComponent.Ammo--;

		var AimForward = PlayerPawn.AimRay.Forward;

		// create projectile object from prefab
		var ProjectilePosition = PlayerPawn.AimRay.Position + (Vector3.Down * 3f);
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
	}

	protected virtual void SetProjectileVelocity(Rigidbody ProjectileRigidbody, Vector3 AimForward)
	{
		ProjectileRigidbody.Velocity = AimForward * ProjectileHorizontalSpeed;
		ProjectileRigidbody.Velocity += Vector3.Up * ProjectileVerticalSpeed;
		ProjectileRigidbody.PhysicsBody.EnableSolidCollisions = false;
	}

	protected TimeSince TimeSinceShot = new();
	public bool CanShoot()
	{
		// these 2 should be ensures?
		if (!Equipment.IsValid()) return false;
		if (!Equipment.Owner.IsValid()) return false;

		if (ReloadComponent.IsReloading && AmmoComponent.Ammo > 0)
		{
			ReloadComponent.TryCancelReload();
		}

		if (Equipment.Owner.IsFrozen)
			return false;

		if (Equipment.Tags.Has("equipping"))
			return false;

		if (TimeSinceShot < FireRate)
			return false;

		if (!AmmoComponent.HasAmmo)
			return false;

		return true;
	}
}

[Title("Sticky Shooter"), Group("Weapon Components")]
public class StickyWeaponComponent : ProjectileWeaponComponent
{
	[Property] public float MaxChargeTime { get; private set; } = 1.6f;

	public TimeSince TimeSinceInputFirstDown { get; private set; } = new();
	public bool WasInputDownLastTick { get; private set; } = false;
	protected override void OnInputUpdate()
	{
		bool KeyDown = IsDown();

		if (WasInputDownLastTick)
		{
			if (!KeyDown && CanShoot())
			{
				Shoot();
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
		var SpeedFactor = 1 + (Math.Min(TimeSinceInputFirstDown, MaxChargeTime) * 0.40f);
		ProjectileRigidbody.Velocity = AimForward * ProjectileHorizontalSpeed * SpeedFactor;
		ProjectileRigidbody.Velocity += Vector3.Up * ProjectileVerticalSpeed;
		ProjectileRigidbody.PhysicsBody.EnableSolidCollisions = false;
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
