using KOTH.UI;
using Sandbox.Diagnostics;
using Sandbox.Events;

namespace KOTH;

public sealed class PlayerAutoRespawner : Component,
	IGameEventHandler<UpdateStateEvent>
{
	[Property, HostSync] public float RespawnDelaySeconds { get; private set; } = 0f;
	[Property] public bool AllowSpectatorsToSpawn { get; set; } = false;

	private Dictionary<PlayerState, TimeSince> PlayersWaitingForSpawn = new();

	void IGameEventHandler<UpdateStateEvent>.OnGameEvent(UpdateStateEvent eventArgs)
	{
		Assert.True(Networking.IsHost);

		foreach (var PlayerState in GameNetworkManager.PlayerStates)
		{
			if (PlayerState.PlayerPawn.IsValid() && PlayerState.PlayerPawn.IsAlive)
			{
				continue;
			}

			if (PlayerState.PlayerStateSpawningState == EPlayerStateSpawningState.WaitingForSpawn)
			{
				if (!PlayersWaitingForSpawn.ContainsKey(PlayerState))
				{
					PlayersWaitingForSpawn.Add(PlayerState, 0);
					continue;
				}

				if (PlayersWaitingForSpawn[PlayerState] < RespawnDelaySeconds)
				{
					continue;
				}

				SpawnPointInfo SpawnPoint = GameUtils.GetRandomSpawnPoint(Team.CounterTerrorist);
				PlayerState.RequestSpawn(SpawnPoint);
				PlayersWaitingForSpawn.Remove(PlayerState);
			}
		}
	}
}
