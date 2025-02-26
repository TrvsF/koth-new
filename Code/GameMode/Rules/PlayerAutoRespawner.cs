using KOTH.UI;
using Sandbox.Diagnostics;
using Sandbox.Events;

namespace KOTH;

public sealed class PlayerAutoRespawner : Component,
	IGameEventHandler<UpdateStateEvent>
{
	[Property, Sync(SyncFlags.FromHost)] public float RespawnDelaySeconds { get; private set; } = 0f;

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

			// NOTE : there appear to be cases where a playerstate can become inactive
			// but still remain in the PlayerStates NetList. Ignore them for now......
			if (!PlayerState.Active)
			{
				continue;
			}

			if (PlayerState.PlayerStateSpawningState == EPlayerStateSpawningState.InstantSpawn)
			{
				SpawnPlayer(PlayerState);
				return;
			}
			
			if (PlayerState.PlayerStateSpawningState == EPlayerStateSpawningState.WaitingForSpawn)
			{
				if (!PlayersWaitingForSpawn.ContainsKey(PlayerState))
				{
					PlayersWaitingForSpawn.Add(PlayerState, 0);
				}

				var TimeWaitingForSpawn = PlayersWaitingForSpawn[PlayerState];

				var TimeTilSpawn = RespawnDelaySeconds - TimeWaitingForSpawn;
				PlayerState.SetTimeTilAttemptedSpawn(TimeTilSpawn);

				if (TimeWaitingForSpawn < RespawnDelaySeconds)
				{
					continue;
				}

				SpawnPlayer(PlayerState);
			}
		}
	}

	public void SpawnPlayer(PlayerState PlayerState)
	{
		SpawnPointInfo SpawnPoint = GameUtils.GetRandomSpawnPoint(PlayerState.Team);
		PlayerState.SpawnPlayerPawn(SpawnPoint);
		PlayerState.SetTimeTilAttemptedSpawn(-1); // TODO : clean?
		PlayersWaitingForSpawn.Remove(PlayerState);
	}
}
