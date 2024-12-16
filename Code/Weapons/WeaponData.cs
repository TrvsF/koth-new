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
	[Category("Base")]
	public string Name { get; set; } = "My Equipment";

	[Category("Base")]
	public string Description { get; set; } = "";

	[Category("Base")]
	public EEquipmentSlot Slot { get; set; }

	/// <summary>
	/// If set, only this team can buy the equipment.
	/// </summary>
	[Category("Base")]
	public Team Team { get; set; }

	/// <summary>
	/// If false, only <see cref="Team"/> can pick up this equipment.
	/// </summary>
	[Category("Base"), HideIf(nameof(Team), Team.Unassigned)]
	public bool CanOtherTeamPickUp { get; set; } = true;

	/// <summary>
	/// If true, owner will drop this equipment if they disconnect.
	/// </summary>
	[Category("Base")]
	public bool DropOnDisconnect { get; set; } = false;

	/// <summary>
	/// The equipment's icon
	/// </summary>
	[Group("Base"), ImageAssetPath] public string Icon { get; set; }

	/// <summary>
	/// Is this equipment shown in the buy menu
	/// </summary>
	[Category("Economy")] public bool IsPurchasable { get; set; } = true;

	/// <summary>
	/// How much is this equipment to buy in the buy menu?
	/// </summary>
	[Category("Economy")] public int Price { get; set; } = 0;

	/// <summary>
	/// How much money do you get per kill with this equipment?
	/// </summary>
	[Category("Economy")] public int KillReward { get; set; } = 300;

	/// <summary>
	/// The prefab to create and attach to the player when spawning it in.
	/// </summary>
	[Category("Prefabs")]
	public GameObject MainPrefab { get; set; }

	/// <summary>
	/// The prefab to create when making a viewmodel for this equipment.
	/// </summary>
	[Category("Prefabs")]
	public GameObject ViewModelPrefab { get; set; }

	// this gives us a reference to its bounds(?)
	[Category("Information")]
	public Model WorldModel { get; set; }

	[Category("Dropping")]
	public Vector3 DroppedSize { get; set; } = new(8, 2, 8);

	[Category("Dropping")]
	public Vector3 DroppedCenter { get; set; } = new(0, 0, 0);

	[Category("Damage")]
	public float? ArmorReduction { get; set; }

	[Category("Damage")]
	public float? HelmetReduction { get; set; }

	public bool IsPurchasableForTeam(Team team)
	{
		return Team == Team.Unassigned || Team == team;
	}
}
