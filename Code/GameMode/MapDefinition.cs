namespace KOTH;

[GameResource("koth/Map Definition", "map", "A map definition")]
public partial class MapDefinition : GameResource
{
	[Property] public string Title { get; set; }
	[Property] public string Description { get; set; }
	[Property] public SceneFile SceneFile { get; set; }
	[Property, ImageAssetPath] public string Background { get; set; }
}
