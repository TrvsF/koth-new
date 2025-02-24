using KOTH.UI;
using KOTH.Utils;
using Sandbox;
using Sandbox.Citizen;
using Sandbox.Diagnostics;
using Sandbox.Events;
using Sandbox.Services;
using System.Reflection.Metadata.Ecma335;

namespace KOTH;

public sealed partial class PlayerPawn : Component, IDescription, Component.ICollisionListener
{
	[Property, Sync(SyncFlags.FromHost)] public FPlayerPawnDefinition PlayerPawnDefinition { get; private set; }
	public string DisplayName { get; private set; } = "UNINITALIZED";

	public void SetPlayerPawnDefinition(FPlayerPawnDefinition PlayerPawnDefinitionIn)
	{
		Assert.True(Networking.IsHost);

		PlayerPawnDefinition = PlayerPawnDefinitionIn;
		IsDummy = PlayerPawnDefinitionIn.IsDummy;
		Team = PlayerPawnDefinitionIn.Team;
	}

	//////////////////////////////////////////////////////////////////////////////////

	[Property] public PlayerBody Body { get; set; }
	[Property] public GameObject Head { get; set; }
	[Property] public GameObject GibPrefab { get; set; } // TODO : this should be in character def
	[Property] public CitizenAnimationHelper AnimationHelper { get; set; }
	[Property] public BoxCollider PlayerBoxCollider { get; set; }
	[Property] public Team DisplayTeam { get => Team; }

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
	public Team Team { get; set; } = Team.Unassigned;
	public Action OnPlayerStart;

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

		CharacterDefinition CharacterDefinition = PlayerPawnDefinition.CharacterDefinition;
		Assert.True(SetMovementVariables(CharacterDefinition));
		
		foreach (Type ComponentType in CharacterDefinition.SpecificComponents)
		{
			if (ComponentType.IsSubclassOf(typeof(Component)))
			{
				var Type = TypeLibrary.GetType(ComponentType);
				GameObject.Components.Create(Type);
			}
		}

		DisplayName = PlayerPawnDefinition.Name;
		GameObject.Name = DisplayName;

		Body.Renderer.Tint = Team.GetColor(false);
		Body.Dresser.ApplyClothing(); // this thing is fkn weird
		Body.Dresser.SetupClothes();

		Tags.Add($"{Team}");

		if (IsLocallyControlled)
		{
			Assert.True(CreatePlayerCamera());

			Body.Renderer.Enabled = false;
			
			Tags.Add("self");

			Inventory.Give(CharacterDefinition.SecondaryWeapon, false);
			Inventory.Give(CharacterDefinition.PrimaryWeapon, true);
		}
		else
		{
			// HACK : all these are HACKS!
			Body.Renderer.Enabled = true;
			Tags.Remove("self");
		}

		// NOTE : these tags are very good for controlling animations (if those can be sync'd)
		TagBinder.BindTag("equipping", () => TimeSinceWeaponDeployed < 0.3f);
		TagBinder.BindTag("no_aiming", () => TimeSinceGroundedChanged < 0.25f);

		// TODO : load in data in a nicer way?
		if (Networking.IsHost)
		{
			DamageComponent.SetHealth(CharacterDefinition.MaxHealth);
		}

		OnPlayerStart?.Invoke();
	}

	protected override void OnUpdate()
	{
		if (IsLocallyControlled)
		{
			CameraTick();
		}

		UpdateCrouch();
		TickVFXs();
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
			Log.Warning("shitbox strikes again!");
			return;
		}

		var wasGrounded = IsGrounded;
		IsGrounded = CharacterController.IsOnGround;

		if (IsGrounded != wasGrounded)
		{
			GroundedChanged(wasGrounded, IsGrounded);
		}

		UpdateZones();

		if (IsDummy && IsAlive)
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

		// TODO : move me!
		CurrentEquipment?.ViewModel?.ModelRenderer?.Set("b_sprint", WishMove == Vector3.Forward);
		CurrentEquipment?.ViewModel?.ModelRenderer?.Set("move_bob", IsGrounded ? 0f : 1f);

		ApplyAcceleration();
		ApplyMovement();
		DebugUpdate();
	}

	// HACK : we should be holding these Objects on the dresser
	private List<GameObject> GetLocalClothes()
	{
		List<GameObject> Clothes = new();
		//foreach (var Child in Body.GameObject.Children)
		//{
		//	if (Child.)
		//}
		return Clothes;
	}

	//////////////////////////////////////////////////////////////////////////////////

	private void DoDummyMovement()
	{
		// if (DummyType.HasFlag(DummyType.Jumper))
		//{
		IsCrouching = true;
		//if (CharacterController.IsOnGround)
		//{
		//	CharacterController.Punch(Vector3.Up * JumpPower);
		//	BroadcastPlayerJumped();
		//}
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
