using KOTH.UI;
using Sandbox.Events;

namespace KOTH;

public partial class PlayerPawn
{
	/// <summary>
	/// Development: should bots follow the player's input?
	/// </summary>
	[ConVar("hc1_bot_follow")] public static bool BotFollowHostInput { get; set; }

	[DeveloperCommand("Suicide", "Player"), ConCmd("kill")]
	private static void Command_Suicide()
	{
		var player = PlayerState.Local?.PlayerPawn;
		if (player is null) return;
		Log.Info("kill");
	}
}
