using KOTH.World;
using Sandbox;
using Sandbox.Events;

namespace KOTH;

public sealed class PayloadCart : Component
{
	public CaptureZone CaptureZone { get => GameObject.GetComponentInChildren<CaptureZone>(); }

	[Property] public float BaseSpeed { get; set; } = 1f;
	[Property] public float BaseHealing { get; set; } = 0.05f;
	[Property] public Team Team { get; private set; } = Team.Unassigned;

	// TODO : hookup to teams

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if (!Networking.IsHost)
		{
			return;
		}

		// do some healing
		foreach (var CapturePlayer in CaptureZone.CapturingPlayers)
		{
			if (!CapturePlayer.IsValid())
			{
				Log.Warning($"invalid player in capture zone {this}");
				continue;
			}

			if (CapturePlayer.Team == Team)
			{
				FHealingRequest HealingRequest = new()
				{
					TargetPlayerPawn = CapturePlayer,
					AttackerPlayerPawn = null,
					BaseHealing = BaseHealing,
					HealingOrigin = GameObject.WorldPosition,
					AllowOverheal = false,
				};
				Scene.Dispatch(new HealingRequestEvent(HealingRequest));
			}
		}
	}

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
