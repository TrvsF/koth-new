using Sandbox;
using Sandbox.Events;

namespace KOTH;

public sealed class HealBeamComponent : InputWeaponComponent
{
	[Property, Category("Healing")] public float HealsPerTick { get; private set; } = .45f;
	[Property, Category("Healing")] public float MaxHealDistance { get; private set; } = 340f;
	[Property, Category("Swing")] public float BaseDamage { get; private set; } = 40f;
	[Property, Category("Swing")] public float BaseKnockback { get; private set; } = 50f;

	public PlayerPawn HealTarget { get; private set; }
	private PlayerPawn PlayerPawn { get => Equipment.Owner; }

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
		// TODO : left click for healing right click for swinging
		bool IsActive = IsDown();

		if (!IsActive)
		{
			// disconnect beam if the inputs been gone long enough
			if (HealTarget.IsValid() && TimeSinceBeamHealingDone > .1f)
			{
				HealTarget = null;
			}

			return; // NOTE : early return
		}

		if (!PlayerPawn.IsValid())
		{
			Log.Error($"local player pawn not valid on heal beam component {this}");
			return; // NOTE : early return
		}

		// if we've got a heal target keep healing them
		if (HealTarget.IsValid())
		{
			return; // NOTE : early return
		}

		// figure out what action we're gonna take (heal or meele)
		var EnemyPawn = GetEnemyTargetIfAny();
		var FriendlyPawn = GetFriendlyTargetIfAny();

		if (EnemyPawn.IsValid() && EnemyPawn.IsAlive)
		{
			TryToHitTarget(EnemyPawn);
		}
		else if (FriendlyPawn.IsValid())
		{
			HealTarget = FriendlyPawn;
			TimeSinceBeamHealingDone = 0;
		}
		else
		{
			Equipment.ViewModel?.ModelRenderer?.Set("b_reload", true); // ?
		}

	}

	private float HitDelay = 1.66f;
	private TimeSince TimeSinceLastAttemptedHit = new();
	private void TryToHitTarget(PlayerPawn EnemyPawn)
	{
		if (TimeSinceLastAttemptedHit < HitDelay)
		{
			return;
		}

		// Equipment.ViewModel?.ModelRenderer?.Set("b_reload", true);

		TimeSinceLastAttemptedHit = 0;
		FDamageRequest DamageRequest = new()
		{
			TargetPlayerPawn = EnemyPawn,
			AttackerPlayerPawn = PlayerPawn,
			DamageOrigin = PlayerPawn.WorldPosition,
			BaseDamage = BaseDamage,
			BaseKnockbackStrength = BaseKnockback,
			DirectImpact = true,
			DamageType = EDamageType.Melee,
		};
		Scene.Dispatch(new DamageRequestEvent(DamageRequest));
	}

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

		// heal em ////////////////////////////////////////////////////
		TimeSinceBeamHealingDone = 0;

		FHealingRequest HealingRequest = new()
		{
			TargetPlayerPawn = HealTarget,
			AttackerPlayerPawn = PlayerPawn,
			BaseHealing = HealsPerTick,
			HealingOrigin = PlayerPawn.WorldPosition,
			AllowOverheal = true,
		};
		Scene.Dispatch(new HealingRequestEvent(HealingRequest));
	}

	private PlayerPawn GetEnemyTargetIfAny()
	{
		List<PlayerPawn> UniqueTargets = GetAllPlayerPawnsIntront(PlayerPawn.AimRay, 200, TeamExtensions.GetOpponents(PlayerPawn.Team), 10);
		return UniqueTargets.Any() ? UniqueTargets.First() : null;

		// TODO : maybe use a box instead?
		//Vector3 BoxBounds = new(150, 100, 50);
		//BBox Box = BBox.FromPositionAndSize(PlayerPawn.Head.Transform.Position, BoxBounds);

		//var EnemyPawn = Scene.Trace.Box(Box, PlayerPawn.AimRay, 150);
		//var EnemyPawnTraceBox = EnemyPawn.WithTag("player")
		//	.WithoutTags("hill")
		//	.RunAll();
	}

	private float PlayerSwitchDelay = .4f;
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
