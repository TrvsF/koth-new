using KOTH.UI;
using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Events;
using static Sandbox.PhysicsContact;

namespace KOTH;

public sealed class DamageComponent : Component
{
	[Property, HostSync] public bool IsGodMode { get; private set; } = false;
	[Property] public Action<float, float> OnHealthChanged { get; set; }

	//////////////////////////////////////////////////////////////////////////////////

	[HostSync, /*Change(nameof(OnHealthPropertyChanged))*/] public float Health { get; private set; } = 100f;
	[HostSync] public float MaxBaseHealth { get; private set; } = 125f;
	public bool IsDead => Health < 0f;

	private float OverhealFactor = 1.3f;
	private float MaxHealthWithOverheal { get => MaxBaseHealth * OverhealFactor; }

	//////////////////////////////////////////////////////////////////////////////////

	// HACK : TODO REWORK

	private PlayerPawn OwnerPawn;
	protected override void OnAwake()
	{
		base.OnAwake();

		OwnerPawn = GameObject.Root.Components.Get<PlayerPawn>();
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

	public void TakeDamage(FDamageTaken DamageTaken)
	{
		Assert.True(Networking.IsHost);

		BroadcastDamage(DamageTaken);

		if (IsGodMode) return;

		Health -= DamageTaken.Damage;

		if (Health > 0f) return;

		BroadcastKill(DamageTaken);

		// TODO : HACK
		OwnerPawn.OnKill();
	}
	
	public void TakeKnockback(Vector3 Knockback)
	{
		GameUtils.GetPlayerFromComponent(this)?.DoKnockback(Knockback);
	}

	//////////////////////////////////////////////////////////////////////////////////

	[Broadcast(NetPermission.HostOnly)]
	private void BroadcastDamage(FDamageTaken DamageTaken)
	{
		GameObject.Root.Dispatch(new DamageTakenEvent(DamageTaken));
	}

	[Broadcast(NetPermission.HostOnly)]
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

	[Broadcast(NetPermission.HostOnly)]
	public void SetGodmode(bool GodMode)
	{
		IsGodMode = GodMode;
	}
}
