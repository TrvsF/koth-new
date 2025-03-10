namespace KOTH;

public partial class PlayerPawn
{
	/// <summary>
	/// Represents the sound event associated with the "Medic" audio cue for the player character.
	/// </summary>
	public FSound MedicSound;

	/// <summary>
	/// Performs audio updates for the player pawn, handling sound-related behavior and interactions.
	/// </summary>
	/// <remarks>
	/// This method primarily updates the audio state by delegating to the SoundTick method of the
	/// audio component.
	/// </remarks>
	private void SoundTick()
	{
		AudioComponent.SoundTick();
		if (Input.Pressed("Use"))
		{
			MedicSound = new FSound(PlayerPawnDefinition.CharacterDefinition.MedicVoiceEvent, WorldPosition, this,
				true);
			AudioComponent.PlaySoundForTeam(MedicSound, Team);
		}
	}
}
