using Sandbox.Diagnostics;
using Sandbox.Events;

namespace KOTH;

public sealed class SwapTeams : Component,
	IGameEventHandler<LeaveStateEvent>
{
	void IGameEventHandler<LeaveStateEvent>.OnGameEvent(LeaveStateEvent eventArgs)
	{
		Assert.True(Networking.IsHost);

		foreach (var PlayerState in GameNetworkManager.PlayerStates)
		{
			if (!PlayerState.IsValid() || PlayerState.Team == Team.Unassigned)
			{
				continue;
			}

			PlayerState.Team = PlayerState.Team.GetOpponents();
		}
	}
}
