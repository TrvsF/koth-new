using Sandbox;
using Sandbox.Events;

namespace KOTH;

public sealed class ClassText : Component,
	IGameEventHandler<PlayerSpawnedEvent>
{
	[RequireComponent] TextRenderer TextRenderer { get; set; }

	public void OnGameEvent(PlayerSpawnedEvent eventArgs)
	{
		var Player = eventArgs.Player;
		if (!Player.IsValid())
		{
			return;
		}

		var Character = PlayerState.Local.RequestedCharacterDefinition;
		if (Character == null)
		{
			return;
		}

		TextRenderer.Text = $"you have picked {Character.CharacterName}" +
			$"\n";
	}

	protected override void OnUpdate()
	{
		// TextRenderer.Text = "cum\ncum";
	}
}
