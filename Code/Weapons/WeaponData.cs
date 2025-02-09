using KOTH;

/// <summary>
/// What slot is this equipment for?
/// </summary>
public enum EEquipmentSlot
{
	Undefined = 0,
	Primary = 1,
	Secondary = 2,
}

/// <summary>
/// A resource definition for a piece of equipment. This could be a weapon, or a deployable, or a gadget, or a grenade.. Anything really.
/// </summary>
[GameResource("koth/Equipment Item", "equip", "", IconBgColor = "#5877E0", Icon = "track_changes")]
public partial class EquipmentResource : GameResource
{
	[Category("Base")] public string Name { get; set; } = "My Equipment";
	[Category("Base")] public string Description { get; set; } = "";
	[Category("Base")] public EEquipmentSlot Slot { get; set; }
	[Group("Base"), ImageAssetPath] public string Icon { get; set; }
	[Category("Prefabs")] public GameObject WorldPrefab { get; set; }
	[Category("Prefabs")] public GameObject ViewModelPrefab { get; set; }
}
