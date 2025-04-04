using KOTH.World;
using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Events;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace KOTH;

public readonly struct FMGEUIData
{
	public FMGEUIData() { }

	public string Player1Name { get; init; } = string.Empty;
	public string Player2Name { get; init; } = string.Empty;
	public int Player1Score { get; init; } = -1;
	public int Player2Score { get; init; } = -1;

	public readonly bool IsValid()
	{
		return Player1Name != string.Empty && Player2Name != string.Empty;
	}
}

public sealed class MGEUI
{
	public static MgeGamemode CurrentMgeGamemode => GameMode.Instance?.StateMachine.CurrentState.GameObject.Components.Get<MgeGamemode>();
}

public sealed class MgeGamemode : Component,
	IGameEventHandler<EnterStateEvent>,
	IGameEventHandler<KillBroadcastEvent>,
	IGameEventHandler<PlayerSpawnedEvent>
{
	[Property] public int KillLimit { get; set; } = 20;

	[Sync(SyncFlags.FromHost)] public NetDictionary<PlayerState, int> PlayerScores { get; set; }

	protected override void OnAwake()
	{
		base.OnAwake();

		if (Networking.IsHost)
		{
			PlayerScores = new();
		}
	}

	public bool QueryUIData(out FMGEUIData UiData)
	{
		if (PlayerScores.Count != 2)
		{
			UiData = new();
			return false;
		}

		var P1 = PlayerScores.ElementAt(0);
		var P2 = PlayerScores.ElementAt(1);

		UiData = new()
		{
			Player1Name = P1.Key.SteamName,
			Player1Score = P1.Value,
			Player2Name = P2.Key.SteamName,
			Player2Score = P2.Value,
		};

		return true;
	}

	public void OnGameEvent(KillBroadcastEvent KillEvent)
	{
		if (!Networking.IsHost)
		{
			return;
		}

		var DamageEvent = KillEvent.DamageEvent;
		OverhealPlayer(DamageEvent.AttackerPlayerState?.PlayerPawn);

		foreach (var PlayerState in PlayerScores.Keys)
		{
			if (PlayerState == DamageEvent.VictimPlayerState)
			{
				continue;
			}

			var Score = PlayerScores[PlayerState] = PlayerScores.GetValueOrDefault(PlayerState) + 1; // !

			if (Score >= KillLimit)
			{
				if (GameObject.GetComponent<StateComponent>() is { } ParentState)
				{
					Assert.IsValid(ParentState.DefaultNextState);
					GameMode.Instance.StateMachine.Transition(ParentState.DefaultNextState);
				}
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

	public void OnGameEvent(PlayerSpawnedEvent PlayerSpawnEvent)
	{
		OverhealPlayer(PlayerSpawnEvent.Player);
	}

	private void OverhealPlayer(PlayerPawn PlayerPawn)
	{
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
