using Sandbox;
using Sandbox.Diagnostics;
using System.Numerics;

namespace KOTH;

public sealed class PlayerDresser : Component, Component.INetworkSpawn
{
	[Property] public SkinnedModelRenderer ModelRenderer { get; set; }
	[Property] public bool ApplyHeightScale { get; set; } = true;
	[Property] public List<ClothingContainer.ClothingEntry> Clothing { get; set; }
	[Property] public PlayerPawn LocalPlayer { get; set; }
	[Property] Material CTMaterial { get; set; }
	[Property] Material TMaterial { get; set; }

	public ClothingContainer EquippedClothes { get; private set; } = null;
	string ClotheJSON { get; set; }

	protected override void OnStart()
	{
		base.OnStart();

		SetupClothes();
	}

	public void OnNetworkSpawn(Connection Owner)
	{
		ClotheJSON = Owner.GetUserData("avatar");

		ApplyClothing();
	}

	public void ApplyClothing()
	{
		Assert.IsValid(ModelRenderer);

		EquippedClothes = ClotheJSON == "" ? new ClothingContainer() : ClothingContainer.CreateFromJson(ClotheJSON);

		if (!ApplyHeightScale)
		{
			EquippedClothes.Height = 1;
		}

		EquippedClothes.AddRange(Clothing);
		EquippedClothes.Normalize();
		EquippedClothes.Apply(ModelRenderer);
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
				else if (LocalPlayer.Team == Team.Terrorist)
				{
					ClothModel.SetMaterial(TMaterial);
				}

				ClothModel.RenderType = LocalPlayer.IsLocallyControlled ? Sandbox.ModelRenderer.ShadowRenderType.ShadowsOnly : Sandbox.ModelRenderer.ShadowRenderType.On;
			}
		}

	}
}
