using Sandbox;
using Sandbox.Diagnostics;

namespace KOTH;

public sealed class PlayerDresser : Component
{
	[Property] public SkinnedModelRenderer BodyTarget { get; set; }
	[Property] public bool ApplyLocalUserClothes { get; set; } = true;
	[Property] public bool ApplyHeightScale { get; set; } = true;
	[Property] public List<ClothingContainer.ClothingEntry> Clothing { get; set; }
	[Property] public PlayerPawn LocalPlayer { get; set; }
	[Property] Material CTMaterial { get; set; }
	[Property] Material TMaterial { get; set; }

	public ClothingContainer EquippedClothes { get; private set; } = null;

	protected override void OnStart()
	{
		base.OnStart();

		Assert.IsValid(LocalPlayer);

		if (LocalPlayer.IsLocallyControlled)
		{
			ApplyClothing();
		}

		SetupClothes();
	}

	public void ApplyClothing()
	{
		Assert.IsValid(BodyTarget);

		EquippedClothes = ApplyLocalUserClothes ? Sandbox.ClothingContainer.CreateFromLocalUser() : new ClothingContainer();
		EquippedClothes.Clothing.Clear();

		if (!ApplyHeightScale)
		{
			EquippedClothes.Height = 1;
		}

		EquippedClothes.AddRange(Clothing);
		EquippedClothes.Normalize();
		EquippedClothes.Apply(BodyTarget);

		// BodyTarget.PostAnimationUpdate();
	}

	public void SetupClothes()
	{
		// why don't we keep a reference of these ANYWHERE when we spawn them? @facepunch

		foreach (var ChildBodyObject in GameObject.Children)
		{
			if (!ChildBodyObject.IsValid() || !ChildBodyObject.Tags.Contains("clothing"))
			{
				continue;
			}

			if (ChildBodyObject.GetComponent<SkinnedModelRenderer>() is { } ClothModel)
			{
				if (LocalPlayer.Team == Team.CounterTerrorist)
				{
					ClothModel.SetMaterial(CTMaterial);
				}
				else
				{
					ClothModel.SetMaterial(TMaterial);
				}
			}
		}
	}
}
