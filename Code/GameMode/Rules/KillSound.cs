using Sandbox.Events;

namespace KOTH;

public sealed class KillSound : Component,
	IGameEventHandler<KillEvent>,
	IGameEventHandler<RoundCounterIncrementedEvent>
{
	[Property] public SoundEvent KillSoundEvent { get; set; }
	[Property] public float BaseSoundPitch { get; set; } = 0.7f;
	[Property] public float SoundPitchPerCount { get; set; } = 0.1f;
	[Property] public int MaxCount { get; set; } = 5;

	int count = 0;

	void IGameEventHandler<RoundCounterIncrementedEvent>.OnGameEvent(RoundCounterIncrementedEvent eventArgs)
	{
		ResetCount();
	}

	[Authority]
	private void ResetCount()
	{
		count = 0;
	}

	void AddCount()
	{
		count++;
		if (count >= MaxCount) count = 0;
	}

	void IGameEventHandler<KillEvent>.OnGameEvent(KillEvent EventArgs)
	{
		var Attacker = GameUtils.GetPlayerFromComponent(EventArgs.DamageEvent.AttackerPlayerPawn);
		var Victim = GameUtils.GetPlayerFromComponent(EventArgs.DamageEvent.VictimPlayerPawn);

		if (Attacker != PlayerState.Viewer.PlayerPawn || !Attacker.IsValid() || !Victim.IsValid())
			return;

		if (Attacker.IsFriendly(Victim))
			return;

		var snd = Sound.Play(KillSoundEvent);
		snd.Pitch = BaseSoundPitch + (count * SoundPitchPerCount);
		AddCount();
	}
}
