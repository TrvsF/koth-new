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

	[Sync] public bool IsReloading { get; set; }
	private bool IsFirstReloadInSequence = false;

	private TimeUntil TimeUntilReload { get; set; }

	private bool LastReload = false;
	protected override void OnUpdate()
	{
		if (!Player.IsValid() || !Player.IsLocallyControlled)
		{
			return;
		}

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

	[Rpc.Broadcast(NetFlags.OwnerOnly)]
	void StartReload()
	{
		if (IsProxy)
		{
			return;
		}

		TimeUntilReload = GetReloadTime();
	}

	[Rpc.Broadcast(NetFlags.OwnerOnly)]
	public void CancelReload()
	{
		if (IsProxy)
		{
			return;
		}

		IsFirstReloadInSequence = true;
		IsReloading = false;
		LastReload = false;
	}

	[Rpc.Broadcast(NetFlags.OwnerOnly)]
	void EndReload()
	{
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
			isDown = false;
			return;
		}

		if (TimeSinceDeployed < PlayerInventory.SwitchCooldown)
		{
			isDown = false;
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
