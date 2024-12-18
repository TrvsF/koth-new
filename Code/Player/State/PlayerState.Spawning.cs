using KOTH.UI;
using KOTH.Utils;
using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Events;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace KOTH;

public struct PlayerPawnDefinition
{
	public PlayerPawnDefinition()
	{
	}

	public CharacterDefinition CharacterDefinition { get; init; }
	// public PlayerState OwnerPlayerState { get; init; }

	public string Name { get; init; } = "UNINITALIZED";
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
	Menu,
	WaitingForSpawn,
	Alive,
	Spectating,
}

//////////////////////////////////////////////////////////////////////////////////

public partial class PlayerState
{
	[Sync(SyncFlags.FromHost)/*, Change(nameof(OnPlayerStateSpawningStateChanged))*/] public EPlayerStateSpawningState PlayerStateSpawningState { get; private set; } = EPlayerStateSpawningState.Menu;

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

	[Sync] public CharacterDefinition RequestedCharacterDefinition { get; private set; }

	//////////////////////////////////////////////////////////////////////////////////

	public void RequestCharacterDefinition(CharacterDefinition CharacterDefintionIn)
	{
		Log.Info("Requested Character");

		RequestedCharacterDefinition = CharacterDefintionIn;
		HACKPlayerSpawnState();
	}

	[Rpc.Host]
	private void HACKPlayerSpawnState()
	{
		PlayerStateSpawningState = EPlayerStateSpawningState.WaitingForSpawn;
	}

	public void RequestSpawn(SpawnPointInfo SpawnPoint)
	{
		Log.Info("Requested Spawn");

		if (PlayerStateSpawningState != EPlayerStateSpawningState.WaitingForSpawn)
		{
			return;
		}

		if (PlayerPawn.IsValid())
		{
			PlayerPawn.GameObject.Root.Destroy();
			PlayerPawn = null;
		}

		SpawnPlayerPawn(Connection, SteamName, RequestedCharacterDefinition, SpawnPoint);
	}


	[Property] public static GameObject DefaultPlayerPawnPrefab { get; private set; }

	private void OnPlayerPawnSpawn()
	{
		Assert.True(Networking.IsHost);
		Assert.True(PlayerPawn.IsValid());

		// PlayerPawn = PlayerPawnOut;
		PlayerPawn.OnDeath += OnPlayerPawnDeath;
		PlayerStateSpawningState = EPlayerStateSpawningState.Alive;

		using (Rpc.FilterInclude(Connection))
		{
			CameraDisableHack();
		}
	}

	private ScreenPanel AssumedSceneCameraObject = null;
	[Rpc.Broadcast]
	private void CameraDisableHack()
	{
		// HACK : understand the camera system more, surely there's a better way!
		if (AssumedSceneCameraObject == null)
		{
			AssumedSceneCameraObject = Scene.Camera.GameObject.Components.Get<ScreenPanel>();
		}
		AssumedSceneCameraObject.Enabled = false;
		//
	}

	[Rpc.Broadcast]
	private void CameraEnableHack()
	{
		if (AssumedSceneCameraObject == null)
		{
			AssumedSceneCameraObject = Scene.Camera.GameObject.Components.Get<ScreenPanel>();
		}
		AssumedSceneCameraObject.Enabled = true;
		Log.Info(AssumedSceneCameraObject.GameObject);
	}

	[Rpc.Host]
	private void SpawnPlayerPawn(Connection OwningConnection, string Name, CharacterDefinition CharacterDefinition, SpawnPointInfo SpawnPoint)
	{
		Assert.True(Networking.IsHost);

		var SpawnPlayerPawnPrefab = DefaultPlayerPawnPrefab.Clone(SpawnPoint.Transform, null, true);
		SpawnPlayerPawnPrefab.Network.SetOrphanedMode(NetworkOrphaned.Destroy);

		var SpawnPlayerPawnComponent = SpawnPlayerPawnPrefab.Components.Get<PlayerPawn>();
		Assert.NotNull(SpawnPlayerPawnComponent);

		PlayerPawnDefinition PlayerPawnDefinition = new()
		{
			CharacterDefinition = CharacterDefinition,
			Name = Name,
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
			CameraEnableHack();
		}
	}
}
