using Sandbox;
using Sandbox.Citizen;
using Sandbox.Diagnostics;
using Sandbox.Events;
using System.Reflection.Metadata.Ecma335;

namespace KOTH;

public record EquipmentDeployedEvent(Equipment Equipment) : IGameEvent;
public record EquipmentHolsteredEvent(Equipment Equipment) : IGameEvent;
public record EquipmentDestroyedEvent(Equipment Equipment) : IGameEvent;

public struct FEquipmentDefinition
{
	public FEquipmentDefinition()
	{
	}

	public string Name { get; set; } = "UNINIT";
	public GameObject ViewmodelPrefab { get; set; } = null;
	public EEquipmentSlot EquipmentSlot { get; set; }
}


// TODO : this could do with a proper look at
public sealed class Equipment : Component, IEquipment, IDescription
{
	internal void BindTag(string tag, Func<bool> predicate) => TagBinder.BindTag(tag, predicate);
	[RequireComponent] public TagBinder TagBinder { get; set; }

	//////////////////////////////////////////////////////////////////////////////////////

	public void Init(FEquipmentDefinition Definition)
	{
		Name = Definition.Name;
		ViewmodelPrefab = Definition.ViewmodelPrefab;
		Slot = Definition.EquipmentSlot;
	}

	[Property] public string Name { get; private set; }
	[Property] public EEquipmentSlot Slot { get; private set; }
	public GameObject ViewmodelPrefab { get; private set; }

	//////////////////////////////////////////////////////////////////////////////////////

	[Property, Group("Components")] public SkinnedModelRenderer ModelRenderer { get; set; }
	[Property, Group("Animation")] public CitizenAnimationHelper.HoldTypes HoldType { get; set; } = CitizenAnimationHelper.HoldTypes.Pistol;
	[Property, Group("Animation")] public CitizenAnimationHelper.Hand Handedness { get; set; } = CitizenAnimationHelper.Hand.Right;
	[Property, Group("Sounds")] public SoundEvent DeploySound { get; set; }
	[Property, Group("GameObjects")] public GameObject Muzzle { get; set; }
	[Property, Group("GameObjects")] public GameObject EjectionPort { get; set; }
	[Property, Group("Mount Points")] public GameObject MountedPrefab { get; set; }

	private PlayerPawn owner;
	public PlayerPawn Owner
	{
		get => owner ??= GameObject.Root.GetComponent<PlayerPawn>(true);
	}

	public InputWeaponComponent GetWeaponComponent()
	{
		return GameObject.GetComponentInChildren<InputWeaponComponent>();
	}

	//////////////////////////////////////////////////////////////////////////////////////

	public string GetAmmoString()
	{
		var WeaponComponent = GameObject.GetComponentInChildren<InputWeaponComponent>();
		if (!WeaponComponent.IsValid())
		{
			return "NA";
		}

		if (WeaponComponent is HealBeamComponent HealBeamComponent)
		{
			return $"{HealBeamComponent.Charge.CeilToInt()}%";
		}

		return WeaponComponent.Ammo.ToString();
	}

	//////////////////////////////////////////////////////////////////////////////////////

	[Sync, Change(nameof(OnIsDeployedPropertyChanged))]
	public bool IsDeployed { get; private set; }
	private bool _wasDeployed { get; set; }
	private bool _hasStarted { get; set; }

	public void UpdateRenderMode(bool force = false)
	{
		var on = force || (Owner.IsValid() && !Owner.IsViewer && IsDeployed);

		if (!Owner.IsValid() && !force)
			on = false;

		ModelRenderer.Enabled = on;
		ModelRenderer.RenderType = on
			? Sandbox.ModelRenderer.ShadowRenderType.On
			: Sandbox.ModelRenderer.ShadowRenderType.ShadowsOnly;
	}

	public ViewModel ViewModel { get; private set; }

	[Rpc.Owner]
	public void Deploy()
	{
		if (IsDeployed)
			return;

		// We must first holster all other equipment items.
		if (Owner.IsValid())
		{
			var equipment = Owner.Inventory.PlayerEquipment.ToList();

			foreach (var item in equipment)
				item.Holster();
		}

		IsDeployed = true;
	}

	[Rpc.Owner]
	public void Holster()
	{
		if (!IsDeployed)
			return;

		IsDeployed = false;
	}

	private void OnIsDeployedPropertyChanged(bool oldValue, bool newValue)
	{
		// Conna: If `OnStart` hasn't been called yet, don't do anything. It'd be nice to have a property on
		// a Component that can indicate this.
		if (!_hasStarted) return;
		UpdateDeployedState();
	}

	private void UpdateDeployedState()
	{
		if (IsDeployed == _wasDeployed)
			return;

		switch (_wasDeployed)
		{
			case false when IsDeployed:
				OnDeployed();
				break;
			case true when !IsDeployed:
				OnHolstered();
				break;
		}

		_wasDeployed = IsDeployed;
	}

	public void ClearViewModel()
	{
		if (ViewModel.IsValid())
			ViewModel.GameObject.Destroy();
	}

	private void CreateViewModel(bool playDeployEffects = true)
	{
		Assert.IsValid(Owner);
		Assert.IsValid(Owner.Camera);
		Assert.IsValid(ViewmodelPrefab);

		ClearViewModel();
		UpdateRenderMode();

		var ViewmodelGameObject = ViewmodelPrefab.Clone(new CloneConfig()
		{
			Transform = new(),
			Parent = Owner.Camera.GameObject,
			StartEnabled = true,
		});

		var ViewModelComponent = ViewmodelGameObject.Components.Get<ViewModel>();
		if (ViewModelComponent == null)
		{
			Log.Warning($"viewmodel component not valid after spawning viewmodel for {Owner}");
			return;
		}

		ViewModelComponent.PlayDeployEffects = playDeployEffects;
		ViewModel = ViewModelComponent;
	}

	protected override void OnStart()
	{
		_wasDeployed = IsDeployed;
		_hasStarted = true;
		if (IsDeployed)
		{
			OnDeployed();
		}
		else
		{
			OnHolstered();
		}
	}

	bool HasCreatedViewModel { get; set; } = false;

	private void OnDeployed()
	{
		// SOMETIMES OWNER ISN'T VALID WHEN WE GET HERE & THAT ISNT GOOD

		if (Owner.IsValid() && Owner.IsViewer)
		{
			CreateViewModel(!HasCreatedViewModel);
		}

		HasCreatedViewModel = true;

		UpdateRenderMode();

		// GOLDEN CHECK MONKEY CODE
		// TODO : make this ASYC!!!!
		//if (Owner.IsValid() && Owner.PlayerState.IsValid() && Owner.PlayerState.LocalStatsSnapshot.IsValid() && Owner.PlayerState.LocalStatsSnapshot.HasGold)
		//{
		//	if (ModelRenderer.IsValid() && ViewModel.IsValid() && ViewModel.ModelRenderer.IsValid())
		//	{
		//		ModelRenderer.Tint = Color.Yellow;
		//		ViewModel.ModelRenderer.Tint = Color.Yellow;
		//	}
		//}

		GameObject.Root.Dispatch(new EquipmentDeployedEvent(this));
	}

	private void OnHolstered()
	{
		UpdateRenderMode();
		ClearViewModel();

		GameObject.Root.Dispatch(new EquipmentHolsteredEvent(this));
	}

	protected override void OnDestroy()
	{
		ClearViewModel();

		// GameObject.Root.Dispatch(new EquipmentDestroyedEvent(this));
	}
}
