using Sandbox.Events;

namespace KOTH;

/// <summary>
/// Handles certain events and plays radio sounds.
/// </summary>
public sealed class PlayRadioOnKill : Component,
	IGameEventHandler<KillEvent>
{
	[Property] public bool PlayEnemyLeftSounds { get; set; } = true;
	[Property] public bool PlayDeathSounds { get; set; } = true;

	private int GetAliveCount(Team team)
	{
		return GameUtils.GetPlayerPawns(team).Where(x => x.IsAlive).Count();
	}

	void IGameEventHandler<KillEvent>.OnGameEvent(KillEvent EventArgs)
	{
		if (!Networking.IsHost)
			return;

		var DamageEvent = EventArgs.DamageEvent;
		var VictimTeam = DamageEvent.VictimPlayerPawn.GameObject.GetTeam();

		if (PlayDeathSounds && GameUtils.GetPlayerFromComponent(DamageEvent.VictimPlayerPawn) is { } player)
			RadioSounds.Play(VictimTeam, RadioSound.TeammateDies);

		if (!PlayEnemyLeftSounds)
			return;

		if (DamageEvent.AttackerPlayerPawn.IsValid())
		{
			if (GetAliveCount(VictimTeam) == 2)
			{
				RadioSounds.Play(VictimTeam.GetOpponents(), RadioSound.TwoEnemiesLeft);
			}
			else if (GetAliveCount(VictimTeam) == 1)
			{
				RadioSounds.Play(VictimTeam.GetOpponents(), RadioSound.OneEnemyLeft);
			}
		}
	}
}
