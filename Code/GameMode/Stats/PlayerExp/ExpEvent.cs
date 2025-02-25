using Sandbox.Events;

namespace KOTH.PlayerExp;

/// <summary>
/// Event for broadcasting exp for players
/// </summary>
/// <param name="Amount">Amount of exp</param>
/// <param name="Origin">Where the exp originated from</param>
public record ExpEvent(int Amount, ExpOrigins Origin) : IGameEvent;
