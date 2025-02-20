using KOTH.UI;
using Sandbox.Diagnostics;
using Sandbox.Events;

namespace KOTH;

public partial class PlayerPawn : 
	IGameEventHandler<DamageGivenEvent>, 
	IGameEventHandler<DamageTakenEvent>
{
	public float Health => DamageComponent.IsValid ? DamageComponent.Health : -1;
	public float MaxHealth => DamageComponent.IsValid ? DamageComponent.MaxBaseHealth : -1;
	public bool IsAlive => DamageComponent.IsValid && !DamageComponent.IsDead;
	public event Action OnDeath;

	protected override void OnEnabled()
	{
		base.OnEnabled();

		Assert.NotNull(DamageComponent);

		DamageComponent.OnDeath += OnKill;

		// TODO : attempt retry or throw 
	}

	public void OnKill(FDamageTaken DamageTaken)
	{
		Assert.True(Networking.IsHost);

		Inventory.Clear();

		if (DamageTaken.Damage > 56)
		{
			CreateGibs();
		}
		else
		{
			CreateRagdoll();
		}

		if (Camera.IsValid())
		{
			Camera.GameObject.Root.Destroy();
		}

		OnDeath?.Invoke();
	}
}
