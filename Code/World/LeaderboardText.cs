using KOTH.PlayerExp;
using Sandbox;
using Sandbox.Diagnostics;

namespace KOTH;

public sealed class LeaderboardText : Component
{
	[Property] public string StatName { get; set; }
	[RequireComponent] public TextRenderer LeaderboardTextRenderer { get; set; }

	public string HeaderText = "Leaderboard";

	protected override void OnStart()
	{
		base.OnStart();

		RefreshLeaderboardText();
	}

	public async void RefreshLeaderboardText()
	{
		LeaderboardTextRenderer.Text = "";

		var Fullboard = Sandbox.Services.Leaderboards.GetFromStat(Game.Ident, StatName);
		Fullboard.SetSortAscending();
		Fullboard.SetAggregationLast();
		await Fullboard.Refresh();

		LeaderboardTextRenderer.Text = $"{HeaderText}\n";
		foreach (var TimeEntry in Fullboard.Entries)
		{
			LeaderboardTextRenderer.Text += $"{TimeEntry.Rank}. {TimeEntry.DisplayName} : {TimeEntry.Value:0.00}s\n";
		}
	}
}
