using Sandbox;

namespace KOTH;

public sealed class LevelText : Component
{
	[RequireComponent] TextRenderer TextRenderer { get; set; }

	protected override void OnUpdate()
	{
		base.OnUpdate();

		TextRenderer.Text = Sandbox.Services.Stats.LocalPlayer.Get("damage-given").ValueString;
	}
}
