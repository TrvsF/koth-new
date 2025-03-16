using Sandbox.Diagnostics;
using Sandbox.Events;

namespace KOTH.PlayerExp;

public class ExpManager : SingletonComponent<ExpManager>,
	IGameEventHandler<KillBroadcastEvent>
{
	private readonly float _levelFactor = 1.8f;
	private readonly int _firstLevelExp = 25;

	public void OnGameEvent(KillBroadcastEvent KillEvent)
	{
		if (!Networking.IsHost)
		{
			return;
		}

		var DamageEvent = KillEvent.DamageEvent;

		if (DamageEvent.IsDummyDamage)
		{
			return;
		}

		var PlayerState = DamageEvent.AttackerPlayerState;

		if (!PlayerState.IsValid())
		{
			// likely means we're a dummy
			return;
		}

		FExpEvent ExpEvent = new()
		{
			Amount = CalculateExp(10, 5),
			Origin = ExpOrigins.Kill,
		};

		Log.Info($"Broadcasting exp event {ExpEvent} to {DamageEvent.AssumedAttackerPlayerPawn}:{PlayerState}:{PlayerState.SteamId}");

		using (Rpc.FilterInclude(PlayerState.Connection))
		{
			ProcessExpEvent(ExpEvent);
		}
	}

	/// <summary>
	/// Calculates exp
	/// </summary>
	/// <param name="baseExp">the lowest the exp could be</param>
	/// <param name="entropy">the highest the exp could be</param>
	/// <returns>a number in between the two values</returns>
	public int CalculateExp(int baseExp, int entropy)
	{
		var random = new Random();
		return random.Next(baseExp, baseExp + entropy + 1);
	}


	/// <summary>
	///  Processes the exp event and call the increment function for exp
	/// </summary>
	/// <param name="expEvent">The event received from the server</param>
	[Rpc.Broadcast]
	private void ProcessExpEvent(FExpEvent expEvent)
	{
		Log.Info($"Received exp event {expEvent.Amount} from {expEvent.Origin}");
		Sandbox.Services.Stats.Increment("player_exp", expEvent.Amount, "origin", expEvent.Origin.ToString());
		Sandbox.Services.Stats.Increment("player_exp_current", expEvent.Amount, "origin", expEvent.Origin.ToString());
		CheckLevelUp();
	}

	/// <summary>
	/// Checks if the player has accumulated enough experience points to level up.
	/// If the threshold for leveling up is met or exceeded, the player's level is incremented,
	/// and the current experience value is adjusted accordingly.
	/// </summary>
	public void CheckLevelUp()
	{
		var exp = Sandbox.Services.Stats.LocalPlayer.Get("player_exp_current");
		var level = GetCurrentLocalLevel();
		// probably dont need to use floor but its safer and stops rounding errors
		var threshold = (int)Math.Floor((level.LastValue * _firstLevelExp) / _levelFactor);

		if (exp.Sum >= threshold)
		{
			// sets value instead of increment
			Sandbox.Services.Stats.SetValue("player_level", level.LastValue + 1);
			Sandbox.Services.Stats.SetValue("player_exp_current", exp.Sum > threshold ? exp.Sum - threshold : 0);
			Log.Info($"Player leveled up: {level.LastValue + 1}");

			Scene.Dispatch(new LevelUpEvent((int) level.LastValue + 1));
		}
	}

	public static Sandbox.Services.Stats.PlayerStat GetCurrentLocalLevel()
	{
		return Sandbox.Services.Stats.LocalPlayer.Get("player_level");
	}

	public static string GetLevelString(ulong SteamId)
	{
		var Stats = Sandbox.Services.Stats.GetPlayerStats(Game.Ident, (long)SteamId);
		return Stats.Get("player_level").LastValue.ToString();
	}
}
