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

	private void CameraTick()
	{
		if (IsAlive)
		{
			EyeAngles += Input.AnalogLook;
			EyeAngles = EyeAngles.WithPitch(EyeAngles.pitch.Clamp(-90, 90));

			// TODO : crouching
			Camera.LocalRotation = Rotation.Identity;
			Camera.LocalPosition = Vector3.Zero;

			Boom.WorldRotation = EyeAngles.ToRotation();
		}
	}

	private bool CreatePlayerCamera()
	{
		var CameraPrefabConfig = new CloneConfig()
		{
			StartEnabled = true,
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
}
