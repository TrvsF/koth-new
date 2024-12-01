using Sandbox.Events;

namespace KOTH.UI;

public record OnScoreAddedEvent : IGameEvent
{
	public int Score { get; set; }
	public string Reason { get; set; }
}

/// <summary>
/// Plop this on something you're using <see cref="ScoreAttribute"/> for. We could codegen this attribute on components that use it to save this hassle.
/// </summary>
public interface IScore
{
	/// <summary>
	/// Looks for a bunch of score attributes from components on a <see cref="PlayerState"/>, and returns a formatted, sorted list of values.
	/// </summary>
	/// <param name="playerState"></param>
	/// <returns></returns>
	public static IEnumerable<(object Value, ScoreAttribute Attribute)> Find(PlayerState playerState)
	{
		var components = playerState.Components.GetAll<IScore>(FindMode.EnabledInSelfAndDescendants);
		var values = new List<(object Value, MemberDescription Member, ScoreAttribute Attribute)>();

		foreach (var comp in components)
		{
			var type = TypeLibrary.GetType(comp.GetType());

			foreach (var member in type.Members)
			{
				if (member.GetCustomAttribute<ScoreAttribute>() is not { } scoreAttribute)
					continue;

				// Support ShowIf, which looks for a method with a boolean return to see if we can display a value
				var show = type.GetMethod(scoreAttribute.ShowIf)?.InvokeWithReturn<bool>(comp, null) ?? true;
				if (!show)
					continue;

				// Support special formatting values
				values.Add((
					string.Format(scoreAttribute.Format, type.GetValue(comp, member.Name)),
					member, scoreAttribute
				));
			}
		}

		return values.OrderBy(x => x.Member.GetCustomAttribute<OrderAttribute>()?.Value ?? 0)
			// We don't need to expose x.Member
			.Select(x => (x.Value, x.Attribute));
	}
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ScoreAttribute : System.Attribute
{
	public string Name { get; set; }
	public string Format { get; set; } = "{0}";
	public string ShowIf { get; set; } = null;

	public ScoreAttribute(string name)
	{
		Name = name;
	}
}

/// <summary>
/// Handles all the player score values.
/// </summary>
public sealed class PlayerScore : Component,
	IGameEventHandler<KillEvent>,
	IGameEventHandler<RoundCounterIncrementedEvent>,
	IGameEventHandler<RoundCounterResetEvent>,
	IGameEventHandler<ResetScoresEvent>,
	IGameEventHandler<HillCapturedEvent>,
	IGameEventHandler<HillWinEvent>,
	IGameEventHandler<HillCappingEvent>,
	IGameEventHandler<HillDecayCapEvent>,
	IScore
{
	[Property] public PlayerState PlayerState { get; set; }

	[HostSync, Property, ReadOnly, Score("Kills")]
	public int Kills { get; set; } = 0;

	[HostSync, Property, ReadOnly, Score("Deaths")]
	public int Deaths { get; set; } = 0;

	[HostSync, Property, ReadOnly, Score("Points"), Order(-1)]
	public int Score { get; private set; } = 0;

	public void AddScore(int score, string reason = null)
	{
		Score += score;

		using (Rpc.FilterInclude(Network.Owner))
		{
			SendScoreAdded(score, reason);
		}
	}

	[Broadcast(NetPermission.HostOnly)]
	private void SendScoreAdded(int score, string reason = null)
	{
		Scene.Dispatch<OnScoreAddedEvent>(new OnScoreAddedEvent()
		{
			Score = score,
			Reason = reason
		});
	}

	[Score("Ratio", Format = "{0:0.00}"), Order(50)]
	public float Ratio => (float)Kills / (float)Deaths.Clamp(1, int.MaxValue);

	[HostSync]
	public NetList<int> ScoreHistory { get; private set; } = new();

	[HostSync]
	public bool WasBombPlanter { get; private set; }

	private const int KillScore = 25;
	private const int AssistScore = 10;
	private const int TeamKillScore = -25;
	private const int SuicideScore = -10;

	// Planting the C4 explosive
	private const int PlantScore = 25;

	// Bomb planter alive when the bomb explodes
	private const int BombExplodePlanterAliveScore = 35;

	// Bomb planter dead when the bomb explodes
	private const int BombExplodePlanterDeadScore = 10;

	// Other Ts alive when the bomb explodes
	private const int BombExplodeTeamAliveScore = 25;

	// Defusing bomb
	private const int DefuserScore = 50;

	// Other CTs alive when the bomb is defused
	private const int DefuseTeamAliveScore = 25;

	void IGameEventHandler<KillEvent>.OnGameEvent(KillEvent EventArgs)
	{
		if (!Networking.IsHost)
			return;

		var DamageEvent = EventArgs.DamageEvent;

		if (!DamageEvent.AttackerPlayerPawn.IsValid()) return;
		if (!DamageEvent.VictimPlayerPawn.IsValid()) return;

		var thisPlayer = PlayerState?.PlayerPawn;
		if (!thisPlayer.IsValid()) return;

		var killerPlayer = GameUtils.GetPlayerFromComponent(DamageEvent.AttackerPlayerPawn);
		var victimPlayer = GameUtils.GetPlayerFromComponent(DamageEvent.VictimPlayerPawn);

		if (!victimPlayer.IsValid()) return;

		if (!killerPlayer.IsValid())
		{
			if (victimPlayer == thisPlayer)
				Deaths++;

			return;
		}

		var isFriendly = killerPlayer.Team == victimPlayer.Team;
		var isSuicide = killerPlayer == victimPlayer;

		if (killerPlayer == thisPlayer)
		{
			if (isFriendly)
			{
				// Killed by friendly/teammate
				Kills--;
				Score += TeamKillScore;
			}
			else if (isSuicide)
			{
				// Killed by suicide
				Kills--;
				AddScore(SuicideScore, "Suicide");
			}
			else
			{
				// Valid kill, add score
				Kills++;
				AddScore(KillScore, "");
			}
		}
		else if (victimPlayer == thisPlayer)
		{
			// Only count as death if this wasn't a team kill
			if (!isFriendly)
			{
				Deaths++;
			}
		}
	}

	void IGameEventHandler<HillCapturedEvent>.OnGameEvent(HillCapturedEvent EventArgs)
	{
		var Team = EventArgs.Team;

		if (HillIndicator.Instance.IsValid())
		{
			Log.Info($"the hill has been capped for {Team} by {EventArgs.PlayerPawns.Count} people");
			HillIndicator.Instance.OnTeamChange(Team);
		}
	}

	void IGameEventHandler<HillWinEvent>.OnGameEvent(HillWinEvent EventArgs)
	{
		var Team = EventArgs.Team;

		if (HillIndicator.Instance.IsValid())
		{
			Log.Info($"the game has been won for {Team}");
			HillIndicator.Instance.OnHillReset();
		}
	}

	void IGameEventHandler<HillCappingEvent>.OnGameEvent(HillCappingEvent EventArgs)
	{
		var Team = EventArgs.Team;
		var CapDelta = EventArgs.CaptureDelta;

		if (HillIndicator.Instance.IsValid())
		{
			HillIndicator.Instance.OnHillCapTick(Team, CapDelta);
		}
	}

	public void OnGameEvent(HillDecayCapEvent EventArgs)
	{
		if (HillIndicator.Instance.IsValid())
		{
			HillIndicator.Instance.Decay(EventArgs.CaptureDelta);
		}
	}

	void IGameEventHandler<ResetScoresEvent>.OnGameEvent(ResetScoresEvent EventArgs)
	{
		Kills = 0;
		Deaths = 0;
		Score = 0;

		ScoreHistory.Clear();

		WasBombPlanter = false;
		HillIndicator.Instance.OnHillReset();
	}

	void IGameEventHandler<RoundCounterIncrementedEvent>.OnGameEvent(RoundCounterIncrementedEvent EventArgs)
	{
		ScoreHistory.Add(Score - ScoreHistory.LastOrDefault());
	}

	void IGameEventHandler<RoundCounterResetEvent>.OnGameEvent(RoundCounterResetEvent EventArgs)
	{
		ScoreHistory.Clear();
	}
}
