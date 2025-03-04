using Sandbox;
using Sandbox.Diagnostics;
using System.Numerics;

namespace KOTH;

public sealed class PlayerDresser : Component
{
	[Property] public SkinnedModelRenderer ModelRenderer { get; set; }
	[Property] public bool ApplyLocalUserClothes { get; set; } = true;
	[Property] public bool ApplyHeightScale { get; set; } = true;
	[Property] public List<ClothingContainer.ClothingEntry> Clothing { get; set; }
	[Property] public PlayerPawn LocalPlayer { get; set; }
	[Property] Material CTMaterial { get; set; }
	[Property] Material TMaterial { get; set; }

	public ClothingContainer EquippedClothes { get; private set; } = null;

	protected override void OnAwake()
	{
		base.OnAwake();

		Assert.IsValid(LocalPlayer);
		LocalPlayer.OnPlayerStart += Initialize;
	}

	private void Initialize()
	{
		ApplyClothing();
		SetupClothes();
	}

	public void ApplyClothing()
	{
		Assert.IsValid(ModelRenderer);

		EquippedClothes = ApplyLocalUserClothes ? Sandbox.ClothingContainer.CreateFromLocalUser() : new ClothingContainer();

		if (LocalPlayer.IsLocallyControlled)
		{
			// If player is locally controlled we don't want to render any clothes rendered as they are not networked
			// We only want to show clothes on other players
			EquippedClothes.Clothing.Clear();
		}

		if (!ApplyHeightScale)
		{
			EquippedClothes.Height = 1;
		}

		EquippedClothes.AddRange(Clothing);
		EquippedClothes.Normalize();
		EquippedClothes.Apply(ModelRenderer);

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
				else if (LocalPlayer.Team == Team.CounterTerrorist)
				{
					ClothModel.SetMaterial(TMaterial);
				}
			}
		}
	}
}
