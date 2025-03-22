using Sandbox.Events;

namespace KOTH;

// TODO : revisit!!

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

	public void SetCurrentEquipment(Equipment Weapon)
	{
		if (CurrentEquipment.IsValid())
		{
			CurrentEquipment.Holster();
		}

		Weapon.Deploy();
	}
}
