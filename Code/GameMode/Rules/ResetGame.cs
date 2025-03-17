using KOTH.UI;
using Sandbox.Diagnostics;
using Sandbox.Events;

namespace KOTH;

public sealed class ResetGame : Component,
	IGameEventHandler<EnterStateEvent>,
	IGameEventHandler<UpdateStateEvent>
{
	bool HasReset = false;

	public void OnGameEvent(EnterStateEvent eventArgs)
	{
		HasReset = false;
	}

	void IGameEventHandler<UpdateStateEvent>.OnGameEvent(UpdateStateEvent eventArgs)
	{
		Assert.True(Networking.IsHost);

		if (HasReset)
		{
			return;
		}

		Log.Info("YERP");

		HasReset = true;

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
