using Sandbox;
using Sandbox.Diagnostics;

namespace KOTH;

public sealed class TimerZones : Component
{
	[Property] Zone EnterZone { get; set; }
	[Property] Zone EndZone { get; set; }
	[Property] LeaderboardText LeaderboardText { get; set; }
	[Property] Vector3 StartPoint { get; set; }

	private bool IsTimerGoing = false;
	private TimeSince TimeSinceEnter;

	protected override void OnStart()
	{
		base.OnStart();

		Assert.IsValid(EnterZone);
		Assert.IsValid(EndZone);
		Assert.IsValid(LeaderboardText);

		EnterZone.OnZoneEnter += StartTimer;
		EndZone.OnZoneEnter += StopTimer;
	}

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		Gizmo.Draw.Color = Color.Green;
		Gizmo.Draw.SolidBox(BBox.FromPositionAndSize(StartPoint, 20));
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
			double RunTime = TimeSinceEnter;
			Log.Info($"stopping timer for {PlayerPawn} @ {RunTime}");

			IsTimerGoing = false;
			LocalSetJumpTimer(RunTime);

			LeaderboardText.HeaderText = $"(leaderboard will take time to update)\nYour last attempt {RunTime:0.00}s";
			LeaderboardText.RefreshLeaderboardText();

			TeleportPlayerToStart(PlayerPawn);
		}
	}

	private void TeleportPlayerToStart(PlayerPawn PlayerPawn)
	{
		if (!PlayerPawn.IsValid())
		{
			return;
		}

		Transform TeleportTransform = GameObject.Transform.Local;
		TeleportTransform.Position += StartPoint;
		PlayerPawn.Teleport(TeleportTransform);
	}

	[Rpc.Broadcast]
	private void LocalSetJumpTimer(double Time)
	{
		Assert.True(Time > 0);

		if (Sandbox.Services.Stats.LocalPlayer.TryGet(LeaderboardText.StatName, out var BestTime))
		{
			if (Time > BestTime.LastValue)
			{
				Log.Info("2slow");
				return;
			}
		}

		Sandbox.Services.Stats.SetValue(LeaderboardText.StatName, Time);
	}
}
