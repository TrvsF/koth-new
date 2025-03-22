using Sandbox.Events;

namespace KOTH;

public enum EReloadType
{
	None = 0,
	Mag,
	Single,
}

// Reload /////////////////////////////////////////////////////////////
public abstract partial class InputWeaponComponent : EquipmentComponent
{
	[Property, Group(".Reload")] public EReloadType ReloadType { get; set; } = EReloadType.None;
	[Property, Group(".Reload")] public bool ReloadWhileNotActive { get; set; } = false;
	[Property, Group(".Reload")] public float ReloadTime { get; set; } = 1.0f;
	const float FirstTimeReloadFactor = 1.2f;

	private bool _IsReloading;
	[Sync]
	public bool IsReloading
	{
		get => _IsReloading;
		private set
		{
			_IsReloading = value;
			if (!_IsReloading)
			{
				IsFirstReloadInSequence = false;
			}
		}
	}
	private bool IsFirstReloadInSequence = false;

	private TimeUntil TimeUntilReload { get; set; }

	private bool LastReload = false;
	protected override void OnUpdate()
	{
		if (!Player.IsValid())
			return;

		if (!Player.IsLocallyControlled)
			return;

		if (IsReloading && !LastReload)
		{
			StartReload();
			LastReload = true;
		}

		if (CanReload() && !IsReloading)
		{
			IsReloading = true;
			return;
		}

		if (IsReloading)
		{
			if (!Equipment.IsDeployed && !ReloadWhileNotActive)
			{
				CancelReload();
			}

			if (0 > TimeUntilReload)
			{
				EndReload();
				LastReload = false;
			}
		}
	}

	public void TryCancelReload()
	{
		if (Player.IsLocallyControlled)
		{
			CancelReload();
		}
	}

	private bool CanReload()
	{
		return !IsAmmoFull;
	}

	private float GetReloadTime()
	{
		var ReloadTimeOut = ReloadTime;
		if (IsFirstReloadInSequence)
		{
			ReloadTimeOut *= FirstTimeReloadFactor;
		}

		return ReloadTimeOut;
	}

	Dictionary<float, SoundEvent> GetReloadSounds()
	{
		if (!HasAmmo) return EmptyReloadSounds;
		return TimedReloadSounds;
	}

	[Rpc.Broadcast(NetFlags.OwnerOnly)]
	void StartReload()
	{
		if (!IsProxy)
		{
			TimeUntilReload = GetReloadTime();
			Equipment.ViewModel?.ModelRenderer?.Set("b_reload", true);
		}

		// Tags will be better so we can just react to stimuli.
		// Equipment.Owner?.BodyRenderer?.Set("b_reload", true);
	}

	[Rpc.Broadcast(NetFlags.OwnerOnly)]
	void CancelReload()
	{
		if (!IsProxy)
		{
			IsReloading = false;
			LastReload = false;
			Equipment.ViewModel?.ModelRenderer?.Set("b_reload", false);
		}

		// TODO : this doesn't seem to work?
		// Equipment.Owner?.BodyRenderer?.Set("b_reload", false);
	}

	[Rpc.Broadcast(NetFlags.OwnerOnly)]
	void EndReload()
	{
		// Tags will be better so we can just react to stimuli.
		Equipment.ViewModel?.ModelRenderer.Set("b_reload", false);

		if (IsProxy)
		{
			return;
		}

		IsFirstReloadInSequence = false;

		switch (ReloadType)
		{
			case EReloadType.Mag:
				{
					IsReloading = false;
					Ammo = MaxAmmo;
					break;
				}

			case EReloadType.Single:
				{
					Ammo++;
					Ammo = Ammo.Clamp(0, MaxAmmo);

					if (Ammo < MaxAmmo)
					{
						StartReload();
					}
					else
					{
						IsReloading = false;
					}
					break;
				}

			case EReloadType.None:
				break;
		}

		foreach (var kv in GetReloadSounds())
		{
			PlayAsyncSound(kv.Key, kv.Value, () => IsReloading);
		}
	}

	public Dictionary<float, SoundEvent> TimedReloadSounds { get; set; } = new();
	public Dictionary<float, SoundEvent> EmptyReloadSounds { get; set; } = new();

	async void PlayAsyncSound(float delay, SoundEvent snd, Func<bool> playCondition = null)
	{
		await GameTask.DelaySeconds(delay);

		// Can we play this sound?
		if (playCondition != null && !playCondition.Invoke())
			return;

		GameObject?.PlaySound(snd);
	}
}


// damage /////////////////////////////////////////////////////////////
public abstract partial class InputWeaponComponent : EquipmentComponent
{
	[Property, Group(".Damage")] protected int BaseDamage { get; set; } = 100;
	[Property, Group(".Damage")] protected float FireRate { get; set; } = 0.2f;
	[Property, Group(".Damage")] protected float KnockbackStrength { get; set; } = 100f;

	bool IsProjectile { get => this is ProjectileWeaponComponent; }
	public (float BaseDamage, float FireRate, float KnockbackStrength) GetWeaponStats()
	{
		var Damage = BaseDamage;
		var Kb = KnockbackStrength;
		
		if (IsProjectile)
		{
			var ProjectileShooter = (ProjectileWeaponComponent)this;
			var ProjectileComp = ProjectileShooter.ProjectilePrefab.GetComponent<Projectile>();

			if (ProjectileComp.IsValid())
			{
				Damage = ProjectileComp.BaseDamage;
				Kb = ProjectileComp.BaseKnockbackStrength;
			}
		}

		return (Damage, FireRate, Kb);
	}
}

// input //////////////////////////////////////////////////////////////
public abstract partial class InputWeaponComponent : EquipmentComponent
{

	[Property, Category(".Input")] public List<string> InputActions { get; set; } = new() { "Attack1" };

	bool isDown = false;

	protected bool IsDown() => isDown;

	protected virtual void OnInputUpdate()
	{
	}

	TimeSince TimeSinceDeployed = 0;
	protected override void OnFixedUpdate()
	{
		if (!Equipment.IsValid() || !Equipment.Owner.IsValid() || !Equipment.Owner.IsLocallyControlled)
		{
			return;
		}

		if (!Equipment.IsDeployed)
		{
			TimeSinceDeployed = 0;
			return;
		}

		if (TimeSinceDeployed < PlayerInventory.SwitchCooldown)
		{
			return;
		}

		foreach (var action in InputActions)
		{
			isDown = Input.Down(action);
		}
		
		OnInputUpdate();
	}
}

// ammo ///////////////////////////////////////////////////////////////
public abstract partial class InputWeaponComponent : EquipmentComponent
{
	[Property, Group(".Ammo"), Sync] public int Ammo { get; set; } = 99;
	[Property, Group(".Ammo")] public int MaxAmmo { get; set; } = 99;
	[Property, Group(".Ammo")] public bool HasAmmo { get => Ammo > 0; }

	public bool IsAmmoFull => Ammo == MaxAmmo;
}
