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

	protected override void OnDestroy()
	{
		base.OnDestroy();
	}

	private void OnTurretDestroy()
	{
		//
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
			ActiveTurretComponent.OwnerPawn = OwnerPawn;
			ActiveTurretComponent.OnDestroyed += OnTurretDestroy;

			// can this be Connection.Local rather than PlayerState.Local.Connection ?
			Projectile.NetworkSpawn(true, PlayerState.Local.Connection);
		}

		//////////////////////////////////////////////////

		bool RequestWeaponEquip = Input.Down("attack2");

		if (RequestWeaponEquip)
		{
			if (!Scene.IsValid())
			{
				Log.Warning($"the scene isn't fucking valid.. on engie component {this} attached to player {OwnerPawn}");
			}

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
					if (HitTurret.OwnerPawn == OwnerPawn)
					{
						HitTurret.SetFromWeaponGameObject(OwnerPawn.Inventory.CurrentWeaponGameObject);
						// TODO
						// unequip weapon
						// cooldown etc
					}
				}
			}
		}
	}

}
