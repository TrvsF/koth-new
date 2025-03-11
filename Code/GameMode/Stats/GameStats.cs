using KOTH.PlayerExp;
using Sandbox.Diagnostics;
using Sandbox.Events;

namespace KOTH;

public struct FPlayerGameStats
{
	int Kills { get; init; }
	float Damage { get; init; }
	float Heals { get; init; }
}

public sealed class GameStats : SingletonComponent<GameStats>,
	IGameEventHandler<DamageBroadcastEvent>,
	IGameEventHandler<HealingBroadcastEvent>,
	IGameEventHandler<KillBroadcastEvent>
{
	[Sync(SyncFlags.FromHost)] public NetDictionary<PlayerState, FPlayerGameStats> PlayerStateStats { get; set; }

	protected override void OnAwake()
	{
		base.OnAwake();

		if (Networking.IsHost)
		{
			PlayerStateStats = new();
		}
	}

	public void OnNewPlayerState(PlayerState PlayerState)
	{
		Log.Info($"adding {PlayerState}");
		PlayerStateStats.Add(PlayerState, new());
	}

	public void ResetAll()
	{
		Assert.True(Networking.IsHost);
	}

	public void OnGameEvent(KillBroadcastEvent eventArgs)
	{
		if (!Networking.IsHost)
		{
			return;
		}

	}

	public void OnGameEvent(HealingBroadcastEvent eventArgs)
	{
		if (!Networking.IsHost)
		{
			return;
		}

	}

	public void OnGameEvent(DamageBroadcastEvent eventArgs)
	{
		if (!Networking.IsHost)
		{
			return;
		}

	}
}
