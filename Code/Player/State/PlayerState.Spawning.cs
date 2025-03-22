using KOTH.UI;
using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Events;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace KOTH;

public struct FPlayerPawnDefinition
{
	public FPlayerPawnDefinition()
	{
	}

	public CharacterDefinition CharacterDefinition { get; init; }
	// public PlayerState OwnerPlayerState { get; init; }

	public string Name { get; init; } = "UNINITALIZED";
	public Team Team { get; init; } = Team.Unassigned;
	public bool IsDummy { get; init; } = false;

	public bool IsValid()
	{
		return CharacterDefinition.IsValid()/* && (OwnerPlayerState.IsValid() || IsBot)*/;
	}
}

//////////////////////////////////////////////////////////////////////////////////

/*
 * State of the player pawn
 * menu when displaying a menu (true when first joining a lobby)
 * waitingforspawn when in a game & requestedcharacterdefinition is not null
 * alive when playing in game
 */
public enum EPlayerStateSpawningState
{
	MainMenu,
	WaitingForSpawn,
	InstantSpawn,
	Alive,
	Spectating,
}

//////////////////////////////////////////////////////////////////////////////////

public partial class PlayerState
{
	[Sync(SyncFlags.FromHost)/*, Change(nameof(OnPlayerStateSpawningStateChanged))*/] public EPlayerStateSpawningState PlayerStateSpawningState { get; private set; } = EPlayerStateSpawningState.MainMenu;

	private void OnPlayerStateSpawningStateChanged()
	{

	}

	//////////////////////////////////////////////////////////////////////////////////

	[Sync(SyncFlags.FromHost)] public float TimeTilAttemptedSpawn { get; private set; } = -1;

	public void SetTimeTilAttemptedSpawn(float TimeTilSpawn)
	{
		Assert.True(Networking.IsHost);

		TimeTilAttemptedSpawn = TimeTilSpawn;
	}

	//////////////////////////////////////////////////////////////////////////////////

	/*
	 * NOTE : Sync will only care about the vaule given by an object's host, so bc this player state is owned 
	 * by the server the requested character def will never sync! this is the workaround for now..
	 */
	public CharacterDefinition RequestedCharacterDefinition { get; private set; } = null;

	private void OnRequestedCharacterDefinitionChanged(CharacterDefinition OldDefinition, CharacterDefinition NewDefinition)
	{
		if (NewDefinition != null)
		{
			PlayerStateSpawningState = EPlayerStateSpawningState.WaitingForSpawn;
		}

		Log.Info($"old : {OldDefinition} | new : {NewDefinition}");
		Log.Info($"Requested Character change 2 {RequestedCharacterDefinition}");
	}

	public void RequestTeamSwap()
	{
		Team = Team.GetOpponents();

		HostSwapTeams(Team);
	}

	[Rpc.Host]
	private void HostSwapTeams(Team Team)
	{
		var SpawnPoint = GameUtils.GetRandomTeamSpawn(Team);
		SpawnPlayerPawn(SpawnPoint);
	}

	public void RequestCharacterDefinition(CharacterDefinition CharacterDefintionIn)
	{
		Log.Info($"Requested Character {CharacterDefintionIn}");

		RequestedCharacterDefinition = CharacterDefintionIn;

		// HACK : we should ensure the owner is requesting this
		// (same with damage, TODO : )
		HostSetCharacterDefinition(CharacterDefintionIn);
	}

	[Rpc.Host]
	private void HostSetCharacterDefinition(CharacterDefinition CharacterDefinition)
	{
		if (!CharacterDefinition.IsValid())
		{
			return;
		}

		RequestedCharacterDefinition = CharacterDefinition;

		// if we come from the main menu we want to spawn instantly
		if (PlayerStateSpawningState == EPlayerStateSpawningState.MainMenu)
		{
			PlayerStateSpawningState = EPlayerStateSpawningState.InstantSpawn;
			return;
		}

		PlayerStateSpawningState = EPlayerStateSpawningState.WaitingForSpawn;
	}

