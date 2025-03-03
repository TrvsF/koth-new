using KOTH.PlayerExp;
using KOTH.UI;
using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Events;
using static Sandbox.PhysicsContact;

namespace KOTH;

public sealed class DamageComponent : Component
{
	public event Action<FDamageTaken> OnDeath;

	[Property, Sync(SyncFlags.FromHost)] public float Health { get; private set; } = 100f;
	[Property, Sync(SyncFlags.FromHost)] public float MaxBaseHealth { get; private set; } = 100f;
	[Sync(SyncFlags.FromHost)] public Team Team { get; private set; } = Team.Unassigned;
	public bool IsDead => Health < 0f;

	//////////////////////////////////////////////////////////////////////////////////

	const float OverhealFactor = 1.5f;
	const float HealDegradePerSecond = 7f;
	private float MaxHealthWithOverheal { get => MaxBaseHealth * OverhealFactor; }

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

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		// if we have overheal slowly drain it
		if (Health > MaxBaseHealth)
		{
			Health = Math.Max(MaxBaseHealth, Health - (Time.Delta * HealDegradePerSecond));
		}
	}

	//////////////////////////////////////////////////////////////////////////////////

	public void Initalize(float MaxBaseHealthIn, Team TeamIn)
	{
		Assert.True(Networking.IsHost);

		MaxBaseHealth = MaxBaseHealthIn;
		Health = MaxBaseHealthIn;
		Team = TeamIn;
	}

	public void Heal(float Healing, bool AllowOverheal)
	{
		Assert.True(Networking.IsHost);

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
}
