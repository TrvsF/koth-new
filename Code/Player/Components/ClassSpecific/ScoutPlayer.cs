using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Events;
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

	const float MaxWallDistance = 10f;
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
		Log.Info(WishInput);
		OwnerPawn.Jump();
		HasWallKicked = true;
	}

}
