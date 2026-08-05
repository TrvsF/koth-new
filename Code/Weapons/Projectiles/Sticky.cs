using Sandbox.Events;
using System;

namespace KOTH;

public sealed class Sticky : Projectile, IGameEventHandler<ProjectileCollideEvent>
{
	[Property, Group("Explosion")] public float MinDetTime { get; set; } = 0.66f;
	[Property, Group("Explosion")] public GameObject ExplosionPrefab { get; set; }
	[Property, Group("Explosion")] public SoundEvent ExplodeSound { get; set; }

	/////////////////////////////////////////////////////////////////////////////////////

	private bool IsAttached = false;
	private GameObject AttachedGameObject = null;
	private Vector3 InitArmedWorldPosition;
	private Transform InitArmedWorldTransformAttachedObject;

	/////////////////////////////////////////////////////////////////////////////////////

	public TimeSince AliveTime { get; private set; } = new();

	public new void Destroy()
	{
		GameObject?.Root?.Destroy();
	}

	protected override void OnStart()
	{
		base.OnStart();

		AliveTime = 0;

		if (ModelRenderer.IsValid())
		{
			SpinBaseRotation = ModelRenderer.WorldRotation;

			if (GameUtils.GetPlayerState(OwnerPlayerPawn.Id) is { } PlayerState)
			{
				ModelRenderer.Tint = PlayerState.Team == Team.Terrorist ? Color.Red : Color.Green;
			}
		}

		SpinAngle = Game.Random.Float(0f, 360f);
		SpinAxis = ComputeSpinAxis(Body.IsValid() ? Body.Velocity : WorldRotation.Forward);
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if (IsAttached)
		{
			if (!AttachedGameObject.IsValid())
			{
				GameObject.Root.Destroy();
				return;
			}

			Vector3 GameObjectOffset = AttachedGameObject.WorldPosition - InitArmedWorldTransformAttachedObject.Position;
			GameObject.WorldPosition = InitArmedWorldPosition + GameObjectOffset;
		}

		UpdateSpin();

		if (AliveTime >= MinDetTime && ModelRenderer.IsValid())
		{
			ModelRenderer.Tint = Color.Yellow;
		}
	}

	public void OnGameEvent(ProjectileCollideEvent EventArgs)
	{
		var Rigidbody = GameObject.Root.Components.Get<Rigidbody>();
		if (!Rigidbody.IsValid())
		{
			Log.Warning($"cannot find rigidboy comp on sticky {this}");
		}

		if (!Rigidbody.MotionEnabled)
		{
			return;
		}

		var HitObject = EventArgs.ProjectileCollision.HitObject;

		if (!HitObject.IsValid())
		{
			return;
		}

		Rigidbody.Velocity = Vector3.Zero;
		Rigidbody.MotionEnabled = false;

		IsAttached = true;
		AttachedGameObject = HitObject;
		InitArmedWorldTransformAttachedObject = HitObject.WorldTransform;
		InitArmedWorldPosition = EventArgs.ProjectileCollision.HitLocation;
	}

	const bool SpinInFlight = true;
	const float SpinSpeed = 1900f;        
	const float SpinReferenceSpeed = 2000f;
	const float SpinMaxScale = 2f;
	const bool SpinScalesWithSpeed = true;

	private ModelRenderer CachedModelRenderer;
	private Rigidbody CachedRigidbody;
	private Rotation SpinBaseRotation = Rotation.Identity;
	private Vector3 SpinAxis = Vector3.Right;
	private float SpinAngle = 0f;

	public ModelRenderer ModelRenderer
	{
		get
		{
			if (!CachedModelRenderer.IsValid())
				CachedModelRenderer = GetComponentInChildren<ModelRenderer>();
			return CachedModelRenderer;
		}
	}

	private Rigidbody Body
	{
		get
		{
			if (!CachedRigidbody.IsValid())
				CachedRigidbody = GameObject.Root.Components.Get<Rigidbody>();
			return CachedRigidbody;
		}
	}

	private void UpdateSpin()
	{
		if (!SpinInFlight || IsAttached)
			return;

		var Renderer = ModelRenderer;
		if (!Renderer.IsValid())
			return;

		Vector3 Velocity = Body.IsValid() ? Body.Velocity : Vector3.Zero;
		float Speed = Velocity.Length;

		if (Speed > 1f)
			SpinAxis = ComputeSpinAxis(Velocity);

		float Scale = 1f;
		if (SpinScalesWithSpeed && SpinReferenceSpeed > 0f)
			Scale = MathX.Clamp(Speed / SpinReferenceSpeed, 0f, SpinMaxScale);

		SpinAngle = (SpinAngle + SpinSpeed * Scale * Time.Delta) % 360f;

		Renderer.WorldRotation = Rotation.FromAxis(SpinAxis, SpinAngle) * SpinBaseRotation;
	}

	private static Vector3 ComputeSpinAxis(Vector3 Velocity)
	{
		// horizontal axis perpendicular to travel -> tumbles through its arc
		Vector3 Axis = Vector3.Cross(Velocity.Normal, Vector3.Up);
		return Axis.LengthSquared < 0.0001f ? Vector3.Right : Axis.Normal;
	}

	[Obsolete]
	public void Explode()
	{
		// TODO : this is likely because the object is destroyed on clients before this
		// code can run (race condition). Maybe somehow call async? 
		if (Transform == null)
		{
			Log.Warning("transform null on sticky");
			return;
		}

		if (ExplosionPrefab.IsValid())
		{
			var Explosion = ExplosionPrefab.Clone(WorldPosition);
			if (Explosion.IsValid())
			{
				Explosion.NetworkSpawn();
			}

			if (ExplodeSound != null)
			{
				GameObject.PlaySound(ExplodeSound, false);
			}
		}

		SimulateExplode(out FProjectileCollision ProjectileCollision, Transform.Position);

		foreach (var DamageComponent in ProjectileCollision.TracedDamageComponents)
		{
			if (!DamageComponent.IsValid())
			{
				continue;
			}

			FDamageRequest DamageRequest = new()
			{
				TargetDamageComponent = DamageComponent,
				AttackerPlayerPawn = OwnerPlayerPawn,
				DamageOrigin = ProjectileCollision.HitLocation,
				TargetOrigin = DamageComponent.WorldPosition,
				BaseDamage = BaseDamage,
				BaseKnockbackStrength = BaseKnockbackStrength,
				DamageType = EDamageType.Projectile,
				DamageFalloffType = EDamageFalloffType.Falloff,
				MaxDamageImpactDistance = ExplosionRadius,
			};

			if (DamageComponent.GameObject.GetComponent<PlayerPawn>() is { } PlayerPawn)
			{
				DamageRequest.TargetPlayerPawn = PlayerPawn;
				DamageRequest.TargetOrigin = PlayerPawn.CenterPosition;
			}

			Scene.Dispatch(new DamageRequestEvent(DamageRequest));

		}
		GameObject.Root.Destroy();
	}
}

