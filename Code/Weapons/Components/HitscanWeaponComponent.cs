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
	[Property, Group("HitScan"), EquipmentResourceProperty] public GameObject ProjectilePrefab { get; set; }
	[Property, Group("Projectile"), EquipmentResourceProperty] public float ProjectileHorizontalSpeed { get; set; } = 600.0f;
	[Property, Group("Projectile"), EquipmentResourceProperty] public float ProjectileVerticalSpeed { get; set; } = 0f;
	[Property, Group("Projectile"), EquipmentResourceProperty] public float FireRate { get; set; } = 0.2f;

	////////////////////////////////////////////////////////////////////////

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

	protected virtual GameObject Shoot()
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
		var ProjectilePosition = PlayerPawn.AimRay.Position + (Vector3.Down * 4f); // magic
		var ProjectileRotation = Rotation.LookAt(AimForward);
		var Projectile = ProjectilePrefab.Clone(ProjectilePosition, ProjectileRotation);

		// give it the player
		var ProjectileComponent = Projectile.Components.Get<Projectile>();
		ProjectileComponent.OwnerPlayerPawn = PlayerPawn;

		var Rigidbody = Projectile.Components.Get<Rigidbody>();
		SetProjectileVelocity(Rigidbody, AimForward);

		Projectile.Root.Tags.Add("self");
		Projectile.NetworkSpawn();
		return Projectile;
	}

	protected virtual void SetProjectileVelocity(Rigidbody ProjectileRigidbody, Vector3 AimForward)
	{
		ProjectileRigidbody.Velocity = AimForward * ProjectileHorizontalSpeed;
		ProjectileRigidbody.Velocity += Vector3.Up * ProjectileVerticalSpeed;
		ProjectileRigidbody.PhysicsBody.EnableSolidCollisions = false;
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
