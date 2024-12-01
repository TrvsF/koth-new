using KOTH.UI;
using Sandbox.Diagnostics;
using Sandbox.Events;

namespace KOTH;

/// <summary>
/// Respawn players after a delay.
/// </summary>
public sealed class PlayerAutoRespawner : Component,
	IGameEventHandler<UpdateStateEvent>
{
	[Property, HostSync] public float RespawnDelaySeconds { get; set; } = 0f;
	[Property] public bool AllowSpectatorsToSpawn { get; set; } = false;

	void IGameEventHandler<UpdateStateEvent>.OnGameEvent(UpdateStateEvent eventArgs)
	{
		// Log.Info("Requested Spawn");

		Assert.True(Networking.IsHost);

		foreach (var PlayerState in GameUtils.AllPlayers)
		{
			if (PlayerState.PlayerPawn.IsValid() && PlayerState.PlayerPawn.IsAlive)
			{
				continue;
			}

			//if (!PlayerState.IsConnected)
			//{
			//	continue;
			//}

			if (PlayerState.PlayerStateSpawningState == EPlayerStateSpawningState.WaitingForSpawn)
			{
				SpawnPointInfo SpawnPoint = GameUtils.GetRandomSpawnPoint(Team.CounterTerrorist);
				PlayerState.RequestSpawn(SpawnPoint);
			}
		}
	}
}
