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

	/*
	 * NOTE : Sync will only care about the vaule given by an object's host, so bc this player state is owned 
	 * by the server the requested character def will never sync! this is the workaround for now..
	 */

	/*[Sync]*/
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

	public void RequestCharacterDefinition(CharacterDefinition CharacterDefintionIn)
	{
		Log.Info($"Requested Character {CharacterDefintionIn}");

		RequestedCharacterDefinition = CharacterDefintionIn;

		HACKPlayerSpawnState(CharacterDefintionIn);
	}

	[Rpc.Host]
	private void HACKPlayerSpawnState(CharacterDefinition CIN)
	{
		RequestedCharacterDefinition = CIN;
		PlayerStateSpawningState = EPlayerStateSpawningState.WaitingForSpawn;
	}

	//////////////////////////////////////////////////////////////////////////////////

	public void RequestSpawn(SpawnPointInfo SpawnPoint)
	{
		Assert.True(Networking.IsHost);

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

		PlayerPawn.OnDeath += OnPlayerPawnDeath;
		PlayerStateSpawningState = EPlayerStateSpawningState.Alive;

		using (Rpc.FilterInclude(Connection))
		{
			CameraDisableHack();
		}
	}

	[Rpc.Broadcast]
	private void CameraDisableHack()
	{
		AssumedSceneCameraObject.Enabled = false;
	}

	[Rpc.Broadcast]
	private void CameraEnableHack()
	{
		AssumedSceneCameraObject.Enabled = true;
	}

	[Rpc.Host]
	private void SpawnPlayerPawn(Connection OwningConnection, string Name, CharacterDefinition CharacterDefinition, SpawnPointInfo SpawnPoint)
	{
		Assert.True(Networking.IsHost);

		Log.Info($"attempting to spawn player {RequestedCharacterDefinition} via {OwningConnection}");

		var SpawnPlayerPawnPrefab = DefaultPlayerPawnPrefab.Clone(SpawnPoint.Transform, null, true);
		SpawnPlayerPawnPrefab.Network.SetOrphanedMode(NetworkOrphaned.Destroy);

		var SpawnPlayerPawnComponent = SpawnPlayerPawnPrefab.Components.Get<PlayerPawn>();
		Assert.NotNull(SpawnPlayerPawnComponent);

		PlayerPawnDefinition PlayerPawnDefinition = new()
		{
			CharacterDefinition = RequestedCharacterDefinition,
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
