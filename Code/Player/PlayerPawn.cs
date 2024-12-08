using KOTH.UI;
using KOTH.Utils;
using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Events;
using Sandbox.Services;
using System.Reflection.Metadata.Ecma335;

namespace KOTH;

public sealed partial class PlayerPawn : Component, IDescription, Component.ICollisionListener
{
	[HostSync] public PlayerPawnDefinition PlayerPawnDefinition { get; private set; }
	public string DisplayName { get; private set; } = "UNINITALIZED";

	public void SetPlayerPawnDefinition(PlayerPawnDefinition CharacterDefinitionIn)
	{
		Assert.True(Networking.IsHost);

		PlayerPawnDefinition = CharacterDefinitionIn;
	}

	//////////////////////////////////////////////////////////////////////////////////

	[Property] public PlayerBody Body { get; set; }
	[Property] public GameObject Head { get; set; }
	[Property] public GameObject GibPrefab { get; set; } // this should be in character def
	[Property] public AnimationHelper AnimationHelper { get; set; }
	[Property] public BoxCollider PlayerBoxCollider { get; set; }

	//////////////////////////////////////////////////////////////////////////////////

	[RequireComponent] public TagBinder TagBinder { get; private set; }
	[RequireComponent] public CharacterController CharacterController { get; private set; }
	[RequireComponent] public HighlightOutline Outline { get; private set; }
	[RequireComponent] public Spotter Spotter { get; private set; }
	[RequireComponent] public Spottable Spottable { get; private set; }

	//////////////////////////////////////////////////////////////////////////////////

	[Property] public bool IsBot { get; private set; } = false;
	[Property] public bool IsLocallyControlled => !IsProxy;
	[Property] public bool IsViewer => PlayerState.Local?.PlayerPawn == this; // TODO : make spectate target in playerpawn?

	//////////////////////////////////////////////////////////////////////////////////

	[RequireComponent] public PlayerInventory Inventory { get; private set; }
	[HostSync] public TimeSince TimeSinceLastRespawn { get; private set; }

	public Team Team;

	public void Teleport(Transform transform)
	{
		Teleport(transform.Position, transform.Rotation);
	}

	[Rpc.Owner]
	public void Teleport(Vector3 position, Rotation rotation)
	{
		Transform.World = new(position, rotation);
		Transform.ClearInterpolation();
		EyeAngles = rotation.Angles();

		if (CharacterController.IsValid())
		{
			CharacterController.Velocity = Vector3.Zero;
			CharacterController.IsOnGround = true;
		}
	}

	//////////////////////////////////////////////////////////////////////////////////

	protected override void OnStart()
	{
		Assert.NotNull(Head);
		Assert.True(PlayerPawnDefinition.IsValid());
		// Assert.NotNull(GibPrefab);

		CharacterDefinition CharacterDefinition = PlayerPawnDefinition.CharacterDefinition;
		Assert.True(SetMovementVariables(CharacterDefinition));
		DisplayName = PlayerPawnDefinition.Name;

		if (IsLocallyControlled)
		{
			Assert.True(CreatePlayerCamera());
			Body.Renderer.Enabled = false;
			Tags.Add("self");
		}

		// NOTE : these tags are very good for controlling animations (if those can be sync'd)
		TagBinder.BindTag("equipping", () => TimeSinceWeaponDeployed < 0.3f);
		TagBinder.BindTag("no_aiming", () => TimeSinceGroundedChanged < 0.25f);

		// TODO : load in data in a nicer way?
		if (Networking.IsHost)
		{
			GiveWeaponToPawn(CharacterDefinition.SecondaryWeapon, false);
			GiveWeaponToPawn(CharacterDefinition.PrimaryWeapon, true);
			DamageComponent.SetHealth(CharacterDefinition.MaxHealth);
		}

		// HACK : turns back on rendering if the host disabled it globally for themself
		if (IsProxy)
		{
			Body.Renderer.Enabled = true;
		}
	}

