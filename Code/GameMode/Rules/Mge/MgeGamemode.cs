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
	IGameEventHandler<LeaveStateEvent>,
	IGameEventHandler<KillBroadcastEvent>
{
	[Property] public int KillLimit { get; set; } = 20;

	[Sync(SyncFlags.FromHost)] NetDictionary<PlayerState, int> PlayerScores { get; set; } = new();

	public void OnGameEvent(KillBroadcastEvent KillEvent)
	{
		var Attacker = KillEvent.DamageEvent.AttackerPlayerState;

		++PlayerScores[Attacker];

		OutputPlayerScores();
	}

	private void OutputPlayerScores()
	{
		Log.Info($"~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
		foreach (var (Player, Score) in PlayerScores)
		{
			Log.Info($"{Player.SteamName}:{Score}");
		}
		Log.Info($"~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
	}

	void IGameEventHandler<EnterStateEvent>.OnGameEvent(EnterStateEvent eventArgs)
	{
		foreach (var PlayerState in GameNetworkManager.PlayerStates)
		{
			PlayerScores.Add(PlayerState, 0);
		}

		OutputPlayerScores();
	}
	
	void IGameEventHandler<LeaveStateEvent>.OnGameEvent(LeaveStateEvent eventArgs)
	{
		PlayerScores.Clear();
	}

	void IGameEventHandler<UpdateStateEvent>.OnGameEvent(UpdateStateEvent eventArgs)
	{
		
	}
}
