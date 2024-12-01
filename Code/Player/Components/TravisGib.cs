using Sandbox;

namespace KOTH;

public sealed class TravisGib : Component
{
	TimeSince AliveTime = new();

	protected override void OnAwake()
	{
		base.OnAwake();

		foreach (var Child in GameObject.Children)
		{
			if (Random.Shared.Next() % 2 == 0)
			{
				Child.Destroy();
			}
		}

		AliveTime = 0;
	}

	protected override void OnUpdate()
	{
		if (AliveTime > 6f)
		{
			GameObject.Destroy();
		}
	}
}
