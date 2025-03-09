#nullable enable
using System.Threading.Tasks;

namespace KOTH;

public partial class PlayerPawn
{
	[Sync(SyncFlags.FromHost)] public List<FSound> CurrentSounds { get; set; }
	private List<FSound> _stoppedSounds = new List<FSound>();

	public Dictionary<FSound, SoundHandle> SoundHandles { get; set; } = new();

	protected override void OnAwake()
	{
		base.OnAwake();

		if (Networking.IsHost)
		{
			CurrentSounds = new();
			SoundHandles = new();
		}
	}

	[Rpc.Broadcast]
	public void PlaySound(FSound soundEvent)
	{
		if (Networking.IsClient)
		{
			Log.Info("Client PlaySound");
			Log.Info("Sound Event " + soundEvent);
		}
		var handle = Sound.Play(soundEvent.SoundEvent, soundEvent.Position);

		Log.Info("Client SoundHandle: " + handle.Name + " soundEventPos: " + soundEvent.Position);

		if (Networking.IsHost)
		{
			CurrentSounds.Add(soundEvent);
		}
		SoundHandles.Add(soundEvent, handle);
	}


	// TODO clean up and add networking

	private void SoundTick()
	{
		foreach (var currentSound in CurrentSounds)
		{
			var soundHandle = SoundHandles.FirstOrDefault(x => x.Key.Equals(currentSound));

			if (soundHandle.Value is null)
			{
				Log.Error("SoundHandle is null");
				_stoppedSounds.Add(currentSound);
				continue;
			}

			Log.Info( "Owner " + soundHandle.Key.Owner);
			if (soundHandle is { Value: not null, Value.Finished: false, Key.UpdatePosition: true, })
			{
				soundHandle.Value.Position = soundHandle.Key.Owner.LocalPosition;
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
				Log.Info("Host Removing sound " + soundHandle.SoundEvent.ResourceName);
				CurrentSounds.Remove(soundHandle);
			}
			if(x)
			{
				Log.Info("Client Removing sound " + soundHandle.SoundEvent.ResourceName);
			}
		}

		_stoppedSounds.Clear();
	}
}

public struct FSound() : IEquatable<FSound>
{
	public SoundEvent SoundEvent { get; init; }
	public Vector3 Position { get; init; }

	public Component Owner { get; init; }
	public bool UpdatePosition { get; init; } = false;

	public bool Equals(FSound other)
	{
		return Equals(SoundEvent, other.SoundEvent) &&
		       UpdatePosition == other.UpdatePosition;
	}

	public override bool Equals(object obj)
	{
		return obj is FSound other && Equals(other);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(SoundEvent, UpdatePosition);
	}
}
