using Sandbox;
using Sandbox.Events;
using static Sandbox.PhysicsContact;

namespace KOTH;

/* should only be fired from the owning connection */
public record ProjectileCollideEvent(FProjectileCollision ProjectileCollision) : IGameEvent;

public record FProjectileCollision
{
	public GameObject HitObject { get; init; }
	public HashSet<PlayerPawn> TracedPlayers { get; init; }
	public Vector3 HitLocation { get; init; }
	public bool IsFirstHit { get; init; }
}

public abstract class Projectile : Component, Component.ICollisionListener
{
	[Property, Group("Base")] public float BaseDamage { get; set; } = 50f;
	[Property, Group("Base")] public float BaseKnockbackStrength { get; set; } = 300f;
	[Property, Group("Base")] public float ExplosionRadius { get; set; } = 128f;

	public PlayerPawn OwnerPlayerPawn { get; set; }

	////////////////////////////////////////////////////////////////////////

	protected void SimulateExplode(out FProjectileCollision OutProjectileCollision, Vector3 ExplosionPoint, GameObject CollisionObject = null)
	{
		var ExplosionLocation = ExplosionPoint;
		var ExplosionTrace = Scene.Trace
			.Sphere(ExplosionRadius, ExplosionLocation, ExplosionLocation)
			.WithoutTags("zone")
			.WithoutTags("consumable")
			.RunAll();

		HashSet<PlayerPawn> TracedPawns = new();
		foreach (var Hit in ExplosionTrace)
		{
			var Target = Hit.GameObject?.Root;
			if (!Target.IsValid())
			{
				Log.Warning("cannot find game object while doing explosion trace");
				continue;
			}

			var TargetPlayerPawn = Target.Root.Components.Get<PlayerPawn>();
			if (!TargetPlayerPawn.IsValid())
			{
				Log.Warning("cannot find player pawn while doing explosion trace");
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
		Log.Info("aaa");
		if (!Network.IsOwner)
		{
			return;
		}

		Log.Info("aaa2");

		var OtherRoot = Collision.Other.GameObject?.Root;
		if (!OtherRoot.IsValid())
		{
			Log.Warning($"Projectile {this} is hit something invalid!");
			return;
		}

		if (OtherRoot == null)
		{
			Log.Warning($"Projectile {this} is hit something invalid!");
			return;
		}
		
		if (OtherRoot == OwnerPlayerPawn?.GameObject.Root)
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
		if (OtherRoot.Components.Get<PlayerPawn>() is PlayerPawn HitPlayerPawn)
		{
			ProjectileCollision.TracedPlayers.Add(HitPlayerPawn);
		}

		Log.Info($"count {ProjectileCollision.TracedPlayers.Count}");
		foreach (var Player in ProjectileCollision.TracedPlayers)
		{
			Log.Info(Player);
		}

		GameObject.Root.Dispatch(new ProjectileCollideEvent(ProjectileCollision));
		Log.Info("DJHFKSDKJFHSD");
		IsInitialHit = false;
	}
}
