using Sandbox.Events;

namespace KOTH;

public class SpawnRule
{
	public Team Team { get; set; }
	public string Tag { get; set; }

	public int MinPlayers { get; set; }
	public int MaxPlayers { get; set; }
}

public sealed class TeamSpawnAssigner : Component,
	ISpawnAssigner
{
	[Property, Title("Tags")]
	public TagSet SpawnTags { get; private set; } = new();

	[Property, InlineEditor]
	public List<SpawnRule> SpawnRules { get; private set; } = new();

	public SpawnPointInfo GetSpawnPoint(PlayerState player)
	{
		var Team = player.Team;
		var Spawns = GameUtils.GetSpawnPoints(Team, SpawnTags.ToArray()).Shuffle();

		if (Spawns.Count == 0 && player.Team != Team.Unassigned)
		{
			Log.Warning($"No spawn points for team {Team}!");
			return GameUtils.GetRandomSpawnPoint(Team.Unassigned);
		}

		return Spawns.FirstOrDefault();
	}
}
