using Sandbox.Events;

namespace KOTH;

public struct FPlayerLookAtInfo
{
	public string Name { get; init; }
	public string Health { get; init; }
	public Team Team { get; init; }
}


public sealed class PlayerUtils
{
	public static bool LookAtInfoQuery(PlayerPawn PlayerPawn, Scene Scene, out FPlayerLookAtInfo LookAtInfo)
	{
		LookAtInfo = new();

		if (!PlayerPawn.IsValid() || !Scene.IsValid())
		{
			return false;
		}

		if (PlayerPawn.CurrentEquipment.GameObject.GetComponent<HealBeamComponent>() is { } HealBeam)
		{
			if (HealBeam.HealTarget.IsValid())
			{
				LookAtInfo = new()
				{
					Name = HealBeam.HealTarget.DisplayName,
					Health = HealBeam.HealTarget.Health.ToString(),
					Team = HealBeam.HealTarget.Team,
				};

				return true;
			}
		}

		var TraceStart = PlayerPawn.AimRay.Position;
		var StartRotation = Rotation.LookAt(PlayerPawn.AimRay.Forward);
		var TraceForward = StartRotation.Forward.Normal;
		var TraceEnd = PlayerPawn.AimRay.Position + TraceForward * 80000f;

		foreach (var TraceElement in ShootHelper.GetShootTraceElements(Scene.Trace, PlayerPawn.GameObject, TraceStart, TraceEnd, DebugOverlaySystem.Current))
		{
			if (!TraceElement.Hit)
			{
				continue;
			}

			if (TraceElement.GameObject.Root.Components.Get<PlayerPawn>(FindMode.EnabledInSelfAndDescendants) is PlayerPawn HitPlayerPawn)
			{
				LookAtInfo = new()
				{
					Name = HitPlayerPawn.DisplayName,
					Health = HitPlayerPawn.Health.ToString(),
					Team = HitPlayerPawn.Team,
				};

				return true;
			}
		}

		return false;
	}
}
