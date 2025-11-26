using Sandbox;
using Sandbox.Diagnostics;

namespace KOTH;

public sealed class UberZone : Zone, Component.ITriggerListener
{
	void ITriggerListener.OnTriggerEnter(Collider Collider)
	{
		var PlayerPawn = Collider.GameObject.Root.Components.Get<PlayerPawn>();

		if (!PlayerPawn.IsValid())
		{
			return;
		}

		PlayerPawn.DamageComponent?.BroadcastUber();
	}

	void ITriggerListener.OnTriggerExit(Collider Collider)
	{
		var PlayerPawn = Collider.GameObject.Root.Components.Get<PlayerPawn>();
		
		if (!PlayerPawn.IsValid())
		{
			return;
		}
	}
}
