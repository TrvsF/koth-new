using KOTH.Notification;
using KOTH.UI;
using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Events;
using Sandbox.Utility;
using System.Net.NetworkInformation;

namespace KOTH;

public enum EPlayerState
{
	Menu,
	Game,
}

public partial class PlayerState : Component, Component.INetworkSpawn
{
	public Connection Connection { get; private set; }
	public bool IsConnected => Connection != null && Connection.IsActive;

	//////////////////////////////////////////////////////////////

	[Sync(SyncFlags.FromHost)] public EPlayerState PlayerStateState { get; private set; }

	//////////////////////////////////////////////////////////////

	[RequireComponent] public PlayerId PlayerId { get; private set; }
	[RequireComponent] public LocalStats LocalStats { get; private set; }

	//////////////////////////////////////////////////////////////

	[Sync(SyncFlags.FromHost), Property] public ulong SteamId { get; private set; }
	[Sync(SyncFlags.FromHost), Property] public string SteamName { get; private set; }
	[Sync(SyncFlags.FromHost), Property] public string PingString { get; set; }

	//////////////////////////////////////////////////////////////

	[Sync(SyncFlags.FromHost), Property] public Team Team { get; set; } // TODO : listen to onteamchange
	[Sync(SyncFlags.FromHost), ValidOrNull] public PlayerPawn PlayerPawn { get; private set; }
	[Sync(SyncFlags.FromHost), ValidOrNull] public PlayerPawn SpectatingTarget { get; private set; }

	//////////////////////////////////////////////////////////////

	// HACK : InitForGame controls the creation of the fake camera
	// the camera system needs a redo for when we do deathcams....

	public bool Initilize(Connection ConnectionIn, bool InitForGame = true)
	{
		Assert.True(Networking.IsHost);
		Assert.NotNull(ConnectionIn);

		Connection = ConnectionIn;
		SteamId = Connection.SteamId;
		SteamName = Connection.DisplayName;

		if (!GameMode.Instance.IsValid())
		{
			// TODO : if not menu
			// Log.Warning($"gamemode not valid when {this} init");
		}
		else
		{
			Team = GameMode.Instance.GetStarterTeam();
		}

		// client rpc
		using (Rpc.FilterInclude(Connection))
		{
			ClientInitilize(InitForGame);
		}

		return true;
	}

	private GameObject AssumedSceneCameraObject = null;
	[Rpc.Broadcast]
	public void ClientInitilize(bool InitForGame = true)
	{
		Local = this;

		if (!InitForGame)
		{
			return;
		}

		if (AssumedSceneCameraObject == null)
		{
			// HACK : cameras that are placed within the scene via the editor are not behaving
			// to how i would assume they would. Workaround for now

			var CameraObject = Scene.CreateObject();
			CameraObject.Components.Create<ScreenPanel>();
			CameraObject.Components.Create<PlayerMenuComponent>();
			CameraObject.Name = "TEMPCAMERA";
			CameraObject.NetworkMode = NetworkMode.Never;

			// HACK : further silly hack to use the transform of a placed camera within the level
			foreach (var Object in Scene.GetAllObjects(false))
			{
				if (Object.Tags.Contains("scenecamera"))
				{
					CameraObject.WorldPosition = Object.WorldPosition;
					CameraObject.WorldRotation = Object.WorldRotation;
				}
			}

			var CameraComp = CameraObject.Components.Create<CameraComponent>();
			CameraComp.Priority = 100;

			AssumedSceneCameraObject = CameraObject;
		}

		Assert.IsValid(AssumedSceneCameraObject);
		AssumedSceneCameraObject.Enabled = true;
	}

	//////////////////////////////////////////////////////////////

	public void OnNetworkSpawn(Connection Owner)
	{
		var Gamemode = GameMode.Instance;

		if (!Gamemode.IsValid())
		{
			return;
		}

		Gamemode.GameStats?.OnNewPlayerState(this);
	}
}
