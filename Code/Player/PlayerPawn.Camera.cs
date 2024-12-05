using KOTH.UI;
using KOTH.Utils;
using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Events;
using Sandbox.Services;

namespace KOTH;

public sealed partial class PlayerPawn
{
	public CameraComponent Camera => Boom.GetComponentInChildren<CameraComponent>();
	public Ray AimRay => new(Boom.WorldPosition + Boom.WorldRotation.Forward, Boom.WorldRotation.Forward);

	////////////////////////////////////////////////////////////////////////

	[Property] public GameObject PlayerCameraPrefab { get; set; }
	[Property] public GameObject Boom { get; private set; }
	[Property] public float BaseFOV { get; private set; } = 90f;

	public bool CreatePlayerCamera(bool StartEnabled)
	{
		var CameraPrefabConfig = new CloneConfig()
		{
			StartEnabled = StartEnabled,
			Parent = Boom,
			Transform = new Transform()
		};

		PlayerCameraPrefab.Clone(CameraPrefabConfig);
		// Camera = PlayerCameraPrefab.GetComponent<CameraComponent>();
		if (!Camera.IsValid())
		{
			return false;
		}

		return true;
	}

	//internal void ResetFromEyes(float eyeHeight)
	//{
	//	// all transform effects are additive to camera local position, so we need to reset it before anything is applied
	//	Camera.LocalPosition = Vector3.Zero;
	//	Camera.LocalRotation = Rotation.Identity;

	//	Boom.WorldRotation = EyeAngles.ToRotation();
	//}

	///////////////////////////////////////////////////////////////////
	// TODO : proper screenshake?
	///////////////////////////////////////////////////////////////////
}
