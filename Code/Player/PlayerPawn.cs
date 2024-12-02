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
	[HostSync] public CharacterDefinition CharacterDefinition { get; private set; }
	public string DisplayName { get; private set; } = "UNINITALIZED";

	public void SetPlayerPawnDefinition(PlayerPawnDefinition PlayerPawnDefinitionIn)
	{
		Assert.True(Networking.IsHost);

		Log.Info($"setting def to {PlayerPawnDefinitionIn}");

		PlayerPawnDefinition = PlayerPawnDefinitionIn;
		CharacterDefinition = PlayerPawnDefinition.CharacterDefinition;

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
	
	protected override void OnStart()
	{
		Assert.NotNull(Head);
		Assert.NotNull(GibPrefab);
		Assert.NotNull(PlayerPawnDefinition);

		ClientInit(); // TODO : headless if server
		Assert.NotNull(CharacterDefinition);

		// TODO : load in data in a nicer way?
		if (Networking.IsHost)
		{
			Scene.Dispatch(new EquipmentRequentEvent(CharacterDefinition.SecondaryWeapon, this, false));
			Scene.Dispatch(new EquipmentRequentEvent(CharacterDefinition.PrimaryWeapon, this, true));
			DamageComponent.SetHealth(CharacterDefinition.MaxHealth);
		}
	}

	protected override void OnUpdate()
	{
		if (IsLocallyControlled)
		{
			if (IsAlive)
			{
				EyeAngles += Input.AnalogLook;
				EyeAngles = EyeAngles.WithPitch(EyeAngles.pitch.Clamp(-90, 90));
			}

			Camera.LocalPosition = Vector3.Zero;
			Camera.LocalRotation = Rotation.Identity;

			Boom.WorldRotation = EyeAngles.ToRotation();
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
		Assert.True(CharacterController.IsValid());
		Assert.True(DamageComponent.IsValid());

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

	private bool ClientInit()
	{
		if (!PlayerPawnDefinition.IsValid() || !CharacterDefinition.IsValid())
		{
			return false;
		}

		// IsLocallyControlled = PlayerPawnDefinition.OwnerPlayerState == PlayerState.Local;
		CharacterDefinition = PlayerPawnDefinition.CharacterDefinition;

		DisplayName = PlayerPawnDefinition.OwnerPlayerState.DisplayName;
		GameObject.Name = $"PlayerPawn:{DisplayName}";

		// NOTE : these tags are very good for controlling animations (if those can be sync'd)
		TagBinder.BindTag("equipping", () => TimeSinceWeaponDeployed < 0.3f);
		TagBinder.BindTag("no_aiming", () => TimeSinceGroundedChanged < 0.25f);

		if (CreatePlayerCamera(IsLocallyControlled))
		{
			if (IsLocallyControlled)
			{
				Body.Renderer.Enabled = false;
				Tags.Add("self");
			}
			else
			{
				// HACK : turns back on rendering if the host disabled it globally for themself
				Body.Renderer.Enabled = true;
			}
			return true;
		}

		return false;
	}

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
