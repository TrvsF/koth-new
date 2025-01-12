namespace KOTH;

// TODO : revisit
[Icon("track_changes")]
public abstract class EquipmentComponent : Component
{
	protected Equipment Equipment { get; set; }

	protected PlayerPawn Player => Equipment.Owner;

	protected void BindTag(string tag, Func<bool> predicate) => Equipment.BindTag(tag, predicate);

	protected override void OnAwake()
	{
		// Cache the weapon on awake
		Equipment = Components.Get<Equipment>(FindMode.EverythingInSelfAndAncestors);

		base.OnAwake();
	}
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class EquipmentResourcePropertyAttribute : Attribute
{

}
