using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Events;
using Sandbox.VR;
using System;
using System.Numerics;

namespace KOTH;

public static class ShootHelper
{
	// public static List<String> IgnoreTags { get; private set; } = [""];

	public static IEnumerable<SceneTraceResult> GetShootTraceElements(SceneTrace SceneTrace, GameObject OriginObject, Vector3 OriginPoint, Vector3 TargetPoint, DebugOverlaySystem DebugOverlay = null)
	{
		var ShotHits = new List<SceneTraceResult>();

		var TraceResults = SceneTrace.Ray(OriginPoint, TargetPoint) // magic
			.UseHitboxes()
			.IgnoreGameObjectHierarchy(OriginObject.Root)
			// .WithoutTags([.. IgnoreTags])
			.Size(Vector3.One)
			.RunAll();

		if (DebugOverlay != null)
		{
			Line Line = new(OriginPoint, TargetPoint);
			DebugOverlay.Line(Line);
		}

		if (!TraceResults.Any())
		{
			return TraceResults; // NOTE : early return
		}

		// Run through and fix the start positions for the traces
		// By using the last end position as the start

		Vector3 StartPosition = TraceResults.ElementAt(0).StartPosition;
		List<SceneTraceResult> FixedPath = new();
		for (int i = 0; i < TraceResults.Count(); i++)
		{
			var Element = TraceResults.ElementAt(i);

			FixedPath.Add(Element with { StartPosition = StartPosition });
			StartPosition = Element.EndPosition;
		}

		var Entries = new List<(SceneTraceResult Trace, float Thickness)>();

		// Then, trace backwards from the end so we can get exit points and thickness
		for (int i = FixedPath.Count - 1; i >= 0; i--)
		{
			var TraceElement = FixedPath.ElementAt(i);

			// Do a trace back, from the end position to the start, this'll give us the LAST entry's exit point.
			var BackTrace = SceneTrace.Ray(TraceElement.EndPosition, TraceElement.StartPosition)
			.UseHitboxes()
			.IgnoreGameObjectHierarchy(OriginObject.Root)
			//.WithoutTags(IgnoreTags)
			.Size(Vector3.One)
			.Run();
			var impact = BackTrace.EndPosition;

			// From that, we can calculate the surface thickness
			float thickness = (TraceElement.StartPosition - impact).Length;

			// Return the element starting at the exit point, it's more useful that way.
			TraceElement = TraceElement with { StartPosition = impact };
			Entries.Insert(0, (TraceElement, thickness));
		}

		float Thickness = 0;
		foreach (var Element in Entries)
		{
			Thickness += Element.Thickness;
			if (Thickness >= 100)
				break;

			ShotHits.Add(Element.Trace);
		}

		return ShotHits;
	}
}


public sealed class TurretComponent : Component
{
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

		Damage = InputWeaponComponent.BaseDamage;
		Firerate = InputWeaponComponent.FireRate;
		KnockbackStrength = InputWeaponComponent.KnockbackStrength;

		var TurretModelRenderer = TurretMuzzleObject.GetComponent<ModelRenderer>();
		if (TurretModelRenderer.IsValid() && WeaponModel.IsValid())
		{
			TurretModelRenderer.Model = WeaponModel;
			TurretModelRenderer.Tint = WeaponTint;
		}

		return true;
	}

	////////////////////////////////////////////////////////////////////////

	[Property] public float Damage { get; private set; } = 1f;
	[Property] public float KnockbackStrength { get; private set; } = 1f;
	[Property] public float Firerate { get; private set; } = 1f;
	[Property] public float Range { get; private set; } = 256f;
	[Property] public GameObject TurretMuzzleObject { get; set; }

	////////////////////////////////////////////////////////////////////////

	[Sync(SyncFlags.FromHost)] public PlayerPawn OwnerPawn { get; private set; }
	[Sync(SyncFlags.FromHost)] public PlayerPawn TargetPawn { get; private set; }

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
		DoTurretFX(TurretMuzzleObject.WorldPosition - TargetPosition, HasFiredShot);
	}

	[Rpc.Broadcast(NetFlags.OwnerOnly)]
	public void DoTurretFX(Vector3 TargetForwardVector, bool IsShooting)
	{
		TurretMuzzleObject.WorldRotation = Rotation.LookAt(TargetForwardVector);
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
					TargetPlayerPawn = HitPlayerPawn,
					// AttackerPlayerPawn = LocalPlayerPawn,
					DamageOrigin = TraceElement.HitPosition,
					BaseDamage = Damage,
					BaseKnockbackStrength = KnockbackStrength,
					DamageType = EDamageType.HitScan,
					DamageFalloffType = EDamageFalloffType.Falloff,
					DoesLessSelfDamage = true,
					MaxFalloffDistance = 5000,
				};

				Scene.Dispatch(new DamageRequestEvent(DamageRequest));
			}
		}

		TimeSinceShot = 0;
		return true;
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

		foreach (var PlayerState in GameNetworkManager.PlayerStates)
		{
			if (!PlayerState.IsValid())
			{
				Log.Warning("player state not valid when finding turret target");
				continue;
			}

			if (!PlayerState.PlayerPawn.IsValid() || !PlayerState.PlayerPawn.IsAlive)
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
