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

	int HealCounter = 0;
	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if (!Networking.IsHost)
		{
			return;
		}

		++HealCounter;
		if (HealCounter < 20)
		{
			return;
		}
		HealCounter = 0;

		// do some healing
		foreach (var CapturePlayer in CaptureZone.CapturingPlayers)
		{
			if (!CapturePlayer.IsValid())
			{
				Log.Warning($"invalid player in capture zone {this}");
				CaptureZone.RemoveInvalidCapturePlayers();
				return;
			}

			if (CapturePlayer.Team == Team)
			{
				FHealingRequest HealingRequest = new()
				{
					TargetDamageComponent = CapturePlayer.DamageComponent,
					TargetPlayerPawn = CapturePlayer,
					AttackerPlayerPawn = null,
					BaseHealing = 1,
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

		var EnemyCount = CaptureZone.CapturingPlayers.Count(Player => Player.Team.GetOpponents() == Team);
		if (EnemyCount > 0)
		{
			return (false, 0);
		}

		var CaptureAmount = CaptureZone.CapturingPlayers.Count(Player => Player.Team == Team);
		return (CaptureAmount > 0, CaptureAmount);
	}
}
