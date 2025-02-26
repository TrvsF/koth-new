using Sandbox.Events;

namespace KOTH.PlayerExp;

/// <summary>
/// Event for broadcasting exp for players
/// </summary>
/// <param name="Amount">Amount of exp</param>
/// <param name="Origin">Where the exp originated from</param>
public record ExpEvent(int Amount, ExpOrigins Origin) : IGameEvent;

/// <summary>
/// Event for broadcasting player level-up notifications.
/// </summary>
/// <param name="Level">The new level the player has achieved</param>
public record LevelUpEvent(int Level) : IGameEvent;
