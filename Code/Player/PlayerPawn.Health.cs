using KOTH.PlayerExp;
using KOTH.UI;
using Sandbox.Diagnostics;
using Sandbox.Events;
using System.ComponentModel.DataAnnotations;

namespace KOTH;

public partial class PlayerPawn
{
	[Property] Material UberMaterial { get; set; }

	public int Health => DamageComponent.IsValid ? DamageComponent.Health : -1;
	public int MaxHealth => DamageComponent.IsValid ? DamageComponent.MaxBaseHealth : -1;
	public bool IsAlive => DamageComponent.IsValid && !DamageComponent.IsDead;
	public event Action OnDeath;

	protected override void OnEnabled()
	{
		base.OnEnabled();

		Assert.NotNull(DamageComponent);

		DamageComponent.OnDeath += OnKill;
	}

	public void OnKill(FDamageTaken DamageTaken)
	{
		Assert.True(Networking.IsHost);

		Inventory.Clear();

		var LookAtLocation = DamageTaken.AssumedAttackerPlayerPawn.IsValid() ? DamageTaken.AssumedAttackerPlayerPawn.WorldPosition : Vector3.Zero;
		using (Rpc.FilterInclude(Network.Owner))
		{
			// CreateDeathCamera(DamageTaken);
		}

		BroadcastLocalPlayerDeath(DamageTaken);

		if (Camera.IsValid())
		{
			Camera.GameObject.Root.Destroy();
		}

		OnDeath?.Invoke();
		GameObject.Root.Destroy();
	}

	[Rpc.Broadcast]
	private void CreateDeathCamera(FDamageTaken DamageTaken)
	{
		var CameraObject = Scene.CreateObject();
		CameraObject.Name = "DEATHCAMERA";
		CameraObject.NetworkMode = NetworkMode.Never;

		if (!DamageTaken.AttackerPlayerState.IsValid())
		{
			return;
		}

		var LookAtLocation = DamageTaken.AssumedAttackerPlayerPawn.IsValid() ? DamageTaken.AssumedAttackerPlayerPawn.WorldPosition : Vector3.Zero;
		var Health = DamageTaken.AssumedAttackerPlayerPawn.IsValid() ? DamageTaken.AssumedAttackerPlayerPawn.Health : 0;

		CameraObject.WorldPosition = WorldPosition + (WorldRotation.Forward * 100f) + (WorldRotation.Up * 50f);
		CameraObject.WorldRotation = Rotation.LookAt(LookAtLocation - CameraObject.WorldPosition);

		CameraObject.Components.Create<ScreenPanel>();
		CameraObject.Components.Create<PlayerDeathHUD>();

		FDeathCameraData DeathCameraData = new()
		{
			KillerName = DamageTaken.AttackerPlayerState.SteamName,
			KillerPlayerState = DamageTaken.AttackerPlayerState,
			KillerHealth = Health,
		};

		PlayerDeathHUD.Instance.SetData(DeathCameraData);

		var CameraComp = CameraObject.Components.Create<CameraComponent>();
		CameraComp.Priority = 101;
		CameraUtils.CurrentCamera = CameraComp;
	}
}
