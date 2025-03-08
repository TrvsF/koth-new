using KOTH.UI;
using Sandbox.Diagnostics;
using Sandbox.Events;

namespace KOTH;

public sealed class PlayerAutoRespawner : Component,
	IGameEventHandler<UpdateStateEvent>
{
	[Property, Sync(SyncFlags.FromHost)] public bool TryUseGamemodeRespawnTime { get; private set; } = false;
	[Property, Sync(SyncFlags.FromHost)] public bool UseSpawnWaves { get; private set; } = false;
	[Property, Sync(SyncFlags.FromHost)] public float DefaultRespawnDelaySeconds { get; private set; } = 0f;

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
					EmplacePlayerInSpawnMap(PlayerState);
				}

				var TimeWaitingForSpawn = PlayersWaitingForSpawn[PlayerState];

				var RespawnDelaySeconds = GetPlayerRespawnTime(PlayerState);
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

	private void SpawnPlayer(PlayerState PlayerState)
	{
		Assert.IsValid(PlayerState);

		Log.Info(PlayerState.Team);

		var SpawnPoint = GameUtils.GetRandomTeamSpawn(PlayerState.Team);
		PlayerState.SpawnPlayerPawn(SpawnPoint);
		PlayerState.SetTimeTilAttemptedSpawn(-1); // TODO : clean?
		PlayersWaitingForSpawn.Remove(PlayerState);
	}

	private void EmplacePlayerInSpawnMap(PlayerState PlayerState)
	{
		if (UseSpawnWaves)
		{
			foreach (var (FoundPlayer, Time) in PlayersWaitingForSpawn)
			{
				if (PlayerState.Team == FoundPlayer.Team)
				{
					if (Time < 3f)
					{
						PlayersWaitingForSpawn.Add(PlayerState, Time);
						return;
					}
				}
			}
		}

		PlayersWaitingForSpawn.Add(PlayerState, 0);
	}

	private float GetPlayerRespawnTime(PlayerState PlayerState)
	{
		Assert.IsValid(PlayerState);

		var RespawnTime = DefaultRespawnDelaySeconds;

		if (TryUseGamemodeRespawnTime)
		{
			foreach (var GameRule in GameObject.Components.GetAll())
			{
				if (GameRule is ITeamSpawnTime TeamSpawnTime)
				{
					if (PlayerState.Team == Team.CounterTerrorist)
					{
						RespawnTime = TeamSpawnTime.CTSpawnTime;
						break;
					}
					else
					{
						RespawnTime = TeamSpawnTime.TSpawnTime;
						break;
					}
				}
			}
		}

		return RespawnTime;
	}
}
