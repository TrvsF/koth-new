namespace KOTH.PlayerExp;

public class ExpManager : Component
{

	/// <summary>
	///  Broadcasts exp event to player to be recorded in Sandbox's 'Stats' Service.
	///  There is a possibility that we could move this off to http server that records the stats directly so we have more
	///	 control over exp stats and other stats.
	/// </summary>
	/// <param name="expEvent">The Event Containing how much exp the player should receive</param>
	/// <param name="player">The player receiving the exp</param>
	public void BroadcastExpEvent(ExpEvent expEvent, PlayerPawn player)
	{
		var playerState = GameUtils.GetPlayer(player.Id);

		Log.Info($"Broadcasting exp event to {player}");
		using (Rpc.FilterInclude(n => n.SteamId == playerState.SteamId))
		{
			ProcessExpEvent(expEvent);
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
	private void ProcessExpEvent(ExpEvent expEvent)
	{
		Log.Info($"Received exp event {expEvent.Amount} from {expEvent.Origin}");
		Sandbox.Services.Stats.Increment("player_exp", expEvent.Amount, "origin", expEvent.Origin.ToString());
		var exp = Sandbox.Services.Stats.LocalPlayer.Get("player_exp");
		Log.Info($"Player exp is now {exp.Sum}");
	}

}
