using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Events;
using Sandbox.VR;
using System;
using System.Diagnostics;
using System.Numerics;

namespace KOTH;

public sealed class TurretComponent : Component
{
	[RequireComponent] public DamageComponent DamageComponent { get; private set; }

	////////////////////////////////////////////////////////////////////////

	[Property] public float MaxHealth { get; private set; } = 1f;
	[Property] public float Damage { get; private set; } = 1f;
	[Property] public float KnockbackStrength { get; private set; } = 1f;
	[Property] public float Firerate { get; private set; } = 1f;
	[Property] public float Range { get; private set; } = 256f;
	[Property] public GameObject TurretMuzzleObject { get; set; }

	[Property] public GameObject TrailPrefab { get; set; }

	////////////////////////////////////////////////////////////////////////

	[Sync(SyncFlags.FromHost)] public PlayerState OwnerState { get; set; }
	[Sync(SyncFlags.FromHost)] public PlayerPawn TargetPawn { get; private set; }

	public Action OnDestroyed = null;

	////////////////////////////////////////////////////////////////////////

	[Property, Sync(SyncFlags.FromHost)] public GameObject EquippedWeaponGameObject { get; private set; } = null;

	[Rpc.Broadcast(NetFlags.OwnerOnly)]
	public void SetFromWeaponGameObject(GameObject WeaponObject)
	{
		if (!WeaponObject.IsValid())
		{
			return;
		}

		var WeaponComponent = WeaponObject.GetComponent<InputWeaponComponent>();
		if (!WeaponComponent.IsValid())
		{
			return;
		}

		Model WeaponModel = null;
		Color WeaponTint = Color.Black;
		// TODO : make work
		// if (WeaponComponent.GameObject.Children.Any())
		//{
		//	var SkinnedModelRenderer = WeaponComponent.GameObject.Children[0].GetComponent<SkinnedModelRenderer>();
		//	if (SkinnedModelRenderer.IsValid())
		//	{

		//		WeaponModel = WeaponComponent.GameObject.Children[0].GetComponent<SkinnedModelRenderer>().Model;
		//		WeaponTint = WeaponComponent.GameObject.Children[0].GetComponent<SkinnedModelRenderer>().Tint;
		//	}
		//}

		var IsDataLoaded = LoadDataFromInputWeaponComponent(WeaponComponent, WeaponModel, WeaponTint);

		if (IsDataLoaded)
		{
			EquippedWeaponGameObject = WeaponObject;
			return;
		}

		return;
	}

	private bool LoadDataFromInputWeaponComponent(InputWeaponComponent InputWeaponComponent, Model WeaponModel, Color WeaponTint)
	{
		if (!InputWeaponComponent.IsValid())
		{
			return false;
		}

		// TODO : check ownership!

		var WeaponStats = InputWeaponComponent.GetWeaponStats();
		Damage = WeaponStats.BaseDamage * 0.66f; // !
		Firerate = WeaponStats.FireRate;
		KnockbackStrength = WeaponStats.KnockbackStrength;

		var TurretModelRenderer = TurretMuzzleObject.GetComponent<ModelRenderer>();
		if (TurretModelRenderer.IsValid() && WeaponModel.IsValid())
		{
			TurretModelRenderer.Model = WeaponModel;
			TurretModelRenderer.Tint = WeaponTint;
		}

		return true;
	}

	////////////////////////////////////////////////////////////////////////

	protected override void OnStart()
	{
		base.OnStart();

		Assert.NotNull(DamageComponent);

		DamageComponent.OnDeath += OnKill;

		if (Networking.IsHost)
		{
			DamageComponent.Initalize(MaxHealth, OwnerState.Team);
		}
	}

	private void OnKill(FDamageTaken DamageTaken)
	{
		OnDestroyed?.Invoke();
		GameObject.Root.Destroy();
	}
	
	////////////////////////////////////////////////////////////////////////

	protected override void OnFixedUpdate()
	{
		ShootTargetIfExists();
	}

