using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KOTH;

internal class OnScreenUtil
{

}

public class FScreenNumberElement
{
	public PlayerPawn PlayerPawn { get; init; } = null;
	public PlayerPawn TargetPawn { get; init; } = null;
	public Vector2 ScreenOffset { get; init; } = Vector2.Zero;
	public bool IsDamage { get; init; } = true;

	public Vector3 Location { get; set; } = Vector3.Zero;
	public TimeUntil DisplayedTime { get; set; } = new();
	public float Damage { get; set; } = 0f;

	public bool IsVisable { get; set; } = true;

	public FScreenNumberElement() { }

	public Vector2 GetRawScreenPos()
	{
		return ScreenOffset.WithY(ScreenOffset.y + (DisplayedTime * 0.075f));
	}
	
	public Vector2 GetScreenPos()
	{
		var IsBehind = false;
		var ScreenPos = PlayerPawn.Camera.PointToScreenNormal(Location, out IsBehind);
		ScreenPos.y += (DisplayedTime * 0.075f);

		return ScreenPos + ScreenOffset;
	}

	public void AddDamage(float InDamage)
	{
		Damage += InDamage;
	}

	public override string ToString()
	{
		if (IsDamage)
		{
			return $"-{Math.Round(Damage)}";
		}
		else
		{
			return $"+{Math.Round(Damage)}";
		}
	}

	// TODO : all these need to be cached, and all instances like it
	public Color GetColour()
	{
		if (IsDamage)
		{
			return Color.Yellow;
		}
		else
		{
			return TeamExtensions.GetColor(PlayerPawn.Team, false);
		}
	}

	public int GetSizePx()
	{
		if (Damage < 50)
		{
			return 24;
		}
		return (int)Math.Max(24, Math.Min(48, Math.Round(Damage * 0.7f)));
	}
}
