using Sandbox;
using Sandbox.Diagnostics;

namespace KOTH;

public sealed class PlayerDresser : Component
{
	[Property] public SkinnedModelRenderer BodyTarget { get; set; }
	[Property] public bool ApplyLocalUserClothes { get; set; } = true;
	[Property] public bool ApplyHeightScale { get; set; } = true;
	[Property] public List<ClothingContainer.ClothingEntry> Clothing { get; set; }

	protected override void OnStart()
	{
		base.OnStart();

		ApplyClothing();
	}

	void ApplyClothing()
	{
		if (IsProxy)
		{
			return;
		}
	
		Assert.IsValid(BodyTarget);

		var ClothingContainer = ApplyLocalUserClothes ? Sandbox.ClothingContainer.CreateFromLocalUser() : new ClothingContainer();

		if (!ApplyHeightScale)
		{
			ClothingContainer.Height = 1;
		}

		ClothingContainer.AddRange(Clothing);
		ClothingContainer.Normalize();
		ClothingContainer.Apply(BodyTarget);

		// BodyTarget.PostAnimationUpdate();
	}
}
