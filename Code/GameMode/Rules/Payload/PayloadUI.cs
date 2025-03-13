using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KOTH;

public struct FPayloadUIData
{
	public int TotalDistance { get; init; }
	public List<(bool IsPoint, float Distance)> OutData { get; init; }
}

public sealed class PayloadUI
{
	public static PayloadGamemode CurrentPayloadGamemode => GameMode.Instance?.Components.Get<PayloadGamemode>();

	public static bool QueryUIData(out FPayloadUIData UiData)
	{
		UiData = new();
		
		if (!CurrentPayloadGamemode.IsValid() || CurrentPayloadGamemode.PayloadPathComponent.IsValid())
		{
			return false;
		}

		CurrentPayloadGamemode.PayloadPathComponent.GetTotalDistance(out var DistanceList);

		return true;
	}
}
