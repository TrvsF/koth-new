using Sandbox;
using Sandbox.Events;

namespace KOTH;

public sealed class HealthPickup : Component, Component.ITriggerListener
{
	[Property] ModelRenderer Model { get; set; }
	[Property] public float HealthPercent { get; set; } = 0.5f;
	[Property] public float RespawnTime { get; set; } = 10f;

	[Sync(SyncFlags.FromHost), Change(nameof(OnActiveChange))] public bool IsAcitve { get; private set; } = true;

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		WorldRotation = Rotation.FromYaw(WorldRotation.Yaw() + 1);

		if (!IsAcitve && TimeSinceDeativate >= RespawnTime)
		{
			IsAcitve = true;
		}
	}

	void ITriggerListener.OnTriggerEnter(Collider Collider)
	{
		if (!IsAcitve || !Networking.IsHost)
		{
			return;
		}

		var PlayerPawn = Collider.GameObject.Root.Components.Get<PlayerPawn>();
		if (PlayerPawn.IsValid())
		{
			if (PlayerPawn.Health < PlayerPawn.MaxHealth)
			{
				FHealingRequest HealingRequest = new()
				{
					TargetPlayerPawn = PlayerPawn,
					BaseHealing = PlayerPawn.MaxHealth * HealthPercent,
					AllowOverheal = false,
				};
				Scene.Dispatch(new HealingRequestEvent(HealingRequest));
				IsAcitve = false;
			}
		}
	}

	private RealTimeSince TimeSinceDeativate = new();

	private void OnActiveChange(bool OldValue, bool NewValue)
	{
		// NOTE : the model should never be invalid
		// but we've had cases where it is, probably
		// because this is fires on 'Active' being replicated
		if (!Model.IsValid())
		{
			return;
		}

		IsAcitve = NewValue;
		Model.Enabled = NewValue;
		if (!NewValue)
		{
			TimeSinceDeativate = 0;
		}
	}
}
