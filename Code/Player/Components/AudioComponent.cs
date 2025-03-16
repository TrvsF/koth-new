#nullable enable
using System.Threading.Tasks;

namespace KOTH;

/// <summary>
/// Manages sound playback within the game, including playing sounds globally or for specific teams,
/// updating sound states, and handling active sound instances.
/// </summary>
/// <remarks>
/// This component is responsible for managing the lifecycle of sounds,
/// including their initiation, positional updates, and cleanup. It provides
/// methods for playing sounds either globally or targeted to specific teams
/// and maintains references to active sound handles.
/// </remarks>
public sealed class AudioComponent : SingletonComponent<AudioComponent>
{
	/// <summary>
	/// A collection of sound events that have been stopped during the audio handling process.
	/// This list is managed internally and is primarily used to track and clean up finished or stopped sounds,
	/// ensuring efficient audio management within the system.
	/// </summary>
	[Property]
	private List<FSound> StoppedSounds { get; set; }

	/// <summary>
	/// A mapping between sound events and their corresponding sound handles used during audio processing.
	/// This dictionary is utilized to associate each active sound event with its control and state information,
	/// enabling management of sound playback, updates, and cleanup within the system.
	/// </summary>
	[Property]
	private Dictionary<FSound, SoundHandle> SoundHandles { get; set; }

	protected override void OnAwake()
	{
		base.OnAwake();
		SoundHandles = new Dictionary<FSound, SoundHandle>();
		StoppedSounds = new List<FSound>();
	}

	/// <summary>
	/// Plays a specified sound for all players on the given team.
	/// </summary>
	/// <param name="soundEvent">The sound event to be played.</param>
	/// <param name="team">The team for which the sound will be played.</param>
	public void PlaySoundForTeam(FSound soundEvent, Team team)
	{
		using (Rpc.FilterInclude(GameUtils.GetPlayers(team).Select(x => x.Connection)))
		{
			PlaySound(soundEvent);
		}
	}

	/// <summary>
	/// Plays a specified sound globally for all players in the game.
	/// </summary>
	/// <param name="soundEvent">The sound event to be played.</param>
	[Rpc.Broadcast]
	public void PlaySound(FSound soundEvent)
	{
		Log.Info("Playing sound " + soundEvent.SoundId);

		var alreadyPlaying = Instance.SoundHandles.Any(x => x.Key.Owner.Id == soundEvent.Owner.Id);
		if (alreadyPlaying) return;

		var handle = Sound.Play(soundEvent.SoundEvent, soundEvent.Position);
		Instance.SoundHandles.Add(soundEvent, handle);
		Log.Info("New Sound event " + soundEvent.SoundId);
	}

	/// <summary>
	/// Updates the positions of active sounds and removes references to completed or invalid ones.
	/// </summary>
	/// <remarks>
	/// This method iterates through all active sound handles. If a sound should update its position,
	/// its position is recalculated. Stopped, finished, or invalid sound handles are marked for cleanup
	/// and later removed.
	/// </remarks>
	public void SoundTick()
	{
		foreach (var soundHandle in Instance.SoundHandles)
		{
			if (!soundHandle.Key.Owner.IsValid())
			{
				soundHandle.Value.Stop();
				Instance.StoppedSounds.Add(soundHandle.Key);
				continue;
			}
			if (soundHandle is { Value: not null, Value.Finished: false, Key.UpdatePosition: true, }
			    && soundHandle.Value.Position != soundHandle.Key.Owner.WorldPosition
			    )
			{
				soundHandle.Value.Position = soundHandle.Key.Owner.WorldPosition;
			}

			if (!soundHandle.Value.IsValid() || soundHandle.Value.Finished || soundHandle.Value.IsStopped)
			{
				Instance.StoppedSounds.Add(soundHandle.Key);
			}
		}

		CleanUpStoppedSounds();
	}

	/// <summary>
	/// Cleans up the references to sounds that have been stopped, finished, or are no longer valid.
	/// </summary>
	/// <remarks>
	/// This method iterates through the list of stopped sounds, attempting to remove their corresponding entries
	/// from the active sound handles dictionary. Logs errors or successful removals as appropriate, then clears
	/// the list of stopped sounds.
	/// </remarks>
	private void CleanUpStoppedSounds()
	{
		foreach (var soundHandle in Instance.StoppedSounds)
		{
			bool x = Instance.SoundHandles.Remove(soundHandle);
			if (x)
			{
				Log.Info("Removed Sound event " + soundHandle.SoundId);
			}
			else
			{
				Log.Error("Failed to remove Sound event " + soundHandle.SoundId);
			}
		}

		Instance.StoppedSounds.Clear();
	}
}

/// <summary>
/// Represents a sound event within the game.
/// This struct is used to encapsulate data necessary to play a sound, including its location,
/// the associated sound event, and the owning component triggering the sound.
/// </summary>
public struct FSound : IEquatable<FSound>
{
	/// <summary>
	/// Represents the underlying data or identity of a playable sound within the audio system.
	/// </summary>
	public SoundEvent SoundEvent { get; init; }

	/// <summary>
	/// Represents the 3D position in the game world where the sound event should occur.
	/// </summary>
	public Vector3 Position { get; init; }

	/// <summary>
	/// A unique identifier for a specific sound event within the game.
	/// This identifier is automatically generated upon the creation of a sound event and is used
	/// to distinguish and manage individual sound instances effectively.
	/// </summary>
	public Guid SoundId { get; init;  }

	/// <summary>
	/// The component responsible for triggering the sound event in the game.
	/// </summary>
	public Component Owner { get; init; }

	/// <summary>
	/// Indicates whether the position of the sound should be updated dynamically.
	/// </summary>
	/// <remarks>
	/// If set to true, the sound's position is recalculated during the sound management process
	/// to match the world position of its owning component. This is particularly useful for
	/// sounds tied to moving objects or entities within the game world. If false, the sound
	/// remains static at its initial position.
	/// </remarks>
	public bool UpdatePosition { get; init; } = false;

	public FSound(SoundEvent soundEvent, Vector3 position, Component owner, bool updatePosition = false)
	{
		SoundEvent = soundEvent;
		Position = position;
		Owner = owner;
		UpdatePosition = updatePosition;
		SoundId = Guid.NewGuid();
	}

	public bool Equals(FSound other)
	{
		return other.SoundId == SoundId;
	}

	public override bool Equals(object obj)
	{
		return obj is FSound other && Equals(other);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(SoundEvent, UpdatePosition, SoundId);
	}
}
