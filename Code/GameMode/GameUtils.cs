using System.IO;

namespace KOTH;

public static partial class GameUtils
{
	public static IEnumerable<PlayerState> AllPlayers => Game.ActiveScene.GetAllComponents<PlayerState>();
	public static PlayerState GetPlayerState(Guid? id) => AllPlayers.FirstOrDefault(n => n.PlayerPawn?.Id == id);
	public static IEnumerable<PlayerState> GetPlayers(Team team) => AllPlayers.Where(x => x.Team == team);

	public static IDescription GetDescription(GameObject go) => go?.Components.Get<IDescription>(FindMode.EverythingInSelfAndDescendants);
	public static IDescription GetDescription(Component component) => GetDescription(component?.GameObject);

	public static List<TeamSpawnPoint> GetAllSpawns()
	{
		if (Game.ActiveScene == null)
		{
			return [];
		}

		return Game.ActiveScene.GetAllComponents<TeamSpawnPoint>().ToList();
	}

	public static TeamSpawnPoint GetRandomTeamSpawn(Team Team)
	{
		return Random.Shared.FromList(GetAllSpawns().Where(Spawn => Spawn.Team == Team && !Spawn.IsDummy).ToList());
	}

	public static List<TeamSpawnPoint> GetDummySpawns()
	{
		return GetAllSpawns().Where(Spawn => Spawn.IsDummy).ToList();
	}
}
