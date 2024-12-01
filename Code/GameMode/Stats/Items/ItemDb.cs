using Sandbox;

namespace KOTH;

[GameResource("koth/ItemDatabase", "itemdb", "", IconBgColor = "#CC5151", Icon = "track_changes")]
public class ItemDb : GameResource
{
	public Dictionary<string, EItem> ItemEntries { get; set; } = new();
}

public enum EItem
{
	GoldenRkt,
	Hat,
}

