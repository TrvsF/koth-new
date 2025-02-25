using Sandbox;
using Sandbox.Diagnostics;

namespace KOTH;

public sealed class SpawnZone : Zone, Component.ITriggerListener
{
	[RequireComponent] BoxCollider TriggerBoxCollider { get; set; }
	[Property] public Team Team { get; private set; } = Team.Unassigned;

	private BoxCollider PlayerBoxCollider { get; set; }
	private List<PlayerPawn> CurrentPlayerPawns = new();

	public bool SetupForLocal()
	{
		bool NeedsBlockingBox = Team.GetOpponents() == PlayerState.Local.Team;

		if (!NeedsBlockingBox && PlayerBoxCollider == null || NeedsBlockingBox && PlayerBoxCollider.IsValid())
		{
			return true;
		}

		if (!PlayerBoxCollider.IsValid())
		{
			PlayerBoxCollider = GameObject.Components.Create<BoxCollider>();
		}

		if (NeedsBlockingBox)
		{
			PlayerBoxCollider.Center = TriggerBoxCollider.Center;
			PlayerBoxCollider.Scale = TriggerBoxCollider.Scale;
			PlayerBoxCollider.Enabled = true;
		}
		else
		{
			PlayerBoxCollider.Enabled = false;
		}

		TriggerBoxCollider.IsTrigger = true;
		PlayerBoxCollider.IsTrigger = false;

		Log.Info($"SetupForLocal NeedsBlockingBox : {NeedsBlockingBox}\n" +
			$"{TriggerBoxCollider} {TriggerBoxCollider.Enabled}\n" +
			$"{PlayerBoxCollider} {PlayerBoxCollider.Enabled}");

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
