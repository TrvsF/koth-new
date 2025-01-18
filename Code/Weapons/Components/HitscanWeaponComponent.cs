using KOTH.UI;
using Sandbox;
using Sandbox.Events;
using System.Net.Http;
using System;
using System.Text;
using Sandbox.Diagnostics;

namespace KOTH;

[Title("Hitscan Shooter"), Group("Weapon Components")]
public class HitscanWeaponComponent : InputWeaponComponent
{
	private enum EHitscanFireType
	{
		SingleShot,
		Continuous,
		Infinite,
	}

	[Property, Group("HitScan")] private EHitscanFireType FireType
	{
		get => GetFireType();
	}

	private EHitscanFireType GetFireType()
	{
		if (MaxAmmo == -1)
		{
			return EHitscanFireType.Infinite;
		}
		else if (MaxAmmo == 1)
		{
			return EHitscanFireType.SingleShot;
		}
		else
		{
			return EHitscanFireType.Continuous;
		}
	}

	[Property, Group("Hitscan")] public float FireRate { get; set; } = 0.2f;

	////////////////////////////////////////////////////////////////////////

	protected override void OnInputUpdate()
	{
		bool IsShooting = IsDown() && CanShoot();

		// TODO : should this be a host/server rpc?
		if (IsProxy)
		{
			return;
		}

		if (IsShooting)
		{
			Shoot();
		}
	}

	protected virtual void Shoot()
	{
		var PlayerPawn = Equipment.Owner;
		if (!PlayerPawn.IsValid())
		{
			return;
		}

		Log.Info("shoot");

		TimeSinceShot = 0;
		Ammo--;

		var AimForward = PlayerPawn.AimRay.Forward;

		foreach (var TraceElement in GetShootTraceElements())
		{
			if (!TraceElement.Hit)
				continue;

			if (TraceElement.Distance == 0)
				continue;

			if (TraceElement.GameObject?.Root.Components.Get<PlayerPawn>(FindMode.EnabledInSelfAndDescendants) is { } player)
			{
				Log.Info("HIT PLAYER");
			}

			// TODO
			// var damage = CalculateDamageFalloff(BaseDamage, tr.Distance);
			// damage = damage.CeilToInt();
		}
	}

	protected TimeSince TimeSinceShot = new();
	protected virtual bool CanShoot()
	{
		// these 2 should be ensures?
		if (!Equipment.IsValid()) return false;
		if (!Equipment.Owner.IsValid()) return false;

		if (IsReloading && Ammo > 0)
		{
			TryCancelReload();
		}

		if (Equipment.Owner.IsFrozen)
			return false;

		if (Equipment.Tags.Has("equipping"))
			return false;

		if (TimeSinceShot < FireRate)
			return false;

		if (!HasAmmo)
			return false;

		return true;
	}

	//////////////////////////////////////////////////////////////

	protected Ray WeaponRay => Equipment.Owner.AimRay;

	protected virtual IEnumerable<SceneTraceResult> GetShootTraceElements()
	{
		var ShotHits = new List<SceneTraceResult>();

		var TraceStart = WeaponRay.Position;
		var StartRotation = Rotation.LookAt(WeaponRay.Forward);
		var TraceForward = StartRotation.Forward.Normal;

		// forward += (Vector3.Random + Vector3.Random + Vector3.Random + Vector3.Random) * (Equipment.Owner.Spread) * 0.25f;
		// forward = forward.Normal;

		var TraceResults = Scene.Trace.Ray(TraceStart, WeaponRay.Position + TraceForward * 9999f) // magic
		   .UseHitboxes()
		   .IgnoreGameObjectHierarchy(GameObject.Root)
		   .WithoutTags("trigger", "playerclip", "movement")
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
