using KOTH;
using System.Threading;
using System.Threading.Tasks;

public sealed class TeleporterExitComponent : BuildingComponent
{

}

public sealed class TeleporterEntrenceComponent : BuildingComponent
{
	[RequireComponent] public BoxCollider TeleportZone { get; private set; }
	[Property] public float TeleportTimeSeconds { get; private set; } = 3f;

	private readonly Dictionary<Collider, CancellationTokenSource> PendingTeleports = new();

	protected override void OnStart()
	{
		base.OnStart();

		TeleportZone.IsTrigger = true;

		if (Networking.IsHost)
		{
			TeleportZone.OnTriggerEnter += OnTeleportEnter;
			TeleportZone.OnTriggerExit += OnTeleportExit;
		}
	}

	private void OnTeleportEnter(Collider Collider)
	{
		if (Collider is null || PendingTeleports.ContainsKey(Collider))
		{
			return;
		}

		var Cts = new CancellationTokenSource();
		PendingTeleports[Collider] = Cts;

		_ = QueueTeleport(Collider, Cts.Token);
	}

	private void OnTeleportExit(Collider Collider)
	{
		if (Collider is null)
			return;

		if (PendingTeleports.Remove(Collider, out var Cts))
		{
			Cts.Cancel();
			Cts.Dispose();
		}
	}

	private async Task QueueTeleport(Collider Collider, CancellationToken Token)
	{
		try
		{
			await Task.DelaySeconds(TeleportTimeSeconds);

			if (Token.IsCancellationRequested || !this.IsValid() || !Collider.IsValid())
			{
				return;
			}

			TeleportCollider(Collider);
		}
		catch (OperationCanceledException) {}
		finally
		{
			PendingTeleports.Remove(Collider);
		}
	}

	private void TeleportCollider(Collider Collider)
	{
		if (Collider == null || Collider.GameObject == null)
		{
			return;
		}

		var PlayerPawn = Collider.GameObject.Root.GetComponent<PlayerPawn>();
		if (PlayerPawn == null)
		{
			return;
		}

		foreach (var Building in GameMode.Instance.BuildingManager.PlayerBuildings.GetOrCreate(OwnerState))
		{
			if (Building.GetComponent<TeleporterExitComponent>() != null)
			{
				PlayerPawn.WorldPosition = Building.WorldPosition + Vector3.Up * 16f;
				return;
			}
		}
	}
}
