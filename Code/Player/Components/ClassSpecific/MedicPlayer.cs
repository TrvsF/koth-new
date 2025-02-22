using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Events;
using System.Diagnostics.Metrics;
using static Sandbox.PhysicsContact;

namespace KOTH;

public sealed class MedicPlayer : Component
{
	[Property] float HealsPerTick { get; set; } = 0.01f;

	public PlayerPawn OwnerPawn { get => GameObject.Root.GetComponent<PlayerPawn>(); }

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if (Networking.IsHost && OwnerPawn.IsValid())
		{
			FHealingRequest HealingRequest = new()
			{
				TargetPlayerPawn = OwnerPawn,
				BaseHealing = HealsPerTick,
				HealingOrigin = GameObject.WorldPosition,
				AllowOverheal = false,
			};
			Scene.Dispatch(new HealingRequestEvent(HealingRequest));
		}
	}
}
