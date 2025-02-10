using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Events;

namespace KOTH;

public partial class PlayerInventory : Component
{
	[RequireComponent] PlayerPawn Player { get; set; }

	public IEnumerable<Equipment> PlayerEquipment => Player.Components.GetAll<Equipment>(FindMode.EverythingInSelfAndDescendants);

	[Property] public GameObject WeaponGameObject { get; set; } // weapon holder
	[Property] public bool CanUnequipCurrentWeapon { get; set; } = false;

	public GameObject CurrentWeaponGameObject { get => Current.GameObject; }
	private Equipment Current => Player.CurrentEquipment;

	public void Clear()
	{
		Assert.True(Networking.IsHost);

		foreach (var Weapon in PlayerEquipment)
		{
			Weapon.ViewModel?.Destroy();
			Weapon.GameObject.Destroy();
			Weapon.Enabled = false;
		}
	}

	protected override void OnUpdate()
	{
		if (!Player.IsLocallyControlled)
			return;

		foreach (var slot in Enum.GetValues<EEquipmentSlot>())
		{
			if (slot == EEquipmentSlot.Undefined)
				continue;

			if (!Input.Pressed($"Slot{(int)slot}"))
				continue;

			SwitchToSlot(slot);
			return;
		}

		var MouseWheelInput = Input.MouseWheel;
		if (MouseWheelInput.y == 0f) return;

		var availableWeapons = PlayerEquipment.OrderBy(x => x.Slot).ToList();
		if (availableWeapons.Count == 0)
			return;

		var currentSlot = 0;
		for (var index = 0; index < availableWeapons.Count; index++)
		{
			var weapon = availableWeapons[index];
			if (!weapon.IsDeployed)
				continue;

			currentSlot = index;
			break;
		}

		var slotDelta = MouseWheelInput.y > 0f ? 1 : -1;
		currentSlot += slotDelta;

		if (currentSlot < 0)
			currentSlot = availableWeapons.Count - 1;
		else if (currentSlot >= availableWeapons.Count)
			currentSlot = 0;

		var weaponToSwitchTo = availableWeapons[currentSlot];
		if (weaponToSwitchTo == Current)
			return;

		Switch(weaponToSwitchTo);
	}

	public void HolsterCurrent()
	{
		Assert.True(!IsProxy || Networking.IsHost);
		Player.SetCurrentEquipment(null);
	}

	public void SwitchToSlot(EEquipmentSlot slot)
	{
		Assert.True(!IsProxy || Networking.IsHost);

		var equipment = PlayerEquipment
			.Where(x => x.Slot == slot)
			.ToArray();

		if (equipment.Length == 0)
			return;

		if (equipment.Length == 1 && Current == equipment[0] && CanUnequipCurrentWeapon)
		{
			HolsterCurrent();
			return;
		}

		var index = Array.IndexOf(equipment, Current);
		Switch(equipment[(index + 1) % equipment.Length]);
	}

	const double SwitchCooldown = 0.15;
	TimeSince TimeSinceLastSwitch = 0;
	public void Switch(Equipment equipment)
	{
		Assert.True(!IsProxy || Networking.IsHost);

		if (!PlayerEquipment.Contains(equipment))
			return;

		if (TimeSinceLastSwitch < SwitchCooldown)
		{
			return;
		}

		TimeSinceLastSwitch = 0;
		Player.SetCurrentEquipment(equipment);
	}

	public void RemoveWeapon(Equipment equipment)
	{
		Assert.True(Networking.IsHost);

		if (!PlayerEquipment.Contains(equipment)) return;

		if (Current == equipment)
		{
			var otherEquipment = PlayerEquipment.Where(x => x != equipment);
			var orderedBySlot = otherEquipment.OrderBy(x => x.Slot);
			var targetWeapon = orderedBySlot.FirstOrDefault();

			if (targetWeapon.IsValid())
				Switch(targetWeapon);
		}

		equipment.GameObject.Destroy();
		equipment.Enabled = false;
	}

	public void Give(EquipmentResource EquipmentResource, bool MakeActive = true)
	{
		Assert.NotNull(EquipmentResource);
		Assert.IsValid(EquipmentResource.WorldPrefab);
		Assert.IsValid(EquipmentResource.ViewModelPrefab);
		Assert.IsValid(Player);

		var EquipmentObject = EquipmentResource.WorldPrefab.Clone(new CloneConfig()
		{
			Transform = new(),
			Parent = WeaponGameObject
		});

		var EquipmentComponent = EquipmentObject.Components.Get<Equipment>(FindMode.EverythingInSelfAndDescendants);
		if (EquipmentComponent == null)
		{
			Log.Warning($"Failed to correctly spawn equipment on player {Player}");
			return;
		}

		// NOTE : loading data when spawning an object is a common paradime, need to understand
		// the best way to do this & have a nice way to repeat...

		FEquipmentDefinition EquipmentComponentStruct = new();
		EquipmentComponentStruct.Name = EquipmentResource.Name;
		EquipmentComponentStruct.ViewmodelPrefab = EquipmentResource.ViewModelPrefab;
		EquipmentComponentStruct.EquipmentSlot = EquipmentResource.Slot;
		EquipmentComponent.Init(EquipmentComponentStruct);

		EquipmentObject.NetworkSpawn(Player.Network.Owner);

		if (MakeActive)
		{
			Player.SetCurrentEquipment(EquipmentComponent);
		}
	}

	public bool HasInSlot(EEquipmentSlot slot)
	{
		return PlayerEquipment.Any(weapon => weapon.Enabled && weapon.Slot == slot);
	}
}
