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
		// TODO : FIX!
		if (!Networking.IsHost)
		{
			return;
		}

		var LocalPlayerPawn = PlayerState.Local?.PlayerPawn;

		if (LocalPlayerPawn.IsValid() && LocalPlayerPawn.IsAlive && Game.ActiveScene.IsValid())
		{
			FDamageRequest DamageRequest = new()
			{
				TargetDamageComponent = LocalPlayerPawn.DamageComponent,
				BaseDamage = 9999,
			};
			Game.ActiveScene.Dispatch(new DamageRequestEvent(DamageRequest));
			Log.Info("kill");
		}
	}
}
