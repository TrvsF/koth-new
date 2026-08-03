using Sandbox;

namespace KOTH;

public sealed class ClassList : Component
{
	[Property] public List<CharacterDefinition> ClassDefinitions { get; private set; }
	// TODO : move me
	[Property] public GameObject TurretPrefab { get; private set; }
	[Property] public GameObject TeleporterPrefab { get; private set; }
}
