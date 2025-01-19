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
	[Sync(SyncFlags.FromHost)] public PlayerPawnDefinition PlayerPawnDefinition { get; private set; }
	public string DisplayName { get; private set; } = "UNINITALIZED";

	public void SetPlayerPawnDefinition(PlayerPawnDefinition PlayerPawnDefinitionIn)
	{
		Assert.True(Networking.IsHost);

		PlayerPawnDefinition = PlayerPawnDefinitionIn;
		IsDummy = PlayerPawnDefinitionIn.IsDummy;
	}

	//////////////////////////////////////////////////////////////////////////////////

	[Property] public PlayerBody Body { get; set; }
	[Property] public GameObject Head { get; set; }
	[Property] public GameObject GibPrefab { get; set; } // TODO : this should be in character def
	[Property] public AnimationHelper AnimationHelper { get; set; }
	[Property] public BoxCollider PlayerBoxCollider { get; set; }

	//////////////////////////////////////////////////////////////////////////////////

	[RequireComponent] public DamageComponent DamageComponent { get; private set; }
	[RequireComponent] public TagBinder TagBinder { get; private set; }
	[RequireComponent] public CharacterController CharacterController { get; private set; }
	[RequireComponent] public HighlightOutline Outline { get; private set; }
	[RequireComponent] public Spotter Spotter { get; private set; }
	[RequireComponent] public Spottable Spottable { get; private set; }
	[RequireComponent] public PlayerInventory Inventory { get; private set; }

	//////////////////////////////////////////////////////////////////////////////////

	[Property] public bool IsDummy { get; private set; } = false;
	[Property] public bool IsLocallyControlled => !IsProxy && !IsDummy;
	[Property] public bool IsViewer => PlayerState.Local?.PlayerPawn == this; // TODO : make spectate target in playerpawn?

	//////////////////////////////////////////////////////////////////////////////////

	[Sync(SyncFlags.FromHost)] public TimeSince TimeSinceLastRespawn { get; private set; }

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
		foreach (Type ComponentT in CharacterDefinition.SpecificComponents)
		{
			if (ComponentT.IsSubclassOf(typeof(Component)))
			{
				GameObject.AddComponent<EngiePlayer>();
			}
		}

		DisplayName = PlayerPawnDefinition.Name;
		GameObject.Name = DisplayName;

		if (IsLocallyControlled)
		{
			Assert.True(CreatePlayerCamera());
			Body.Renderer.Enabled = false;
			Tags.Add("self");
		}
		else
		{
			// HACK : turns back on rendering if the host disabled it globally for themself
			Body.Renderer.Enabled = true;

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
			CameraTick();
		}
		else
		{
			_smoothEyeAngles = Angles.Lerp(_smoothEyeAngles, _rawEyeAngles, Time.Delta / Scene.NetworkRate);
		}

		// TODO : move me?
		if (IsAlive /*HACK for proxy characters when body dies b4 health knows*/&& Body.IsValid())
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

		if (IsDummy)
		{
			DoDummyMovement();
			return; // NOTE : early return
		}

		if (!IsAlive || !IsLocallyControlled)
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
		// if (DummyType.HasFlag(DummyType.Jumper))
		{
			IsCrouching = true;
			if (CharacterController.IsOnGround)
			{
				CharacterController.Punch(Vector3.Up * JumpPower);
				BroadcastPlayerJumped();
			}
		}

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
