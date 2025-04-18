using KOTH.PlayerExp;
using Sandbox.Diagnostics;
using Sandbox.Events;

namespace KOTH;

public struct FPlayerGameStat
{
	public FPlayerGameStat() { }

	public int Kills { get; set; } = 0;
	public int Deaths { get; set; } = 0;
	public double Damage { get; set; } = 0f;
	public double Heals { get; set; } = 0f;
}

public sealed class GameStats : SingletonComponent<GameStats>,
	IGameEventHandler<DamageBroadcastEvent>,
	IGameEventHandler<HealingBroadcastEvent>,
	IGameEventHandler<KillBroadcastEvent>
{
	[Sync(SyncFlags.FromHost)] public NetDictionary<PlayerState, FPlayerGameStat> PlayerStateStats { get; set; }

	///////////////////////////////////////////////////////////////////////////////

	protected override void OnAwake()
	{
		base.OnAwake();

		if (Networking.IsHost)
		{
			PlayerStateStats = new();
		}
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		foreach (var PlayerState in GameNetworkManager.PlayerStates)
		{
			PlayerStateStats.GetOrCreate(PlayerState);
		}
	}

	public void ResetAll()
	{
		Assert.True(Networking.IsHost);
	}

	public FPlayerGameStat GetStat(PlayerState PlayerState)
	{
		return PlayerStateStats[PlayerState];
	}

	///////////////////////////////////////////////////////////////////////////////

	public void OnGameEvent(KillBroadcastEvent KillEvent)
	{
		if (!Networking.IsHost)
		{
			return;
		}

		var Kill = KillEvent.DamageEvent;

		if (Kill.AttackerPlayerState.IsValid())
		{
			var Stats = PlayerStateStats[Kill.AttackerPlayerState];
			++Stats.Kills;
			PlayerStateStats[Kill.AttackerPlayerState] = Stats;

			using (Rpc.FilterInclude(Kill.AttackerPlayerState.Connection))
			{
				LocalIncrementPlayerStat("kills-new", 1);
			}
		}

		if (Kill.VictimPlayerState.IsValid())
		{
			var Stats = PlayerStateStats[Kill.VictimPlayerState];
			++Stats.Deaths;
			PlayerStateStats[Kill.VictimPlayerState] = Stats;

			using (Rpc.FilterInclude(Kill.VictimPlayerState.Connection))
			{
				LocalIncrementPlayerStat("deaths-new", 1);
			}
		}
	}

	public void OnGameEvent(HealingBroadcastEvent HealingEvent)
	{
		if (!Networking.IsHost)
		{
			return;
		}

		var Heals = HealingEvent.HealingRequest;
		var HealsPlayerState = Heals.HealerPlayerState;

		if (HealsPlayerState.IsValid())
		{
			var Stats = PlayerStateStats[HealsPlayerState];
			Stats.Heals += Heals.Heals;
			PlayerStateStats[HealsPlayerState] = Stats;

			using (Rpc.FilterInclude(HealsPlayerState.Connection))
			{
				LocalIncrementPlayerStat("heals-given-new", Heals.Heals);
			}
		}
	}

	public void OnGameEvent(DamageBroadcastEvent DamageEvent)
	{
		if (!Networking.IsHost)
		{
			return;
		}

		var Damage = DamageEvent.DamageEvent;
		var DamagePlayerState = Damage.AttackerPlayerState;

		if (DamagePlayerState == Damage.VictimPlayerState)
		{
			return;
		}

		if (DamagePlayerState.IsValid())
		{
			var Stats = PlayerStateStats[DamagePlayerState];
			Stats.Damage += Damage.Damage;
			PlayerStateStats[DamagePlayerState] = Stats;

			using (Rpc.FilterInclude(DamagePlayerState.Connection))
			{
				LocalIncrementPlayerStat("dmg-given-new", Damage.Damage);
			}
		}
	}

	///////////////////////////////////////////////////////////////////////////////

	[Rpc.Broadcast]
	private void LocalIncrementPlayerStat(string StatName, float Value)
	{
		Sandbox.Services.Stats.Increment(StatName, Value);
	}

	// debug
	//protected override void OnFixedUpdate()
	//{
	//	base.OnFixedUpdate();

	//	if (!Networking.IsHost)
	//	{
	//		return;
	//	}

	//	foreach (var (PlayerState, Data) in PlayerStateStats)
	//	{
	//		Log.Info($"{PlayerState}:{Data.Damage}:{Data.Heals}:{Data.Kills}:{Data.Deaths}");
	//	}
	//}
}
