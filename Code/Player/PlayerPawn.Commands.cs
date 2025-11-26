using KOTH.UI;
using Sandbox.Events;

namespace KOTH;

public partial class PlayerPawn
{
	[ConCmd("kill")]
	private static void ConKill()
	{
		var LocalPlayerPawn = PlayerState.Local?.PlayerPawn;

		if (LocalPlayerPawn == null)
		{
			return;
		}

		if (LocalPlayerPawn.IsValid() && LocalPlayerPawn.IsAlive)
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
