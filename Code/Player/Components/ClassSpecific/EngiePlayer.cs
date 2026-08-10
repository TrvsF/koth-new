using Sandbox;
using Sandbox.Diagnostics;

namespace KOTH;

public enum EBuildingType
{
	Turret,
	TpEnter,
	TpExit,
}

public sealed class EngiePlayer : Component
{
	public PlayerPawn OwnerPawn { get => GameObject.Root.GetComponent<PlayerPawn>(); }

	private int BuildingIndexInput = 0;
	private int BuildingIndex => ((BuildingIndexInput % BuildingToType.Count) + BuildingToType.Count) % BuildingToType.Count;
	private Dictionary<GameObject, EBuildingType> BuildingToType;

	protected override void OnStart()
	{
		base.OnStart();

		BuildingToType = new()
		{
			{ GameMode.Instance.BuildingManager.TurretPrefab, EBuildingType.Turret },
			{ GameMode.Instance.BuildingManager.EnterTeleporterPrefab, EBuildingType.TpEnter },
			{ GameMode.Instance.BuildingManager.ExitTeleporterPrefab, EBuildingType.TpExit },
		};
	}

	public bool IsPreviewingBuilding { get => BuildingPreviewObject.IsValid(); }
	private GameObject BuildingPreviewObject = null;

	private Vector3 GetTurretSpawnLocation()
	{
		Assert.IsValid(OwnerPawn);
		return OwnerPawn.CenterPosition + (OwnerPawn.AimRay.Forward * 128);
	}

	private bool IsTurretInWorld()
	{
		foreach (var Building in GameMode.Instance.BuildingManager.PlayerBuildings.GetOrCreate(PlayerState.Local))
		{
			if (Building.GetComponent<TurretComponent>() != null)
			{
				return true;
			}
		}

		return false;
	}

	private void CreateBuildingPreview()
	{
		var Prefab = BuildingToType.ElementAt(BuildingIndex).Key;

		BuildingPreviewObject = Prefab.Clone(GetTurretSpawnLocation(), OwnerPawn.Boom.WorldRotation);
		BuildingPreviewObject.NetworkMode = NetworkMode.Never;

		foreach (var Component in BuildingPreviewObject.Components.GetAll())
		{
			if (Component is SkinnedModelRenderer { } SkinnedModelRenderer)
			{
				SkinnedModelRenderer.Tint = SkinnedModelRenderer.Tint.WithAlpha(0.8f);
				continue;
			}

			Component.Enabled = false;
		}
	}

	private void DestroyBuildingPreview()
	{
		if (!BuildingPreviewObject.IsValid())
		{
			return;
		}

		BuildingPreviewObject.Destroy();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if (IsProxy || !OwnerPawn.IsValid() || !OwnerPawn.IsLocallyControlled)
		{
			return;
		}

		if (BuildingPreviewObject.IsValid())
		{
			BuildingPreviewObject.WorldPosition = GetTurretSpawnLocation();
			BuildingPreviewObject.WorldRotation = OwnerPawn.Boom.WorldRotation;
		}

		///////////////////////////////////////////////////

		if (IsPreviewingBuilding)
		{
			if (Input.MouseWheel != 0)
			{
				DestroyBuildingPreview();
				BuildingIndexInput += (int) Math.Round(Input.MouseWheel.y);
				CreateBuildingPreview();
			}
		}

		bool RequestBuilding = Input.Pressed("use");

		if (RequestBuilding)
		{
			//if (IsTurretInWorld())
			//{
			//	switch (BuildingToType.ElementAt(BuildingIndex).Value)
			//	{
			//		case EBuildingType.Turret:
			//			GameMode.Instance.BuildingManager.ServerDestroyEnterTeleporter(PlayerState.Local);
			//			break;
			//		case EBuildingType.TpEnter:
			//			GameMode.Instance.BuildingManager.ServerRequestEnterTeleporter(PlayerState.Local);
			//			break;
			//		case EBuildingType.TpExit:
			//			GameMode.Instance.BuildingManager.ServerRequestExitTeleporter(PlayerState.Local);
			//			break;
			//	}
			//}

			if (!IsPreviewingBuilding)
			{
				CreateBuildingPreview();
			}
			else if (!IsTurretInWorld())
			{
				DestroyBuildingPreview();

				switch (BuildingToType.ElementAt(BuildingIndex).Value)
				{
					case EBuildingType.Turret:
						GameMode.Instance.BuildingManager.ServerRequestTurret(PlayerState.Local);
						break;
					case EBuildingType.TpEnter:
						GameMode.Instance.BuildingManager.ServerRequestEnterTeleporter(PlayerState.Local);
						break;
					case EBuildingType.TpExit:
						GameMode.Instance.BuildingManager.ServerRequestExitTeleporter(PlayerState.Local);
						break;
				}
			}
		}
	}

}
