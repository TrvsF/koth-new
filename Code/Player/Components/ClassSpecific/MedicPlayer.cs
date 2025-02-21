using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Events;
using System.Diagnostics.Metrics;
using static Sandbox.PhysicsContact;

namespace KOTH;

public sealed class MedicPlayer : Component
{
	public PlayerPawn OwnerPawn { get => GameObject.Root.GetComponent<PlayerPawn>(); }

	public void Uber(float UberAmount)
	{
		Assert.True(Networking.IsHost);
			
		Assert.IsValid(OwnerPawn);
		Assert.IsValid(OwnerPawn.DamageComponent);

		Log.Info($"uber {UberAmount}");
	}
}
