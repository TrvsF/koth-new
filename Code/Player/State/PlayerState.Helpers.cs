using KOTH.UI;
using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Events;
using System.Collections.Generic;

namespace KOTH;

public partial class PlayerState
{
	// [HostSync] public static NetDictionary<Connection, PlayerState> PlayerStateConnectionPairs { get; private set; } = new();
	// TODO : 

	public static PlayerState Local { get; private set; }
	public static PlayerState Viewer { get; private set; }
}