	//////////////////////////////////////////////////////////////////////////////////

	public async void SpawnPlayerPawn(TeamSpawnPoint SpawnPoint)
	{
		Assert.True(Networking.IsHost);

		if (PlayerPawn.IsValid())
		{
			PlayerPawn.GameObject.Root.Destroy();
			PlayerPawn = null;
			OnLocalDeath();

			// HACK : my fault- however we need to wait for the client to run its
			// local death stuff before spawning it- this is only for the very
			// specfic case when we swap teams...
			await Task.Delay(2000); 
		}

		if (!SpawnPoint.IsValid())
		{
			Log.Warning($"trying to spawn player {this} with invalid spawn point");
			return;
		}

		SpawnPlayerPawn(Connection, SteamName, RequestedCharacterDefinition, SpawnPoint);
	}


	[Property] public static GameObject DefaultPlayerPawnPrefab { get; private set; }

	private void OnPlayerPawnSpawn()
	{
		Assert.True(Networking.IsHost);
		Assert.True(PlayerPawn.IsValid());

		PlayerPawn.OnDeath += OnPlayerPawnDeath;
		PlayerStateSpawningState = EPlayerStateSpawningState.Alive;

		using (Rpc.FilterInclude(Connection))
		{
			CameraDisableHack();
			BroadcastPlayerSpawn(PlayerPawn);
		}
	}

	[Rpc.Broadcast] // broadcast filter
	private void BroadcastPlayerSpawn(PlayerPawn PlayerPawn)
	{
		Scene.Dispatch(new LocalPlayerSpawnedEvent(PlayerPawn));
	}

	[Rpc.Broadcast] // broadcast filter
	private void CameraDisableHack()
	{
		OverviewCameraObject.Enabled = false;
	}

	[Rpc.Broadcast] // broadcast filter
	private void CameraEnableHack()
	{
		OverviewCameraObject.Enabled = true;
	}

	[Rpc.Host]
	private void SpawnPlayerPawn(Connection OwningConnection, string Name, CharacterDefinition CharacterDefinition, TeamSpawnPoint SpawnPoint)
	{
		Assert.True(Networking.IsHost);
		Assert.IsValid(SpawnPoint);

		Log.Info($"attempting to spawn player {RequestedCharacterDefinition} for {OwningConnection}");

		var SpawnPlayerPawnPrefab = DefaultPlayerPawnPrefab.Clone(SpawnPoint.GameObject.WorldTransform, null, true);
		SpawnPlayerPawnPrefab.Network.SetOrphanedMode(NetworkOrphaned.Destroy);

		var SpawnPlayerPawnComponent = SpawnPlayerPawnPrefab.Components.Get<PlayerPawn>();
		Assert.NotNull(SpawnPlayerPawnComponent);

		var TeamIn = Team; // :P
		FPlayerPawnDefinition PlayerPawnDefinition = new()
		{
			CharacterDefinition = RequestedCharacterDefinition,
			Name = Name,
			Team = TeamIn,
		};

		SpawnPlayerPawnComponent.SetPlayerPawnDefinition(PlayerPawnDefinition);

		if (!SpawnPlayerPawnPrefab.NetworkSpawn(OwningConnection))
		{
			SpawnPlayerPawnPrefab.Destroy();
			return;
		}

		PlayerPawn = SpawnPlayerPawnComponent;
		OnPlayerPawnSpawn();
	}

	void OnPlayerPawnDeath()
	{
		Assert.True(Networking.IsHost);

		PlayerStateSpawningState = EPlayerStateSpawningState.WaitingForSpawn;

		using (Rpc.FilterInclude(Connection))
		{
			// CameraEnableHack();
			OnLocalDeath();
		}
	}

	[Rpc.Broadcast]
	public void OnLocalDeath()
	{
		Scene.Dispatch(new LocalPlayerDiedEvent());
	}
}
