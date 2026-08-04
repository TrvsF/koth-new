using KOTH.UI;
using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Events;
using System.Collections.Generic;

namespace KOTH;

public partial class PlayerState
{
	// TODO : create connection -> player state map?
	public static PlayerState Local { get; private set; }
	public static bool IsPaused { get; set; }

	protected override void OnUpdate()
	{
		base.OnUpdate();

		CameraUtils.LocalTick();

		if (Input.EscapePressed)
		{
			Input.EscapePressed = false;
			IsPaused = !IsPaused;
		}
	}
}
