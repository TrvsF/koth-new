using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Events;
using System;
using System.Collections.Generic;

namespace KOTH;

public sealed class BotSystem : SingletonComponent<BotSystem>,
	IGameEventHandler<KillBroadcastEvent>
{
	[Property] public GameObject DummyPrefab { get; private set; } = null;
	public Dictionary<TeamSpawnPoint, PlayerPawn> DummyPlayerPawns { get; private set; }

	protected override void OnStart()
	{
		base.OnStart();

		if (!Networking.IsHost)
		{
			return;
		}

		DummyPlayerPawns = new();

		foreach (var SpawnPoint in GameUtils.GetDummySpawns())
		{
			SpawnPlayerPawn(Connection.Host, "Dummy", SpawnPoint.RandomSpawn ? WorldUtil.GetRandomCharacter() : SpawnPoint.CharacterDefinition, SpawnPoint);
		}
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		foreach (var (SpawnPoint, Player) in DummyPlayerPawns)
		{
			if (!Player.IsValid() || !Player.IsAlive)
			{
				SpawnPlayerPawn(Connection.Host, "Dummy", SpawnPoint.RandomSpawn ? WorldUtil.GetRandomCharacter() : SpawnPoint.CharacterDefinition, SpawnPoint);
			}
		}
	}

	private void SpawnPlayerPawn(Connection OwningConnection, string Name, CharacterDefinition CharacterDefinition, TeamSpawnPoint SpawnPoint)
	{
		Assert.True(Networking.IsHost);
		Assert.IsValid(SpawnPoint);

		var PlayerPawnClone = DummyPrefab.Clone(SpawnPoint.GameObject.WorldTransform);
		PlayerPawnClone.Network.SetOrphanedMode(NetworkOrphaned.Destroy);

		var SpawnPlayerPawnComponent = PlayerPawnClone.Components.Get<PlayerPawn>();
		Assert.NotNull(SpawnPlayerPawnComponent);

		FPlayerPawnDefinition PlayerPawnDefinition = new()
		{
			CharacterDefinition = CharacterDefinition,
			Name = Name,
			IsDummy = true,
			Team = SpawnPoint.Team,
		};

		SpawnPlayerPawnComponent.SetPlayerPawnDefinition(PlayerPawnDefinition);
		SpawnPlayerPawnComponent.IsJumper = SpawnPoint.Jumper;
		SpawnPlayerPawnComponent.IsWalker = SpawnPoint.Walker;

		if (!PlayerPawnClone.NetworkSpawn(OwningConnection))
		{
			PlayerPawnClone.Destroy();
			return;
		}

		DummyPlayerPawns[SpawnPoint] = SpawnPlayerPawnComponent;
	}

	void IGameEventHandler<KillBroadcastEvent>.OnGameEvent(KillBroadcastEvent EventArgs)
	{
	}
}
