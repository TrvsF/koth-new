
using System.Diagnostics;

/// <summary>
/// A region of the map with some specific gameplay purpose.
/// The extents of the zone are defined by <see cref="BoxCollider"/>s attached to this object.
/// </summary>
public class Zone : Component
{
	[Property] public Color Color { get; set; } = Color.White;

	private readonly HashSet<BoxCollider> _colliders = new();

	public event Action<Collider> OnZoneEnter;
	
	protected override void OnValidate()
	{
		UpdateColliders();
	}

	protected override void OnEnabled()
	{
		UpdateColliders();
	}

	private void ZoneEnter(Collider Collider)
	{
		OnZoneEnter.Invoke(Collider);
	}

	private void UpdateColliders()
	{
		_colliders.Clear();

		foreach (var collider in Components.GetAll<BoxCollider>())
		{
			if (!collider.IsTrigger)
			{
				return;
			}

			_colliders.Add(collider);

			collider.GameObject.Tags.Add("zone");
			collider.OnTriggerEnter += ZoneEnter;
		}
	}

	/// <summary>
	/// Returns all zones that contain the given position.
	/// </summary>
	public static IEnumerable<Zone> GetAt(Vector3 pos)
	{
		var result = Game.ActiveScene.Trace
			.Sphere(0.001f, pos, pos) // Doesn't work with Ray?
			.HitTriggersOnly()
			.WithTag("zone")
			.RunAll() ?? Array.Empty<SceneTraceResult>();

		return result
			.Select(x => x.GameObject.Components.GetInAncestorsOrSelf<Zone>())
			.Where(x => x != null)
			.Distinct();
	}

	protected override void DrawGizmos()
	{
		Gizmo.Draw.Color = Color.WithAlpha(Gizmo.IsSelected ? 0.5f : 0.25f);

		foreach (var collider in Components.GetAll<BoxCollider>().Where(x => x.IsTrigger))
		{
			Gizmo.Transform = collider.Transform.World;
			Gizmo.Draw.SolidBox(BBox.FromPositionAndSize(collider.Center, collider.Scale));
		}
	}
}
