using Sandbox;
using Sandbox.Diagnostics;

namespace KOTH;

public sealed class SpawnZone : Zone, Component.ITriggerListener
{
	[RequireComponent] BoxCollider TriggerBoxCollider { get; set; }
	[Property] public Team Team { get; private set; } = Team.Unassigned;
	[Property] public float SpawnTime { get; private set; } = 0f;

	private List<PlayerPawn> CurrentPlayerPawns = new();

	public void CreatePlayerCollisionBox()
	{
		Assert.True(Team.GetOpponents() == PlayerState.Local.Team);

		TriggerBoxCollider.IsTrigger = false;
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
