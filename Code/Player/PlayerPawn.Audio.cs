#nullable enable
using System.Threading.Tasks;

namespace KOTH;

public partial class PlayerPawn
{

	private List<FSound> _stoppedSounds = new List<FSound>();

	private Dictionary<FSound, SoundHandle> SoundHandles { get; set; } = new();

	[Rpc.Broadcast]
	public void PlaySound(FSound soundEvent)
	{
		if (Networking.IsClient)
		{
			Log.Info("Client PlaySound");
			Log.Info("Sound Event " + soundEvent);
		}
		else
		{
			Log.Info("Host PlaySound");
			Log.Info("Sound Event " + soundEvent.SoundId);

		}
		var handle = Sound.Play(soundEvent.SoundEvent, soundEvent.Position);

		Log.Info("Client SoundHandle: " + handle.Name + " soundEventPos: " + soundEvent.Position + " Handle" + handle.IsPlaying);

		SoundHandles.Add(soundEvent, handle);

		if (Networking.IsHost)
		{
			Log.Info("Host Adding Sound Event to list " + soundEvent.SoundId);
		}
		else
		{
			Log.Info("New Sound event " + soundEvent.SoundId);
		}
	}


	// TODO clean up and add networking

	private void SoundTick()
	{
		if (Networking.IsHost)
		{
			Log.Info("Host SoundTick " + SoundHandles.Count);
		}
		foreach (var soundHandle in SoundHandles)
		{
			Log.Info( $"Owner {soundHandle.Key.Owner}");
			if (soundHandle is { Value: not null, Value.Finished: false, Key.UpdatePosition: true, })
			{
				Log.Info("Updating sound position");
				soundHandle.Value.Position = soundHandle.Key.Owner.WorldPosition;
			}

			if (!soundHandle.Value.IsValid() || soundHandle.Value.Finished || soundHandle.Value.IsStopped)
			{
				_stoppedSounds.Add(soundHandle.Key);
			}
		}

		CleanUpStoppedSounds();
	}

	private void CleanUpStoppedSounds()
	{
		foreach (var soundHandle in _stoppedSounds)
		{

			bool x = SoundHandles.Remove(soundHandle);

			if (Networking.IsHost)
			{
				Log.Info("Host Removing sound" + soundHandle.SoundEvent.ResourceName);
			}
			if(x)
			{
				Log.Info("Client Removing sound " + soundHandle.SoundEvent.ResourceName);
			}
		}

		_stoppedSounds.Clear();
	}
}

public struct FSound : IEquatable<FSound>
{
	public SoundEvent SoundEvent { get; init; }
	public Vector3 Position { get; init; }

	public Guid SoundId { get; init;  }

	public Component Owner { get; init; }
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
