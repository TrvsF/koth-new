using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Events;
using System;
using System.Runtime.InteropServices;
using static Sandbox.PhysicsContact;

namespace KOTH;

public sealed class ScoutPlayer : Component
{
	public PlayerPawn OwnerPawn { get => GameObject.Root.GetComponent<PlayerPawn>(); }

	protected override void OnStart()
	{
		base.OnStart();

		// TODO : get all owned objects & assign turret to us
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
	}

	const float MaxWallDistance = 48f;
	private bool HasWallKicked = false;

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if (IsProxy)
		{
			return;
		}

		Assert.IsValid(OwnerPawn);
		
		if (OwnerPawn.IsGrounded)
		{
			HasWallKicked = false;
			return;
		}

		bool RequestedWallKick = Input.Pressed("jump") && !HasWallKicked;
		if (!RequestedWallKick)
		{
			return;
		}

		var WishInput = OwnerPawn.WishMove;
		var WishInputInverted = WishInput * -1f;
		var YawRotation = OwnerPawn.Camera.WorldRotation.Yaw();

		Log.Info(WishInputInverted);
		Log.Info(YawRotation);

		var RotatedInput = WishInput.RotateAround(Vector3.Zero, Rotation.FromYaw(YawRotation));
		var RotatedInputInverted = WishInputInverted.RotateAround(Vector3.Zero, Rotation.FromYaw(YawRotation));
		RotatedInputInverted = RotatedInputInverted.Normal;

		Log.Info(RotatedInputInverted);
		Ray HitRay = new(OwnerPawn.WorldPosition, RotatedInputInverted);

		Line HitLine = new(OwnerPawn.WorldPosition, RotatedInputInverted, MaxWallDistance);
		DebugOverlay.Line(HitLine, Color.Red, 100);
		
		var Hits = Scene.Trace.Ray(HitRay, MaxWallDistance)
			.IgnoreGameObjectHierarchy(OwnerPawn.GameObject)
			.RunAll();

		foreach (var Hit in Hits)
		{
			Log.Info(Hit.GameObject);
			var PunchVector = RotatedInput.WithZ(1f) * 250f;
			OwnerPawn.Punch(PunchVector);
			HasWallKicked = true;
			return;
		}
	}

	private bool CanWallkick()
	{
		return false;
	}
}
