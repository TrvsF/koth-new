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
		var PlayerStats = $"{Connection.Local.DisplayName} : Level {LocalPlayerLevelObject.LastValue} : {LocalStats.KillsStat.Value}";

		TextRenderer.Text = $"{PlayerStats}";
	}
}
