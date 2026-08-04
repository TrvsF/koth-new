using Sandbox;
using Sandbox.Events;
using Sandbox.Utility;

namespace KOTH;

public sealed class KothWinCondition : Component,
	IGameEventHandler<HillWinEvent>
{
	[Property, Sync(SyncFlags.FromHost)]
	public int TargetScore { get; set; } = 3;

	[Sync(SyncFlags.FromHost)]
	public NetDictionary<Team, int> Scores { get; private set; } = new();

	[Property]
	public StateComponent RoundWinState { get; set; }

	[Property]
	public StateComponent TGameWinState { get; set; }

	[Property]
	public StateComponent CTGameWinState { get; set; }

	///////////////////////////////////////////////////////////////////////////

	protected override void OnAwake()
	{
		base.OnAwake();

		if (Networking.IsHost)
		{
			//if (Scores != null)
			//{
			Scores = new()
				{
				// TODO : will need to add any extra teams here as well! not pragmatic!
				{ Team.CounterTerrorist, 0 }, { Team.Terrorist, 0 }, { Team.Unassigned, 0 },
				};
			//}
		}

	}

	public void OnGameEvent(HillWinEvent EventArgs)
	{
		Log.Info($"all : scores {Scores[Team.CounterTerrorist]} : {Scores[Team.Terrorist]}");

		if (!Networking.IsHost)
			return;

		var Hill = EventArgs.Hill;
		if (!Hill.IsValid())
		{
			Log.Error("Invalid hill captured!");
			return;
		}

		Team WinningTeam = EventArgs.Team;
		var TeamScore = ++Scores[WinningTeam];

		if (TeamScore >= TargetScore)
		{
			var NextState = WinningTeam == Team.CounterTerrorist ? CTGameWinState : TGameWinState;
			GameMode.Instance.StateMachine.Transition(NextState);
		}
		else
		{
			GameMode.Instance.StateMachine.Transition(RoundWinState);
		}

		Log.Info($"host : scores {Scores[Team.CounterTerrorist]} : {Scores[Team.Terrorist]}");
	}
}
