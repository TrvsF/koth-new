namespace KOTH;

[Title("Sticky Tracker"), Group("Weapon Components")]
public partial class StickyTrackerComponent : Component
{
	Queue<Sticky> Stickies { get; } = new();
	public int StickyCount { get => Stickies.Count; }

	public void AddSticky(Sticky Sticky)
	{
		if (Stickies.Count == 6)
		{
			Stickies.Dequeue().Explode();
		}
		Stickies.Enqueue(Sticky);
	}

	public void Detonate()
	{
		// relys on Stickies being in cronological order
		// (which it always should be!)

		var DetonatedStickies = 0;
		foreach (var Sticky in Stickies)
		{
			if (Sticky.AliveTime >= Sticky.MinDetTime)
			{
				Sticky.Explode();
				++DetonatedStickies;
			}
		}

		for (int Index = 0; Index < DetonatedStickies; ++Index)
		{
			Stickies.Dequeue();
		}
	}

	protected override void OnDestroy()
	{
		// TODO : understand the lifecycle of these
		// methods more, this is erroring when leaving
		// the game...

		//foreach (var Sticky in Stickies)
		//{
		//	Sticky.GameObject.Destroy();
		//}

		base.OnDestroy();
	}
}
