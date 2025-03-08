using Sandbox;

namespace KOTH;

public sealed class SpawnDoor : Component
{
	[Property] public SkinnedModelRenderer ModelRenderer { get; set; }
	[Property] public bool Open { get; set; } = true;
	[Property] public float ZOffset { get; set; } = 156f;

	bool IsOpen = false;
	float TargetZOffset = 0f;
	float CurrentZOffset = 0f;

	float StartZ = 0f;
	protected override void OnStart()
	{
		base.OnStart();

		StartZ = ModelRenderer.WorldPosition.z;
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		IsOpen = Open;

		if (IsOpen)
		{
			TargetZOffset = ZOffset;
		}
		else
		{
			TargetZOffset = 0;
		}

		if (!CurrentZOffset.AlmostEqual(TargetZOffset))
		{
			CurrentZOffset = MathX.Lerp(CurrentZOffset, TargetZOffset, 0.077f);
			ModelRenderer.WorldPosition = ModelRenderer.WorldPosition.WithZ(StartZ + CurrentZOffset);
		}
	}
}
