using KOTH.UI;
using KOTH.Utils;
using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Events;

namespace KOTH;

public record PlayerPawnDefinition
{
	public CharacterDefinition CharacterDefinition { get; set; }
	public PlayerState OwnerPlayerState { get; set; }
	public bool IsBot { get; set; }

	public bool IsValid()
	{
		return CharacterDefinition.IsValid() && (OwnerPlayerState.IsValid() || IsBot);
	}
}

public enum EPlayerStateSpawningState
{
	None,
	InWorld,
	WaitingForSpawn,
	Spectating,
}

public partial class PlayerState
{
	[HostSync] public EPlayerStateSpawningState PlayerStateSpawningState { get; private set; } = EPlayerStateSpawningState.WaitingForSpawn;

	//////////////////////////////////////////////////////////////////////////////////

	[Property] public GameObject DefaultPlayerPawnPrefab { get; private set; }
	[Sync] public CharacterDefinition RequestedCharacterDefinition { get; private set; }

	//////////////////////////////////////////////////////////////////////////////////

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if (PlayerPawn.IsValid())
		{
			if (!PlayerPawn.IsAlive)
			{
				PlayerStateSpawningState = EPlayerStateSpawningState.WaitingForSpawn;
			}
		}
	}

	public void RequestCharacterDefinition()
	{
		Log.Info("Requested Character");


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

		RequestedCharacterDefinition = WorldUtil.GetRandomCharacter();
		PlayerPawnDefinition PlayerPawnDef = new()
		{ 
			CharacterDefinition = RequestedCharacterDefinition,
			OwnerPlayerState = this,
		};

		if (SpawnPlayerPawnPrefab.NetworkSpawn(Connection))
		{
			SpawnPlayerPawnComponent.SetPlayerPawnDefinition(PlayerPawnDef);

			PlayerPawn = SpawnPlayerPawnComponent;
			PlayerStateSpawningState = EPlayerStateSpawningState.InWorld;
		}
		else
		{
			SpawnPlayerPawnPrefab.Destroy();
			Log.Warning($"failed to spawn player pawn for client {SteamName}:{SteamId}");
		}
	}

	void OnPlayerStateStateChanged()
	{

	}
}
