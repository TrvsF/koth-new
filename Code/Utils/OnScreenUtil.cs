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
	public PlayerPawn PlayerPawn { get; set; } = null;
	public PlayerPawn TargetPawn { get; set; } = null;
	public Vector2 ScreenOffset { get; set; } = Vector2.Zero;
	public float Damage { get; set; } = 0f;
	public bool IsDamage { get; set; } = true;
	public TimeUntil DisplayedTime { get; set; } = new();
	public Vector3 Location = Vector3.Zero;

	public bool IsVisable { get; private set; } = true;

	public FScreenNumberElement() { }

	public void Init()
	{
		if (!PlayerPawn.IsValid())
		{
			return;
		}

		//if (TargetPawn.IsValid())
		//{
		//	var TargetDistance = PlayerPawn.WorldPosition.Distance(TargetPawn.WorldPosition);
		//	var ZOffset = MathX.Lerp(0, 64, (1 / TargetDistance) * 10);
		//	Location.z += ZOffset;
		//}

		DisplayedTime = 0;
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
			return $"-{Damage.CeilToInt()}";
		}
		else
		{
			return $"+{Damage.CeilToInt()}";
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
