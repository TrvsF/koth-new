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
	public bool IsPaused = false;

	// HACK : doing the pause here because we can't listen
	// to Input.EscapePressed within the UI update method..
	protected override void OnUpdate()
	{
		base.OnUpdate();

		if (Local != this)
		{
			return;
		}

		if (Input.EscapePressed)
		{
			Input.EscapePressed = false;
			IsPaused = !IsPaused;
		}
	}
}
