using KOTH.UI;
using KOTH.Utils;
using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Events;
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
	public bool IsBot { get; init; } = false;

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

	[Property] public GameObject DefaultPlayerPawnPrefab { get; private set; }
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

		SpawnPlayerPawn(SpawnPoint);
	}

	private ScreenPanel AssumedSceneCameraObject = null;

	[Rpc.Host]
	private void SpawnPlayerPawn(SpawnPointInfo SpawnPoint)
	{
		Assert.True(Networking.IsHost);

		if (PlayerPawn.IsValid())
		{
			PlayerPawn.GameObject.Root.Destroy();
			PlayerPawn = null;
		}

		var SpawnPlayerPawnPrefab = DefaultPlayerPawnPrefab.Clone(SpawnPoint.Transform, null, true);
		SpawnPlayerPawnPrefab.Network.SetOrphanedMode(NetworkOrphaned.Destroy);

		var SpawnPlayerPawnComponent = SpawnPlayerPawnPrefab.Components.Get<PlayerPawn>();
		Assert.NotNull(SpawnPlayerPawnComponent);

		PlayerPawnDefinition PlayerPawnDefinition = new()
		{
			CharacterDefinition = RequestedCharacterDefinition,
			Name = SteamName,
		};

		SpawnPlayerPawnComponent.SetPlayerPawnDefinition(PlayerPawnDefinition);
		if (SpawnPlayerPawnPrefab.NetworkSpawn(Connection))
		{
			// HACK : understand the camera system more, surely there's a better way!
			if (AssumedSceneCameraObject == null)
			{
				AssumedSceneCameraObject = Scene.Camera.GameObject.Components.Get< ScreenPanel>();
			}
			AssumedSceneCameraObject.Enabled = false;
			//

			PlayerPawn = SpawnPlayerPawnComponent;
			PlayerPawn.OnDeath += OnPlayerPawnDeath;
			PlayerStateSpawningState = EPlayerStateSpawningState.Alive;
		}
		else
		{
			SpawnPlayerPawnPrefab.Destroy();
			Log.Warning($"failed to spawn player pawn for client {SteamName}:{SteamId}");
		}
	}

	void OnPlayerPawnDeath()
	{
		Assert.True(Networking.IsHost);

		PlayerStateSpawningState = EPlayerStateSpawningState.WaitingForSpawn;
		AssumedSceneCameraObject.Enabled = true;
	}
}
