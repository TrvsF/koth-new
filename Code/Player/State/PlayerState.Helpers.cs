using KOTH.UI;
using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Events;
using System.Collections.Generic;

namespace KOTH;

public partial class PlayerState
{
	// TODO : yuk singleton, TODO : create connection -> player state map?
	public static PlayerState Local { get; private set; }
}
