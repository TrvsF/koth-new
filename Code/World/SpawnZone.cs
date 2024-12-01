using Sandbox;

namespace KOTH;

public sealed class SpawnZone : Component, Component.ITriggerListener
{
	[Property] public Team Team { get; private set; } = Team.Unassigned;
	[RequireComponent] public Zone Zone { get; private set; }

	private List<PlayerPawn> CurrentPlayerPawns = new();

	protected override void OnUpdate()
	{
		base.OnUpdate();

		foreach (var PlayerPawn in CurrentPlayerPawns)
		{
			//if (PlayerPawn.IsValid() && !PlayerPawn.IsDummy && PlayerPawn.IsAlive)
			//{
			//	if (PlayerPawn.CharacterDefinition != PlayerPawn.PlayerState.RequestedCharacterDefinition)
			//	{
			//		PlayerPawn.PlayerState.Respawn(true);
			//	}
			//}
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
