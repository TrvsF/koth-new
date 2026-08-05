using Sandbox;

namespace KOTH;

[GameResource("koth/CharacterDefinition", "chardef", "", IconBgColor = "#C45A21", Icon = "track_changes")]
public partial class CharacterDefinition : GameResource
{
	[Category("Character")] public string CharacterName { get; set; } = "Mark Nutt";
	[Category("Character")] public string Description { get; set; } = "suck my nutt";
	[Category("Character")] public Color Skin { get; set; } = Color.White;
	[Category("Character")] public int Height { get; set; } = 5;
	[Category("Character")] public int Fat { get; set; } = 5;

	//////////////////////////////////////////////////////////////////////////
	
	[Category("Class")] public List<Type> SpecificComponents { get; set; }
	
	//////////////////////////////////////////////////////////////////////////

	[Category("Damage")] public int MaxHealth { get; set; } = 100;
	[Category("Damage")] public float WeightKnockbackFactor { get; set; } = 1f;

	//////////////////////////////////////////////////////////////////////////

	[Category("Weapon")] public EquipmentResource PrimaryWeapon { get; set; }
	[Category("Weapon")] public EquipmentResource SecondaryWeapon { get; set; }

	//////////////////////////////////////////////////////////////////////////

	[Category("Movement")] public float WalkSpeed { get; set; } = 200f;
	[Category("Movement")] public float WalkFriction { get; set; } = 5f;
	[Category("Movement")] public float CrouchingFriction { get; set; } = 7f;
	[Category("Movement")] public float JumpPower { get; set; } = 300f;
	[Category("Movement")] public float CrouchLerpSpeed { get; set; } = 60f;
	[Category("Movement")] public float SlowCrouchLerpSpeed { get; set; } = 60f;

	// acceleration
	[Category("Movement")] public float AirAcceleration { get; set; } = 10f;
	[Category("Movement")] public float BaseAcceleration { get; set; } = 8f;
	[Category("Movement")] public float CrouchingAcceleration { get; set; } = 8f;
	[Category("Movement")] public float MaxAcceleration { get; set; } = 12f;
	[Category("Movement")] public float AirMaxAcceleration { get; set; } = 64f;
	[Category("Voices")] public SoundEvent MedicVoiceEvent{ get; set; }
}
