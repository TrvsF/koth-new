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
	public static CameraComponent CurrentCamera { get; set; } = null;
}
