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

	protected override void OnStart()
	{
		base.OnStart();

		Assert.IsValid(Player);

		ModelRenderer.Enabled = !Player.IsLocallyControlled;
	}

	internal void Ragdoll(FDamageTaken DamageTaken)
	{
		Physics.Enabled = true;
		ModelRenderer.UseAnimGraph = false;

		GameObject.Tags.Set("ragdoll", true);

		foreach (var Body in Physics.PhysicsGroup.Bodies)
		{
			Body.ApplyImpulseAt(DamageTaken.DamageLocation, DamageTaken.Damage * 5f);
		}

		Transform.ClearInterpolation();

		var TimedDestroyComponent = GameObject.AddComponent<TimedDestroyComponent>();
		TimedDestroyComponent.Time = 10f;
	}
}
