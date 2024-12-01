using Sandbox;

namespace KOTH;

public sealed class TeleportZone : Component, Component.ITriggerListener
{
	[RequireComponent] public Zone Zone { get; private set; }
	[Property] public Vector3 TeleportLocation { get; private set; }
	[Property] public Rotation TeleportRotation { get; private set; }

	void ITriggerListener.OnTriggerEnter(Collider Collider)
	{
		var PlayerPawn = Collider.GameObject.Root.Components.Get<PlayerPawn>();
		if (PlayerPawn.IsValid())
		{
			Transform TeleportTransform = GameObject.Transform.Local;
			TeleportTransform.Position += TeleportLocation;
			TeleportTransform.Rotation = Rotation.FromYaw(TeleportRotation.Yaw());
			PlayerPawn.Teleport(TeleportTransform);
		}
	}

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		Gizmo.Draw.Color = Color.Cyan;
		Gizmo.Draw.SolidBox(BBox.FromPositionAndSize(TeleportLocation, 20));
		Gizmo.Draw.Arrow(TeleportLocation, TeleportLocation + TeleportRotation.Forward * 50);
	}
}
