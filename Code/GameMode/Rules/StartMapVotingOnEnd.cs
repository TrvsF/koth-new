using Sandbox.Events;

namespace KOTH;

/// <summary>
/// Show a warning message when entering this state, then throw everyone back to the menu when this state ends.
/// </summary>
public sealed class StartMapVotingOnEnd : Component,
	IGameEventHandler<EnterStateEvent>
{
	void IGameEventHandler<EnterStateEvent>.OnGameEvent(EnterStateEvent eventArgs)
	{
		// Start the vote system
		MapVoteSystem.Create();
	}
}
