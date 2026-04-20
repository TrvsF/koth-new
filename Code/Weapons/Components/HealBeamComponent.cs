using Sandbox;
using Sandbox.Events;

namespace KOTH;

public sealed class HealBeamBeam : Component
{
	public PlayerPawn HealTarget { get; set; }

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if (!HealTarget.IsValid())
		{
			Log.Warning("ohno");
			return;
		}

		WorldPosition = (GameObject.Parent.WorldPosition + HealTarget.WorldPosition) / 2;
		WorldPosition += Vector3.Up * 25;
		WorldRotation = Rotation.LookAt(HealTarget.WorldPosition - GameObject.Parent.WorldPosition);
	}
}

public sealed class HealBeamComponent : InputWeaponComponent
{
	[Property, Category("Healing")] public float TimePerOneHeal { get; set; } = .45f;
	[Property, Category("Healing")] public float MaxHealDistance { get; set; } = 330f;
	[Property, Category("Healing")] public float MaxCharge { get; set; } = 200f;
	[Property, Category("Healing")] public float ChargeBuildRate { get; set; } = .03f;
	[Property, Category("Healing")] public float ChargeDegradeRate { get; set; } = .05f;
	[Property, Category("Healing")] public GameObject HealBeamBeamPrefab { get; set; }

	////////////////////////////////////////////////////////////////////////////////////////////////////

	[Sync] public PlayerPawn HealTarget { get; private set; }
	[Sync] public bool IsUbered { get; private set; } = false;
	[Sync] public float Charge { get; private set; } = 0f;
	public PlayerPawn PlayerPawn { get => Equipment.Owner; }
	private HealBeamBeam HealBeamBeam { get; set; }

	////////////////////////////////////////////////////////////////////////////////////////////////////

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if (!PlayerPawn.IsValid())
		{
			return;
		}

		if (HealTarget.IsValid())
		{
			if (!HealBeamBeam.IsValid())
			{
				CloneConfig CloneConfig = new();
				CloneConfig.StartEnabled = true;
				CloneConfig.Parent = GameObject;
				HealBeamBeam = HealBeamBeamPrefab.Clone(CloneConfig).GetComponent<HealBeamBeam>();
			}

			HealBeamBeam.HealTarget = HealTarget;
		}
		else if (HealBeamBeam.IsValid())
		{
			HealBeamBeam.DestroyGameObject();
		}

