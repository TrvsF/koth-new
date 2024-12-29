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

	[Sync(SyncFlags.FromHost), /*Change(nameof(OnHealthPropertyChanged))*/] public float Health { get; private set; } = 100f;
	[Sync(SyncFlags.FromHost)] public float MaxBaseHealth { get; private set; } = 100f;
	public bool IsDead => Health < 0f;

	private float OverhealFactor = 1.33f;
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

	//////////////////////////////////////////////////////////////////////////////////

	public void SetHealth(float MaxBaseHealthIn)
	{
		Assert.True(Networking.IsHost);

		Log.Info($"setting hp to {MaxBaseHealthIn}");

		// NOTE : this doesn't seem to work for clients??
		// the new syncs are werid..

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
			OwnerPawn.OnKill(DamageTaken);
		}
	}
	
	public void TakeKnockback(Vector3 Knockback)
	{
		GameUtils.GetPlayerFromComponent(this)?.DoKnockback(Knockback);
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
