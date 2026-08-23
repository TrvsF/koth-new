using KOTH.World;
using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Events;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace KOTH;

public readonly struct FDMUIData
{
	public FDMUIData() { }

	public string Player1Name { get; init; } = string.Empty;
	public string Player2Name { get; init; } = string.Empty;
	public string Player3Name { get; init; } = string.Empty;
	public int Player1Score { get; init; } = -1;
	public int Player2Score { get; init; } = -1;
	public int Player3Score { get; init; } = -1;

	public readonly bool IsValid()
	{
		return Player1Name != string.Empty && Player2Name != string.Empty;
	}

	public readonly bool IsThree()
	{
		return IsValid() && Player3Name != string.Empty;
	}
}

public sealed class DMUI
{
	public static DmGamemode CurrentDmGamemode => GameMode.Instance?.StateMachine.CurrentState.GameObject.Components.Get<DmGamemode>();
}

public sealed class DmGamemode : Component,
	IGameEventHandler<EnterStateEvent>,
	IGameEventHandler<UpdateStateEvent>,
	IGameEventHandler<KillBroadcastEvent>
{
	[Property] public int KillLimit { get; set; } = 100;

	[Sync(SyncFlags.FromHost)] public NetDictionary<PlayerState, int> PlayerScores { get; set; }

	protected override void OnAwake()
	{
		base.OnAwake();

		if (Networking.IsHost)
		{
			PlayerScores = new();
		}
	}

	public bool QueryUIData(out FDMUIData UiData)
	{
		if (PlayerScores.Count < 2)
		{
			UiData = new();
			return false;
		}

		var Top = PlayerScores.OrderByDescending(Kvp => Kvp.Value).Take(3).ToArray();

		var P1 = Top.ElementAt(0);
		var P2 = Top.ElementAt(1);
		if (PlayerScores.Count > 2)
		{
			var P3 = Top.ElementAt(2);

			UiData = new()
			{
				Player1Name = P1.Key.SteamName,
				Player1Score = P1.Value,
				Player2Name = P2.Key.SteamName,
				Player2Score = P2.Value,
				Player3Name = P3.Key.SteamName,
				Player3Score = P3.Value,
			};
		}
		else
		{
			UiData = new()
			{
				Player1Name = P1.Key.SteamName,
				Player1Score = P1.Value,
				Player2Name = P2.Key.SteamName,
				Player2Score = P2.Value,
			};
		}

		Log.Info(PlayerScores.Count);

		return true;
	}

	public void OnGameEvent(KillBroadcastEvent KillEvent)
	{
		if (!Networking.IsHost)
		{
			return;
		}

		// HACK, connection events too fast, maybe need a player state active event?
		foreach (var PlayerState in GameNetworkManager.PlayerStates)
		{
			if (!PlayerScores.ContainsKey(PlayerState))
			{
				PlayerScores.Add(PlayerState, 0);
			}
		}
		foreach (var (Player, _) in PlayerScores)
		{
			if (!GameNetworkManager.PlayerStates.Contains(Player))
			{
				PlayerScores.Remove(Player);
			}
		}

		var DamageEvent = KillEvent.DamageEvent;
		if (DamageEvent.VictimPlayerState == null)
		{
			return;
		}

		var Attacker = DamageEvent.AttackerPlayerState;

		if (!Attacker.IsValid())
		{
			return;
		}

		if (!PlayerScores.TryGetValue(Attacker, out var Score))
		{
			return;
		}

		PlayerScores[Attacker] = ++Score;

		if (Score < KillLimit)
		{
			return;
		}

		// TODO : make a song & dance
		if (GetComponent<StateComponent>() is { } ParentState)
		{
			Assert.IsValid(ParentState.DefaultNextState);
			GameMode.Instance.StateMachine.Transition(ParentState.DefaultNextState);
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

	public void OnGameEvent(UpdateStateEvent eventArgs)
	{
		if (GameNetworkManager.PlayerStates.Count < 2)
		{
			if (GameObject.GetComponent<StateComponent>() is { } ParentState)
			{
				Assert.IsValid(ParentState.DefaultNextState);
				GameMode.Instance.StateMachine.Transition(ParentState.DefaultNextState);
			}
		}
	}
}
