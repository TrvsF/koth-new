namespace KOTH;

[Title("Sticky Tracker"), Group("Weapon Components")]
public partial class StickyTrackerComponent : Component
{
	Queue<Sticky> Stickies { get; } = new();
	public int StickyCount { get => Stickies.Count; }

	protected override void OnDestroy()
	{
		for (int Index = 0; Index < StickyCount; ++Index)
		{
			var Sticky = Stickies.Dequeue();
			if (!Sticky.IsValid() || !Sticky.GameObject.Root.IsValid())
			{
				Log.Warning("unable to destroy sticky");
				continue;
			}

			Sticky.GameObject.Root.Destroy();
		}
	}

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
}
