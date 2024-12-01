using Sandbox;

namespace KOTH;

public sealed class MenuShape : Component
{
	[Property] private ModelRenderer Model { get; set; }

	protected override void OnFixedUpdate()
	{
		if (Model.IsValid())
		{
			WorldRotation = WorldRotation.RotateAroundAxis(Vector3.Forward, 1.33f);
			WorldRotation = WorldRotation.RotateAroundAxis(Vector3.Up, 1f);
			// WorldRotation = WorldRotation.RotateAroundAxis(Vector3.Left, 0.6f);
		}
	}
}
