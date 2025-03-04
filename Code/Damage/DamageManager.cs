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

	const float SelfDamageMultiplyer = 0.25f;

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

		// we've taken damage without an attacker pawn, apply & return early ///////////////////
		if (!AttackerPlayerPawn.IsValid())
		{
			FDamageTaken EnvDamageTaken = new()
			{
				VictimPlayerPawn = DamageRequest.TargetPlayerPawn,
				Damage = Damage,
				DamageLocation = DamageOrigin,
			};

			TargetDamageComponent.TakeDamage(EnvDamageTaken);
			return; // NOTE : early return
		}

		var TargetPlayerPawn = DamageRequest.TargetPlayerPawn;
		
		// team check ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		if (TargetDamageComponent.Team == AttackerPlayerPawn.Team && TargetPlayerPawn != AttackerPlayerPawn && !TargetPlayerPawn.IsDummy)
		{
			return; // NOTE : early return
		}

		var TargetCenter = DamageRequest.TargetOrigin;

		// calculate damage ///////////////////////////////////////////
		if (DamageRequest.DamageFalloffType != EDamageFalloffType.None)
		{
			var TargetToImpactDistance = TargetCenter.Distance(DamageOrigin);
			var TargetToAttackerDistance = TargetCenter.Distance(AttackerPlayerPawn.CenterPosition);

			float MaxDamage = Damage;
			if (DamageRequest.DamageType == EDamageType.Projectile && !DamageRequest.DirectImpact)
			{
				var DamageDistanceLerp = TargetToImpactDistance / DamageRequest.MaxDamageImpactDistance;
				MaxDamage = MathX.Lerp(MaxDamage, MaxDamage * 0.5f, DamageDistanceLerp);
			}

			float MinDamage = MaxDamage * 0.33f;
			float DamageLerp = TargetToAttackerDistance / 1600f;

			if (DamageRequest.DamageFalloffType == EDamageFalloffType.Falloff)
			{
				Damage = MathX.Lerp(MaxDamage, MinDamage, DamageLerp);
			}
			else
			{
				Damage = MathX.Lerp(MinDamage, MaxDamage, DamageLerp);
			}
		}


		// knockback ////////////////
		var Knockback = Vector3.Zero; 
		if (TargetPlayerPawn.IsValid())
		{
			var DirectionVec = (TargetCenter - DamageOrigin).Normal;
			var CrouchFactor = TargetPlayerPawn.IsCrouching ? 62 : 82;
			var KnockbackFactor = Damage * (DamageRequest.BaseKnockbackStrength / CrouchFactor);

			Knockback = DirectionVec * KnockbackFactor * TargetPlayerPawn.WeightFactor;
			TargetPlayerPawn.DoKnockback(Knockback);
		}

		if (KnockbackOnly)
		{
			return; // !
		}

		// deal the damage /////////////////////////////////////////
		bool WasSelfDamage = TargetPlayerPawn == AttackerPlayerPawn;
		if (WasSelfDamage && DamageRequest.DoesLessSelfDamage)
		{
			Damage *= SelfDamageMultiplyer;
		}

		FDamageTaken DamageTaken = new()
		{
			VictimGameObject = TargetDamageComponent.GameObject,
			AttackerPlayerPawn = AttackerPlayerPawn,
			VictimPlayerPawn = TargetPlayerPawn,
			Damage = Damage,
			DamageLocation = DamageOrigin,
			DamageType = DamageRequest.DamageType,
		};

		TargetDamageComponent.TakeDamage(DamageTaken);
		AttackerPlayerPawn.GameObject.Root.Dispatch(new DamageGivenEvent(DamageTaken));

		Log.Info($"{Damage:0.0}:{Knockback.Length:0.0} damage:kb has been taken {AttackerPlayerPawn.DisplayName}:{AttackerPlayerPawn.Health}" +
			$" -> {TargetPlayerPawn?.DisplayName}:{TargetDamageComponent.Health}");
	}

	[Rpc.Host]
	private void ServerInflictHealing(PlayerPawn Target, PlayerPawn Giver, float Healing, bool AllowOverhealing)
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

		FHealingDone HealingDone = new()
		{
			TargetPlayerPawn = Target,
			HealerPlayerPawn = Giver,
			Healing = Healing,
		};
		Scene.Dispatch(new HealingGivenEvent(HealingDone));

		Target.DamageComponent.Heal(Healing, AllowOverhealing);
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
