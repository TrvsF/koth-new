namespace KOTH;


public static class MapSystem
{
	public static IEnumerable<MapDefinition> All
	{
		get
		{
			return ResourceLibrary.GetAll<MapDefinition>();
		}
	}
}
