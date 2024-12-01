using Sandbox.Events;

namespace KOTH;

/// <summary>
/// Keep players frozen while this state is active.
/// </summary>
public sealed class FreezePlayers : Component,
	IGameEventHandler<EnterStateEvent>,
	IGameEventHandler<LeaveStateEvent>,
	IGameEventHandler<PlayerSpawnedEvent>
{
	[Property][HostSync] public int FreezeTime { get; set; }

	/////////////////////////////////////////////////////////

	[HostSync] private bool IsFrozen { get; set; } = true;
	[HostSync] private TimeSince TimeSinceEnterState { get; set; } = new();

	void IGameEventHandler<EnterStateEvent>.OnGameEvent(EnterStateEvent eventArgs)
	{
		foreach (var player in GameUtils.PlayerPawns)
		{
			player.IsFrozen = IsFrozen;
		}
	}

	void IGameEventHandler<LeaveStateEvent>.OnGameEvent(LeaveStateEvent eventArgs)
	{
		foreach (var player in GameUtils.PlayerPawns)
		{
			player.IsFrozen = false;
		}
	}

	void IGameEventHandler<PlayerSpawnedEvent>.OnGameEvent(PlayerSpawnedEvent eventArgs)
	{
		eventArgs.Player.IsFrozen = IsFrozen;
	}

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
