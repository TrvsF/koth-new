using Sandbox;

namespace KOTH;

public sealed class MapLocation : Component
{
	[RequireComponent]
	public Zone Zone { get; private set; }
}
