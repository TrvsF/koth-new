using Sandbox.Network;
using System.Threading.Tasks;
using Sandbox.Events;
using System.Threading.Channels;
using Sandbox.Diagnostics;
using Sandbox;

namespace KOTH;

public enum EGameNetworkMode
{
	None,
	Menu,
	Singleplayer,
	Multiplayer,
}

public sealed class GameNetworkManager : SingletonComponent<GameNetworkManager>, Component.INetworkListener
{
	private GameObject Actor { get; set; }

	protected override void OnStart()
	{
		base.OnStart();

		if (!GameObject.Root.IsValid())
		{
			throw new Exception($"Expected a valid object for the game network manager");
		}

		Actor = GameObject.Root;
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	[Sync(SyncFlags.FromHost)] public static NetList<PlayerState> PlayerStates { get; set; } = new();

	////////////////////////////////////////////////////////////////////////////////////////////////

	[Property] public GameObject PlayerStatePrefab { get; set; } = null;
	[Property] public EGameNetworkMode NetworkMode { get; set; } = EGameNetworkMode.None;

	////////////////////////////////////////////////////////////////////////////////////////////////

	protected override async Task OnLoad()
	{
		if (Scene.IsEditor)
		{
			return;
		}

		PlayerId.Init();

		switch (NetworkMode)
		{
			case EGameNetworkMode.Singleplayer:
				StartClient(Connection.Local);
				break;
			case EGameNetworkMode.Multiplayer:
				if (!Networking.IsActive)
				{
					bool Joined = await Networking.JoinBestLobby(Game.Ident);
					if (!Joined)
					{
						Log.Info("starting own lobby...");
						CreateLobby();
					}
				}
				break;
			case EGameNetworkMode.Menu:
				break;
			case EGameNetworkMode.None:
				break;
		}
	}

	protected override void OnDestroy()
	{
		PlayerStates.Clear();

		base.OnDestroy();
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	private static bool CreateLobby(string LobbyName = "awesomelobby", LobbyPrivacy Privacy = LobbyPrivacy.Public)
	{
		LobbyConfig Config = new();
		Config.Name = LobbyName;
		Config.DestroyWhenHostLeaves = false;
		Config.MaxPlayers = 16;
		Config.Privacy = Privacy;

		Networking.CreateLobby(Config);

		return true;
	}

	void INetworkListener.OnActive(Connection ConnectionChannel)
	{
		Log.Info($"Connection activating with name = {ConnectionChannel.DisplayName}:{ConnectionChannel.Ping} | is host = {ConnectionChannel.IsHost}");

		// TODO : if we're a dedicated server init a different way!
		StartClient(ConnectionChannel);
	}

	void INetworkListener.OnDisconnected(Connection ConnectionChannel)
	{
		// Assert.True(Networking.IsHost); // after changing hosts this assert fails :)

		Log.Info("disconnection event");

		PlayerState PlayerStateToDestroy = null;
		foreach (var PlayerState in PlayerStates)
		{
			if (PlayerState.Connection == ConnectionChannel)
			{
				PlayerStateToDestroy = PlayerState;
			}
		}

		if (PlayerStateToDestroy != null)
		{
			PlayerStates.Remove(PlayerStateToDestroy);

			if (PlayerStateToDestroy.PlayerPawn.IsValid())
			{
				PlayerStateToDestroy.PlayerPawn.GameObject.Root.Destroy();
			}

			PlayerStateToDestroy.GameObject.Root.Destroy();
		}
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	private void StartClient(Connection ConnectionChannel)
	{
		bool CreatedPlayerState = CreatePlayerState(ConnectionChannel, out GameObject PlayerState, out PlayerState PlayerStateComponent);

		if (!CreatedPlayerState)
		{
			Networking.Disconnect();
			throw new Exception($"Something went wrong when trying to create PlayerState for {ConnectionChannel.DisplayName}");
		}

		PlayerStates.Add(PlayerStateComponent);
	}

	bool CreatePlayerState(Connection ConnectionChannel, out GameObject PlayerState, out PlayerState PlayerStateComponent)
	{
		Assert.True(Networking.IsHost);
		Assert.True(PlayerStatePrefab.IsValid(), "Could not spawn player as no PlayerStatePrefab assigned to network manager");

		// TODO : visit
		//CloneConfig CloneConfig = new();
		//CloneConfig.StartEnabled = true;
		//CloneConfig.Parent = Actor;
		//CloneConfig.Transform = new();

		PlayerState = PlayerStatePrefab.Clone(/*CloneConfig*/);
		PlayerState.Name = $"PlayerState:{ConnectionChannel.DisplayName}";
		PlayerState.Network.SetOrphanedMode(NetworkOrphaned.Destroy);
		PlayerState.NetworkSpawn(Connection.Host);

		PlayerStateComponent = PlayerState.Components.Get<PlayerState>();

		if (!PlayerStateComponent.IsValid())
		{
			throw new Exception($"Could not spawn player as no PlayerStatePrefab assigned to network manager for {ConnectionChannel.DisplayName}");
		}

		if (PlayerStateComponent.Initilize(ConnectionChannel))
		{
			return true;
		}

		PlayerState.DestroyImmediate();
		return false;
	}
}
