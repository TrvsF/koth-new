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

public sealed class GameNetworkManager : Component, Component.INetworkListener
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

	[HostSync] public static NetList<PlayerState> PlayerStates { get; set; } = new();

	////////////////////////////////////////////////////////////////////////////////////////////////

	[Property] public GameObject PlayerStatePrefab { get; set; } = null;
	[Property] public EGameNetworkMode NetworkMode { get; set; } = EGameNetworkMode.None;

	////////////////////////////////////////////////////////////////////////////////////////////////

	// NOTE : only called on host
	void INetworkListener.OnActive(Connection ConnectionChannel)
	{
		Log.Info($"Connection activating with name = {ConnectionChannel.DisplayName}:{ConnectionChannel.Ping} | is host = {ConnectionChannel.IsHost}");

		// TODO : if we're server don't do any of this, but still init the world
		StartClient(ConnectionChannel);
	}

	void INetworkListener.OnDisconnected(Connection ConnectionChannel)
	{
		Log.Info("disconnection event");

		foreach (var PlayerState in PlayerStates)
		{
			if (PlayerState.Connection == ConnectionChannel)
			{
				if (PlayerState.PlayerPawn.IsValid())
				{
					PlayerState.PlayerPawn.GameObject.Root.Destroy();
				}
				PlayerState.GameObject.Root.Destroy();
			}
		}
	}

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
				bool Joined = await Networking.JoinBestLobby("koth");
				if (!Joined)
				{
					Log.Info("starting own lobby...");
					CreateLobby();
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

	private void StartClient(Connection ConnectionChannel)
	{
		if (CreatePlayerState(ConnectionChannel, out GameObject PlayerState, out PlayerState PlayerStateComponent))
		{
			// SpawnCreatePlayer(PlayerStateComponent, ConnectionChannel);
		}
		else
		{
			Networking.Disconnect();
			throw new Exception($"Something went wrong when trying to create PlayerState for {ConnectionChannel.DisplayName}");
		}
	}

	bool CreatePlayerState(Connection ConnectionChannel, out GameObject PlayerState, out PlayerState PlayerStateComponent)
	{
		Assert.True(Networking.IsHost);
		Assert.True(PlayerStatePrefab.IsValid(), "Could not spawn player as no PlayerStatePrefab assigned to network manager");

		var CloneConfig = new CloneConfig()
		{
			StartEnabled = true,
			Parent = Actor,
			Transform = new Transform()
		};

		PlayerState = PlayerStatePrefab.Clone(/*CloneConfig*/);
		PlayerState.Name = $"PlayerState:{ConnectionChannel.DisplayName}";
		PlayerState.Network.SetOrphanedMode(NetworkOrphaned.Destroy);
		PlayerState.NetworkSpawn(Connection.Host);

		// TODO : set player state gameobject parent to our actor
		// PlayerState.Root.SetParent(Actor);

		PlayerStateComponent = PlayerState.Components.Get<PlayerState>();
			
		if (!PlayerStateComponent.IsValid())
		{
			throw new Exception($"Could not spawn player as no PlayerStatePrefab assigned to network manager for {ConnectionChannel.DisplayName}");
		}

		if (PlayerStateComponent.Initilize(ConnectionChannel))
		{
			PlayerStates.Add(PlayerStateComponent);
			return true;
		}

		PlayerState.DestroyImmediate();
		return false;
	}

	private bool CreateLobby()
	{
		LobbyConfig Config = new();
		Config.Name = "awesomelobby";
		Config.DestroyWhenHostLeaves = true;

		Networking.CreateLobby(Config);

		return true;
	}
}
