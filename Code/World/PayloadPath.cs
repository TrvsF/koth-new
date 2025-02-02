using Sandbox;

namespace KOTH;

public sealed class PayloadPathNode
{
	[Property] public List<Vector3> SegmentPoints { get; set; } = [];

	public bool IsValidPath()
	{
		return SegmentPoints.Count > 1;
	}
}

public sealed class PayloadPath : Component
{
	[Property] public Color CaptureNodeColour { get; set; } = Color.Red;
	[Property] public Color NodeColour { get; set; } = Color.Green;
	[Property] public List<PayloadPathNode> PathSegments { get; set; } = [];

	public (Vector3 Position, Rotation Rotation) GetStartPositionRotation()
	{
		if (PathSegments.Count == 0 || !PathSegments[0].IsValidPath())
		{
			Log.Warning($"PathNodes is empty on payload path {this}");
			return (Vector3.Zero, Rotation.Identity);
		}

		var FirstSegment = PathSegments[0];
		var StartLocation = FirstSegment.SegmentPoints[0];
		var StartForward = FirstSegment.SegmentPoints[1] - StartLocation;
		var StartRotaion = Rotation.LookAt(StartForward.Normal);
		return (StartLocation + GameObject.WorldPosition, StartRotaion);
	}

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		foreach (var Node in PathSegments)
		{
			Gizmo.Draw.Color = NodeColour;
			for (int NodeIndex = 0; NodeIndex < Node.SegmentPoints.Count; ++NodeIndex)
			{
				if (NodeIndex == Node.SegmentPoints.Count - 1)
				{
					Gizmo.Draw.Color = CaptureNodeColour;
				}

				Gizmo.Draw.SolidBox(BBox.FromPositionAndSize(Node.SegmentPoints[NodeIndex], 8));
			}
		}
	}
}
