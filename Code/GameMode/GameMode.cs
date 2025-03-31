using KOTH.Notification;
using KOTH.PlayerExp;
using Sandbox.Diagnostics;
using Sandbox.Events;
using Sandbox.Utility;

namespace KOTH;

public record GamemodeInitializedEvent(string Title) : IGameEvent;

public sealed partial class GameMode : SingletonComponent<GameMode>,
	IGameEventHandler<LocalPlayerSpawnedEvent>,
	IGameEventHandler<LocalPlayerDiedEvent>,
	IGameEventHandler<LevelUpEvent>
{
	[Property] public string Title { get; set; } = "KOTH";
	[Property] public string NothingState { get; set; } = "";

	/////////////////////////////////////////////////////////////

	[RequireComponent] public DamageManager DamageManager { get; private set; }
	[RequireComponent] public ClassList ClassList { get; private set; }
	[RequireComponent] public TextChat TextChat { get; private set; }
	[RequireComponent] public NotificationManager NotificationManager { get; private set; }
	[RequireComponent] public ExpManager ExpManager { get; private set; }
	[RequireComponent] public AudioComponent AudioComponent { get; private set; }
	public GameStats GameStats { get => GameObject.GetComponent<GameStats>(); }

	/////////////////////////////////////////////////////////////

	private StateMachineComponent _stateMachine;

	public StateMachineComponent StateMachine =>
		_stateMachine ??= Components.GetInDescendantsOrSelf<StateMachineComponent>();

	/////////////////////////////////////////////////////////////
	
	public string GetTimeLeftString()
	{
		if (float.IsInfinity(StateMachine.NextStateTime) || StateMachine.CurrentState.DefaultDuration == 999)
		{
			return NothingState;
		}
		
		var Remaining = StateMachine.NextStateTime - Time.Now;
		var TimeRemaining = TimeSpan.FromSeconds(Remaining);
		var FormattedTime = string.Format("{0:D2}:{1:D2}", (int)TimeRemaining.TotalMinutes, TimeRemaining.Seconds);

		return FormattedTime;
	}

	/////////////////////////////////////////////////////////////

	protected override void OnAwake()
	{
		if (Networking.IsHost)
		{
			if (Instance is { IsValid: true, Active: true, Scene: { } scene } && scene == Scene)
			{
				GameObject.Enabled = false;
				return;
			}
		}

		base.OnAwake();
	}

	protected override void OnStart()
	{
		base.OnStart();

		NotificationManager.AddNotification(new FNotification()
		{
			Message = $"Welcome to {Title}",
			Duration = 5,
			Zone = ENotificationZone.Center,
			Image = "images/square.png"
		});

		if (Networking.IsHost)
		{
			MapSpawnZones = new();
			foreach (var Object in Scene.GetAllObjects(true))
			{
				if (Object.GetComponent<SpawnZone>() is { } SpawnZone)
				{
					MapSpawnZones.Add(Object);
				}
			}
		}
	}

	/////////////////////////////////////////////////////////////

	[Sync] public NetList<GameObject> MapSpawnZones { get; set; }
	private List<GameObject> SpawnZoneBlockers = new();

	void IGameEventHandler<LocalPlayerSpawnedEvent>.OnGameEvent(LocalPlayerSpawnedEvent PlayerSpawnedEvent)
	{
		var SpawnedPlayer = PlayerSpawnedEvent.Player;
		if (!SpawnedPlayer.IsValid())
		{
			Log.Warning($"{this} got invalid player for PlayerSpawnedEvent");
			return;
		}

		// TODO : we want to accept the scene's version of the spawn as truth but create our own
		// not-networked version that we can act on. This spawns another zone every time u spawn :(

		foreach (var TempZone in SpawnZoneBlockers)
		{
			Log.Warning("?");
			TempZone.Destroy();
		}

		SpawnZoneBlockers.Clear();

		foreach (var SpawnZoneObject in MapSpawnZones)
		{
			var SpawnZone = SpawnZoneObject.GetComponent<SpawnZone>();

			if (!SpawnZone.IsValid())
			{
				Log.Warning($"Invalid spawnzone in MapSpawnZones on {this}");
				continue;
			}

			if (SpawnZone.Team.GetOpponents() == PlayerState.Local.Team)
			{
				var ClonedSpawnObject = SpawnZoneObject.Clone(SpawnZoneObject.WorldPosition,
					SpawnZoneObject.WorldRotation, SpawnZoneObject.WorldScale);
				ClonedSpawnObject.NetworkMode = NetworkMode.Never;

				var ClonedSpawnZone = ClonedSpawnObject.GetComponent<SpawnZone>();
				if (ClonedSpawnZone.IsValid())
				{
					Log.Info("Creating Blocker");
					ClonedSpawnZone.CreatePlayerCollisionBox();
					SpawnZoneBlockers.Add(ClonedSpawnObject);
				}
				else
				{
					Log.Warning($"failed to create clone of spawn zone {SpawnZone}");
					ClonedSpawnObject.Destroy();
				}
			}
		}
	}

	public void OnGameEvent(LocalPlayerDiedEvent eventArgs)
	{
		foreach (var TempZone in SpawnZoneBlockers)
		{
			TempZone.Destroy();
		}

		SpawnZoneBlockers.Clear();
	}

	/////////////////////////////////////////////////////////////

	public Team GetStarterTeam()
	{
		int Ts = 0;
		int CTs = 0;

		foreach (var PlayerState in GameNetworkManager.PlayerStates)
		{
			if (PlayerState.IsValid())
			{
				if (PlayerState.Team == Team.CounterTerrorist)
				{
					++CTs;
				}

				if (PlayerState.Team == Team.Terrorist)
				{
					++Ts;
				}
			}
		}

		if (Ts > CTs)
		{
			return Team.CounterTerrorist;
		}
		else
		{
			return Team.Terrorist;
		}
	}

	/////////////////////////////////////////////////////////////

	private StateComponent _prevState;
	private readonly Dictionary<Type, Component> _componentCache = new();

	/// <summary>
	/// Gets the given component from within the game mode's object hierarchy, or null if not found / enabled.
	/// </summary>
	public T Get<T>(bool required = false)
		where T : class
	{
		if (_prevState != StateMachine.CurrentState)
		{
			_prevState = StateMachine.CurrentState;
			_componentCache.Clear();
		}

		if (!_componentCache.TryGetValue(typeof(T), out var component) || component is { IsValid: false } ||
			component is { Active: false })
		{
			component = Components.GetInDescendantsOrSelf<T>() as Component;
			_componentCache[typeof(T)] = component;
		}

		if (required && component is not T)
		{
			throw new Exception($"Expected a {typeof(T).Name} to be active in the {nameof(GameMode)}!");
		}

		return component as T;
	}

	public void OnGameEvent(LevelUpEvent eventArgs)
	{
		NotificationManager.AddNotification(new FNotification()
		{
			Message = $"Level Up: {eventArgs.Level}",
			Duration = 5,
			Zone = ENotificationZone.Center
		});
	}
}
