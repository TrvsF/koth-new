using Sandbox.Events;

namespace KOTH;

public record HealingRequestEvent(FHealingRequest HealingRequest) : IGameEvent;

public record HealingGivenEvent(FHealingDone HealingRequest) : IGameEvent;

public record FHealingRequest
{
	public PlayerPawn TargetPlayerPawn { get; init; }
	public PlayerPawn AttackerPlayerPawn { get; init; }
	public float BaseHealing { get; init; } = 0f;
	public bool AllowOverheal { get; init; } = false;

	public EHealingType HealingType { get; init; } = EHealingType.Continuous;
	public Vector3 HealingOrigin { get; init; } = Vector3.Zero;

	public RealTimeSince TimeSinceEvent { get; init; } = 0;

	public bool IsValid()
	{
		return TargetPlayerPawn.IsValid();
	}
}

public record FHealingDone
{
	public PlayerPawn TargetPlayerPawn { get; init; }
	public PlayerPawn HealerPlayerPawn { get; init; }
	public float Healing { get; init; } = 0f;
}

public enum EHealingType
{
	Continuous = 0,
	Projectile,
}
