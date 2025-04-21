using KOTH.PlayerExp;
using KOTH.UI;
using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Events;
using Sandbox.Utility;
using static Sandbox.PhysicsContact;

namespace KOTH;

public sealed class DamageComponent : Component
{
	public event Action<FDamageTaken> OnDeath;

	[Property, Sync(SyncFlags.FromHost)] public int Health { get; private set; } = 100;
	[Property, Sync(SyncFlags.FromHost)] public int MaxBaseHealth { get; private set; } = 100;
	[Sync(SyncFlags.FromHost)] public Team Team { get; private set; } = Team.Unassigned;
	public bool IsDead => Health < 0f;

	//////////////////////////////////////////////////////////////////////////////////

	public bool IsUbered { get; private set; } = false;
	private TimeSince TimeSinceLastHealFromBeam = 1;
	private TimeSince TimeSinceLastUbered = 1;

	//////////////////////////////////////////////////////////////////////////////////

	const float OverhealFactor = 1.5f;
	private int MaxHealthWithOverheal { get => (MaxBaseHealth * OverhealFactor).CeilToInt(); }

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

	TimeSince TimeSinceHealthDegrade = 0;
	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if (!Networking.IsHost)
		{
			return;
		}

		// if we have overheal slowly drain it
		if (Health > MaxBaseHealth && TimeSinceHealthDegrade > 0.2f && TimeSinceLastHealFromBeam > 0.08f)
		{
			--Health;
			TimeSinceHealthDegrade = 0;
		}

		IsUbered = TimeSinceLastUbered < 0.33f;
	}

	//////////////////////////////////////////////////////////////////////////////////

	public void Initalize(int MaxBaseHealthIn, Team TeamIn)
	{
		Assert.True(Networking.IsHost);

		MaxBaseHealth = MaxBaseHealthIn;
		Health = MaxBaseHealthIn;
		Team = TeamIn;
	}

	//////////////////////////////////////////////////////////////////////////////////

	public void Heal(FHealingRequest Heals)
	{
		Assert.True(Networking.IsHost);

		if (Heals.HealingType == EHealingType.Continuous)
		{
			TimeSinceLastHealFromBeam = 0;
		}

		var MaxHealth = Heals.AllowOverheal ? MaxHealthWithOverheal : MaxBaseHealth;
		var RequestedHealth = Health + Heals.BaseHealing;
		
		var Healing = Heals.BaseHealing;

		if (RequestedHealth > MaxHealth)
		{
			var HealingDone = MaxHealth - Health;
			Healing = HealingDone < 0 ? 0 : HealingDone;
		}

		Health += Healing;

		//////////////////////////////////////////
		// feels right to have this here ¯\_(ツ)_/¯
		FHealingReceived HealingDoneMessage = new()
		{
			TargetPlayerState = GameUtils.GetPlayerState(Heals.TargetPlayerPawn?.Id),
			HealerPlayerState = GameUtils.GetPlayerState(Heals.HealerPlayerPawn?.Id),
			TargetPlayerPawn = Heals.TargetPlayerPawn,
			HealerPlayerPawn = Heals.HealerPlayerPawn,
			Heals = Healing,
			HealingType = Heals.HealingType,
		};

		BroadcastHeals(HealingDoneMessage);
	}

	public void Uber()
	{
		TimeSinceLastUbered = 0;
	}

	public void TakeDamage(FDamageTaken DamageTaken)
	{
		Assert.True(Networking.IsHost);

		if (IsUbered)
		{
			return;
		}

		Health -= DamageTaken.Damage.FloorToInt();
		BroadcastDamage(DamageTaken);

		if (Health <= 0f)
		{
			BroadcastKill(DamageTaken);
			OnDeath?.Invoke(DamageTaken);
		}
	}

	//////////////////////////////////////////////////////////////////////////////////

	[Rpc.Broadcast(NetFlags.HostOnly)]
	private void BroadcastHeals(FHealingReceived HealingDone)
	{
		Scene.Dispatch(new HealingBroadcastEvent(HealingDone));
	}

	[Rpc.Broadcast(NetFlags.HostOnly)]
	private void BroadcastDamage(FDamageTaken DamageTaken)
	{
		Scene.Dispatch(new DamageBroadcastEvent(DamageTaken));
	}

	[Rpc.Broadcast(NetFlags.HostOnly)]
	private void BroadcastKill(FDamageTaken DamageTaken)
	{
		Scene.Dispatch(new KillBroadcastEvent(DamageTaken));
	}

	//////////////////////////////////////////////////////////////////////////////////

	public string GetHealthString()
	{
		return Health.ToString();
	}

	public string GetMaxHealthString()
	{
		return MaxBaseHealth.ToString();
	}

	public bool IsOverhealed()
	{
		return Health > MaxBaseHealth;
	}
}
