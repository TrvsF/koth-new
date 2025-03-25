using Sandbox.Events;

namespace KOTH;

public record HealingRequestEvent(FHealingRequest HealingRequest) : IGameEvent;
public record HealingBroadcastEvent(FHealingReceived HealingRequest) : IGameEvent;

public record FHealingRequest
{
	public DamageComponent TargetDamageComponent { get; init; }
	public PlayerPawn TargetPlayerPawn { get; init; }
	public PlayerPawn HealerPlayerPawn { get; init; }
	public int BaseHealing { get; init; } = 0;
	public bool AllowOverheal { get; init; } = false;

	public EHealingType HealingType { get; init; } = EHealingType.Continuous;
	public Vector3 HealingOrigin { get; init; } = Vector3.Zero;

	public RealTimeSince TimeSinceEvent { get; init; } = 0;

	public bool IsValid()
	{
		return TargetPlayerPawn.IsValid();
	}
}

public record FHealingReceived
{
	public PlayerPawn TargetPlayerPawn { get; init; }
	public PlayerPawn HealerPlayerPawn { get; init; }
	public PlayerState TargetPlayerState { get; init; }
	public PlayerState HealerPlayerState { get; init; }
	public int Heals { get; init; } = 0;
	public EHealingType HealingType { get; init; } = EHealingType.Continuous;
}

public enum EHealingType
{
	Continuous = 0,
	OneOff,
}
