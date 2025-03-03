using System.Text.Json.Nodes;
using KOTH;

public sealed class TeamSpawnPoint : Component
{
	private static Model Model = Model.Load("models/editor/spawnpoint.vmdl");

	[Property] public Team Team { get; set; } = Team.Unassigned;
	[Property][HideIf("IsDummy", true)] public GameObject SpawnZone { get; set; } = null;

	[Property] public bool IsDummy { get; set; } = false;
	[Property][HideIf("IsDummy", false)] public bool Jumper { get; set; }

	protected override void DrawGizmos()
	{
		Gizmo.Hitbox.Model(Model);
		if (IsDummy)
		{
			Gizmo.Draw.Color = Color.Red.WithAlpha((Gizmo.IsHovered || Gizmo.IsSelected) ? 0.7f : 0.5f);
		}
		else
		{
			Gizmo.Draw.Color = Team.GetColor(true).WithAlpha((Gizmo.IsHovered || Gizmo.IsSelected) ? 0.7f : 0.5f);
		}

		var so = Gizmo.Draw.Model(Model);

		if (so is not null)
		{
			so.Flags.CastShadows = true;
		}
	}

	private static JsonArray CopiedSpawnPoints { get; } = new();


	[DeveloperCommand("Clear Spawn Points", "Player")]
	private static void Dev_ClearSpawnPoints()
	{
		Log.Info($"Cleared {CopiedSpawnPoints.Count} spawn point(s) from clipboard");

		CopiedSpawnPoints.Clear();
		Clipboard.SetText("[]");
	}

	[DeveloperCommand("Add Spawn Point", "Player")]
	private static void Dev_CopySpawnPoint()
	{
		var player = PlayerState.Local.PlayerPawn;
		if (player is null) return;

		var obj = new JsonObject
		{
			{ "__guid", Guid.NewGuid() },
			{ "Name", "Spawn" },
			{ "Position", Json.ToNode( player.Transform.Position ) },
			{ "Rotation", Json.ToNode( Rotation.FromYaw( player.EyeAngles.yaw ) ) },
			{ "Enabled", true },
			{
				"Components",
				new JsonArray
				{
					new JsonObject
					{
						{ "__type", TypeLibrary.GetType<TeamSpawnPoint>().FullName },
						{ "__guid", Guid.NewGuid() }
					}
				}
			}
		};

		CopiedSpawnPoints.Add(obj);

		Log.Info($"Added spawn point to clipboard");
		Clipboard.SetText(CopiedSpawnPoints.ToString());
	}
}
