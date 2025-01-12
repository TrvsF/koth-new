using Sandbox.Events;

namespace KOTH;

public abstract class InputWeaponComponent : EquipmentComponent
{
	[Property, Group("Reload")] public EReloadType ReloadType { get; set; } = EReloadType.None;
	[Property, Group("Reload")] public bool ReloadWhileNotActive { get; set; } = false;
	[Property, Group("Reload")] public float ReloadTime { get; set; } = 1.0f;
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

	protected override void OnUpdate()
	{
		if (!Player.IsValid())
			return;

		if (!Player.IsLocallyControlled)
			return;

		if (CanReload() && !IsReloading)
		{
			IsFirstReloadInSequence = true;
			StartReload();
		}

		if (!Equipment.IsDeployed && !ReloadWhileNotActive)
		{
			CancelReload();
		}

		if (IsReloading && TimeUntilReload)
		{
			EndReload();
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
		return !IsReloading && !IsAmmoFull;
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
			IsReloading = true;
			TimeUntilReload = GetReloadTime();
		}

		// Tags will be better so we can just react to stimuli.
		Equipment.ViewModel?.ModelRenderer?.Set("b_reload", true);
		// Equipment.Owner?.BodyRenderer?.Set("b_reload", true);
	}

	[Rpc.Broadcast(NetFlags.OwnerOnly)]
	void CancelReload()
	{
		if (!IsProxy)
			IsReloading = false;

		// TODO : this doesn't seem to work?
		Equipment.ViewModel?.ModelRenderer?.Set("b_reload", false);
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

	////////////////////////////////////////////////////////////////////////
	
	[Property, Group("Ammo"), Sync] public int Ammo { get; set; } = 0;
	[Property, Group("Ammo")] public int MaxAmmo { get; set; } = 30;
	[Property, Group("Ammo")] public bool HasAmmo => Ammo > 0;

	public bool IsAmmoFull => Ammo == MaxAmmo;

	////////////////////////////////////////////////////////////////////////

	[Property, Category("Base")] public List<string> InputActions { get; set; } = new() { "Attack1" };
	[Property, Category("Base")] public bool RequiresAllInputActions { get; set; }
	[Property, Category("Base")] public Action<InputWeaponComponent> OnInputAction { get; set; }

	bool isDown = false;

	protected bool IsDown() => isDown;

	protected virtual void OnInput()
	{
		if (CanReload())
		{
			StartReload();
		}
	}

	protected virtual void OnInputUp()
	{
	}

	protected virtual void OnInputDown()
	{
	}

	protected virtual void OnInputUpdate()
	{
	}

	protected override void OnFixedUpdate()
	{
		if (!Equipment.IsValid())
			return;

		// Don't execute weapon components on weapons that aren't deployed.
		if (!Equipment.IsDeployed)
			return;

		if (!Equipment.Owner.IsValid())
			return;

		// We only care about input actions coming from the owning object.
		if (!Equipment.Owner.IsLocallyControlled)
			return;

		OnInputUpdate();

		bool matched = false;

		foreach (var action in InputActions)
		{
			var down = Input.Down(action);

			if (RequiresAllInputActions && !down)
			{
				matched = false;
				break;
			}
			if (down)
			{
				matched = true;
			}
		}

		if (matched)
		{
			OnInput();
			OnInputAction?.Invoke(this);

			if (!isDown)
			{
				OnInputDown();
				isDown = true;
			}
		}
		else
		{
			if (isDown)
			{
				OnInputUp();
				isDown = false;
			}
		}
	}
}
