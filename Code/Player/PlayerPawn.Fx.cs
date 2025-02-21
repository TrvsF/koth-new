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

	TimeSince TimeSinceLastUberMessage = 0;
	[Rpc.Broadcast] // TODO : make host only
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

	[Rpc.Broadcast(NetFlags.HostOnly)]
	private void CreateGibs()
	{
		if (GibPrefab.IsValid())
		{
			var Gibs = GibPrefab.Clone(WorldPosition);
			foreach (var ChildGib in Gibs.Root.Children)
			{
				var Rigidbody = ChildGib.Components.Get<Rigidbody>();
				if (Rigidbody.IsValid())
				{
					Rigidbody.Velocity = new(Random.Shared.Int(-1000, 1000), Random.Shared.Int(-1000, 1000), Random.Shared.Int(-1000, 1000));
				}
			}
			Gibs.NetworkSpawn();
		}

		Body?.Destroy();
	}

	[Rpc.Broadcast(NetFlags.HostOnly)]
	private void CreateRagdoll()
	{
		if (!Body.IsValid())
			return;

		Body.SetRagdoll(true);
		Body.GameObject.SetParent(null, true);
	}

	private void ResetBody()
	{
		if (Body is not null)
		{
			Body.DamageTakenForce = Vector3.Zero;
		}

		PlayerBoxCollider.Enabled = true;

		// Components.Get<HumanOutfitter>(FindMode.EnabledInSelfAndDescendants)?.UpdateFromTeam(Team);
	}

	void IGameEventHandler<DamageTakenEvent>.OnGameEvent(DamageTakenEvent EventArgs)
	{
		var DamageEvent = EventArgs.DamageEvent;

		var VictimGameobject = GameUtils.GetPlayerFromComponent(DamageEvent.AttackerPlayerPawn);
		var DamageLocation = DamageEvent.DamageLocation;
	}

	void IGameEventHandler<DamageGivenEvent>.OnGameEvent(DamageGivenEvent EventArgs)
	{
		OnDamageGiven(EventArgs.DamageEvent.VictimPlayerPawn, EventArgs.DamageEvent.Damage);
	}

	[Rpc.Broadcast]
	void OnDamageGiven(PlayerPawn Target, float Damage)
	{
		if (!IsViewer)
		{
			return;
		}

		DamageNumbers.Instance?.OnHit(Damage, Target);
	}
}
