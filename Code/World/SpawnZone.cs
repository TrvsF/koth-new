using Sandbox;
using Sandbox.Diagnostics;

namespace KOTH;

public sealed class SpawnZone : Zone, Component.ITriggerListener
{
	[RequireComponent] BoxCollider TriggerBoxCollider { get; set; }
	[Property] public Team Team { get; private set; } = Team.Unassigned;

	private BoxCollider BlockingBox { get; set; } = null;
	private List<PlayerPawn> CurrentPlayerPawns = new();

	public bool SetupForLocal()
	{
		bool NeedsBlockingBox = Team.GetOpponents() == PlayerState.Local.Team;

		if (!NeedsBlockingBox && BlockingBox == null || NeedsBlockingBox && BlockingBox.IsValid())
		{
			return true;
		}

		if (NeedsBlockingBox)
		{
			BlockingBox = GameObject.AddComponent<BoxCollider>();
			BlockingBox = TriggerBoxCollider;
			BlockingBox.IsTrigger = false;
		}
		else
		{
			TriggerBoxCollider.IsTrigger = true;
			BlockingBox.Destroy();
			BlockingBox = null;
		}

		return true;
	}

	protected override void OnStart()
	{
		base.OnStart();

		Tags.Add($"{Team}-spawn");
	}

	void ITriggerListener.OnTriggerEnter(Collider Collider)
	{
		var PlayerPawn = Collider.GameObject.Root.Components.Get<PlayerPawn>();

		if (!PlayerPawn.IsValid())
		{
			return;
		}


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
