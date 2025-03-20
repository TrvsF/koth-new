using KOTH.UI;

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
	public static CameraComponent DeathCamera { get; set; } = null;
	public static CameraComponent OverviewCamera { get; set; } = null;

	///////////////////////////////////////////////

	public static void LocalTick()
	{
		if (TimeSinceDeathCameraCreated > DeathCameraTime && DeathCamera.IsValid())
		{
			DeathCamera.GameObject.Destroy();
			if (!PlayerState.Local.PlayerPawn.IsValid())
			{
				PlayerState.Local.OverviewCameraObject.Enabled = true;
			}
		}

		if (DeathCamera.IsValid())
		{
			DeathCamera.GameObject.WorldRotation = Rotation.LookAt(DeathCameraLookAtPawn.Head.WorldPosition - DeathCamera.GameObject.WorldPosition);
		}
	}

	const float DeathCameraTime = 3;

	static bool AreInDeathCameraState = false;
	static PlayerPawn DeathCameraLookAtPawn = null;
	static TimeSince TimeSinceDeathCameraCreated = new();

	public static bool CreateDeathCamera(Scene Scene, Vector3 SpawnPosition, FDamageTaken DamageTaken)
	{
		var CameraObject = Scene.CreateObject();
		CameraObject.Name = "DEATHCAMERA";
		CameraObject.NetworkMode = NetworkMode.Never;

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

		AreInDeathCameraState = true;
		TimeSinceDeathCameraCreated = 0;

		DeathCamera = CameraComp;
		DeathCamera.GameObject.WorldPosition = SpawnPosition;
		return DeathCamera.IsValid();
	}

	////////////////////////////////////////////////////

	public static bool CreateOverviewCamera(Scene Scene)
	{
		var CameraObject = Scene.CreateObject();
		CameraObject.Components.Create<ScreenPanel>();
		CameraObject.Components.Create<PlayerMenuComponent>();
		CameraObject.Name = "TEMPCAMERA";
		CameraObject.NetworkMode = NetworkMode.Never;

		// HACK : further silly hack to use the transform of a placed camera within the level
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

		OverviewCamera = CameraComp;
		return OverviewCamera.IsValid();
	}
}
