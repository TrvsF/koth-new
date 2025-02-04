using Sandbox;

namespace KOTH;

public sealed class SpawnZone : Zone, Component.ITriggerListener
{
	[Property] public Team Team { get; private set; } = Team.Unassigned;

	private List<PlayerPawn> CurrentPlayerPawns = new();

	protected override void OnStart()
	{
		var Box = GameObject.GetComponent<BoxCollider>();
		if (!Box.IsValid())
		{
			Log.Warning($"Box is invalid on spawnzone {this}");
			return;
		}

		var BoxTrace = Scene.Trace.Box(Box.KeyframeBody.GetBounds(), WorldPosition, WorldPosition).RunAll();
		foreach (var Hits in BoxTrace)
		{
			Log.Info(Hits.GameObject);
		}
	}

	void ITriggerListener.OnTriggerEnter(Collider Collider)
	{
		var PlayerPawn = Collider.GameObject.Root.Components.Get<PlayerPawn>();
		if (PlayerPawn.IsValid() && PlayerPawn.Team == Team)
		{
			CurrentPlayerPawns.Add(PlayerPawn);
			PlayerPawn.DamageComponent.SetGodmode(true);
		}
	}

	void ITriggerListener.OnTriggerExit(Collider Collider)
	{
		var PlayerPawn = Collider.GameObject.Root.Components.Get<PlayerPawn>();
		if (PlayerPawn.IsValid() && PlayerPawn.Team == Team)
		{
			CurrentPlayerPawns.Remove(PlayerPawn);
			PlayerPawn.DamageComponent.SetGodmode(false);
		}
	}
}
