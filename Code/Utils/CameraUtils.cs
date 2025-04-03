using KOTH.UI;
using System.Linq;

namespace KOTH;

public struct FDeathCameraData
{
	public FDeathCameraData() { }

	public string KillerName { get; set; } = "";

	public PlayerState KillerPlayerState { get; set; }
	public int KillerHealth { get; set; } = -1;

	public readonly bool IsValid()
	{
		return KillerName != "";
	}
}

public sealed class CameraUtils
{
	// private static CameraComponent PlayerCamera { get; set; } = null;
	private static CameraComponent DeathCamera = null;
	private static CameraComponent OverviewCamera = null;

	public static void SetActiveCamera(CameraComponent CameraComponent)
	{
		TurnOffCameras();

		CameraComponent.GameObject.Enabled = true;
	}

	public static void TurnOffCameras()
	{
		if (DeathCamera.IsValid())
		{
			DeathCamera.GameObject.Enabled = false;
		}

		if (OverviewCamera.IsValid())
		{
			OverviewCamera.GameObject.Enabled = false;
		}
	}

	///////////////////////////////////////////////

	public static void LocalTick()
	{
		if (!DeathCamera.IsValid() || !DeathCamera.GameObject.IsValid() || !DeathCamera.GameObject.Enabled)
		{
			return;
		}

		if (TimeSinceDeathCameraCreated > DeathCameraTime)
		{
			CreateSetOverviewCamera(LastDeathcameraScene);
			DeathCamera.GameObject.Enabled = false;
		}
		else
		{
			DeathCamera.GameObject.WorldRotation = Rotation.LookAt(DeathCameraLookAtPawn.Head.WorldPosition - DeathCamera.GameObject.WorldPosition);
		}
	}

	const float DeathCameraTime = 3;
	static Scene LastDeathcameraScene = null;
	static PlayerPawn DeathCameraLookAtPawn = null;
	static TimeSince TimeSinceDeathCameraCreated = new();

	public static CameraComponent CreateSetDeathCamera(Scene Scene, Vector3 SpawnPosition, FDamageTaken DamageTaken)
	{
		LastDeathcameraScene = Scene; // HACK

		var CameraObject = Scene.CreateObject();
		CameraObject.Name = "DEATHCAMERA";
		CameraObject.NetworkMode = NetworkMode.Never;

		if (!DamageTaken.AssumedAttackerPlayerPawn.IsValid())
		{
			return CreateSetOverviewCamera(Scene);
		}

		DeathCameraLookAtPawn = DamageTaken.AssumedAttackerPlayerPawn;

		CameraObject.Components.Create<ScreenPanel>();
		CameraObject.Components.Create<PlayerDeathHUD>();

		var Health = DamageTaken.AssumedAttackerPlayerPawn.IsValid() ? DamageTaken.AssumedAttackerPlayerPawn.Health : 0;
		FDeathCameraData DeathCameraData = new()
		{
			KillerName = DamageTaken.AttackerPlayerState.SteamName,
			KillerPlayerState = DamageTaken.AttackerPlayerState,
			KillerHealth = Health,
		};

		PlayerDeathHUD.Instance.SetData(DeathCameraData);

		var CameraComp = CameraObject.Components.Create<CameraComponent>();
		CameraComp.Priority = 101;

		TimeSinceDeathCameraCreated = 0;

		DeathCamera = CameraComp;
		DeathCamera.GameObject.WorldPosition = SpawnPosition;

		SetActiveCamera(DeathCamera);

		return DeathCamera;
	}

	////////////////////////////////////////////////////

	public static CameraComponent CreateSetOverviewCamera(Scene Scene, bool Override = false)
	{
		if (Override && OverviewCamera.IsValid())
		{
			OverviewCamera.GameObject.Destroy();
		}

		if (!OverviewCamera.IsValid())
		{
			var CameraObject = Scene.CreateObject();
			CameraObject.Components.Create<ScreenPanel>();
			CameraObject.Components.Create<PlayerMenuComponent>();
			CameraObject.Name = "TEMPCAMERA";
			CameraObject.NetworkMode = NetworkMode.Never;

			// HACK : use the transform of a placed camera within the level
			foreach (var Object in Scene.GetAllObjects(false))
			{
				if (Object.Tags.Contains("scenecamera"))
				{
					CameraObject.WorldPosition = Object.WorldPosition;
					CameraObject.WorldRotation = Object.WorldRotation;
				}
			}

			var CameraComp = CameraObject.Components.Create<CameraComponent>();
			CameraComp.Priority = 100;
			CameraComp.Enabled = true;

			OverviewCamera = CameraComp;
		}

		SetActiveCamera(OverviewCamera);

		return OverviewCamera;
	}
}
