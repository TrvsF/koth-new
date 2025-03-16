using Sandbox.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KOTH;

internal static class WorldUtil
{
	public static CharacterDefinition GetRandomCharacter()
	{
		var ClassList = GameMode.Instance.Components.Get<ClassList>();
		Assert.NotNull(ClassList);

		return Random.Shared.FromList(ClassList.ClassDefinitions);
	}
}

public class DamageComponentTupleComparer : IEqualityComparer<(DamageComponent DamageComponent, Vector3 HitLocation)>
{
	public bool Equals((DamageComponent DamageComponent, Vector3 HitLocation) x, (DamageComponent DamageComponent, Vector3 HitLocation) y)
	{
		// Compare only the DamageComponent part
		return x.DamageComponent == y.DamageComponent;
	}

	public int GetHashCode((DamageComponent DamageComponent, Vector3 HitLocation) obj)
	{
		// Use the hash code of the DamageComponent
		return obj.DamageComponent.GetHashCode();
	}
}

public static class ShootHelper
{
	// public static List<String> IgnoreTags { get; private set; } = [""];

	public static HashSet<(DamageComponent DamageComponent, Vector3 HitLocation)> GetDamageComponentsFromTrace(SceneTrace SceneTrace, GameObject OriginObject, Vector3 OriginPoint, Vector3 TargetPoint, out Transform FirstImpactTransform, DebugOverlaySystem DebugOverlay = null)
	{
		var TraceElements = GetShootTraceElements(SceneTrace, OriginObject, OriginPoint, TargetPoint, DebugOverlay);

		var FirstTrace = TraceElements.FirstOrDefault();
		FirstImpactTransform = new(FirstTrace.HitPosition, Rotation.LookAt(-FirstTrace.Normal, Vector3.Random), 0);

		var TraceElementsFiltered = TraceElements
			.Where(Trace => Trace.Hit)
			.Where(Trace => Trace.GameObject.Root.GetComponent<DamageComponent>() != null)
			.Select(Trace => (Trace.GameObject.Root.GetComponent<DamageComponent>(), Trace.HitPosition));

		return TraceElementsFiltered.ToHashSet(new DamageComponentTupleComparer());
	}

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
			if (Thickness >= 10)
				break;

			ShotHits.Add(Element.Trace);
		}

		return ShotHits;
	}
}

