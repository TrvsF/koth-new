using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Events;
using Sandbox.Utility;
using System.ComponentModel.DataAnnotations;
using static Sandbox.PhysicsContact;

namespace KOTH;

// TODO : rename health manager
public sealed class DamageManager : SingletonComponent<DamageManager>,
	IGameEventHandler<DamageRequestEvent>,
	IGameEventHandler<HealingRequestEvent>
{
	[Property] public SoundEvent HitSound { get; set; }

	// special bool for jumper gamemode
	[Property] public bool KnockbackOnly { get; private set; } = false;

	const float SelfDamageMultiplyer = 0.2f;
	const float PlayerDistanceFalloffMaxBound = 1000;

	/////////////////////////////////////////////////////////////////////////////////////////////

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

	/////////////////////////////////////////////////////////////////////////////////////////////

	const float MaxKB = 1800f;
	private static Vector3 CalculateKnockback(Vector3 DirectionVec, float Damage, float WeaponKnockbackStrength, float WeightKnockbackFactor, bool IsCrouching)
	{
		var CrouchFactor = IsCrouching ? 62 : 82;
		var KnockbackFactor = Damage * (WeaponKnockbackStrength / CrouchFactor);

		KnockbackFactor = Math.Min(KnockbackFactor, MaxKB);
		return DirectionVec * KnockbackFactor * WeightKnockbackFactor;
	}

	/////////////////////////////////////////////////////////////////////////////////////////////
	// host only broadcasts
	/////////////////////////////////////////////////////////////////////////////////////////////

	[Rpc.Host]
	private void ServerInflictDamageToPlayer(FDamageRequest DamageRequest)
	{
		Assert.True(Networking.IsHost);

		var DamageOrigin = DamageRequest.DamageOrigin;
		var TargetDamageComponent = DamageRequest.TargetDamageComponent;
		var AttackerPlayerPawn = DamageRequest.AttackerPlayerPawn;
		var Damage = DamageRequest.BaseDamage;

		if (!TargetDamageComponent.IsValid() || TargetDamageComponent.Health <= 0)
		{
			return;
		}

		// we've taken damage without an attacker pawn, apply & return early ////////////////////////////////
		if (!AttackerPlayerPawn.IsValid())
		{
			FDamageTaken EnvDamageTaken = new()
			{
				AttackerPlayerPawn = null,
				VictimPlayerPawn = DamageRequest.TargetPlayerPawn,
				Damage = Damage,
				DamageLocation = DamageOrigin,
			};

			TargetDamageComponent.TakeDamage(EnvDamageTaken);
			return; // NOTE : early return
		}

		// team check ////////////////////////////////
		//if (TargetDamageComponent.Team == AttackerPlayerPawn.Team && TargetDamageComponent != AttackerPlayerPawn && !TargetDamageComponent.IsDummy)
		//{
		//	// return; // NOTE : early return
		//}

		// we want to target the hit object's center of mass
		var TargetPoint = DamageRequest.TargetOrigin;

		// calculate damage /////////////
		switch (DamageRequest.DamageType)
		{
			case EDamageType.HitScan: // meant to follow thru
			case EDamageType.Projectile:
				{
					var TargetToImpactDistance = TargetPoint.Distance(DamageOrigin);
					var TargetToAttackerDistance = TargetPoint.Distance(AttackerPlayerPawn.CenterPosition);

					if (DamageRequest.DamageFalloffType == EDamageFalloffType.Falloff)
					{
						float MaxDamageInterpFactor = TargetToAttackerDistance / PlayerDistanceFalloffMaxBound;
						float MaxDamage = MathX.Lerp(Damage, Damage * .33f, MaxDamageInterpFactor);

						// if a direct then tighten its damage falloff
						float MinDamage = DamageRequest.DirectImpact ? Damage * .33f : Damage * .15f;

						float DamageInterpFactor = TargetToImpactDistance / 200f;
						//Log.Info($"max : {MaxDamage}, min : {MinDamage}, Lerp : {DamageInterpFactor}");
						Damage = MathX.Lerp(MaxDamage, MinDamage, DamageInterpFactor);
					}
					else if (DamageRequest.DamageFalloffType == EDamageFalloffType.Rampup)
					{
						float MinDamage = DamageRequest.DirectImpact ? Damage * 0.4f : Damage * .15f;
						float InterpFactor = TargetToImpactDistance / 300f;
						Damage = MathX.Lerp(MinDamage, Damage, InterpFactor);
					}
				}
				break;

			case EDamageType.Melee:
				// TODO
				break;
		}

		// knockback ////////////////
		var Knockback = Vector3.Zero;
		if (DamageRequest.TargetPlayerPawn.IsValid())
		{ 
			var DirectionVec = (TargetPoint - DamageOrigin).Normal;
			Knockback = CalculateKnockback(DirectionVec, Damage, DamageRequest.BaseKnockbackStrength,
				DamageRequest.TargetPlayerPawn.WeightFactor, DamageRequest.TargetPlayerPawn.IsCrouching);

			DamageRequest.TargetPlayerPawn.DoKnockback(Knockback);
		}

		bool WasSelfDamage = DamageRequest.TargetPlayerPawn == AttackerPlayerPawn;
		if (WasSelfDamage && DamageRequest.DoesLessSelfDamage)
		{
			Damage *= SelfDamageMultiplyer;
		}

		if (KnockbackOnly)
		{
			return;
		}
		
		FDamageTaken DamageTaken = new()
		{
			AttackerPlayerPawn = AttackerPlayerPawn,
			VictimPlayerPawn = DamageRequest.TargetPlayerPawn,
			Damage = Damage,
			DamageLocation = DamageOrigin,
		};

		// deal the damage ///////////////////////////
		TargetDamageComponent.TakeDamage(DamageTaken);

		AttackerPlayerPawn.GameObject.Root.Dispatch(new DamageGivenEvent(DamageTaken));

		Log.Info($"{Damage:0.0}:{Knockback.Length:0.0} damage:kb has been taken {AttackerPlayerPawn.DisplayName}:{AttackerPlayerPawn.Health}" +
			$" -> {DamageRequest.TargetPlayerPawn?.DisplayName}:{TargetDamageComponent.Health}");
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
