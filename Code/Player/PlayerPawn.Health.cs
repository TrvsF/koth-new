using KOTH.PlayerExp;
using KOTH.UI;
using Sandbox.Diagnostics;
using Sandbox.Events;

namespace KOTH;

public partial class PlayerPawn
{
	[Property] Material UberMaterial { get; set; }

	public float Health => DamageComponent.IsValid ? DamageComponent.Health : -1;
	public float MaxHealth => DamageComponent.IsValid ? DamageComponent.MaxBaseHealth : -1;
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

		BroadcastLocalPlayerDeath(DamageTaken);

		if (Camera.IsValid())
		{
			Camera.GameObject.Root.Destroy();
		}

		OnDeath?.Invoke();
		GameObject.Root.Destroy();
	}
}
