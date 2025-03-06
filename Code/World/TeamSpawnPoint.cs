using System.Text.Json.Nodes;
using KOTH;

public sealed class TeamSpawnPoint : Component
{
	private static Model Model = Model.Load("models/editor/spawnpoint.vmdl");

	[Property] public Team Team { get; set; } = Team.Unassigned;
	[Property] public bool IsDummy { get; set; } = false;

	[Property][HideIf(nameof(IsDummy), true)] public GameObject SpawnZone { get; set; } = null;

	[Property][HideIf(nameof(IsDummy), false)] public bool Jumper { get; set; } = false;
	[Property][HideIf(nameof(IsDummy), false)] public bool Walker { get; set; } = false;

	protected override void DrawGizmos()
	{
		Gizmo.Hitbox.Model(Model);
		Gizmo.Draw.Color = Team.GetColor(false).WithAlpha((Gizmo.IsHovered || Gizmo.IsSelected) ? 1f : 0.7f);

		var SceneModel = Gizmo.Draw.Model(Model);

		if (SceneModel is not null)
		{
			SceneModel.Flags.CastShadows = true;
		}
	}
}
