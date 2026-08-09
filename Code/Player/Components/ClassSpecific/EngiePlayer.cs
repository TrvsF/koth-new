using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Events;
using static Sandbox.PhysicsContact;

namespace KOTH;

public sealed class EngiePlayer : Component
{
	public PlayerPawn OwnerPawn { get => GameObject.Root.GetComponent<PlayerPawn>(); }
	public bool IsPreviewingTurret { get => TurretPreviewObject.IsValid(); }
	
	private GameObject TurretPreviewObject = null;

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

	private void CreateTurretPreview()
	{
		TurretPreviewObject = GameMode.Instance.BuildingManager.TurretPrefab.Clone(GetTurretSpawnLocation(), OwnerPawn.Boom.WorldRotation);
		TurretPreviewObject.NetworkMode = NetworkMode.Never;

		foreach (var Component in TurretPreviewObject.Components.GetAll())
		{
			if (Component is SkinnedModelRenderer { } SkinnedModelRenderer)
			{
				SkinnedModelRenderer.Tint = SkinnedModelRenderer.Tint.WithAlpha(0.8f);
				continue;
			}

			Component.Enabled = false;
		}
	}

	private void DestroyTurretPreview()
	{
		if (!TurretPreviewObject.IsValid())
		{
			return;
		}

		TurretPreviewObject.Destroy();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if (IsProxy || !OwnerPawn.IsValid() || !OwnerPawn.IsLocallyControlled)
		{
			return;
		}

		if (TurretPreviewObject.IsValid())
		{
			TurretPreviewObject.WorldPosition = GetTurretSpawnLocation();
			TurretPreviewObject.WorldRotation = OwnerPawn.Boom.WorldRotation;
		}

		///////////////////////////////////////////////////

		bool RequestBuilding = Input.Pressed("use");

		if (RequestBuilding)
		{
			if (IsTurretInWorld())
			{
				GameMode.Instance.BuildingManager.ServerDestroyTurret(PlayerState.Local);
			}

			if (!IsPreviewingTurret)
			{
				CreateTurretPreview();
			}
			else if (!IsTurretInWorld())
			{
				DestroyTurretPreview();
				GameMode.Instance.BuildingManager.ServerRequestTurret(PlayerState.Local);
			}
		}
	}

}
