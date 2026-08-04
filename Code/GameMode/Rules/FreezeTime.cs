using Sandbox.Events;

namespace KOTH;

/// <summary>
/// Keep players frozen while this state is active.
/// </summary>
public sealed class FreezePlayers : Component
{
	[Property][Sync(SyncFlags.FromHost)] public int FreezeTime { get; set; }

	/////////////////////////////////////////////////////////

	[Sync(SyncFlags.FromHost)] private bool IsFrozen { get; set; } = true;
	[Sync(SyncFlags.FromHost)] private TimeSince TimeSinceEnterState { get; set; } = new();

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if (TimeSinceEnterState > FreezeTime && FreezeTime != -1)
		{
			// do i need this to be a host rpc?
			IsFrozen = false;
		}
	}
}
