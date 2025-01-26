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
/**/		return;
		}

		bool RequestBuilding = Input.Down("deploy");

		if (RequestBuilding && !IsTurretInWorld)
		{
			Log.Info("deploy");

			var Projectile = GameMode.Instance.ClassList.TurretPrefab.Clone(GameObject.Root.WorldPosition + (OwnerPawn.AimRay.Forward * 128), Rotation.Identity);
			ActiveTurretComponent = Projectile.Components.Get<TurretComponent>();

			Projectile.NetworkSpawn();
		}
	}

}
