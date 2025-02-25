using KOTH;
using Sandbox.Events;

public record PlayerConnectedEvent(PlayerState PlayerState) : IGameEvent;
public record LocalPlayerSpawnedEvent(PlayerPawn Player) : IGameEvent;
public record LocalPlayerDiedEvent() : IGameEvent;
public record ResetScoresEvent : IGameEvent;

// TODO : either do this for everything or nothing
/// <summary>
/// Dispatches a <see cref="ResetScoresEvent"/> when this state is entered.
/// </summary>
public sealed class ResetScores : Component,
	IGameEventHandler<EnterStateEvent>
{
	void IGameEventHandler<EnterStateEvent>.OnGameEvent(EnterStateEvent eventArgs)
	{
		Scene.Dispatch(new ResetScoresEvent());
	}
}
