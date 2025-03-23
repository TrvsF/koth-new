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

public partial class PlayerState : Component
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

	public GameObject OverviewCameraObject = null;
	[Rpc.Broadcast]
	public void ClientInitilize(bool InitForGame = true)
	{
		Local = this;

		if (!InitForGame)
		{
			return;
		}

		CameraUtils.CreateOverviewCamera(Scene);
		OverviewCameraObject = CameraUtils.OverviewCamera.GameObject;

		Assert.IsValid(OverviewCameraObject);
		OverviewCameraObject.Enabled = true;
	}
}
