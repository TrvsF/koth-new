using Sandbox.Diagnostics;
using static Sandbox.VertexLayout;

namespace KOTH;

public sealed class PlayerBody : Component
{
	[RequireComponent] public SkinnedModelRenderer ModelRenderer { get; set; }
	[RequireComponent] public PlayerDresser Dresser { get; set; }
	[RequireComponent] public ModelPhysics Physics { get; set; }
	[Property] public PlayerPawn Player { get; set; }

	////////////////////////////////////////////////////////////////////////////////////////////////

	protected override void OnAwake()
	{
		base.OnAwake();

		Assert.IsValid(Player);
		Player.OnPlayerStart += Initialize;
	}

	public static readonly List<float> Fats = [0.66f, 0.75f, 0.8f, 0.85f, 0.9f, 1f, 1.05f, 1.1f, 1.15f, 1.2f, 1.25f];
	public static readonly List<float> Heights = [0.66f, 0.75f, 0.8f, 0.85f, 0.9f, 1f, 1.05f, 1.1f, 1.15f, 1.2f, 1.25f];

	private void Initialize()
	{
		var CharacterDefinition = Player.PlayerPawnDefinition.CharacterDefinition;
		Assert.NotNull(CharacterDefinition);

		ModelRenderer.RenderType = Player.IsLocallyControlled ? Sandbox.ModelRenderer.ShadowRenderType.ShadowsOnly : Sandbox.ModelRenderer.ShadowRenderType.On;
		ModelRenderer.Tint = CharacterDefinition.Skin;
		ModelRenderer.WorldScale = new(Fats[CharacterDefinition.Fat], Fats[CharacterDefinition.Fat], Heights[CharacterDefinition.Height]);
	}

	internal void Ragdoll(FDamageTaken DamageTaken)
	{
		Physics.Enabled = true;
		ModelRenderer.UseAnimGraph = false;
		Player.PlayerBoxCollider.Enabled = false;

		GameObject.Tags.Set("ragdoll", true);

		Transform.ClearInterpolation();

		var TimedDestroyComponent = GameObject.AddComponent<TimedDestroyComponent>();
		TimedDestroyComponent.Time = 10f;
	}
}