	private void ShootTargetIfExists()
	{
		var TargetPlayerPawn = GetTargetPlayerPawn();
		if (TargetPlayerPawn == null)
		{
			return;
		}

		var TargetPosition = TargetPlayerPawn.CenterPosition;

		var HasFiredShot = FireShot(TargetPosition);
		DoTurretFX(TargetPosition, HasFiredShot);
	}

	////////////////////////////////////////////////////////////////////////
	
	[Rpc.Broadcast(NetFlags.OwnerOnly)]
	public void DoTurretFX(Vector3 TargetPosition, bool IsShooting)
	{
		if (!TurretMuzzleObject.IsValid())
		{
			Log.Warning($"no muzzle m8 {this}");
			return;
		}

		TurretMuzzleObject.WorldRotation = Rotation.LookAt(TurretMuzzleObject.WorldPosition - TargetPosition);

		if (!IsShooting)
		{
			return;
		}

		if (TrailPrefab.IsValid())
		{
			var Lerp = 0f;
			while (Lerp < 1f)
			{
				var Position = Vector3.Lerp(TurretMuzzleObject.WorldPosition, TargetPosition, Lerp);
				TrailPrefab.Clone(Position);
				Lerp += 0.033f;
			}
		}
	}

	private TimeSince TimeSinceShot = new();
	private bool FireShot(Vector3 TargetPosition)
	{
		if (!Networking.IsHost)
		{
			return false;
		}

		if (TimeSinceShot < Firerate)
		{
			return false;
		}

		var Shots = ShootHelper.GetShootTraceElements(Scene.Trace, GameObject, TurretMuzzleObject.WorldPosition, TargetPosition, DebugOverlay);
		foreach (var TraceElement in Shots)
		{
			if (!TraceElement.Hit)
			{
				continue;
			}

			if (TraceElement.GameObject.Root.Components.Get<PlayerPawn>(FindMode.EnabledInSelfAndDescendants) is { } HitPlayerPawn)
			{
				FDamageRequest DamageRequest = new()
				{
					TargetDamageComponent = HitPlayerPawn.DamageComponent,
					AttackerPlayerPawn = OwnerState.PlayerPawn,
					TargetPlayerPawn = HitPlayerPawn,
					DamageOrigin = TraceElement.HitPosition,
					TargetOrigin = HitPlayerPawn.CenterPosition,
					BaseDamage = Damage,
					BaseKnockbackStrength = KnockbackStrength,
					DamageType = EDamageType.HitScan,
					DamageFalloffType = EDamageFalloffType.Falloff,
				};

				Scene.Dispatch(new DamageRequestEvent(DamageRequest));
				
				TimeSinceShot = 0;
				return true;
			}
		}

		return false;
	}

	private bool IsPlayerPawnValidTarget(PlayerPawn PlayerPawn, out float OutDistance)
	{
		OutDistance = float.PositiveInfinity;

		if (!PlayerPawn.IsValid() || !PlayerPawn.IsAlive)
		{
			return false;
		}

		var Distance = Math.Abs((PlayerPawn.WorldPosition - GameObject.WorldPosition).Length);
		OutDistance = Distance;

		if (Distance > Range)
		{
			return false;
		}

		return true;
	}

	private PlayerPawn GetTargetPlayerPawn()
	{
		if (IsPlayerPawnValidTarget(TargetPawn, out _))
		{
			return TargetPawn;
		}

		PlayerPawn BestTarget = null;
		float ShortestDistance = float.PositiveInfinity;

		foreach (var PlayerState in GameNetworkManager.PlayerStates) // TODO : change to player pawns in world
		{
			if (!PlayerState.IsValid())
			{
				Log.Warning("player state not valid when finding turret target");
				continue;
			}

			if (!PlayerState.PlayerPawn.IsValid() || !PlayerState.PlayerPawn.IsAlive || PlayerState.Team == DamageComponent.Team)
			{
				continue;
			}

			var Player = PlayerState.PlayerPawn;
			if (IsPlayerPawnValidTarget(Player, out var Distance))
			{
				if (Distance > ShortestDistance)
				{
					continue;
				}

				ShortestDistance = Distance;
				BestTarget = Player;
			}
		}

		return BestTarget;
	}
}
