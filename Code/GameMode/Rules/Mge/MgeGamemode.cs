using KOTH.World;
using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Events;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace KOTH;

public sealed class MgeGamemode : Component,
	IGameEventHandler<UpdateStateEvent>,
	IGameEventHandler<EnterStateEvent>,
	IGameEventHandler<KillBroadcastEvent>,
	IGameEventHandler<PlayerSpawnedEvent>
{
	[Property] public int KillLimit { get; set; } = 20;

	[Sync(SyncFlags.FromHost)] NetDictionary<PlayerState, int> PlayerScores { get; set; } = new();

	public void OnGameEvent(KillBroadcastEvent KillEvent)
	{
		var Attacker = KillEvent.DamageEvent.AttackerPlayerState;

		if (!Attacker.IsValid())
		{
			return;
		}

		if (Attacker.PlayerPawn.IsValid())
		{
			FHealingRequest OverhealRequest = new()
			{
				TargetDamageComponent = Attacker.PlayerPawn.DamageComponent,
				TargetPlayerPawn = Attacker.PlayerPawn,
				HealingOrigin = Attacker.PlayerPawn.WorldPosition,
				BaseHealing = 300,
				AllowOverheal = true,
				HealingType = EHealingType.OneOff,
			};
			Scene.Dispatch(new HealingRequestEvent(OverhealRequest));
		}

		var Score = PlayerScores[Attacker] = PlayerScores.GetValueOrDefault(Attacker) + 1; // !

		if (Score >= KillLimit)
		{
			if (GameObject.GetComponent<StateComponent>() is { } ParentState)
			{
				Assert.IsValid(ParentState.DefaultNextState);
				GameMode.Instance.StateMachine.Transition(ParentState.DefaultNextState);
			}
		}
	}

	void IGameEventHandler<EnterStateEvent>.OnGameEvent(EnterStateEvent eventArgs)
	{
		PlayerScores.Clear();

		foreach (var PlayerState in GameNetworkManager.PlayerStates)
		{
			PlayerScores.Add(PlayerState, 0);
		}
	}

	void IGameEventHandler<UpdateStateEvent>.OnGameEvent(UpdateStateEvent eventArgs)
	{
		
	}

	public void OnGameEvent(PlayerSpawnedEvent PlayerSpawnEvent)
	{
		var PlayerPawn = PlayerSpawnEvent.Player;
		if (!PlayerPawn.IsValid())
		{
			return;
		}

		FHealingRequest OverhealRequest = new()
		{
			TargetDamageComponent = PlayerPawn.DamageComponent,
			TargetPlayerPawn = PlayerPawn,
			HealingOrigin = PlayerPawn.WorldPosition,
			BaseHealing = 300,
			AllowOverheal = true,
			HealingType = EHealingType.OneOff,
		};
		Scene.Dispatch(new HealingRequestEvent(OverhealRequest));
	}
}
