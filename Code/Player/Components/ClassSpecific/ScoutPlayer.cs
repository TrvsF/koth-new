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

		if (!OwnerPawn.IsValid() || !OwnerPawn.IsLocallyControlled)
		{
			return;
		}

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

		if (WishMove.Length > 1f)
		{
			WishMove = WishMove.Normal * 1.5f;
		}
		else
		{
			WishMove = WishMove.Normal * 1.1f;
		}

		var YawRotation = PlayerPawn.Camera.WorldRotation.Yaw();
		var WorldWishMove = WishMove.RotateAround(Vector3.Zero, Rotation.FromYaw(YawRotation));
		var CheckVector = WorldWishMove * -1f;

		PunchVector = WorldWishMove.WithZ(1f) * WallkickPower;

		Ray HitRay = new(PlayerPawn.WorldPosition, CheckVector);
		var Hits = Scene.Trace.Ray(HitRay, MaxWallDistance)
			.IgnoreGameObjectHierarchy(PlayerPawn.GameObject)
			.RunAll();

		bool CanWallKick = false;
		foreach (var _ in Hits) // asuming it geometry
		{
			CanWallKick = true;
		}

		// debug
		// Line HitLine = new(PlayerPawn.WorldPosition, CheckVector, MaxWallDistance);
		// DebugOverlaySystem.Current.Line(HitLine, CanWallKick ? Color.Green : Color.Red, 100);

		return CanWallKick;
	}
}
