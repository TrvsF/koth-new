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
	public static void BroadcastExpEvent(ExpEvent expEvent, PlayerPawn player)
	{
		var playerState = GameUtils.GetPlayer(player.Id);

		Log.Info("Broadcasting exp event");
		using (Rpc.FilterInclude(n => n.SteamId == playerState.SteamId))
		{
			ProcessExpEvent(expEvent);
		}
	}


	/// <summary>
	///  Processes the exp event and call the increment function for exp
	/// </summary>
	/// <param name="expEvent">The event received from the server</param>
	[Rpc.Broadcast]
	private static void ProcessExpEvent(ExpEvent expEvent)
	{
		Log.Info("Received exp event");
		Sandbox.Services.Stats.Increment("player_exp", expEvent.Amount, "origin", expEvent.Origin.ToString());
	}

}
