using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Events;
using static Sandbox.PhysicsContact;

namespace KOTH;

/* should only be fired from the owning connection */
public record ProjectileCollideEvent(FProjectileCollision ProjectileCollision) : IGameEvent;

public record FProjectileCollision
{
	public GameObject HitObject { get; init; }
	public HashSet<DamageComponent> TracedDamageComponents { get; init; }
	public Vector3 HitLocation { get; init; }
	public bool IsFirstHit { get; init; }
}

public abstract class Projectile : Component, Component.ICollisionListener
{
	[Property, Group("Base")] public int BaseDamage { get; set; } = 50;
	[Property, Group("Base")] public float BaseKnockbackStrength { get; set; } = 300f;
	[Property, Group("Base")] public float ExplosionRadius { get; set; } = 128f;

	public PlayerPawn OwnerPlayerPawn { get; set; }

	////////////////////////////////////////////////////////////////////////

	protected readonly float LocalInvisableTime = .05f;
	protected TimeSince TimeSinceSpawn = 0;

	protected override void OnStart()
	{
		base.OnStart();

		TimeSinceSpawn = 0;
	}

	////////////////////////////////////////////////////////////////////////

	protected void SimulateExplode(out FProjectileCollision OutProjectileCollision, Vector3 ExplosionPoint, GameObject CollisionObject = null)
	{
		var ExplosionLocation = ExplosionPoint;
		var ExplosionTrace = Scene.Trace
			.Sphere(ExplosionRadius, ExplosionLocation, ExplosionLocation)
			.WithoutTags("zone")
			.WithoutTags("consumable")
			.RunAll();

		HashSet<DamageComponent> TracedPawns = new();
		foreach (var Hit in ExplosionTrace)
		{
			var Target = Hit.GameObject?.Root;
			if (!Target.IsValid())
			{
				Log.Warning("cannot find game object while doing explosion trace");
				continue;
			}

			var TargetDamageComponent = Target.Root.Components.Get<DamageComponent>();
			if (!TargetDamageComponent.IsValid())
			{
				continue;
			}

			TracedPawns.Add(TargetDamageComponent);
		}

		OutProjectileCollision = new()
		{
			HitObject = CollisionObject,
			TracedDamageComponents = TracedPawns,
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
		if (Collision.Other.Body == null)
		{
			return;
		}
		if (OtherRoot == null)
		{
			Log.Warning($"Projectile {this} is hit something invalid!");
			return;
		}
		if (!OtherRoot.IsValid())
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
			Log.Warning($"Projectile {this} has mising contact point, using fallback point..");
			ContactPoint = Collision.Other.GameObject.WorldPosition;
		}

		SimulateExplode(out FProjectileCollision ProjectileCollision, ContactPoint, OtherRoot);
		if (OtherRoot.Components.Get<DamageComponent>() is DamageComponent HitDamageComponent)
		{
			ProjectileCollision.TracedDamageComponents.Add(HitDamageComponent);
		}

		GameObject.Root.Dispatch(new ProjectileCollideEvent(ProjectileCollision));
		IsInitialHit = false;
	}
}
