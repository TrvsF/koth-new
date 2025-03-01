using KOTH.PlayerExp;
using KOTH.UI;
using Sandbox.Diagnostics;
using Sandbox.Events;

namespace KOTH;

public partial class PlayerPawn :
	IGameEventHandler<DamageGivenEvent>,
	IGameEventHandler<DamageTakenEvent>
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

		BroadcastOnPlayerDeath(DamageTaken);

		if (Camera.IsValid())
		{
			Camera.GameObject.Root.Destroy();
		}

		// UNCOMMENT ID CHECK BEFORE PUBLISHING
		if (this is { IsDummy: false } && DamageTaken.AttackerPlayerPawn is { IsDummy: false } /*&& Id != DamageTaken.VictimPlayerPawn.Id*/)
		{
			FExpEvent ExpEvent = new()
			{
				Amount = ExpManager.CalculateExp(10, 5),
				Origin = ExpOrigins.Kill,
			};

			ExpManager.BroadcastExpEvent(ExpEvent, DamageTaken.AttackerPlayerPawn);
		}

		OnDeath?.Invoke();
		GameObject.Root.Destroy();
	}
}
