using Sandbox.Events;

namespace KOTH;

public enum EReloadType
{
	Mag,
	Single,
	None,
}

[Title("Reload"), Group("Weapon Components")]
public partial class ReloadWeaponComponent : InputWeaponComponent,
	IGameEventHandler<EquipmentHolsteredEvent>
{
	[Property] public EReloadType ReloadType { get; set; } = EReloadType.None;
	[Property] public bool ReloadWhileNotActive { get; set; } = false;
	[Property] public float ReloadTime { get; set; } = 1.0f;
	const float FirstTimeReloadFactor = 1.2f;

	[Property] public AmmoComponent AmmoComponent { get; set; }

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


	protected override void OnInput()
	{
		if (CanReload())
		{
			StartReload();
		}
	}

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

	void IGameEventHandler<EquipmentHolsteredEvent>.OnGameEvent(EquipmentHolsteredEvent eventArgs)
	{

	}

	public void TryCancelReload()
	{
		if (Player.IsLocallyControlled)
		{
			CancelReload();
		}
	}

	bool CanReload()
	{
		return !IsReloading && AmmoComponent.IsValid() && !AmmoComponent.IsFull;
	}

	float GetReloadTime()
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
		if (!AmmoComponent.HasAmmo) return EmptyReloadSounds;
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
		if (!IsProxy)
		{
			IsFirstReloadInSequence = false;

			switch (ReloadType)
			{
				case EReloadType.Mag:
					{
						IsReloading = false;
						AmmoComponent.Ammo = AmmoComponent.MaxAmmo;
						break;
					}

				case EReloadType.Single:
					{
						AmmoComponent.Ammo++;
						AmmoComponent.Ammo = AmmoComponent.Ammo.Clamp(0, AmmoComponent.MaxAmmo);

						if (AmmoComponent.Ammo < AmmoComponent.MaxAmmo)
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

			///////////////////////////////////////////

			foreach (var kv in GetReloadSounds())
			{
				// Play this sound after a certain time but only if we're reloading.
				PlayAsyncSound(kv.Key, kv.Value, () => IsReloading);
			}
		}

		// Tags will be better so we can just react to stimuli.
		Equipment.ViewModel?.ModelRenderer.Set("b_reload", false);
	}

	[Property] public Dictionary<float, SoundEvent> TimedReloadSounds { get; set; } = new();
	[Property] public Dictionary<float, SoundEvent> EmptyReloadSounds { get; set; } = new();

	async void PlayAsyncSound(float delay, SoundEvent snd, Func<bool> playCondition = null)
	{
		await GameTask.DelaySeconds(delay);

		// Can we play this sound?
		if (playCondition != null && !playCondition.Invoke())
			return;

		GameObject?.PlaySound(snd);
	}
}
