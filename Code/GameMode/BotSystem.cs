using KOTH.Utils;
using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Events;
using System.Collections.Generic;

namespace KOTH;

public sealed class BotSystem : Component,
	IGameEventHandler<KillEvent>
{
	[Property] public GameObject DummyPrefab { get; private set; } = null;
	public static List<PlayerPawn> DummyPlayerPawns { get; private set; } = new();

	private static void AddDummy(PlayerPawn Dummy)
	{
		if (!Dummy.IsDummy)
		{
			return;
		}

		DummyPlayerPawns.Add(Dummy);
	}

	protected override void OnStart()
	{
		base.OnStart();

		foreach (var SpawnPoint in GameUtils.GetDummySpawnPoints())
		{
			SpawnPlayerPawn(Connection.Host, "Dummy", WorldUtil.GetRandomCharacter(), SpawnPoint);
		}
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		
	}

	[Rpc.Host]
	private void SpawnPlayerPawn(Connection OwningConnection, string Name, CharacterDefinition CharacterDefinition, SpawnPointInfo SpawnPoint)
	{
		Assert.True(Networking.IsHost);

		var SpawnPlayerPawnPrefab = PlayerState.DefaultPlayerPawnPrefab.Clone(SpawnPoint.Transform, null, true);
		SpawnPlayerPawnPrefab.Network.SetOrphanedMode(NetworkOrphaned.Destroy);

		var SpawnPlayerPawnComponent = SpawnPlayerPawnPrefab.Components.Get<PlayerPawn>();
		Assert.NotNull(SpawnPlayerPawnComponent);

		PlayerPawnDefinition PlayerPawnDefinition = new()
		{
			CharacterDefinition = CharacterDefinition,
			Name = Name,
			IsDummy = true,
		};

		SpawnPlayerPawnComponent.SetPlayerPawnDefinition(PlayerPawnDefinition);

		if (!SpawnPlayerPawnPrefab.NetworkSpawn(OwningConnection))
		{
			SpawnPlayerPawnPrefab.Destroy();
			return;
		}

		DummyPlayerPawns.Add(SpawnPlayerPawnComponent);
	}

	void IGameEventHandler<KillEvent>.OnGameEvent(KillEvent EventArgs)
	{
		var DeadPawn = EventArgs.DamageEvent.VictimPlayerPawn;
		if (!DeadPawn.IsValid())
		{
			Log.Warning("trying to handle the respawn of an invalid pawn");
			return;
		}

		// if (!DeadPawn.IsDummy)
		{
			return;
		}

		if (!DummyPrefab.IsValid())
		{
			return;
		}

		// var Spawns = GameUtils.GetDummySpawnPoints(DeadPawn.DummyType).Shuffle();
		// if (Spawns.Any())
		{
			// SpawnDummy(DeadPawn.DummyType, Spawns[0].Transform);
		}

		DeadPawn.DestroyGameObject();
	}
}
