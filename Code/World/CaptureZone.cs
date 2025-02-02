using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KOTH.World
{
	public sealed class CaptureZone : Zone, Component.ITriggerListener
	{
		[Sync(SyncFlags.FromHost)] public NetList<PlayerPawn> CapturingPlayers { get; private set; } = new();

		void ITriggerListener.OnTriggerEnter(Collider Collider)
		{
			if (Networking.IsHost)
			{
				var PlayerPawn = Collider.GameObject.Root.GetComponent<PlayerPawn>();

				if (!PlayerPawn.IsValid())
				{
					return;
				}

				CapturingPlayers.Add(PlayerPawn);
			}
		}

		void ITriggerListener.OnTriggerExit(Collider Collider)
		{
			if (Networking.IsHost)
			{
				var PlayerPawn = Collider.GameObject.Root.GetComponent<PlayerPawn>();

				if (!PlayerPawn.IsValid())
				{
					return;
				}

				if (!CapturingPlayers.Contains(PlayerPawn))
				{
					Log.Warning($"player left capture zone {this} but was not considered entering");
					return;
				}

				CapturingPlayers.Remove(PlayerPawn);
			}
		}
	}
}
