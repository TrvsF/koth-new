using Sandbox;

namespace KOTH;

public sealed class MenuShapeRotator : Component
{
	[RequireComponent] ModelRenderer ModelRenderer { get; set; }

	protected override void OnUpdate()
	{

		GameObject.WorldRotation = GameObject.WorldRotation.RotateAroundAxis(WorldRotation.Forward, .1f);
	}
}
