using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Events;
using System.Diagnostics.Metrics;
using static Sandbox.PhysicsContact;

namespace KOTH;

public sealed class MedicPlayer : Component
{
	[Property] float TimePer1Heals { get; set; } = 1.33f;

	public PlayerPawn OwnerPawn { get => GameObject.Root.GetComponent<PlayerPawn>(); }

	TimeSince TimeSinceLastHeal = 0;
	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if (!Networking.IsHost || !OwnerPawn.IsValid())
		{
			return;
		}

		if (TimeSinceLastHeal > TimePer1Heals)
		{
			FHealingRequest HealingRequest = new()
			{
				TargetPlayerPawn = OwnerPawn,
				BaseHealing = 3,
				HealingOrigin = GameObject.WorldPosition,
				AllowOverheal = false,
			};
			Scene.Dispatch(new HealingRequestEvent(HealingRequest));

			TimeSinceLastHeal = 0;
		}
	}
}
