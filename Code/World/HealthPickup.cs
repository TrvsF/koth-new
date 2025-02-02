using Sandbox;
using Sandbox.Events;

namespace KOTH;

public sealed class HealthPickup : Component, Component.ITriggerListener
{
	[RequireComponent] ModelRenderer Model { get; set; }

	[Property] public float HealthPercent { get; set; } = 0.5f;
	[Property] public float RespawnTime { get; set; } = 10f;
	[HostSync] public bool IsAcitve { get; private set; } = true;

	protected override void OnFixedUpdate()
	{
		Transform.Rotation = Rotation.FromYaw(Transform.Rotation.Yaw() + 1);

		if (!IsAcitve && TimeSinceDeativate >= RespawnTime)
		{
			Activate();
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
				Deativate();
			}
		}
	}

	private RealTimeSince TimeSinceDeativate = new();

	[Broadcast]
	private void Deativate()
	{
		IsAcitve = false;
		Model.Enabled = false;
		TimeSinceDeativate = 0;
	}

	[Broadcast]
	private void Activate()
	{
		IsAcitve = true;
		Model.Enabled = true;
	}
}
