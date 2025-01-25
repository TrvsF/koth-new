using Sandbox;
using Sandbox.Events;
using static Sandbox.PhysicsContact;

namespace KOTH;

public sealed class EngiePlayer : Component
{
	public TurretComponent ActiveTurretComponent { get; private set; }
	public bool IsTurretInWorld { get => ActiveTurretComponent.IsValid(); }

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

			var Projectile = GameMode.Instance.ClassList.TurretPrefab.Clone(GameObject.Root.WorldPosition, Rotation.Identity);
			ActiveTurretComponent = Projectile.Components.Get<TurretComponent>();

			Projectile.NetworkSpawn();
		}
	}

}
