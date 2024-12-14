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

public enum EPlayerStateSpawningState
{
	None,
	Dead,
	Alive,
	WaitingForSpawn,
	Spectating,
}

//////////////////////////////////////////////////////////////////////////////////

public partial class PlayerState
{
	[HostSync] public EPlayerStateSpawningState PlayerStateSpawningState { get; private set; } = EPlayerStateSpawningState.WaitingForSpawn;

	//////////////////////////////////////////////////////////////////////////////////

	[Property] public GameObject DefaultPlayerPawnPrefab { get; private set; }
	[Sync] public CharacterDefinition RequestedCharacterDefinition { get; private set; }

	//////////////////////////////////////////////////////////////////////////////////

	public void RequestCharacterDefinition(CharacterDefinition CharacterDefintionIn)
	{
		Log.Info("Requested Character");

		RequestedCharacterDefinition = CharacterDefintionIn;
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
	}

	void OnPlayerStateStateChanged()
	{

	}
}
