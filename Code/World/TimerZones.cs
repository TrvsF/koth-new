using Sandbox;
using Sandbox.Diagnostics;

namespace KOTH;

public sealed class TimerZones : Component
{
	[Property] Zone EnterZone { get; set; }
	[Property] Zone EndZone { get; set; }

	private bool IsTimerGoing = false;
	private TimeSince TimeSinceEnter;

	protected override void OnStart()
	{
		base.OnStart();

		EnterZone.OnZoneEnter += StartTimer;
		EndZone.OnZoneEnter += StopTimer;
	}

	private void StartTimer(Collider Collider)
	{
		if (IsTimerGoing)
		{
			return;
		}

		if (Collider.GameObject.Root.GetComponent<PlayerPawn>() is { } PlayerPawn)
		{
			Log.Info($"starting timer for {PlayerPawn}");
			TimeSinceEnter = 0;
			IsTimerGoing = true;
		}
	}

	private void StopTimer(Collider Collider)
	{
		if (!IsTimerGoing)
		{
			return;
		}

		if (Collider.GameObject.Root.GetComponent<PlayerPawn>() is { } PlayerPawn)
		{
			Log.Info($"stopping timer for {PlayerPawn} @ {TimeSinceEnter}");
			IsTimerGoing = false;
			LocalSetJumpTimer(TimeSinceEnter);
		}
	}

	[Rpc.Broadcast]
	private static void LocalSetJumpTimer(double Time)
	{
		Assert.True(Time > 0);

		if (Sandbox.Services.Stats.LocalPlayer.TryGet("jump1_time", out var BestTime))
		{
			if (Time > BestTime.LastValue)
			{
				Log.Info("2slow");
				return;
			}
		}

		Sandbox.Services.Stats.SetValue("jump1_time", Time);
	}
}
