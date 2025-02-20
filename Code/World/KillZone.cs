using Sandbox;
using Sandbox.Events;

namespace KOTH;

public sealed class KillZone : Component, Component.ITriggerListener
{
	[RequireComponent] public Zone Zone { get; private set; }

	void ITriggerListener.OnTriggerEnter(Collider Collider)
	{
		var PlayerPawn = Collider.GameObject.Root.Components.Get<PlayerPawn>();
		if (PlayerPawn.IsValid())
		{
			FDamageRequest DamageRequest = new()
			{
				TargetDamageComponent = PlayerPawn.DamageComponent,
				AttackerPlayerPawn = null,
				DamageOrigin = 0,
				BaseDamage = float.MaxValue,
				BaseKnockbackStrength = 0,
				DirectImpact = true,
				DamageType = EDamageType.Melee,
			};
			Game.ActiveScene.Dispatch(new DamageRequestEvent(DamageRequest));
		}
	}
}
