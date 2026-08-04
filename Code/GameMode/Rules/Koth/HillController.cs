using Sandbox;
using Sandbox.Events;

namespace KOTH;

public sealed class HillController : Component, IGameEventHandler<EnterStateEvent>
{
	[Property] public bool HillEnabled { get; set; } = false;
	[Sync(SyncFlags.FromHost)] public Hill ActiveHill { get; private set; }

	void IGameEventHandler<EnterStateEvent>.OnGameEvent(EnterStateEvent eventArgs)
	{
		GetActiveHill()?.SetHillActive(HillEnabled);
	}

	public Hill GetActiveHill()
	{
		if (ActiveHill.IsValid())
		{
			return ActiveHill;
		}

		foreach (var GameObject in Scene.GetAllObjects(true))
		{
			var Hill = GameObject.Components.Get<Hill>();
			if (Hill.IsValid())
			{
				ActiveHill = Hill;
				break;
			}
		}
		return ActiveHill;
	}
}
