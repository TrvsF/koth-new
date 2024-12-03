using Sandbox.Events;

namespace KOTH;

public partial class PlayerPawn :
	IGameEventHandler<EquipmentDeployedEvent>,
	IGameEventHandler<EquipmentHolsteredEvent>
{
	[Property, ReadOnly] public Equipment CurrentEquipment { get; private set; }

	void IGameEventHandler<EquipmentDeployedEvent>.OnGameEvent(EquipmentDeployedEvent eventArgs)
	{
		CurrentEquipment = eventArgs.Equipment;
	}

	void IGameEventHandler<EquipmentHolsteredEvent>.OnGameEvent(EquipmentHolsteredEvent eventArgs)
	{
		if (eventArgs.Equipment == CurrentEquipment)
			CurrentEquipment = null;
	}

	[Rpc.Owner]
	private void SetCurrentWeapon(Equipment equipment)
	{
		SetCurrentEquipment(equipment);
	}

	[Rpc.Owner]
	private void ClearCurrentWeapon()
	{
		CurrentEquipment?.Holster();
	}

	public void Holster()
	{
		if (IsProxy)
		{
			if (Networking.IsHost)
				ClearCurrentWeapon();

			return;
		}

		CurrentEquipment?.Holster();
	}

	public TimeSince TimeSinceWeaponDeployed { get; private set; }

	public void SetCurrentEquipment(Equipment Weapon)
	{
		if (IsProxy)
		{
			if (Networking.IsHost)
				SetCurrentWeapon(Weapon);

			return;
		}

		TimeSinceWeaponDeployed = 0;

		if (CurrentEquipment.IsValid())
		{
			CurrentEquipment.Holster();
		}
		Weapon.Deploy();
	}

	public void ClearViewModel()
	{
		foreach (var Weapon in Inventory.Equipment)
		{
			Weapon.ClearViewModel();
		}
	}

	public void CreateViewModel(bool playDeployEffects = true)
	{
		var Weapon = CurrentEquipment;
		if (Weapon.IsValid())
			Weapon.CreateViewModel(playDeployEffects);
	}
}
