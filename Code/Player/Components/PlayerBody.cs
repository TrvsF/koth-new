using static Sandbox.VertexLayout;

namespace KOTH;

public sealed class PlayerBody : Component
{
	[Property] public SkinnedModelRenderer Renderer { get; set; }
	[Property] public ModelPhysics Physics { get; set; }
	[Property] public PlayerPawn Player { get; set; }

	////////////////////////////////////////////////////////////////////////////////////////////////

	internal void Ragdoll(FDamageTaken DamageTaken)
	{
		Physics.Enabled = true;
		Renderer.UseAnimGraph = false;

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
