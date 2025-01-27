using Sandbox;
using Sandbox.Events;
using static Sandbox.PhysicsContact;

namespace KOTH;

public sealed class EngiePlayer : Component
{
	public PlayerPawn OwnerPawn { get => GameObject.Root.GetComponent<PlayerPawn>(); }

	public TurretComponent ActiveTurretComponent { get; private set; }
	public bool IsTurretInWorld { get => ActiveTurretComponent.IsValid(); }

	protected override void OnStart()
	{
		base.OnStart();

		// TODO : get all owned objects & assign turret to us
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if (IsProxy)
		{
			return;
		}

		// TODO : make methods

		///////////////////////////////////////////////////

		bool RequestBuilding = Input.Down("deploy");

		if (RequestBuilding && !IsTurretInWorld)
		{
			var Projectile = GameMode.Instance.ClassList.TurretPrefab.Clone(GameObject.Root.WorldPosition + (OwnerPawn.AimRay.Forward * 128), Rotation.Identity);
			ActiveTurretComponent = Projectile.Components.Get<TurretComponent>();

			// can this be Connection.Local rather than PlayerState.Local.Connection ?
			Projectile.NetworkSpawn(true, PlayerState.Local.Connection);
		}

		//////////////////////////////////////////////////

		bool RequestWeaponEquip = Input.Down("attack2");

		if (RequestWeaponEquip)
		{
			var TraceResults = Scene.Trace.Ray(OwnerPawn.CenterPosition, OwnerPawn.CenterPosition + (OwnerPawn.AimRay.Forward * 256f)) // magic
			.UseHitboxes()
			.IgnoreGameObjectHierarchy(OwnerPawn.GameObject.Root)
			.Size(Vector3.One)
			.RunAll();

			foreach (var TraceElement in TraceResults)
			{
				if (!TraceElement.Hit)
				{
					continue;
				}

				if (TraceElement.GameObject.Root.Components.Get<TurretComponent>(FindMode.EnabledInSelfAndDescendants) is { } HitTurret)
				{
					HitTurret.SetFromWeaponGameObject(OwnerPawn.Inventory.CurrentWeaponGameObject);
					// unequip weapon
					// cooldown etc
				}
			}
		}
	}

}
