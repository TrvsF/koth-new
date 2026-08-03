using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Events;
using Sandbox.VR;
using System;
using System.Diagnostics;
using System.Numerics;

namespace KOTH;

public sealed class TeleporterComponent : Component
{
	[RequireComponent] public DamageComponent DamageComponent { get; private set; }

	////////////////////////////////////////////////////////////////////////

	[Property] public float TeleportTimeSeconds { get; private set; } = 2f;
	[Property] public int MaxHealth { get; private set; } = 100;

	////////////////////////////////////////////////////////////////////////

	[Sync(SyncFlags.FromHost)] public PlayerState OwnerState { get; set; }

	public Action OnDestroyed = null;

	////////////////////////////////////////////////////////////////////////

	protected override void OnStart()
	{
		base.OnStart();

		Assert.NotNull(DamageComponent);
		Assert.NotNull(OwnerState);

		DamageComponent.OnDeath += OnKill;

		if (Networking.IsHost)
		{
			DamageComponent.Initalize(MaxHealth, OwnerState.Team);
		}
	}

	private void OnKill(FDamageTaken DamageTaken)
	{
		OnDestroyed?.Invoke();
		GameObject.Root.Destroy();
	}
	
	////////////////////////////////////////////////////////////////////////

	protected override void OnFixedUpdate()
	{
		if (Networking.IsHost)
		{
			
		}
	}
}
