using KOTH.UI;
using Sandbox;
using Sandbox.Events;

namespace KOTH;

public sealed class Hill : Component, Component.ITriggerListener,
	IGameEventHandler<ResetScoresEvent>
{
	[RequireComponent] public Zone Zone { get; private set; }

	[Property] public float CaptureTime { get; set; } = 5f;
	[Property] public float WinTime { get; set; } = 10f;

	[HostSync] public bool IsActive { get; private set; } = false;
	[HostSync] public Team OwningTeam { get; private set; } = Team.Unassigned;
	[HostSync] private NetDictionary<Team, float> TeamsTimer { get; set; } = new();
	[HostSync] private NetList<PlayerPawn> CurrentHillPlayers { get; set; } = new();

	////////////////////////////////////////////////////////////////////////

	protected override void OnValidate()
	{
		base.OnValidate();

		Zone.Color = Color.Yellow;
		Zone.DisplayName = "Hill";
	}

	protected override void OnStart()
	{
		base.OnStart();

		IsActive = true;
		HillCaptureTime = 0;

		using (Rpc.FilterInclude(Connection.Host))
		{
			ResetHill();
		}
	}

	public float GetTeamsTimeLeft(Team Team)
	{
		return TeamsTimer[Team];
	}

	[Broadcast(NetPermission.HostOnly)]
	public void SetHillActive(bool Active)
	{
		IsActive = Active;
		ResetHill();
	}

	[Broadcast(NetPermission.HostOnly)]
	public void ResetHill()
	{
		TeamsTimer = new()
		{
			{ Team.CounterTerrorist, WinTime },
			{ Team.Terrorist, WinTime },
		};
		OwningTeam = Team.Unassigned;
		IsFirstPassOfRound = true;
		SetChildGeometryColour(Color.White);
	}

	[HostSync] public RealTimeSince HillCaptureTime { get; set; } = new();
	bool IsCapping = false;
	bool IsFirstPassOfRound = true;
	protected override void OnUpdate()
	{
		if (!Networking.IsHost || !IsActive)
		{
			return;
		}

		if (IsFirstPassOfRound)
		{
			HillCaptureTime = 0;
			IsFirstPassOfRound = false;
		}

		if (CurrentHillPlayers.Count == 0)
		{
			if (HillCaptureTime > 0)
			{
				BroadcastHillDecay();
				HillCaptureTime = Math.Max(0, HillCaptureTime - (Time.Delta * 2f));
			}

			IsCapping = false;
		}

		bool IsContested = false;
		List<Team> TeamsOnHill = new();

		// evil hack to remove hill players
		foreach (var PlayerPawn in CurrentHillPlayers.Reverse())
		{
			if (!PlayerPawn.IsValid())
			{
				CurrentHillPlayers.Remove(PlayerPawn);
				continue;
			}

			var PlayerTeam = TeamExtensions.GetTeam(PlayerPawn.GameObject);
			if (!TeamsOnHill.Contains(PlayerTeam))
			{
				TeamsOnHill.Add(PlayerTeam);
			}
		}

		if (TeamsOnHill.Count == 1)
		{
			var CurrentTeamOnHill = TeamsOnHill[0];
			if (CurrentTeamOnHill != OwningTeam)
			{
				// start capping
				if (!IsCapping)
				{
					// CaptureTimeSince = 0;
					IsCapping = true;
				}

				// cap
				if (HillCaptureTime >= CaptureTime)
				{
					OwningTeam = CurrentTeamOnHill;
					HillCaptureTime = 0;
					IsCapping = false;

					BroadcastHillCapture();
				}

				BroadcastHillCapping(CurrentTeamOnHill);
			}
		}
		else if (TeamsOnHill.Count == 2)
		{
			IsContested = true;
		}

		// if someone owns the hill countdown
		if (OwningTeam != Team.Unassigned)
		{
			if (TeamsTimer[OwningTeam] <= 0)
			{
				if (!IsContested)
				{
					BroadcastHillWin();
					ResetHill();
				}
			}
			else
			{
				TeamsTimer[OwningTeam] -= Time.Delta;
			}
		}
	}

	private void SetChildGeometryColour(Color Colour)
	{
		foreach (var Child in GameObject.Root.Children)
		{
			var ChildModel = Child.Components.Get<ModelRenderer>();
			if (ChildModel.IsValid())
			{
				ChildModel.Tint = Colour;
			}
		}
	}

	[Broadcast(NetPermission.HostOnly)]
	public void BroadcastHillDecay()
	{
		Scene.Dispatch(new HillDecayCapEvent(HillCaptureTime / CaptureTime, this)); ;
	}

	[Broadcast(NetPermission.HostOnly)]
	public void BroadcastHillCapping(Team Team)
	{
		Scene.Dispatch(new HillCappingEvent(Team, HillCaptureTime / CaptureTime, this)); ;
	}

	[Broadcast(NetPermission.HostOnly)]
	public void BroadcastHillCapture()
	{
		Scene.Dispatch(new HillCapturedEvent(CurrentHillPlayers.ToList(), OwningTeam, this));
		SetChildGeometryColour(TeamExtensions.GetColor(OwningTeam, true));
	}

	[Broadcast(NetPermission.HostOnly)]
	public void BroadcastHillWin()
	{
		Scene.Dispatch(new HillWinEvent(OwningTeam, this));
	}

	void ITriggerListener.OnTriggerEnter(Collider Collider)
	{
		var PlayerPawn = Collider.GameObject.Root.Components.Get<PlayerPawn>();
		if (PlayerPawn.IsValid())
		{
			CurrentHillPlayers.Add(PlayerPawn);
		}
	}

	void ITriggerListener.OnTriggerExit(Collider Collider)
	{
		var PlayerPawn = Collider.GameObject.Root.Components.Get<PlayerPawn>();
		if (PlayerPawn.IsValid())
		{
			CurrentHillPlayers.Remove(PlayerPawn);
		}
	}

	public void OnGameEvent(ResetScoresEvent eventArgs)
	{
		// TODO : is this just a verbosity thing?
		using (Rpc.FilterInclude(Connection.Host))
		{
			ResetHill();
		}
	}
}
