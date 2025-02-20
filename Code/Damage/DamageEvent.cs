using Sandbox.Events;

namespace KOTH;

public record DamageRequestEvent(FDamageRequest DamageRequest) : IGameEvent;

public record DamageTakenEvent(FDamageTaken DamageEvent) : IGameEvent;
public record DamageGivenEvent(FDamageTaken DamageEvent) : IGameEvent;
public record KillEvent(FDamageTaken DamageEvent) : IGameEvent;

// out --------------------------
public record FDamageRequest
{
	// TODO : constructor for this bunch
	public EDamageType DamageType { get; init; } = EDamageType.HitScan;
	public DamageComponent TargetDamageComponent { get; init; } = null;

	public PlayerPawn AttackerPlayerPawn { get; init; } = null;
	public PlayerPawn TargetPlayerPawn { get; set; } = null;

	public Vector3 DamageOrigin { get; init; } = Vector3.Zero;
	public Vector3 TargetOrigin { get; set; } = Vector3.Zero;

	public float BaseDamage { get; init; } = 0f;
	public float BaseKnockbackStrength { get; init; } = 0f;

	public EDamageFalloffType DamageFalloffType { get; init; } = EDamageFalloffType.Falloff;
	public float MaxFalloffDistance { get; init; } = 300f;
	public bool DirectImpact { get; init; } = false;

	public bool DoesLessSelfDamage { get; init; } = false;

	 RealTimeSince TimeSinceEvent { get; init; } = 0;

	public bool IsValid()
	{
		return TargetDamageComponent.IsValid();
	}
}

// in --------------------------
public record FDamageTaken
{
	public PlayerPawn VictimPlayerPawn { get; init; }
	public PlayerPawn AttackerPlayerPawn { get; init; }
	public float Damage { get; init; } = 0f;
	public Vector3 DamageLocation { get; init; } = Vector3.Zero;

	public RealTimeSince TimeSinceEvent { get; init; } = 0;
}

// --------------------------
public enum EDamageType
{
	HitScan = 0,
	Projectile,
	Melee,
}

public enum EDamageFalloffType
{
	None,
	Falloff,
	Rampup,
}
