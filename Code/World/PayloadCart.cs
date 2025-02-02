using KOTH.World;
using Sandbox;

namespace KOTH;

public sealed class PayloadCart : Component
{
	public CaptureZone CaptureZone { get => GameObject.GetComponentInChildren<CaptureZone>(); }

	[Property] public float BaseSpeed { get; private set; } = .66f;

	// TODO : hookup to teams

	public (bool IsCapturing, int CaptureFactor) GetCaptureData()
	{
		if (CaptureZone == null)
		{
			Log.Warning($"capture zone not valid on payload {this}");
			return (false, 0);
		}

		// TODO : class check for cap rate
		var CaptureAmount = CaptureZone.CapturingPlayers.Count;
		return (CaptureAmount > 0, CaptureAmount);
	}
}