		if (!IsProxy)
		{
			TryDoHealingToTarget();

			if (IsUbered)
			{
				Charge -= ChargeDegradeRate;

				if (Charge <= 0)
				{
					Charge = 0;
					IsUbered = false;
				}
			}
		}
	}

	private TimeSince TimeSinceBeamHealingDone = new();
	protected override void OnInputUpdate()
	{
		if (!PlayerPawn.IsValid())
		{
			Log.Error($"local player pawn not valid on heal beam component {this}");
			return; // NOTE : early return
		}

		if (IsProxy)
		{
			return;
		}

		bool HealInput = IsDown();
		bool UberInput = Input.Pressed("attack2");
		bool MeleeInput = Input.Pressed("attack3");

		if (UberInput)
		{
			if (!IsUbered && Charge >= 100)
			{
				IsUbered = true;
			}
		}

		if (IsUbered)
		{
			Equipment?.Owner?.DamageComponent.BroadcastUber();
			HealTarget?.DamageComponent.BroadcastUber();
		}

		if (!HealInput)
		{
			// disconnect beam if the inputs been gone long enough, or if we're meleeing
			if (HealTarget.IsValid() && TimeSinceBeamHealingDone > .1f || MeleeInput)
			{
				HealTarget = null;
			}

			if (MeleeInput)
			{
				MeleeSwing();
			}

			return; // NOTE : early return
		}

		// if we've got a heal target keep healing them
		if (HealTarget.IsValid())
		{
			return; // NOTE : early return
		}

		var FriendlyPawn = GetFriendlyTargetIfAny();

		Equipment.ViewModel?.ModelRenderer?.Set("b_jump", true);

		if (FriendlyPawn.IsValid())
		{
			HealTarget = FriendlyPawn;
			TimeSinceBeamHealingDone = 0;
		}
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////
	
	private bool MeleeSwing()
	{
		if (TimeSinceLastAttemptedHit < FireRate)
		{
			return false;
		}

		Equipment.ViewModel?.ModelRenderer?.Set("b_attack", true);
		
		var EnemyPawn = GetEnemyTargetIfAny();
		if (EnemyPawn.IsValid() && EnemyPawn.IsAlive)
		{
			TryToHitTarget(EnemyPawn);
		}

		TimeSinceLastAttemptedHit = 0;
		return true;
	}

	private TimeSince TimeSinceLastAttemptedHit = new();
	private void TryToHitTarget(PlayerPawn EnemyPawn)
	{
		FDamageRequest DamageRequest = new()
		{
			TargetDamageComponent = EnemyPawn.DamageComponent,
			AttackerPlayerPawn = PlayerPawn,
			TargetPlayerPawn = EnemyPawn,
			DamageOrigin = PlayerPawn.WorldPosition,
			TargetOrigin = EnemyPawn.CenterPosition,
			BaseDamage = BaseDamage,
			BaseKnockbackStrength = KnockbackStrength,
			DirectImpact = true,
			DamageType = EDamageType.Melee,
		};
		Scene.Dispatch(new DamageRequestEvent(DamageRequest));
	}

	private PlayerPawn GetEnemyTargetIfAny()
	{
		List<PlayerPawn> UniqueTargets = GetAllPlayerPawnsIntront(PlayerPawn.AimRay, 200, 10);
		return UniqueTargets.Any() ? UniqueTargets.First() : null;
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////

	private void TryDoHealingToTarget()
	{
		// if our target is invalid then remove our heal taget
		if (!HealTarget.IsValid() || !IsDown())
		{
			HealTarget = null;
			return; // NOTE : early return
		}

		var TargetToPlayerDistance = HealTarget.WorldPosition.Distance(PlayerPawn.WorldPosition);
		if (TargetToPlayerDistance > MaxHealDistance || !HealTarget.IsAlive)
		{
			HealTarget = null;
			return; // NOTE : early return
		}

		if (!IsUbered && Charge <= MaxCharge)
		{
			Charge = Math.Min(ChargeBuildRate + Charge, MaxCharge);
		}
		
		if (TimeSinceBeamHealingDone >= TimePerOneHeal)
		{
			FHealingRequest HealingRequest = new()
			{
				TargetDamageComponent = HealTarget.DamageComponent,
				TargetPlayerPawn = HealTarget,
				HealerPlayerPawn = PlayerPawn,
				BaseHealing = 1,
				HealingOrigin = PlayerPawn.WorldPosition,
				AllowOverheal = true,
				HealingType = EHealingType.Continuous,
			};
			Scene.Dispatch(new HealingRequestEvent(HealingRequest));

			TimeSinceBeamHealingDone = 0;
		}
	}

	const float PlayerSwitchDelay = .4f;
	private PlayerPawn GetFriendlyTargetIfAny()
	{
		// TODO : fix
		if (TimeSinceBeamHealingDone < PlayerSwitchDelay)
		{
			return null;
		}

		foreach (var Target in GetAllPlayerPawnsIntront(PlayerPawn.AimRay, 280, 25))
		{
			if (Target.Team == Player.Team)
			{
				return Target;
			}
		}

		return null;
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////

	public List<PlayerPawn> GetAllPlayerPawnsIntront(Ray AimRay, int Distance, int WiderOffset = 0)
	{
		// calc 3 rays to check for collision 
		// TODO : make this a cone

		List<PlayerPawn> PlayerPawns = new();

		for (var Offset = -1; Offset <= 1; ++Offset)
		{
			Vector3 OffsetVec = Vector3.Cross(AimRay.Forward, Vector3.Up * Offset).Normal;
			Vector3 StartingLocation = AimRay.Position + (OffsetVec * WiderOffset);
			
			var PlayerPawnTrace = Scene.Trace.Ray(StartingLocation, StartingLocation + (AimRay.Forward * Distance))
			.WithTag("player")
			.WithoutTags("hill")
			.WithoutTags("consumeable")
			.RunAll();

			foreach (var Hit in PlayerPawnTrace)
			{
				var Target = Hit.GameObject?.Root;
				if (!Target.IsValid())
				{
					continue;
				}

				var TargetPlayerPawn = Target.Root.Components.Get<PlayerPawn>();
				if (!TargetPlayerPawn.IsValid() || TargetPlayerPawn == PlayerPawn)
				{
					continue;
				}

				if (!PlayerPawns.Contains(TargetPlayerPawn))
				{
					PlayerPawns.Add(TargetPlayerPawn);
				}
			}
		}

		return PlayerPawns;
	}
}
