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

	private void OnDummyDeath()
	{

	}

	protected override void OnStart()
	{
		base.OnStart();

		if (!Networking.IsHost)
		{
			return;
		}

		foreach (var SpawnPoint in GameUtils.GetDummySpawns())
		{
			SpawnPlayerPawn(Connection.Host, "Dummy", WorldUtil.GetRandomCharacter(), SpawnPoint);
		}
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
	}

	[Rpc.Host]
	private void SpawnPlayerPawn(Connection OwningConnection, string Name, CharacterDefinition CharacterDefinition, TeamSpawnPoint SpawnPoint)
	{
		Assert.True(Networking.IsHost);

		var SpawnPlayerPawnPrefab = PlayerState.DefaultPlayerPawnPrefab.Clone(SpawnPoint.GameObject.WorldTransform);
		SpawnPlayerPawnPrefab.Network.SetOrphanedMode(NetworkOrphaned.Destroy);

		var SpawnPlayerPawnComponent = SpawnPlayerPawnPrefab.Components.Get<PlayerPawn>();
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
		SpawnPlayerPawnComponent.OnDeath += OnDummyDeath;

		if (!SpawnPlayerPawnPrefab.NetworkSpawn(OwningConnection))
		{
			SpawnPlayerPawnPrefab.Destroy();
			return;
		}

		DummyPlayerPawns.Add(SpawnPlayerPawnComponent);
	}

	void IGameEventHandler<KillEvent>.OnGameEvent(KillEvent EventArgs)
	{
	}
}
