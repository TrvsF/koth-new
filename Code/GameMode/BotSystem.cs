using Sandbox;
using Sandbox.Events;
using System.Collections.Generic;

namespace KOTH;

public sealed class BotSystem : Component,
	IGameEventHandler<KillEvent>
{
	[Property] public GameObject DummyPrefab { get; private set; } = null;

	protected override void OnStart()
	{
		if (!Networking.IsHost)
		{
			return;
		}

		base.OnStart();

		foreach (var SpawnPoint in Game.ActiveScene
		.GetAllComponents<TeamSpawnPoint>()
		.Where(Spawn => Spawn.IsDummy))
		{
			// SpawnDummy(SpawnPoint.DummyType, SpawnPoint.Transform.World);
		}
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
