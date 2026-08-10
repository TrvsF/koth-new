using Sandbox;
using Sandbox.Diagnostics;

namespace KOTH;

public sealed class BuildingManager : Component
{
	[Property] public GameObject TurretPrefab { get; private set; }
	[Property] public GameObject EnterTeleporterPrefab { get; private set; }
	[Property] public GameObject ExitTeleporterPrefab { get; private set; }

	[Sync(SyncFlags.FromHost)] public NetDictionary<PlayerState, List<GameObject>> PlayerBuildings { get; private set; } = new();

	[Rpc.Host]
	public void ServerRequestTurret(PlayerState RequestingState)
	{
		if (RequestingState == null || RequestingState.PlayerPawn == null)
		{
			return;
		}

		Build_ServerOnly(TurretPrefab, RequestingState);
	}

	[Rpc.Host]
	public void ServerDestroyTurret(PlayerState RequestingState)
	{
		if (RequestingState == null)
		{
			return;
		}

		foreach (var Building in PlayerBuildings.GetOrCreate(RequestingState))
		{
			if (Building.GetComponent<TurretComponent>() != null)
			{
				Destroy_ServerOnly(Building, RequestingState);
				return;
			}
		}
	}

	[Rpc.Host]
	public void ServerRequestEnterTeleporter(PlayerState RequestingState)
	{
		if (RequestingState == null || RequestingState.PlayerPawn == null)
		{
			return;
		}

		Build_ServerOnly(EnterTeleporterPrefab, RequestingState);
	}

	[Rpc.Host]
	public void ServerDestroyEnterTeleporter(PlayerState RequestingState)
	{
		if (RequestingState == null)
		{
			return;
		}

		foreach (var Building in PlayerBuildings.GetOrCreate(RequestingState))
		{
			if (Building.GetComponent<TeleporterEntrenceComponent>() != null)
			{
				Destroy_ServerOnly(Building, RequestingState);
				return;
			}
		}
	}

	[Rpc.Host]
	public void ServerRequestExitTeleporter(PlayerState RequestingState)
	{
		if (RequestingState == null || RequestingState.PlayerPawn == null)
		{
			return;
		}

		Build_ServerOnly(ExitTeleporterPrefab, RequestingState);
	}

	[Rpc.Host]
	public void ServerDestroyExitTeleporter(PlayerState RequestingState)
	{
		if (RequestingState == null)
		{
			return;
		}

		foreach (var Building in PlayerBuildings.GetOrCreate(RequestingState))
		{
			if (Building.GetComponent<TeleporterExitComponent>() != null)
			{
				Destroy_ServerOnly(Building, RequestingState);
				return;
			}
		}
	}

	private void Build_ServerOnly(GameObject BuildingPrefab, PlayerState RequestingState)
	{
		Assert.True(Networking.IsHost);

		if (RequestingState == null || RequestingState.PlayerPawn == null)
		{
			return;
		}

		var RequestingPawn = RequestingState.PlayerPawn;

		Transform SpawnTransform = new()
		{
			Position = RequestingPawn.CenterPosition + (RequestingPawn.AimRay.Forward * 128),
			Rotation = RequestingPawn.Boom.WorldRotation
		};

		var BuildingClone = BuildingPrefab.Clone(SpawnTransform);
		var BuildingCloneComponent = BuildingClone.Components.Get<BuildingComponent>();
		BuildingCloneComponent.OwnerState = RequestingState;

		// if it's a turret we want to yoink the gun & setup some stats
		if (BuildingCloneComponent is TurretComponent TurretComponent)
		{
			if (!RequestingPawn.Inventory.CurrentWeaponGameObject.IsValid())
			{
				Log.Error("failed to spawn turret bc of some bullshit");
				BuildingClone.Destroy();
				return;
			}

			TurretComponent.SetFromWeaponGameObject(RequestingPawn.Inventory.CurrentWeaponGameObject);
			RequestingPawn.Inventory.RemoveWeapon(RequestingPawn.CurrentEquipment);
		}

		Assert.True(BuildingClone.NetworkSpawn());

		var PlayerBuildingsObjects = PlayerBuildings.GetOrCreate(RequestingState);
		PlayerBuildingsObjects.Add(BuildingClone);
	}

	private void Destroy_ServerOnly(GameObject BuildingToDestroy, PlayerState RequestingState)
	{
		Assert.True(Networking.IsHost);

		if (RequestingState == null)
		{
			return;
		}

		var PlayerBuildingsObjects = PlayerBuildings.GetOrCreate(RequestingState);
		PlayerBuildingsObjects.Remove(BuildingToDestroy);

		// if it's a turret give back to the world, if it's real
		if (BuildingToDestroy.GetComponent<TurretComponent>() is TurretComponent TurretComponent && RequestingState.PlayerPawn != null)
		{
			if (TurretComponent.EquippedSlot == EEquipmentSlot.Primary)
			{
				RequestingState.PlayerPawn.Inventory.Give(RequestingState.PlayerPawn.PlayerPawnDefinition.CharacterDefinition.PrimaryWeapon, false);
			}
			else if (TurretComponent.EquippedSlot == EEquipmentSlot.Secondary)
			{
				RequestingState.PlayerPawn.Inventory.Give(RequestingState.PlayerPawn.PlayerPawnDefinition.CharacterDefinition.SecondaryWeapon, false);
			}
		}

		BuildingToDestroy.Destroy();
	}
}
