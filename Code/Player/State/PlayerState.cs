using KOTH.UI;
using KOTH.Utils;
using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Events;

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
	public string DisplayName => $"{SteamName}{(!IsConnected ? " (Disconnected)" : "")}";

	//////////////////////////////////////////////////////////////

	[Sync(SyncFlags.FromHost)] public EPlayerState PlayerStateState { get; private set; }

	//////////////////////////////////////////////////////////////

	public Color PlayerColor => PlayerColors.Instance?.GetColor(this) ?? Team.GetColor(false);

	[RequireComponent] public PlayerId PlayerId { get; private set; }
	[RequireComponent] public MedicPlayer LocalStatsSnapshot { get; private set; }

	//////////////////////////////////////////////////////////////

	[Sync(SyncFlags.FromHost), Property] public ulong SteamId { get; private set; }
	[Sync(SyncFlags.FromHost), Property] public string SteamName { get; private set; }

	//////////////////////////////////////////////////////////////

	[Sync(SyncFlags.FromHost), Property] public Team Team { get; private set; } // TODO : listen to onteamchange
	[Sync(SyncFlags.FromHost), ValidOrNull] public PlayerPawn PlayerPawn { get; private set; }
	[Sync(SyncFlags.FromHost), ValidOrNull] public PlayerPawn SpectatingTarget { get; private set; }

	//////////////////////////////////////////////////////////////

	protected override void OnStart()
	{
		base.OnStart();

		//var Stats = GameMode.Instance.GetStats();
		//if (Stats.IsValid())
		//{
		//	LocalStatsSnapshot.SetLocalStatsObject(Stats.GetPlayerStatsSnapshot((long)SteamId));
		//}
	}

	private int TeamIndex = 0;
	public bool Initilize(Connection ConnectionIn)
	{
		Assert.True(Networking.IsHost);
		Assert.True(ConnectionIn != null);

		Connection = ConnectionIn;
		SteamId = Connection.SteamId;
		SteamName = Connection.DisplayName;
		Team = GameMode.Instance.GetStarterTeam();
		// RequestedCharacterDefinition = WorldUtil.GetRandomCharacter();

		// client rpc
		using (Rpc.FilterInclude(Connection))
		{
			ClientInitilize();
		}

		return true;
	}

	private GameObject AssumedSceneCameraObject = null;
	[Rpc.Broadcast]
	public void ClientInitilize()
	{
		Local = this;

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

			AssumedSceneCameraObject = Scene.Camera.GameObject;
		}
		AssumedSceneCameraObject.Enabled = true;
	}

	public void AssignTeam(Team team)
	{
		Assert.True(Networking.IsHost);

		Team = team;
	}
}
