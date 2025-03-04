using KOTH.UI;
using Sandbox;
using Sandbox.Citizen;
using Sandbox.Diagnostics;
using Sandbox.Events;

namespace KOTH;

public partial class PlayerPawn :
	IGameEventHandler<DamageGivenEvent>,
	IGameEventHandler<DamageTakenEvent>,
	IGameEventHandler<HealingGivenEvent>
{
	private void TickVFXs()
	{
		if (IsAlive /*HACK for proxy characters when body dies b4 health knows*/&& Body.IsValid())
		{
			Assert.True(Body.IsValid());
			Assert.True(AnimationHelper.IsValid());

			Body.WorldRotation = Rotation.FromYaw(EyeAngles.yaw);

			AnimationHelper.WithVelocity(CharacterController.Velocity);
			AnimationHelper.WithWishVelocity(WishVelocity);
			AnimationHelper.WithLook(EyeAngles.Forward, 1, 1, 1.0f);
			AnimationHelper.IsGrounded = IsGrounded;
			AnimationHelper.DuckLevel = IsCrouching ? .5f : 0;
			AnimationHelper.MoveStyle = CitizenAnimationHelper.MoveStyles.Run;
			AnimationHelper.HoldType = CitizenAnimationHelper.HoldTypes.Shotgun;
			AnimationHelper.Handedness = CitizenAnimationHelper.Hand.Both;
			AnimationHelper.IsWeaponLowered = false;
			AnimationHelper.AimBodyWeight = 1f;

			if (CurrentEquipment.IsValid())
			{
				AnimationHelper.HoldType = CurrentEquipment.HoldType;
				AnimationHelper.Handedness = CurrentEquipment.Handedness;
			}
			else
			{
				AnimationHelper.HoldType = CitizenAnimationHelper.HoldTypes.None;
			}

			var ClosestPlayerObject = GetClosestPlayerGameobject();

			// TODO : this would be fun but its causing issues with base look
			//if (ClosestPlayerObject != null)
			//{
			//	AnimationHelper.LookAtEnabled = true;
			//	AnimationHelper.LookAt = ClosestPlayerObject;
			//}
			//else
			//{
			//	AnimationHelper.LookAtEnabled = false;
			//}
		}

		if (TimeSinceLastUberMessage > .8f)
		{
			ClearMaterial();
		}
	}

	/////////////////////////////////////////////////////////////////////////////////

	private GameObject GetClosestPlayerGameobject()
	{
		GameObject ClosestPlayerObject = null;
		foreach (var PlayerState in GameNetworkManager.PlayerStates)
		{
			if (!PlayerState.PlayerPawn.IsValid() || !PlayerState.PlayerPawn.IsAlive || PlayerState == PlayerState.Local)
			{
				continue;
			}

			var Distance = Boom.WorldPosition.Distance(PlayerState.PlayerPawn.Boom.WorldPosition);

			if (ClosestPlayerObject == null && Distance < 500f)
			{
				ClosestPlayerObject = PlayerState.PlayerPawn.Boom;
			}
			else if (ClosestPlayerObject.IsValid())
			{
				if (Boom.WorldPosition.Distance(ClosestPlayerObject.WorldPosition) > Distance)
				{
					ClosestPlayerObject = PlayerState.PlayerPawn.Boom;
				}
			}
		}

		return ClosestPlayerObject;
	}

	/////////////////////////////////////////////////////////////////////////////////

	TimeSince TimeSinceLastUberMessage = 0;
	[Rpc.Broadcast]
	public void Uber()
	{
		TimeSinceLastUberMessage = 0;
		Body.ModelRenderer.SetMaterial(UberMaterial);
	}

	[Rpc.Broadcast]
	public void ClearMaterial()
	{
		Body.ModelRenderer.ClearMaterialOverrides();
	}

	/////////////////////////////////////////////////////////////////////////////////

	[Property] public GameObject DeathBlood { get; set; }

	const float GibMinDamage = 50f; // TODO : should be based on last hp!
	const float GibForce = 66f;

	[Rpc.Broadcast(NetFlags.HostOnly)]
	private void BroadcastOnPlayerDeath(FDamageTaken DamageTaken)
	{
		if (!Body.IsValid())
		{
			return;
		}

		if (DeathBlood.IsValid())
		{
			DeathBlood.Clone(CenterPosition);
		}

		if (DamageTaken.Damage > GibMinDamage)
		{
			CreateGibs(DamageTaken);
			Body.Destroy();
		}
		else
		{
			Body.Ragdoll(DamageTaken);
			Body.GameObject.SetParent(null, true);
		}
	}

	private void CreateGibs(FDamageTaken DamageTaken)
	{
		if (GibPrefab.IsValid())
		{
			var Gibs = GibPrefab.Clone(WorldPosition);
			foreach (var ChildGib in Gibs.Root.Children)
			{
				var Rigidbody = ChildGib.Components.Get<Rigidbody>();
				if (Rigidbody.IsValid())
				{
					// TODO : explode more outward
					Rigidbody.Velocity = (CenterPosition - DamageTaken.DamageLocation) * GibForce;
				}
			}
			Gibs.NetworkSpawn();
		}
	}

	/////////////////////////////////////////////////////////////////////////////////

	[Property] public GameObject BloodSquirt { get; set; }

	void IGameEventHandler<DamageTakenEvent>.OnGameEvent(DamageTakenEvent EventArgs)
	{
		OnDamageTaken(EventArgs.DamageEvent);
	}

	void IGameEventHandler<DamageGivenEvent>.OnGameEvent(DamageGivenEvent EventArgs)
	{
		OnDamageGiven(EventArgs.DamageEvent);
	}
	
	void IGameEventHandler<HealingGivenEvent>.OnGameEvent(HealingGivenEvent EventArgs)
	{
		OnDamageTaken(EventArgs.HealingRequest);
	}

	[Rpc.Broadcast(NetFlags.HostOnly)]
	public void OnDamageTaken(FHealingDone HealingDone)
	{
		if (HealingDone.TargetPlayerPawn == this)
		{
			UI.Health.Instance?.OnHealing(HealingDone);
		}
	}

	[Rpc.Broadcast(NetFlags.HostOnly)]
	public void OnDamageTaken(FDamageTaken DamageTaken)
	{
		if (IsLocallyControlled)
		{
			var DamageLocation = DamageTaken.DamageLocation;
			UI.DamageIndicatorNew.Instance?.OnHit(DamageLocation);
			UI.Health.Instance?.OnDamage(DamageTaken);
		}

		if (BloodSquirt.IsValid())
		{
			BloodSquirt.Clone(DamageTaken.DamageLocation);
		}
	}

	[Rpc.Broadcast(NetFlags.HostOnly)]
	public void OnDamageGiven(FDamageTaken DamageGiven)
	{
		if (IsLocallyControlled)
		{
			UI.DamageNumbers.Instance?.OnHit(DamageGiven);
		}
	}
}
