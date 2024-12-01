namespace KOTH;

public partial class PlayerBody : Component
{
	[Property] public SkinnedModelRenderer Renderer { get; set; }
	[Property] public ModelPhysics Physics { get; set; }
	[Property] public PlayerPawn Player { get; set; }

	////////////////////////////////////////////////////////////////////////////////////////////////

	// TODO : hookup to internal death event?
	public Vector3 DamageTakenPosition { get; set; }
	public Vector3 DamageTakenForce { get; set; }

	public bool IsRagdoll => Physics.Enabled;

	////////////////////////////////////////////////////////////////////////////////////////////////

	internal void SetRagdoll(bool IsRagdoll)
	{
		Physics.Enabled = IsRagdoll;
		Renderer.UseAnimGraph = !IsRagdoll;

		GameObject.Tags.Set("ragdoll", IsRagdoll);

		if (!IsRagdoll)
		{
			GameObject.LocalPosition = Vector3.Zero;
			GameObject.LocalRotation = Rotation.Identity;
		}

		if (IsRagdoll && DamageTakenForce.LengthSquared > 0f)
			ApplyRagdollImpulses(DamageTakenPosition, DamageTakenForce);

		Transform.ClearInterpolation();
	}

	internal void ApplyRagdollImpulses(Vector3 Position, Vector3 Force)
	{
		if (!Physics.IsValid() || !Physics.PhysicsGroup.IsValid())
			return;

		foreach (var body in Physics.PhysicsGroup.Bodies)
		{
			body.ApplyImpulseAt(Position, Force);
		}
	}
}
