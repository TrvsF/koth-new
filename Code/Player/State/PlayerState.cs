using KOTH.UI;
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

	[HostSync, Change(nameof(OnPlayerStateStateChanged))] public EPlayerState PlayerStateState { get; set; }

	//////////////////////////////////////////////////////////////

	public Color PlayerColor => PlayerColors.Instance?.GetColor(this) ?? Team.GetColor(false);

	[RequireComponent] public PlayerId PlayerId { get; private set; }
	[RequireComponent] public LocalStats LocalStatsSnapshot { get; set; }

	//////////////////////////////////////////////////////////////

	[HostSync, Property] public ulong SteamId { get; set; }
	[HostSync] private string SteamName { get; set; }
	
	//////////////////////////////////////////////////////////////

	[HostSync, Property] public Team Team { get; private set; } // TODO : listen to onteamchange
	[HostSync, ValidOrNull] public PlayerPawn PlayerPawn { get; set; }
	[HostSync, ValidOrNull] public PlayerPawn SpectatingTarget { get; set; }

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

	public bool Initilize(Connection ConnectionIn)
	{
		Assert.True(Networking.IsHost);
		Assert.True(ConnectionIn != null);

		Connection = ConnectionIn;
		SteamId = Connection.SteamId;
		SteamName = Connection.DisplayName;
		AssignTeam(Team.Unassigned);

		// client rpc
		using (Rpc.FilterInclude(Connection))
		{
			ClientInitilize();
		}

		return true;
	}

	[Rpc.Broadcast]
	public void ClientInitilize()
	{
		Local = this;
	}

	public void AssignTeam(Team team)
	{
		Assert.True(Networking.IsHost);

		Team = team;
	}

	public void RequestNewClass(CharacterDefinition NewClass)
	{
		Assert.True(GameObject.Network.IsOwner);
		
		RequestedCharacterDefinition = NewClass;
	}
}
