using Sandbox.Events;

namespace KOTH;

public record HillCapturedEvent(List<PlayerPawn> PlayerPawns, Team Team, Hill Hill) : IGameEvent;
public record HillWinEvent(Team Team, Hill Hill) : IGameEvent;
public record HillCappingEvent(Team Team, float CaptureDelta, Hill Hill) : IGameEvent;
public record HillDecayCapEvent(float CaptureDelta, Hill Hill) : IGameEvent;

[Title("Hill Captured Event")]
public class HillCapturedEventComponent : GameEventComponent<HillCapturedEvent> { }

[Title("Hill Won Event")]
public class HillWinEventComponent : GameEventComponent<HillWinEvent> { }

[Title("Hill Capping Event")]
public class HillCappingEventComponent : GameEventComponent<HillCappingEvent> { }

[Title("Hill Decay Event")]
public class HillDecayCapEventComponent : GameEventComponent<HillDecayCapEvent> { }
