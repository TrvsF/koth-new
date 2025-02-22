using KOTH.UI;
using Sandbox;
using Sandbox.Citizen;
using Sandbox.Diagnostics;
using Sandbox.Events;

namespace KOTH;

public partial class PlayerPawn
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
			AnimationHelper.IsGrounded = IsGrounded;
			AnimationHelper.DuckLevel = IsCrouching ? .5f : 0;
			AnimationHelper.WithLook(EyeAngles.Forward, 1, 1, 1.0f);
			AnimationHelper.MoveStyle = CitizenAnimationHelper.MoveStyles.Run;
			AnimationHelper.HoldType = CitizenAnimationHelper.HoldTypes.Shotgun;
			AnimationHelper.Handedness = CitizenAnimationHelper.Hand.Both;
			AnimationHelper.IsWeaponLowered = false;
			AnimationHelper.AimBodyWeight = 1f;
			// AnimationHelper.DuckLevel = (MathF.Abs(_smoothEyeHeight) / 32.0f);
			// AnimationHelper.HoldType = CurrentHoldType;
			// AnimationHelper.Handedness = CurrentEquipment.IsValid() ? CurrentEquipment.Handedness : AnimationHelper.Hand.Both;

			// CurrentHoldType = CurrentEquipment.IsValid() ? CurrentEquipment.GetHoldType() : AnimationHelper.HoldTypes.None;
		}

		if (TimeSinceLastUberMessage > .8f)
		{
			ClearMaterial();
		}
	}
	
	/////////////////////////////////////////////////////////////////////////////////

	TimeSince TimeSinceLastUberMessage = 0;
	[Rpc.Broadcast]
	public void Uber()
	{
		TimeSinceLastUberMessage = 0;
		Body.Renderer.SetMaterial(UberMaterial);
	}

	[Rpc.Broadcast]
	public void ClearMaterial()
	{
		Body.Renderer.ClearMaterialOverrides();
	}

	/////////////////////////////////////////////////////////////////////////////////

	const float GibMinDamage = 60f; // TODO : should be based on last hp!
	const float GibForce = 66f;

	[Rpc.Broadcast(NetFlags.HostOnly)]
	private void BroadcastOnPlayerDeath(FDamageTaken DamageTaken)
	{
		if (!Body.IsValid())
		{
			return;
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

	void IGameEventHandler<DamageTakenEvent>.OnGameEvent(DamageTakenEvent EventArgs)
	{
		var DamageEvent = EventArgs.DamageEvent;

		var VictimGameobject = GameUtils.GetPlayerFromComponent(DamageEvent.AttackerPlayerPawn);
		var DamageLocation = DamageEvent.DamageLocation;
	}

	void IGameEventHandler<DamageGivenEvent>.OnGameEvent(DamageGivenEvent EventArgs)
	{
		OnDamageGiven(EventArgs.DamageEvent);
	}

	[Rpc.Broadcast]
	void OnDamageGiven(FDamageTaken DamageTaken)
	{
		if (!IsViewer)
		{
			return;
		}

		DamageNumbers.Instance?.OnHit(DamageTaken);
	}
}
