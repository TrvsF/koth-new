using Sandbox;
using Sandbox.Events;
using static Sandbox.PhysicsContact;

namespace KOTH;

public record ProjectileCollideEvent(FProjectileCollision ProjectileCollision) : IGameEvent;

public record FProjectileCollision
{
	public GameObject HitObject { get; init; }
	public List<PlayerPawn> TracedPlayers { get; init; }
	public Vector3 HitLocation { get; init; }
	public bool IsFirstHit { get; init; }
}

public abstract class Projectile : Component, Component.ICollisionListener
{
	[Property] public float ExplosionRadius { get; set; } = 128f;
	[Property] public GameObject ImpactPrefab { get; set; }
	[Property] public GameObject ImpactPlayerPrefab { get; set; }

	[Sync(SyncFlags.FromHost)] public PlayerPawn OwnerPlayerPawn { get; set; }

	////////////////////////////////////////////////////////////////////////

	[Rpc.Broadcast]
	private void DoSurfaceHitFx(Vector3 Location, bool CollideWithPlayer = false)
	{
		if (Networking.IsHost)
		{
			if (CollideWithPlayer)
			{
				//var PlayerImpact = ImpactPlayerPrefab.Clone(Location);
				//if (PlayerImpact.IsValid())
				//{
				//	PlayerImpact.NetworkSpawn();
				//}
			}

			if (ImpactPrefab.IsValid())
			{
				var Impact = ImpactPrefab.Clone(Location);
				if (Impact.IsValid())
				{
					Impact.NetworkSpawn();
				}
			}
		}
	}

	protected void SimulateExplode(out FProjectileCollision OutProjectileCollision, Vector3 ExplosionPoint, GameObject CollisionObject = null)
	{
		var ExplosionLocation = ExplosionPoint;
		var ExplosionTrace = Scene.Trace
			.Sphere(ExplosionRadius, ExplosionLocation, ExplosionLocation)
			.WithoutTags("zone")
			.WithoutTags("consumable")
			.RunAll();

		List<PlayerPawn> TracedPawns = new();
		foreach (var Hit in ExplosionTrace)
		{
			var Target = Hit.GameObject?.Root;
			if (!Target.IsValid())
			{
				Log.Warning("cannot find game object while doing explosion trace");
				continue;
			}

			var TargetPlayerPawn = Target.Components.Get<PlayerPawn>();
			if (!TargetPlayerPawn.IsValid())
			{
				// Log.Warning("cannot find player pawn while doing explosion trace");
				continue;
			}

			TracedPawns.Add(TargetPlayerPawn);
		}

		OutProjectileCollision = new()
		{
			HitObject = CollisionObject,
			TracedPlayers = TracedPawns,
			HitLocation = ExplosionLocation,
			IsFirstHit = IsInitialHit,
		};
	}

	private bool IsInitialHit = true;
	void ICollisionListener.OnCollisionStart(Collision Collision)
	{
		if (!Network.IsOwner)
		{
			return;
		}

		var OtherRoot = Collision.Other.GameObject?.Root;
		if (!OtherRoot.IsValid())
		{
			return;
		}

		if (OtherRoot == null || OtherRoot == OwnerPlayerPawn?.GameObject.Root)
		{
			Log.Warning($"Projectile {this} is colliding with spawner player!");
			return;
		}

		if (!IsInitialHit)
		{
			Log.Warning($"Projectile {this} hit twice");
			return;
		}

		var ContactPoint = Collision.Contact.Point;

		if (ContactPoint == Vector3.Zero)
		{
			Log.Warning($"Projectile {this} has mising contact point");
			return;
		}

		SimulateExplode(out FProjectileCollision ProjectileCollision, ContactPoint, OtherRoot);
		GameObject.Root.Dispatch(new ProjectileCollideEvent(ProjectileCollision));

		DoSurfaceHitFx(ContactPoint, OtherRoot.Components.Get<PlayerPawn>().IsValid());

		IsInitialHit = false;
	}
}
