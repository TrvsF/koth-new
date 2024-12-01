using KOTH.UI;
using Sandbox.Events;

namespace KOTH;

/// <summary>
/// Shows a countdown based on the duration of the current state.
/// </summary>
public sealed class ShowCountDown : Component,
	IGameEventHandler<EnterStateEvent>,
	IGameEventHandler<LeaveStateEvent>
{
	void IGameEventHandler<EnterStateEvent>.OnGameEvent(EnterStateEvent eventArgs)
	{
		GameMode.Instance.ShowStateCountDownTimer();
	}

	void IGameEventHandler<LeaveStateEvent>.OnGameEvent(LeaveStateEvent eventArgs)
	{
		GameMode.Instance.HideTimer();
	}
}

public sealed class ShowStatusText : Component,
	IGameEventHandler<EnterStateEvent>,
	IGameEventHandler<LeaveStateEvent>
{
	[Property][HostSync] public bool IsKoth { get; set; } = false;

	[Property, HideIf(nameof(IsKoth), true)] public bool BothTeams { get; set; } = true;
	[Property, HideIf(nameof(IsKoth), true), HideIf(nameof(BothTeams), true)] public Team Team { get; set; }
	[Property, HideIf(nameof(IsKoth), true)] public string StatusText { get; set; } = "";

	/////////////////////////////////////////////////////////////////////////////////////////////

	void IGameEventHandler<EnterStateEvent>.OnGameEvent(EnterStateEvent eventArgs)
	{
		if (!string.IsNullOrEmpty(StatusText))
		{
			if (BothTeams)
			{
				GameMode.Instance.ShowStatusText(StatusText);
			}
			else
			{
				GameMode.Instance.ShowStatusText(Team, StatusText);
			}
		}
	}

	void IGameEventHandler<LeaveStateEvent>.OnGameEvent(LeaveStateEvent eventArgs)
	{
		if (!string.IsNullOrEmpty(StatusText))
		{
			if (BothTeams)
			{
				GameMode.Instance.HideStatusText();
			}
			else
			{
				GameMode.Instance.HideStatusText(Team);
			}
		}
	}
}

/// <summary>
/// Shows a toast in the middle of the screen for the duration of this state.
/// </summary>
public sealed class ShowToast : Component,
	IGameEventHandler<EnterStateEvent>,
	IGameEventHandler<LeaveStateEvent>
{
	[Property] public string Message { get; set; }
	[Property] public ToastType Type { get; set; }

	void IGameEventHandler<EnterStateEvent>.OnGameEvent(EnterStateEvent eventArgs)
	{
		if (!string.IsNullOrEmpty(Message))
		{
			GameMode.Instance.ShowToast(Message, Type, eventArgs.State.DefaultDuration);
		}
	}

	void IGameEventHandler<LeaveStateEvent>.OnGameEvent(LeaveStateEvent eventArgs)
	{
		if (!string.IsNullOrEmpty(Message))
		{
			GameMode.Instance.HideToast();
		}
	}
}

/// <summary>
/// Display a special icon in the game status display.
/// </summary>
public sealed class ShowStatusIcon : Component
{
	/// <summary>
	/// Path to the icon image to show.
	/// </summary>
	[Property]
	public string IconPath { get; set; } = "/ui/items/c4.png";
}
