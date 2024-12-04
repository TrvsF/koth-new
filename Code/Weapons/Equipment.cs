using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Events;

namespace KOTH;

public record EquipmentDeployedEvent(Equipment Equipment) : IGameEvent;
public record EquipmentHolsteredEvent(Equipment Equipment) : IGameEvent;
public record EquipmentDestroyedEvent(Equipment Equipment) : IGameEvent;

/// <summary>
/// An equipment component.
/// </summary>
public partial class Equipment : Component, Component.INetworkListener, IEquipment, IDescription
{
	internal void BindTag(string tag, Func<bool> predicate) => TagBinder.BindTag(tag, predicate);
	[RequireComponent] public TagBinder TagBinder { get; set; }

	[Property, Group("Resources")] public EquipmentResource Resource { get; set; }
	[Property, Group("Components")] public SkinnedModelRenderer ModelRenderer { get; set; }
	[Property, Group("Animation")] protected AnimationHelper.HoldTypes HoldType { get; set; } = AnimationHelper.HoldTypes.Rifle;
	[Property, Group("Animation")] public AnimationHelper.Hand Handedness { get; set; } = AnimationHelper.Hand.Right;
	[Property, Group("Sounds")] public SoundEvent DeploySound { get; set; }
	[Property, Group("Movement")] public float SpeedPenalty { get; set; } = 0f;
	[Property, Group("GameObjects")] public GameObject Muzzle { get; set; }
	[Property, Group("GameObjects")] public GameObject EjectionPort { get; set; }
	[Property, Group("Mount Points")] public GameObject MountedPrefab { get; set; }
	[Property, Group("UI")] public bool UseCrosshair { get; set; } = true;


	[HostSync] public Guid OwnerId { get; set; }
	private PlayerPawn owner;
	public PlayerPawn Owner
	{
		get => owner ??= Scene.Directory.FindComponentByGuid(OwnerId) as PlayerPawn;
	}

	string IDescription.DisplayName => Resource.Name;

	[Sync, Change(nameof(OnIsDeployedPropertyChanged))]
	public bool IsDeployed { get; private set; }
	private bool _wasDeployed { get; set; }
	private bool _hasStarted { get; set; }

	[DeveloperCommand("Toggle View Model", "Visuals")]
	private static void ToggleViewModel()
	{
		var ViewerPlayerPawn = PlayerState.Local.PlayerPawn;

		ViewerPlayerPawn.CurrentEquipment.ViewModel.ModelRenderer.Enabled = !ViewerPlayerPawn.CurrentEquipment.ViewModel.ModelRenderer.Enabled;
		ViewerPlayerPawn.CurrentEquipment.ViewModel.Arms.Enabled = !ViewerPlayerPawn.CurrentEquipment.ViewModel.Arms.Enabled;
	}

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

	private ViewModel viewModel;

	public ViewModel ViewModel
	{
		get => viewModel;
		set
		{
			viewModel = value;

			if (viewModel.IsValid())
			{
				viewModel.Equipment = this;
			}
		}
	}

	void INetworkListener.OnDisconnected(Connection connection)
	{
		if (!Networking.IsHost)
			return;

		if (!Resource.DropOnDisconnect)
			return;

		var player = GameUtils.PlayerPawns.FirstOrDefault(x => x.Network.Owner == connection);
		if (!player.IsValid()) return;
	}

	[Rpc.Owner]
	public void Deploy()
	{
		if (IsDeployed)
			return;

		// We must first holster all other equipment items.
		if (Owner.IsValid())
		{
			var equipment = Owner.Inventory.Equipment.ToList();

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

	public virtual AnimationHelper.HoldTypes GetHoldType()
	{
		return HoldType;
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

	public void CreateViewModel(bool playDeployEffects = true)
	{
		Assert.IsValid(Owner);
		Assert.IsValid(Resource);

		ClearViewModel();
		UpdateRenderMode();

		if (Resource.ViewModelPrefab.IsValid())
		{
			// Create the equipment prefab and put it on the equipment gameobject.
			var viewModelGameObject = Resource.ViewModelPrefab.Clone(new CloneConfig()
			{
				Transform = new(),
				Parent = Owner.Boom,
				StartEnabled = true,
			});

			var ViewModelComponent = viewModelGameObject.Components.Get<ViewModel>();
			ViewModelComponent.PlayDeployEffects = playDeployEffects;

			// equipment needs to know about the ViewModel
			ViewModel = ViewModelComponent;

			viewModelGameObject.BreakFromPrefab();
		}

		if (!playDeployEffects)
		{
			return;
		}

		if (DeploySound is null)
		{
			return;
		}

		var Sound = Sandbox.Sound.Play(DeploySound, WorldPosition);
		if (!Sound.IsValid())
		{
			return; 
		}

		Sound.ListenLocal = !IsProxy;
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

	protected virtual void OnDeployed()
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

	protected virtual void OnHolstered()
	{
		UpdateRenderMode();
		ClearViewModel();

		GameObject.Root.Dispatch(new EquipmentHolsteredEvent(this));
	}

	protected override void OnDestroy()
	{
		ClearViewModel();

		GameObject.Root.Dispatch(new EquipmentDestroyedEvent(this));
	}
}
