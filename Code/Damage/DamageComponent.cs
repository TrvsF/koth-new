using KOTH.UI;
using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Events;
using static Sandbox.PhysicsContact;

namespace KOTH;

public sealed class DamageComponent : Component
{
	[Property, HostSync] public bool IsGodMode { get; private set; } = false;

	//////////////////////////////////////////////////////////////////////////////////

	public event Action<FDamageTaken> OnDeath;

	[Property, Sync(SyncFlags.FromHost)] public float Health { get; private set; } = 100f;
	[Property, Sync(SyncFlags.FromHost)] public float MaxBaseHealth { get; private set; } = 100f;
	public bool IsDead => Health < 0f;

	private float OverhealFactor = 1.33f;
	private float MaxHealthWithOverheal { get => MaxBaseHealth * OverhealFactor; }

	//////////////////////////////////////////////////////////////////////////////////

	// HACK : TODO REWORK

	protected override void OnAwake()
	{
		base.OnAwake();

		var OwnerPawn = GameObject.Root.Components.Get<PlayerPawn>();

		// HACK : even worse, health is being overwritten because Sync(SyncFlags.FromHost)
		// will still accept values from the owning pawn even if not the host
		if (OwnerPawn.IsValid() && OwnerPawn.PlayerPawnDefinition.IsValid())
		{
			MaxBaseHealth = OwnerPawn.PlayerPawnDefinition.CharacterDefinition.MaxHealth;
			Health = OwnerPawn.PlayerPawnDefinition.CharacterDefinition.MaxHealth;
		}
	}

	//////////////////////////////////////////////////////////////////////////////////

	private float HealDegradeFactor = 7f;
	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		// if we have overheal slowly drain it
		if (Health > MaxBaseHealth)
		{
			Health = Math.Max(MaxBaseHealth, Health - (Time.Delta * HealDegradeFactor));
		}
	}

	//////////////////////////////////////////////////////////////////////////////////

	public void SetHealth(float MaxBaseHealthIn)
	{
		Assert.True(Networking.IsHost);

		MaxBaseHealth = MaxBaseHealthIn;
		Health = MaxBaseHealthIn;
	}

	TimeSince TimeSinceLastHeal = new();
	public void Heal(float Healing, bool AllowOverheal)
	{
		Assert.True(Networking.IsHost);

		TimeSinceLastHeal = 0;

		if (!AllowOverheal && Health >= MaxBaseHealth)
		{
			return; // NOTE : early return
		}

		var MaxHealth = AllowOverheal ? MaxHealthWithOverheal : MaxBaseHealth;
		Health = Math.Min(MaxHealth, Health + Healing);
	}

	//////////////////////////////////////////////////////////////////////////////////

	public void TakeDamage(FDamageTaken DamageTaken)
	{
		Assert.True(Networking.IsHost);

		Health -= DamageTaken.Damage;
		BroadcastDamage(DamageTaken);

		if (Health <= 0f)
		{
			BroadcastKill(DamageTaken);
			OnDeath?.Invoke(DamageTaken);
		}
	}

	//////////////////////////////////////////////////////////////////////////////////

	[Rpc.Broadcast(NetFlags.HostOnly)]
	private void BroadcastDamage(FDamageTaken DamageTaken)
	{
		GameObject.Root.Dispatch(new DamageTakenEvent(DamageTaken));
	}

	[Rpc.Broadcast(NetFlags.HostOnly)]
	private void BroadcastKill(FDamageTaken DamageTaken)
	{
		Scene.Dispatch(new KillEvent(DamageTaken));
	}

	//////////////////////////////////////////////////////////////////////////////////

	public string GetHealthString()
	{
		return Health.CeilToInt().ToString();
	}

	public string GetMaxHealthString()
	{
		return MaxBaseHealth.CeilToInt().ToString();
	}

	//////////////////////////////////////////////////////////////////////////////////

	[Rpc.Broadcast(NetFlags.HostOnly)]
	public void SetGodmode(bool GodMode)
	{
		IsGodMode = GodMode;
	}
}
