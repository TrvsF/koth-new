using Sandbox.Events;

namespace KOTH;

public sealed class WaitForPlayers : Component,
	IGameEventHandler<EnterStateEvent>,
	IGameEventHandler<UpdateStateEvent>
{
	[Property, Sync(SyncFlags.FromHost)] public int MinPlayerCount { get; set; } = 2;

	void IGameEventHandler<EnterStateEvent>.OnGameEvent(EnterStateEvent eventArgs)
	{
	}

	void IGameEventHandler<UpdateStateEvent>.OnGameEvent(UpdateStateEvent eventArgs)
	{
		var PlayerCount = GameNetworkManager.PlayerStates.Count;

		if (PlayerCount >= MinPlayerCount)
		{
			GameMode.Instance.StateMachine.Transition(eventArgs.State.DefaultNextState);
		}

		// HACK : seems like states are now always timed (-1 doesn't make it inf)
		// so if the time is 999 periodically reset
		if (GameObject.GetComponent<StateComponent>() is { } ParentState)
		{
			if (ParentState.DefaultDuration == 999 && HACKTimeSinceLastReset > 500)
			{
				HACKTimeSinceLastReset = 0;
				GameMode.Instance.StateMachine.Transition(ParentState);
			}
		}
	}
	TimeSince HACKTimeSinceLastReset = new();
}
