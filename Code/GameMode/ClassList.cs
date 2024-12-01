using Sandbox;

namespace KOTH;

public sealed class ClassList : Component
{
	[Property] public List<CharacterDefinition> ClassDefinitions { get; set; }
}
