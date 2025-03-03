using KOTH.UI;
using Sandbox.Diagnostics;
using Sandbox.Events;

namespace KOTH;

public sealed class ResetGame : Component,
	IGameEventHandler<EnterStateEvent>
{
	void IGameEventHandler<EnterStateEvent>.OnGameEvent(EnterStateEvent eventArgs)
	{
		Assert.True(Networking.IsHost);

		foreach (var PlayerState in GameNetworkManager.PlayerStates)
		{
			if (!PlayerState.IsValid() || PlayerState.RequestedCharacterDefinition == null)
			{
				continue;
			}

			var SpawnPoint = GameUtils.GetRandomTeamSpawn(PlayerState.Team);
			PlayerState.SpawnPlayerPawn(SpawnPoint);
		}
	}
}
