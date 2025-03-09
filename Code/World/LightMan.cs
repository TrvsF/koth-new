using Sandbox;

namespace KOTH;

public sealed class LightMan : Component
{
	private bool Forwards = false;
	private float Rotate = 0f;

	private void RollForwards()
	{
		Forwards = Random.Shared.Next(2) == 0; 
	}

	private void RollRotate()
	{
		Rotate = (float)(Random.Shared.NextDouble() * 0.8 - 0.4);
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if (Random.Shared.Next(50) == 0)
		{
			RollForwards();
		}

		if (Random.Shared.Next(50) == 0)
		{
			RollRotate();
		}

		if (Forwards)
		{
			GameObject.WorldPosition += GameObject.WorldRotation.Forward * .08f;
		}

		GameObject.WorldRotation = GameObject.WorldRotation.RotateAroundAxis(Vector3.Up, Rotate);
	}
}
