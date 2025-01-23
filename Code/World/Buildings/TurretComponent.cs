using Sandbox;
using Sandbox.Events;
using Sandbox.VR;
using System;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using static Sandbox.PhysicsContact;

namespace KOTH;

public static class ShootHelper
{
	public static List<String> IgnoreTags { get; private set; } = [""];

	public static IEnumerable<SceneTraceResult> GetShootTraceElements(SceneTrace SceneTrace, GameObject OriginObject, Vector3 OriginPoint, Vector3 TargetPoint, DebugOverlaySystem DebugOverlay = null)
	{
		var ShotHits = new List<SceneTraceResult>();

		var TraceResults = SceneTrace.Ray(OriginPoint, TargetPoint) // magic
			.UseHitboxes()
			.IgnoreGameObjectHierarchy(OriginObject.Root)
			.WithoutTags([.. IgnoreTags])
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
			if (Thickness >= 10)
				break;

			ShotHits.Add(Element.Trace);
		}

		return ShotHits;
	}
}


public sealed class TurretComponent : Component
{
	[Property] public GameObject TurretMuzzleObject { get; private set; }

	////////////////////////////////////////////////////////////////////////

	public PlayerPawn OwnerPawn { get; private set; }

	////////////////////////////////////////////////////////////////////////

	protected override void OnFixedUpdate()
	{
		if (!Networking.IsHost)
		{
			return;
		}

		ShootTargetPlayerPawn();
	}

	private bool ShootTargetPlayerPawn()
	{
		var TargetPlayerPawn = GetTargetPlayerPawn();
		if (TargetPlayerPawn == null)
		{
/**/		return false;
		}

		var TargetPosition = TargetPlayerPawn.CenterPosition;

		var TargetForwardVector = TurretMuzzleObject.WorldPosition - TargetPosition;
		TurretMuzzleObject.WorldRotation = Rotation.LookAt(TargetForwardVector);

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
					BaseDamage = 0,
					BaseKnockbackStrength = 500,
					DamageType = EDamageType.HitScan,
					DamageFalloffType = EDamageFalloffType.Falloff,
					DoesLessSelfDamage = true,
					MaxFalloffDistance = 5000,
				};

				Scene.Dispatch(new DamageRequestEvent(DamageRequest));

/**/			return true;
			}
		}

		return false;
	}

	private PlayerPawn GetTargetPlayerPawn()
	{
		return PlayerState.Local.PlayerPawn;
	}
}