	[Rpc.Host]
	private void GiveWeaponToPawn(EquipmentResource Weapon, bool ShouldActivate)
	{
		Inventory.Give(Weapon, ShouldActivate);
	}

	protected override void OnUpdate()
	{
		if (IsLocallyControlled)
		{
			if (IsAlive)
			{
				EyeAngles += Input.AnalogLook;
				EyeAngles = EyeAngles.WithPitch(EyeAngles.pitch.Clamp(-90, 90));

				Camera.LocalPosition = Vector3.Zero;
				Camera.LocalRotation = Rotation.Identity;

				Boom.WorldRotation = EyeAngles.ToRotation();
			}

			
		}
		else
		{
			_smoothEyeAngles = Angles.Lerp(_smoothEyeAngles, _rawEyeAngles, Time.Delta / Scene.NetworkRate);
		}

		// TODO : move me?
		if (IsAlive)
		{
			Assert.True(Body.IsValid());
			Assert.True(AnimationHelper.IsValid());

			Body.WorldRotation = Rotation.FromYaw(EyeAngles.yaw);

			AnimationHelper.WithVelocity(CharacterController.Velocity);
			AnimationHelper.WithWishVelocity(WishVelocity);
			AnimationHelper.IsGrounded = IsGrounded;
			AnimationHelper.WithLook(EyeAngles.Forward, 1, 1, 1.0f);
			AnimationHelper.MoveStyle = AnimationHelper.MoveStyles.Run;
			AnimationHelper.DuckLevel = (MathF.Abs(_smoothEyeHeight) / 32.0f);
			AnimationHelper.HoldType = CurrentHoldType;
			AnimationHelper.Handedness = CurrentEquipment.IsValid() ? CurrentEquipment.Handedness : AnimationHelper.Hand.Both;
			AnimationHelper.AimBodyWeight = 0.1f;

			CurrentHoldType = CurrentEquipment.IsValid() ? CurrentEquipment.GetHoldType() : AnimationHelper.HoldTypes.None;
		}

		UpdateCrouch();
	}

	public SceneTraceResult CachedEyeTrace { get; private set; }
	protected override void OnFixedUpdate()
	{
		// TODO : these have been downgraded til sbox has a proper component
		// system (or has a fixed update that waits til its components oneanbled is fired(?))
		//Assert.True(CharacterController.IsValid());
		//Assert.True(DamageComponent.IsValid());

		if (!CharacterController.IsValid() || !DamageComponent.IsValid())
		{
			return;
		}

		var wasGrounded = IsGrounded;
		IsGrounded = CharacterController.IsOnGround;

		if (IsGrounded != wasGrounded)
		{
			GroundedChanged(wasGrounded, IsGrounded);
		}

		UpdateZones();

		if (DamageComponent == null || !IsAlive || !IsLocallyControlled)
		{
			return; // NOTE : early return
		}

		if (IsViewer)
		{
			CachedEyeTrace = Scene.Trace.Ray(AimRay, 100000f)
				.IgnoreGameObjectHierarchy(GameObject)
				.WithoutTags("ragdoll", "movement")
				.UseHitboxes()
				.Run();
		}

		_previousVelocity = CharacterController.Velocity;

		BuildWishInput();
		BuildWishVelocity();
		BuildInput();

		ApplyAcceleration();
		ApplyMovement();
	}

	//////////////////////////////////////////////////////////////////////////////////

	private void DoDummyMovement()
	{
		//if (DummyType.HasFlag(DummyType.Jumper))
		//{
		//	IsCrouching = true;
		//	if (CharacterController.IsOnGround)
		//	{
		//		CharacterController.Punch(Vector3.Up * CharacterDefinition.JumpPower);
		//		BroadcastPlayerJumped();
		//	}
		//}

		//if (DummyType.HasFlag(DummyType.Walker))
		//{
		//	WishMove += Vector3.Forward;
		//	BuildWishVelocity();
		//	BuildInput();
		//}

		ApplyAcceleration();
		ApplyMovement();
	}
}
