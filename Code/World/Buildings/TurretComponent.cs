using Sandbox;
using Sandbox.VR;
using System;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using static Sandbox.PhysicsContact;

namespace KOTH;

public sealed class TurretComponent : Component
{
	[Property] public GameObject TurretMuzzleObject { get; private set; }
	
	////////////////////////////////////////////////////////////////////////
	
	public PlayerPawn OwnerPawn { get; private set; }

	////////////////////////////////////////////////////////////////////////

	protected override void OnUpdate()
	{
		ShootTargetPlayerPawn();
	}

	private bool ShootTargetPlayerPawn()
	{
		var TargetPlayerPawn = GetTargetPlayerPawn();
		if (TargetPlayerPawn == null)
		{
			return false; // NOTE : early return
		}

		var TargetForwardVector = TurretMuzzleObject.WorldPosition - TargetPlayerPawn.CenterPosition;
		TurretMuzzleObject.WorldRotation = Rotation.LookAt(TargetForwardVector);

		foreach (var TraceElement in GetShootTraceElements(TargetForwardVector))
		{
			if (!TraceElement.Hit)
			{
				continue;
			}

			Log.Info("HIT");
			return true;
		}

		return false;
	}

	private PlayerPawn GetTargetPlayerPawn()
	{
		return PlayerState.Local.PlayerPawn;
	}

	private IEnumerable<SceneTraceResult> GetShootTraceElements(Vector3 TargetPoint)
	{
		var ShotHits = new List<SceneTraceResult>();

		var TraceStart = TurretMuzzleObject.WorldPosition;
		var StartRotation = Rotation.LookAt(TargetPoint);
		var TraceForward = StartRotation.Forward.Normal;

		var TraceResults = Scene.Trace.Ray(TraceStart, TurretMuzzleObject.WorldPosition + TraceForward * 400f) // magic
		   .UseHitboxes()
		   .IgnoreGameObjectHierarchy(GameObject.Root)
		   .WithoutTags("trigger", "self", "turret")
		   .Size(Vector3.One)
		   .RunAll();

		if (!TraceResults.Any()) return TraceResults;

		// Run through and fix the start positions for the traces
		// By using the last end position as the start

		int depth = 0;
		Vector3 startPos = TraceResults.ElementAt(0).StartPosition;
		List<SceneTraceResult> fixedPath = new();
		for (int i = 0; i < TraceResults.Count(); i++)
		{
			var el = TraceResults.ElementAt(i);

			fixedPath.Add(el with { StartPosition = startPos });
			startPos = el.EndPosition;
		}

		var entries = new List<(SceneTraceResult Trace, float Thickness)>();

		// Then, trace backwards from the end so we can get exit points and thickness
		for (int i = fixedPath.Count - 1; i >= 0; i--)
		{
			var TraceElement = fixedPath.ElementAt(i);

			// Do a trace back, from the end position to the start, this'll give us the LAST entry's exit point.
			var backTrace = Scene.Trace.Ray(TraceElement.EndPosition, TraceElement.StartPosition)
			.UseHitboxes()
			.IgnoreGameObjectHierarchy(GameObject.Root)
			.WithoutTags("trigger", "playerclip", "movement")
			.Size(Vector3.One)
			.Run();
			var impact = backTrace.EndPosition;

			// From that, we can calculate the surface thickness
			float thickness = (TraceElement.StartPosition - impact).Length;

			// Return the element starting at the exit point, it's more useful that way.
			TraceElement = TraceElement with { StartPosition = impact };
			entries.Insert(0, (TraceElement, thickness));
		}

		depth = 0;
		float accThickness = 0;
		foreach (var el in entries)
		{
			accThickness += el.Thickness;
			if (accThickness >= 100)
				break;

			ShotHits.Add(el.Trace);
			// DrawLineSegment( el.Trace.StartPosition, el.Trace.EndPosition, depth, fixedPath.Count() );
			depth++;
		}

		return ShotHits;
	}
}
