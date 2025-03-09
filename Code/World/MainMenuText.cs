using KOTH.PlayerExp;
using Sandbox;
using Sandbox.Diagnostics;

namespace KOTH;

public sealed class MainMenuText : Component
{
	[RequireComponent] public TextRenderer TextRenderer { get; set; }

	protected override void OnStart()
	{
		base.OnStart();

		Assert.NotNull(Connection.Local);

		TextRenderer.Text = "";

		var LocalStats = PlayerState.Local.LocalStats;

		var LocalPlayerLevelObject = ExpManager.GetCurrentLocalLevel();
		var PlayerWelcome = $"welcome {Connection.Local.DisplayName} : Level {LocalPlayerLevelObject.LastValue}";
		var PlayerStats = $"kills {LocalStats.KillsStat.Value} : Deaths {LocalStats.KillsStat.Value}";

		TextRenderer.Text = $"{PlayerWelcome}\n" +
			$"{PlayerStats}";
	}
}
