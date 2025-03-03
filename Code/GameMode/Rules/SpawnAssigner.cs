namespace KOTH;

public record struct SpawnPointInfo(Transform Transform, Team TeamIn = Team.Unassigned)
{
	public Vector3 Position => Transform.Position;
	public Rotation Rotation => Transform.Rotation;
	public Team Team => TeamIn;
}
