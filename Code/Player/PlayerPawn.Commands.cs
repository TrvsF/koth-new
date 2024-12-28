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
		Host_Suicide();
	}

	[Rpc.Owner]
	private static void Host_Suicide()
	{
		var LocalPawn = Game.ActiveScene.GetAllComponents<PlayerPawn>()
			.FirstOrDefault(p => p.Network.Owner == Rpc.Caller);

		if (!LocalPawn.IsValid())
			return;

		FDamageRequest DamageRequest = new()
		{
			TargetPlayerPawn = LocalPawn,
			AttackerPlayerPawn = null,
			DamageOrigin = 0,
			BaseDamage = float.MaxValue,
			BaseKnockbackStrength = 0,
			DirectImpact = true,
			DamageType = EDamageType.Melee,
		};
		Game.ActiveScene.Dispatch(new DamageRequestEvent(DamageRequest));
	}
}
