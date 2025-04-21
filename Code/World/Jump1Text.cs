using KOTH.PlayerExp;
using Sandbox;
using Sandbox.Diagnostics;

namespace KOTH;

public sealed class Jump1Text : Component
{
	[RequireComponent] public TextRenderer LeaderboardText { get; set; }
	
	protected override void OnStart()
	{
		base.OnStart();

		LeaderboardText.Text = "";
		SetLeaderboardText();
	}

	private async void SetLeaderboardText()
	{
		var Fullboard = Sandbox.Services.Leaderboards.GetFromStat(Game.Ident, "jump1_time");
		Fullboard.SetSortAscending();
		Fullboard.SetAggregationLast();
		await Fullboard.Refresh();

		LeaderboardText.Text = "Leaderboard\n";
		foreach (var TimeEntry in Fullboard.Entries)
		{
			LeaderboardText.Text += $"{TimeEntry.Rank}. {TimeEntry.DisplayName} : {TimeEntry.Value:0.00}s\n";
		}
	}
}
