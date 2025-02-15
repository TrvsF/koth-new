using Sandbox.Diagnostics;

namespace KOTH;

public sealed class ScoutPlayer : Component
{
	public PlayerPawn OwnerPawn { get => GameObject.Root.GetComponent<PlayerPawn>(); }

	const float WallkickPower = 300f;
	const float MaxWallDistance = 48f;
	private bool HasWallKicked = false;

	Vector3 LastDifferentInput = Vector3.Zero;
	Vector3 LastInput = Vector3.Zero;
	protected override void OnUpdate()
	{
		base.OnUpdate();

		if (IsProxy)
		{
			return;
		}

		Assert.IsValid(OwnerPawn);

		var WishInput = OwnerPawn.WishMove.WithZ(0);
		if (WishInput != LastInput)
		{
			LastDifferentInput = LastInput;
		}
		LastInput = WishInput;

		if (OwnerPawn.IsGrounded)
		{
			HasWallKicked = false;
			return; // !
		}

		bool RequestedWallKick = Input.Pressed("jump") && !HasWallKicked;
		if (!RequestedWallKick)
		{
			return; // !
		}

		if (HasWallKicked = CanAttemptWallKick(WishInput, OwnerPawn, Scene, out Vector3 PunchVector))
		{
			OwnerPawn.Punch(PunchVector);
			HasWallKicked = true;
			return; // !
		}

		// try try again
		if (!HasWallKicked)
		{
			if (HasWallKicked = CanAttemptWallKick(LastDifferentInput, OwnerPawn, Scene, out Vector3 DontCarePunchVector))
			{
				OwnerPawn.Punch(PunchVector * 0.8f);
			}
		}
	}

	private static bool CanAttemptWallKick(Vector3 WishMove, PlayerPawn PlayerPawn, Scene Scene, out Vector3 PunchVector)
	{
		if (!PlayerPawn.IsValid() || !Scene.IsValid())
		{
			PunchVector = Vector3.Zero;
			return false;
		}

		var WishInputInverted = WishMove * -1f;
		var YawRotation = PlayerPawn.Camera.WorldRotation.Yaw();

		var RotatedInput = WishMove.RotateAround(Vector3.Zero, Rotation.FromYaw(YawRotation));
		var RotatedInputInverted = WishInputInverted.RotateAround(Vector3.Zero, Rotation.FromYaw(YawRotation));
		RotatedInputInverted = RotatedInputInverted.Normal;

		PunchVector = RotatedInput.WithZ(1f) * WallkickPower;

		Ray HitRay = new(PlayerPawn.WorldPosition, RotatedInputInverted);
		var Hits = Scene.Trace.Ray(HitRay, MaxWallDistance)
			.IgnoreGameObjectHierarchy(PlayerPawn.GameObject)
			.RunAll();

		bool CanWallKick = false;
		foreach (var _ in Hits) // asuming it geometry
		{
			CanWallKick = true;
		}

		// debug
		//Line HitLine = new(OwnerPawn.WorldPosition, RotatedInputInverted, MaxWallDistance);
		//DebugOverlay.Line(HitLine, CanWallKick ? Color.Green : Color.Red, 100);

		return CanWallKick;
	}
}
