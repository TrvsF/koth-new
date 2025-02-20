using Sandbox;
using Sandbox.Events;

namespace KOTH;

public sealed class HealBeamComponent : InputWeaponComponent
{
	[Property, Category("Healing")] public float HealsPerTick { get; private set; } = .45f;
	[Property, Category("Healing")] public float MaxHealDistance { get; private set; } = 340f;

	////////////////////////////////////////////////////////////////////////////////////////////////////

	public PlayerPawn HealTarget { get; private set; }
	private PlayerPawn PlayerPawn { get => Equipment.Owner; }

	////////////////////////////////////////////////////////////////////////////////////////////////////

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if (!PlayerPawn.IsValid())
		{
			return;
		}

		TryDoHealingToTarget();
	}

	private TimeSince TimeSinceBeamHealingDone = new();
	protected override void OnInputUpdate()
	{
		if (!PlayerPawn.IsValid())
		{
			Log.Error($"local player pawn not valid on heal beam component {this}");
			return; // NOTE : early return
		}

		bool HealInput = IsDown();
		bool MeleeInput = Input.Pressed("attack2");

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

		return true;
	}

	private TimeSince TimeSinceLastAttemptedHit = new();
	private void TryToHitTarget(PlayerPawn EnemyPawn)
	{	
		TimeSinceLastAttemptedHit = 0;
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
		List<PlayerPawn> UniqueTargets = GetAllPlayerPawnsIntront(PlayerPawn.AimRay, 200, TeamExtensions.GetOpponents(PlayerPawn.Team), 10);
		return UniqueTargets.Any() ? UniqueTargets.First() : null;
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////

	private void TryDoHealingToTarget()
	{
		// if our target is invalid then remove our heal taget
		if (!HealTarget.IsValid() || !IsDown())
		{
			return; // NOTE : early return
		}

		var TargetToPlayerDistance = HealTarget.WorldPosition.Distance(PlayerPawn.WorldPosition);
		if (TargetToPlayerDistance > MaxHealDistance || !HealTarget.IsAlive)
		{
			HealTarget = null;
			return; // NOTE : early return
		}

		FHealingRequest HealingRequest = new()
		{
			TargetPlayerPawn = HealTarget,
			AttackerPlayerPawn = PlayerPawn,
			BaseHealing = HealsPerTick,
			HealingOrigin = PlayerPawn.WorldPosition,
			AllowOverheal = true,
		};
		Scene.Dispatch(new HealingRequestEvent(HealingRequest));

		TimeSinceBeamHealingDone = 0;
	}

	const float PlayerSwitchDelay = .4f;
	private PlayerPawn GetFriendlyTargetIfAny()
	{
		// TODO : fix
		if (TimeSinceBeamHealingDone < PlayerSwitchDelay)
		{
			return null;
		}

		List<PlayerPawn> UniqueTargets = GetAllPlayerPawnsIntront(PlayerPawn.AimRay, 280, PlayerPawn.Team, 25);
		return UniqueTargets.Any() ? UniqueTargets.First() : null;
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////

	private List<PlayerPawn> GetAllPlayerPawnsIntront(Ray AimRay, int Distance, Team PlayerTeam, int WiderOffset = 0)
	{
		// calc 3 rays to check for collision 
		// TODO : make this a cone

		List<PlayerPawn> PlayerPawns = new();

		for (var Offset = -1; Offset <= 1; ++Offset)
		{
			Vector3 OffsetVec = Vector3.Cross(AimRay.Forward, Vector3.Up * Offset).Normal;
			Vector3 StartingLocation = AimRay.Position + (OffsetVec * WiderOffset);
			Gizmo.Draw.Line(StartingLocation, StartingLocation + (AimRay.Forward * Distance));
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
				if (!TargetPlayerPawn.IsValid() || TargetPlayerPawn == PlayerPawn || TargetPlayerPawn.Team != PlayerTeam)
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
