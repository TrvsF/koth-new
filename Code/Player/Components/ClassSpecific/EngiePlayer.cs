using Sandbox;
using Sandbox.Events;
using static Sandbox.PhysicsContact;

namespace KOTH;

public sealed class EngiePlayer : Component
{
	[Property] public GameObject TurretPrefab { get; private set; }

	////////////////////////////////////////////////////////////////////////

	[Sync(SyncFlags.FromHost)] public PlayerPawn OwnerPlayerPawn { get; set; }


}
