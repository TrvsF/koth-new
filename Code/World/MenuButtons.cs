using Sandbox;

namespace KOTH;

public sealed class MenuButtonManager : Component
{
	protected override void OnUpdate()
	{
		base.OnUpdate();

		Mouse.Visibility = MouseVisibility.Visible;

		if (Input.Pressed("attack1"))
		{
			var ClickRay = Scene.Camera.ScreenPixelToRay(Mouse.Position);
			var ClickTraces = Scene.Trace.Ray(ClickRay.Position, ClickRay.Position + ClickRay.Forward * 100000f).RunAll();

			foreach (var ClickTrace in ClickTraces)
			{
				if (!ClickTrace.Hit)
				{
					continue;
				}

				if (ClickTrace.GameObject.GetComponent<MenuButton>() is { } HitButton)
				{
					HitButton.OnClick();
					return;
				}
			}
		}
	}
}

public class MenuButton : Component
{
	[RequireComponent] protected BoxCollider TextCollider { get; set; }
	[RequireComponent] protected TextRenderer TextRenderer { get; set; }

	protected override void OnStart()
	{
		base.OnStart();

		TextRenderer.Billboard = TextRenderer.BillboardMode.Always;
		TextRenderer.Scale = 0.03f;
		TextCollider.Scale = new(5f, 15f, 2.5f);
	}

	public virtual void OnClick() {}
}

public class SinglePlayerButton : MenuButton
{
	bool MadeKids = false;
	List<GameObject> Kids = new();

	public override void OnClick()
	{
		base.OnClick();

		MadeKids = !MadeKids;

		if (!MadeKids)
		{
			foreach (var Kid in Kids)
			{
				Kid.Destroy();
			}

			Kids.Clear();
			return;
		}

		CloneConfig Config = new()
		{
			StartEnabled = true,
			Transform = WorldTransform.WithPosition(WorldPosition + Vector3.Down * 3f),
		};

		var ChildJump = Scene.CreateObject().Clone(Config);
		ChildJump.AddComponent<JumpButton>();
		Kids.Add(ChildJump);

		CloneConfig Config2 = new()
		{
			StartEnabled = true,
			Transform = WorldTransform.WithPosition(WorldPosition + Vector3.Down * 6f),
		};

		var ChildBotz = Scene.CreateObject().Clone(Config2);
		ChildBotz.AddComponent<BotzButton>();
		Kids.Add(ChildBotz);
	}
}

public class BotzButton : MenuButton
{
	protected override void OnStart()
	{
		base.OnStart();

		TextRenderer.Text = "Botz";
	}

	public override void OnClick()
	{
		base.OnClick();

		Game.ActiveScene.LoadFromFile("scenes/multiplayer_test/multiplayertest.scene");
	}
}

public class JumpButton : MenuButton
{
	protected override void OnStart()
	{
		base.OnStart();

		TextRenderer.Text = "Jump";
	}

	public override void OnClick()
	{
		base.OnClick();

		Game.ActiveScene.LoadFromFile("scenes/jumpmap/jump1.scene");
	}
}

public class MultiPlayerButton : MenuButton
{
	bool MadeKids = false;
	List<GameObject> Kids = new();

	protected override void OnStart()
	{
		base.OnStart();

		RefreshLobbyStats();
	}

	public override void OnClick()
	{
		base.OnClick();

		RefreshLobbyStats();

		MadeKids = !MadeKids;

		if (!MadeKids)
		{
			foreach (var Kid in Kids)
			{
				Kid.Destroy();
			}

			Kids.Clear();
			return;
		}

		CloneConfig Config = new()
		{
			StartEnabled = true,
			Transform = WorldTransform.WithPosition(WorldPosition + Vector3.Down * 3f),
		};

		var ChildMge = Scene.CreateObject().Clone(Config);
		ChildMge.AddComponent<MgeButton>();
		Kids.Add(ChildMge);

		CloneConfig Config2 = new()
		{
			StartEnabled = true,
			Transform = WorldTransform.WithPosition(WorldPosition + Vector3.Down * 6f),
		};

		var ChildPayload = Scene.CreateObject().Clone(Config2);
		ChildPayload.AddComponent<PayloadButton>();
		Kids.Add(ChildPayload);
	}

	public static int NumLobbies = 0;
	public static int NumPlayers = 0;
	public static int NumMgePlayers = 0;
	public static int NumPayloadPlayers = 0;

	public static async void RefreshLobbyStats()
	{
		NumLobbies = 0;
		NumPlayers = 0;
		NumMgePlayers = 0;
		NumPayloadPlayers = 0;

		var Lobbies = await Networking.QueryLobbies(Game.Ident);
		NumLobbies = Lobbies.Count;

		foreach (var Lobby in Lobbies)
		{
			NumPlayers += Lobby.Members;
			if (Lobby.Name.StartsWith("mge_"))
			{
				NumMgePlayers += Lobby.Members;
			}
			else if (Lobby.Name.StartsWith("pl_"))
			{
				NumPayloadPlayers += Lobby.Members;
			}
		}
	}
}

public class MgeButton : MenuButton
{
	private bool Trying = false;

	protected override void OnStart()
	{
		base.OnStart();

		TextRenderer.Text = $"1v1 {MultiPlayerButton.NumMgePlayers}p";
	}

	public override void OnClick()
	{
		base.OnClick();

		LoadMge();
	}

	private async void LoadMge()
	{
		if (Networking.IsConnecting || Trying)
		{
			Log.Info("connecting...");
			return;
		}

		if (Game.IsEditor)
		{
			Game.ActiveScene.LoadFromFile("scenes/mge/mge.scene");
			return;
		}

		Trying = true;
		var Lobbies = await Networking.QueryLobbies(Game.Ident);
		foreach (var Lobby in Lobbies)
		{
			if (Lobby.Name.StartsWith("mge_") && !Lobby.IsFull)
			{
				if (await Networking.TryConnectSteamId(Lobby.LobbyId))
				{
					return;
				}
			}
		}
		Game.ActiveScene.LoadFromFile("scenes/mge/mge.scene");
		Trying = false;
	}
}

public class PayloadButton : MenuButton
{
	private bool Trying = false;

	protected override void OnStart()
	{
		base.OnStart();

		TextRenderer.Text = $"Payload {MultiPlayerButton.NumPayloadPlayers}p";
	}

	public override void OnClick()
	{
		base.OnClick();

		LoadPayload();
	}

	private async void LoadPayload()
	{
		if (Networking.IsConnecting || Trying)
		{
			Log.Info("connecting...");
			return;
		}

		if (Game.IsEditor)
		{
			Game.ActiveScene.LoadFromFile("scenes/pl_sheff/sheff.scene");
			return;
		}

		Trying = true;
		var Lobbies = await Networking.QueryLobbies(Game.Ident);
		foreach (var Lobby in Lobbies)
		{
			if (Lobby.Name.StartsWith("pl_") && !Lobby.IsFull)
			{
				if (await Networking.TryConnectSteamId(Lobby.LobbyId))
				{
					return;
				}
			}
		}
		Game.ActiveScene.LoadFromFile("scenes/pl_sheff/sheff.scene");
		Trying = false;
	}
}

public class SettingsButton : MenuButton
{
	public override void OnClick()
	{
		base.OnClick();
	}
}
