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

	[Property] public int MaxHealth { get; private set; } = 100;
	[Property] public GameObject TurretMuzzleObject { get; set; }
	[Sync] public int Damage { get; private set; } = 1;
	[Sync] public float KnockbackStrength { get; private set; } = 1f;
	[Sync] public float Firerate { get; private set; } = 1f;
	[Sync] public float Range { get; private set; } = 360f;

	[Property] public GameObject TrailPrefab { get; set; }

	////////////////////////////////////////////////////////////////////////

	[Sync(SyncFlags.FromHost)] public PlayerState OwnerState { get; set; }
	[Sync(SyncFlags.FromHost)] public PlayerPawn TargetPawn { get; private set; }

	public Action OnDestroyed = null;

	////////////////////////////////////////////////////////////////////////

	[Property, Sync(SyncFlags.FromHost)] public GameObject EquippedWeaponGameObject { get; private set; } = null;

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

		var IsDataLoaded = LoadDataFromInputWeaponComponent(WeaponComponent, WeaponModel, WeaponTint);

		if (IsDataLoaded)
		{
			EquippedWeaponGameObject = WeaponObject;
			return;
		}
	}

	private bool LoadDataFromInputWeaponComponent(InputWeaponComponent InputWeaponComponent, Model WeaponModel, Color WeaponTint)
	{
		if (!InputWeaponComponent.IsValid())
		{
			return false;
		}

		// TODO : check ownership!

		var WeaponStats = InputWeaponComponent.GetWeaponStats();
		Damage = (WeaponStats.BaseDamage * 0.66f).FloorToInt(); // !
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
		if (Networking.IsHost)
		{
			ShootTargetIfExists();
			
			if (WorldRotation.Up.z < 0.5)
			{
				GameObject.Root.Destroy();
			}
		}
	}

	private void ShootTargetIfExists()
	{
		var TargetPlayerPawn = GetTargetPlayerPawn();
		if (TargetPlayerPawn == null)
		{
			return;
		}

		var TargetPosition = TargetPlayerPawn.CenterPosition;
		FireShot(TargetPosition);
	}

	////////////////////////////////////////////////////////////////////////
	
	[Rpc.Broadcast(NetFlags.OwnerOnly)]
	public void DoTurretFX(Vector3 TargetPosition)
	{
		if (!TurretMuzzleObject.IsValid())
		{
			Log.Warning($"no muzzle m8 {this}");
			return;
		}

		TurretMuzzleObject.WorldRotation = Rotation.LookAt(TurretMuzzleObject.WorldPosition - TargetPosition);

		//if (TrailPrefab.IsValid())
		//{
		//	var Lerp = 0f;
		//	while (Lerp < 1f)
		//	{
		//		var Position = Vector3.Lerp(TurretMuzzleObject.WorldPosition, TargetPosition, Lerp);
		//		TrailPrefab.Clone(Position);
		//		Lerp += 0.033f;
		//	}
		//}
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

				DoTurretFX(TargetPosition);

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

		// TODO : track playerpawns better, this is slow
		foreach (var GameObject in Scene.GetAllObjects(true))
		{
			if (GameObject.GetComponentInChildren<PlayerPawn>() is { } PlayerPawn)
			{
				if (PlayerPawn.Team != DamageComponent.Team)
				{
					if (IsPlayerPawnValidTarget(PlayerPawn, out var Distance))
					{
						if (Distance > ShortestDistance)
						{
							continue;
						}

						ShortestDistance = Distance;
						BestTarget = PlayerPawn;
					}
				}
			}
		}

		return BestTarget;
	}
}
