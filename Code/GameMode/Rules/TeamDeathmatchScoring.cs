using KOTH;
using Sandbox.Events;

namespace KOTH;

public sealed class TeamDeathmatchScoring : Component,
	IGameEventHandler<KillEvent>
{
	void IGameEventHandler<KillEvent>.OnGameEvent(KillEvent EventArgs)
	{
		if (!Networking.IsHost)
			return;

		var DamageEvent = EventArgs.DamageEvent;

		if (GameUtils.GetPlayerFromComponent(DamageEvent.AttackerPlayerPawn) is not { } killerPlayer)
			return;

		if (GameUtils.GetPlayerFromComponent(DamageEvent.VictimPlayerPawn) is not { } victimPlayer)
			return;

		if (killerPlayer.IsFriendly(victimPlayer))
			return;

		if (killerPlayer.Team == Team.Unassigned)
			return;

		if (victimPlayer.Team == Team.Unassigned)
			return;

		GameMode.Instance.Get<TeamScoring>()?.IncrementScore(killerPlayer.Team);
	}
}
