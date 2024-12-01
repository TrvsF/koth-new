using KOTH.UI;
using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Events;

namespace KOTH;

public record OnPlayerRagdolledEvent : IGameEvent
{
	public float DestroyTime { get; set; } = 0f;
}

public partial class PlayerPawn
{
	[RequireComponent] public PlayerInventory Inventory { get; private set; }
	[HostSync] public TimeSince TimeSinceLastRespawn { get; private set; }

	public Team Team;

	public void OnRespawn()
	{
		Assert.True(Networking.IsHost);

		//OnHostRespawn();
		//OnClientRespawn();
	}

	private void OnHostRespawn()
	{
		Assert.True(Networking.IsHost);

		_previousVelocity = Vector3.Zero;

		// DamageComponent.SetHealth(DamageComponent.MaxBaseHealth);

		TimeSinceLastRespawn = 0f;

		ResetBody();

		Scene.Dispatch(new PlayerSpawnedEvent(this));
	}

	[Authority]
	private void OnClientRespawn()
	{
		var LocalCamera = Scene.Components.Get<CameraComponent>();
		if (LocalCamera.IsValid())
		{
			LocalCamera.GameObject.Components.Get<ScreenPanel>()?.Destroy();
		}

		// Possess();

		Tags.Add("self");
	}

	public void Teleport(Transform transform)
	{
		Teleport(transform.Position, transform.Rotation);
	}

	[Authority]
	public void Teleport(Vector3 position, Rotation rotation)
	{
		Transform.World = new(position, rotation);
		Transform.ClearInterpolation();
		EyeAngles = rotation.Angles();

		if (CharacterController.IsValid())
		{
			CharacterController.Velocity = Vector3.Zero;
			CharacterController.IsOnGround = true;
		}
	}

	[Broadcast(NetPermission.HostOnly)]
	private void CreateGibs()
	{
		if (Body.IsValid())
		{
			Body.SetRagdoll(true);
			Body.GameObject.SetParent(null, true);
			// Body.GameObject.Name = $"Ragdoll ({DisplayName})";
			var DestroyComponent = Body.Components.Create<TimedDestroyComponent>();
			DestroyComponent.Time = 0;
		}

		if (GibPrefab.IsValid())
		{
			var Gibs = GibPrefab.Clone(Transform.Position);
			foreach (var ChildGib in Gibs.Root.Children)
			{
				var Rigidbody = ChildGib.Components.Get<Rigidbody>();
				if (Rigidbody.IsValid())
				{
					Rigidbody.Velocity = new(Random.Shared.Int(-1000, 1000), Random.Shared.Int(-1000, 1000), Random.Shared.Int(-1000, 1000));
				}
			}
			Gibs.NetworkSpawn();
		}

		Body = null;
	}

	[Broadcast(NetPermission.HostOnly)]
	private void CreateRagdoll()
	{
		if (!Body.IsValid())
			return;

		Body.SetRagdoll(true);
		Body.GameObject.SetParent(null, true);
		// Body.GameObject.Name = $"Ragdoll ({DisplayName})";

		var ev = new OnPlayerRagdolledEvent();
		Scene.Dispatch(ev);

		if (ev.DestroyTime > 0f)
		{
			var comp = Body.Components.Create<TimedDestroyComponent>();
			comp.Time = ev.DestroyTime;
		}

		Body = null;
	}

	private void ResetBody()
	{
		if (Body is not null)
		{
			Body.DamageTakenForce = Vector3.Zero;
		}

		PlayerBoxCollider.Enabled = true;

		// Components.Get<HumanOutfitter>(FindMode.EnabledInSelfAndDescendants)?.UpdateFromTeam(Team);
	}
}
