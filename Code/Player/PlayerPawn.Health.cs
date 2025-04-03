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

		if (Camera.IsValid())
		{
			Camera.GameObject.Root.Destroy();
		}

		// TODO : camera system should have to go thru PlayerState
		if (!IsDummy)
		{
			using (Rpc.FilterInclude(Network.Owner))
			{
				LocalCreateDeathCamera(DamageTaken);
			}
		}

		if (WalkSoundHandle.IsValid())
		{
			WalkSoundHandle.Stop();
			WalkSoundHandle = null;
		}


		BroadcastLocalPlayerDeath(DamageTaken);

		OnDeath?.Invoke();
		GameObject.Root.Destroy();
	}

	[Rpc.Broadcast]
	private void LocalCreateDeathCamera(FDamageTaken DamageTaken)
	{
		Log.Info($"{Scene} {Head} {DamageTaken}");

		CameraUtils.CreateSetDeathCamera(Scene, Head.WorldPosition, DamageTaken);
	}
}
