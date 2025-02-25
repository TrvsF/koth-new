using KOTH.Utils;
using Sandbox.Diagnostics;
using Sandbox.Events;

namespace KOTH;

public record GamemodeInitializedEvent(string Title) : IGameEvent;

public sealed partial class GameMode : SingletonComponent<GameMode>,
	IGameEventHandler<PlayerSpawnedEvent>
{
	[Property] public string Title { get; set; }

	/////////////////////////////////////////////////////////////

	[RequireComponent] public DamageManager DamageManager { get; private set; }
	[RequireComponent] public ClassList ClassList { get; private set; }
	[RequireComponent] public Stats Stats { get; private set; }
	[RequireComponent] public TextChat TextChat { get; private set; }

	/////////////////////////////////////////////////////////////

	private StateMachineComponent _stateMachine;
	public StateMachineComponent StateMachine => _stateMachine ??= Components.GetInDescendantsOrSelf<StateMachineComponent>();

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

	/////////////////////////////////////////////////////////////

	void IGameEventHandler<PlayerSpawnedEvent>.OnGameEvent(PlayerSpawnedEvent PlayerSpawnedEvent)
	{
		Log.Info("player spawn event");

		var SpawnedPlayer = PlayerSpawnedEvent.Player;
		if (!SpawnedPlayer.IsValid())
		{
			Log.Warning($"{this} got invalid player for PlayerSpawnedEvent");
			return;
		}

		// TODO : we want to accept the scene's version of the spawn as truth but create our own
		// not-networked version that we can act on. This spawns another zone every time u spawn :(
		foreach (var Object in Scene.GetAllObjects(false))
		{
			if (Object.GetComponent<SpawnZone>() is { } SpawnZone)
			{
				var ClonedSpawnObject = Object.Clone(Object.WorldPosition, Object.WorldRotation, Object.WorldScale);
				ClonedSpawnObject.NetworkMode = NetworkMode.Never;
				var ClonedSpawnZone = ClonedSpawnObject.GetComponent<SpawnZone>();
				if (ClonedSpawnZone.IsValid())
				{
					ClonedSpawnZone.SetupForLocal();
				}
				else
				{
					Log.Warning("FUCK");
				}
			}
		}
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

	public Stats GetStats()
	{
		return Components.Get<Stats>();
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

		if (!_componentCache.TryGetValue(typeof(T), out var component) || component is { IsValid: false } || component is { Active: false })
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
}
