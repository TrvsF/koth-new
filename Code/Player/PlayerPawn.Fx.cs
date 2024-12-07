using KOTH.UI;
using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Events;

namespace KOTH;

public partial class PlayerPawn
{
	[Rpc.Broadcast(NetFlags.HostOnly)]
	private void CreateGibs()
	{
		if (Body.IsValid())
		{
			Body.SetRagdoll(true);
			Body.GameObject.SetParent(null, true);
			// Body.GameObject.Name = $"Ragdoll ({DisplayName})";
			var DestroyComponent = Body.Components.Create<TimedDestroyComponent>();
			DestroyComponent.Time = 0;
		}

		if (GibPrefab.IsValid())
		{
			var Gibs = GibPrefab.Clone(WorldPosition);
			foreach (var ChildGib in Gibs.Root.Children)
			{
				var Rigidbody = ChildGib.Components.Get<Rigidbody>();
				if (Rigidbody.IsValid())
				{
					Rigidbody.Velocity = new(Random.Shared.Int(-1000, 1000), Random.Shared.Int(-1000, 1000), Random.Shared.Int(-1000, 1000));
				}
			}
			Gibs.NetworkSpawn();
		}

		// Body = null;
	}

	[Rpc.Broadcast(NetFlags.HostOnly)]
	private void CreateRagdoll()
	{
		if (!Body.IsValid())
			return;

		Body.SetRagdoll(true);
		Body.GameObject.SetParent(null, true);
		// Body.GameObject.Name = $"Ragdoll ({DisplayName})";

		//var ev = new OnPlayerRagdolledEvent();
		//Scene.Dispatch(ev);

		//if (ev.DestroyTime > 0f)
		//{
		//	var comp = Body.Components.Create<TimedDestroyComponent>();
		//	comp.Time = ev.DestroyTime;
		//}

		// Body = null;
	}

	private void ResetBody()
	{
		if (Body is not null)
		{
			Body.DamageTakenForce = Vector3.Zero;
		}

		PlayerBoxCollider.Enabled = true;

		// Components.Get<HumanOutfitter>(FindMode.EnabledInSelfAndDescendants)?.UpdateFromTeam(Team);
	}
}
