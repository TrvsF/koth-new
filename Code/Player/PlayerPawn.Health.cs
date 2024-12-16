using KOTH.UI;
using Sandbox.Diagnostics;
using Sandbox.Events;

namespace KOTH;

public partial class PlayerPawn : 
	IGameEventHandler<DamageGivenEvent>, 
	IGameEventHandler<DamageTakenEvent>,
	IGameEventHandler<HealingGivenEvent>
{
	public DamageComponent DamageComponent => Components.Get<DamageComponent>();

	public float Health => DamageComponent.IsValid ? DamageComponent.Health : -1;
	public float MaxHealth => DamageComponent.IsValid ? DamageComponent.MaxBaseHealth : -1;
	public bool IsAlive => DamageComponent.IsValid && !DamageComponent.IsDead;
	public event Action OnDeath;

	protected override void OnEnabled()
	{
		base.OnEnabled();

		Assert.NotNull(DamageComponent);

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

	void IGameEventHandler<DamageTakenEvent>.OnGameEvent(DamageTakenEvent EventArgs)
	{
		var DamageEvent = EventArgs.DamageEvent;

		var VictimGameobject = GameUtils.GetPlayerFromComponent(DamageEvent.AttackerPlayerPawn);
		var DamageLocation = DamageEvent.DamageLocation;

		// TODO : repplace
		AnimationHelper.ProceduralHitReaction(DamageEvent.Damage / 100f, DamageLocation);

		//if (IsViewer)
		//{
		//	DamageIndicator.Current?.OnHit(DamageLocation);
		//	DamageIndicatorNew.Instance?.OnHit(DamageLocation);
		//}

		TimeUntilAccelerationRecovered = 1;
		AccelerationAddedScale = 0.5f;

		// --------------------
		// fx
		//if (BloodEffect.IsValid())
		//{
		//	BloodEffect?.Clone(new CloneConfig()
		//	{
		//		StartEnabled = true,
		//		Transform = new(DamageLocation),
		//		Name = $"Blood effect from ({GameObject})"
		//	});
		//}

		//if (BloodImpactSound is not null)
		//{
		//	var snd = Sound.Play(BloodImpactSound, DamageLocation);
		//	snd.ListenLocal = IsViewer;
		//}
	}

	// TODO : everything IsViewer needs to happen on the camera

	public void OnGameEvent(HealingGivenEvent EventArgs)
	{
		//if (IsViewer)
		//{
		//	var HealInfo = EventArgs.HealingRequest;

		//	DamageNumbers.Instance?.OnHealth(HealInfo.Healing, HealInfo.TargetPlayerPawn);
		//}
	}

	void IGameEventHandler<DamageGivenEvent>.OnGameEvent(DamageGivenEvent EventArgs)
	{
		//if (IsViewer)
		//{
		//	var DamageEvent = EventArgs.DamageEvent;
		//	DamageNumbers.Instance?.OnHit(DamageEvent.Damage, DamageEvent.VictimPlayerPawn);
		//}
	}
}
