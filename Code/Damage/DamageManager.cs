using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Events;
using Sandbox.Utility;
using System.ComponentModel.DataAnnotations;
using static Sandbox.PhysicsContact;

namespace KOTH;

// TODO : why is this a singleton? what is a singleton in this sense? <- is it the rpcs?
// TODO : rename health manager
public sealed class DamageManager : SingletonComponent<DamageManager>,
	IGameEventHandler<DamageRequestEvent>,
	IGameEventHandler<HealingRequestEvent>
{
	[Property] public SoundEvent HitSound { get; set; }

	// special bool for jumper gamemode
	[Property] public bool KnockbackOnly { get; private set; } = false;


	const float SelfDamageMultiplyer = 0.2f;
	const float PlayerDistanceFalloffMaxBound = 1600;

	public void OnGameEvent(HealingRequestEvent EventArgs)
	{
		var HealingRequest = EventArgs.HealingRequest;
		if (!HealingRequest.IsValid())
		{
			Log.Warning($"tried to parse HealingRequestEvent that is not valid {EventArgs} {this}");
			return;
		}

		var TargetPlayerPawn = HealingRequest.TargetPlayerPawn;

		switch (HealingRequest.HealingType)
		{
			case EHealingType.Continuous:
				ServerInflictHealing(TargetPlayerPawn, HealingRequest.AttackerPlayerPawn, HealingRequest.BaseHealing, HealingRequest.AllowOverheal);
				// HealingRequest.AttackerPlayerPawn.GameObject.Root.Dispatch(new HealingGivenEvent(new(HealingRequest.TargetPlayerPawn, HealingRequest.BaseHealing)));
				break;
			case EHealingType.Projectile:
				break;
		}
	}

	public void OnGameEvent(DamageRequestEvent EventArgs)
	{
		var DamageRequest = EventArgs.DamageRequest;
		if (!DamageRequest.IsValid())
		{
			Log.Warning($"tried to parse DamageRequestEvent that is not valid {EventArgs} {this}");
			return;
		}

		// tell server to do damage from request
		ServerInflictDamageToPlayer(DamageRequest);
	}

	const float MaxKB = 1200f;
	private static Vector3 CalculateKnockback(Vector3 DirectionVec, float Damage, float WeaponKnockbackStrength, float WeightFactor, bool IsCrouching)
	{
		var CrouchFactor = IsCrouching ? 62 : 82;
		var KnockbackFactor = Damage * (WeaponKnockbackStrength / CrouchFactor);

		KnockbackFactor = Math.Min(KnockbackFactor, MaxKB);
		return DirectionVec * KnockbackFactor * WeightFactor;
	}

	/////////////////////////////////////////////////////////////////////////////////////////////
	// host only broadcasts
	/////////////////////////////////////////////////////////////////////////////////////////////

	[Rpc.Host]
	private void ServerInflictDamageToPlayer(FDamageRequest DamageRequest)
	{
		Assert.True(Networking.IsHost);

		var DamageOrigin = DamageRequest.DamageOrigin;
		var TargetPlayerPawn = DamageRequest.TargetPlayerPawn;
		var AttackerPlayerPawn = DamageRequest.AttackerPlayerPawn;
		var Damage = DamageRequest.BaseDamage;

		// // ---------------------- we've taken damage without an attacker pawn, apply & return early
		if (!AttackerPlayerPawn.IsValid())
		{
			FDamageTaken EnvDamageTaken = new()
			{
				AttackerPlayerPawn = AttackerPlayerPawn,
				VictimPlayerPawn = TargetPlayerPawn,
				Damage = Damage,
				DamageLocation = DamageOrigin,
			};

			TargetPlayerPawn.DamageComponent.TakeDamage(EnvDamageTaken);
			return; // NOTE : early return
		}

		// ---------------------- team check
		if (TargetPlayerPawn.Team == AttackerPlayerPawn.Team && TargetPlayerPawn != AttackerPlayerPawn/* && !TargetPlayerPawn.IsDummy*/)
		{
			return; // NOTE : early return
		}

		var TargetPoint = TargetPlayerPawn.CenterPosition;
		var TargetToAttackerDistance = TargetPoint.Distance(AttackerPlayerPawn.CenterPosition);

		// ---------------------- calculate damage
		switch (DamageRequest.DamageType)
		{
			case EDamageType.HitScan:
				// TODO
				break;

			case EDamageType.Projectile:
				{
					var TargetToImpactDistance = TargetPoint.Distance(DamageOrigin);

					if (DamageRequest.DamageFalloffType == EDamageFalloffType.Falloff)
					{
						float MaxDamageInterpFactor = TargetToAttackerDistance / PlayerDistanceFalloffMaxBound;
						float MaxDamage = MathX.Lerp(Damage, Damage * 0.33f, MaxDamageInterpFactor);
						// if a direct then tighten its damage falloff
						float MinDamage = DamageRequest.DirectImpact ? Damage * .33f : Damage * .15f;

						float DamageInterpFactor = TargetToImpactDistance / 200f;
						Damage = MathX.Lerp(MaxDamage, MinDamage, DamageInterpFactor);
					}
					else if (DamageRequest.DamageFalloffType == EDamageFalloffType.Rampup)
					{
						float MinDamage = DamageRequest.DirectImpact ? Damage * 0.4f : Damage * .15f;
						float InterpFactor = TargetToImpactDistance / 300;
						Damage = MathX.Lerp(MinDamage, Damage, InterpFactor);
					}
				}
				break;

			case EDamageType.Melee:
				// TODO
				break;
		}

		var DirectionVec = (TargetPoint - DamageOrigin).Normal;
		var Knockback = CalculateKnockback(DirectionVec, Damage, DamageRequest.BaseKnockbackStrength,
			TargetPlayerPawn.WeightFactor, TargetPlayerPawn.IsCrouching);

		bool WasSelfDamage = TargetPlayerPawn == AttackerPlayerPawn;
		if (WasSelfDamage && DamageRequest.DoesLessSelfDamage)
		{
			Damage *= SelfDamageMultiplyer;
		}

		FDamageTaken DamageTaken = new()
		{
			AttackerPlayerPawn = AttackerPlayerPawn,
			VictimPlayerPawn = TargetPlayerPawn,
			Damage = Damage,
			DamageLocation = DamageOrigin,
		};

		// ---------------------- deal the damage
		if (KnockbackOnly)
		{
			TargetPlayerPawn.DamageComponent.TakeKnockback(Knockback);
			return;
		}

		if (TargetPlayerPawn.Body.IsValid()) // TODO : move me
		{
			TargetPlayerPawn.Body.DamageTakenForce = Knockback * .66f;
		}
		TargetPlayerPawn.DamageComponent.TakeKnockback(Knockback);
		TargetPlayerPawn.DamageComponent.TakeDamage(DamageTaken);

		AttackerPlayerPawn.GameObject.Root.Dispatch(new DamageGivenEvent(DamageTaken));

		Log.Info($"{Damage} damage has been taken {AttackerPlayerPawn?.DisplayName} -> {TargetPlayerPawn.DisplayName}");


		// ---------------------- stats
		//if (TargetPlayerPawn.PlayerState.IsValid())
		//{

		//}

		//if (AttackerPlayerPawn.PlayerState.IsValid())
		//{
		//	using (Rpc.FilterInclude(AttackerPlayerPawn.PlayerState.Connection))
		//	{
		//		ClientDidDamage(AttackerPlayerPawn.PlayerState, AttackerPlayerPawn, Damage, WasSelfDamage);
		//	}
		//}
	}

	[Rpc.Host]
	private static void ServerInflictHealing(PlayerPawn Target, PlayerPawn Giver, float Healing, bool AllowOverhealing)
	{
		if (!Networking.IsHost)
		{
			Log.Warning("Trying to invoke server damage methods from client");
			return;
		}

		if (!Target.IsValid())
		{
			Log.Warning($"Invalid Target when trying to inflict healing on {Target}");
			return;
		}

		if (!Target.DamageComponent.IsValid())
		{
			Log.Warning($"Invalid damage comp when trying to inflict healing on {Target}");
			return;
		}

		Target.DamageComponent.Heal(Healing, AllowOverhealing);

		// ---------------------- stats
		//if (Target.PlayerState.IsValid())
		//{

		//}

		//if (Giver.PlayerState.IsValid())
		//{

		//}
	}

	[Rpc.Broadcast(NetFlags.HostOnly)]
	private void ClientDidDamage(PlayerState PlayerState, PlayerPawn PlayerPawn, float Damage, bool WasSelfDamage)
	{
		if (!WasSelfDamage)
		{
			var HitSound = Sound.Play(this.HitSound, PlayerPawn.WorldPosition);
			if (!HitSound.IsValid()) return;

			HitSound.Volume = 0.5f;
			HitSound.Pitch = MathX.Lerp(1.2f, 0.7f, Damage * .02f);
			HitSound.ListenLocal = true;
		}
	}
}
