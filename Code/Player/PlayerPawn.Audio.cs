#nullable enable
using System.Threading.Tasks;

namespace KOTH;

public partial class PlayerPawn
{
	public List<FSound> CurrentSounds = new List<FSound>();
	private List<FSound> _stoppedSounds = new List<FSound>();

	public void PlaySound(FSound soundEvent)
	{
		var handle = Sound.Play(soundEvent.SoundEvent, soundEvent.Position);
		soundEvent.Handle = handle;
		CurrentSounds.Add(soundEvent);
	}


	// TODO clean up and add networking

	private void SoundTick()
	{
		foreach (var soundHandle in CurrentSounds)
		{
			if (soundHandle.Handle == null) continue;
			if (soundHandle is { Handle: not null, Handle.Finished: false, UpdatePosition: true, })
			{
				soundHandle.Handle.Position = soundHandle.Owner.LocalPosition;
			}

			if (!soundHandle.Handle.IsValid() || soundHandle.Handle.Finished || soundHandle.Handle.IsStopped)
			{
				_stoppedSounds.Add(soundHandle);
			}
		}

		CleanUpStoppedSounds();
	}

	private void CleanUpStoppedSounds()
	{
		foreach (var soundHandle in _stoppedSounds)
		{
			CurrentSounds.Remove(soundHandle);
		}
	}
}

public struct FSound() : IEquatable<FSound>
{
	public SoundEvent SoundEvent { get; init; }
	public Vector3 Position { get; init; }

	public Component Owner { get; init; }
	public bool UpdatePosition { get; init; } = false;

	public SoundHandle? Handle { get; set; } = null;

	public bool Equals(FSound other)
	{
		return Equals(SoundEvent, other.SoundEvent) && Equals(Owner, other.Owner) &&
		       UpdatePosition == other.UpdatePosition && Equals(Handle, other.Handle);
	}

	public override bool Equals(object obj)
	{
		return obj is FSound other && Equals(other);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(SoundEvent, UpdatePosition, Handle, Owner);
	}
}
