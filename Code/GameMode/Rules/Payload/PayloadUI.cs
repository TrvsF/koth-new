using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KOTH;

public readonly struct FPayloadUIData
{
	public int TotalDistance { get; init; }
	public List<(float Distance, float CaptureAmount)> OutData { get; init; }
}

public sealed class PayloadUI
{
	public static PayloadGamemode CurrentPayloadGamemode => GameMode.Instance.StateMachine.CurrentState.GameObject.Components.Get<PayloadGamemode>();

	public static bool QueryUIData(out FPayloadUIData UiData)
	{
		if (!CurrentPayloadGamemode.IsValid() || !CurrentPayloadGamemode.PayloadPathComponent.IsValid())
		{
			UiData = new();
			return false;
		}

		var TotalDistance = CurrentPayloadGamemode.GetUIData(out var DistanceList);

		UiData = new()
		{
			TotalDistance = TotalDistance.FloorToInt(),
			OutData = new(),
		};

		foreach ((float Distance, float CaptureAmount) in DistanceList)
		{
			UiData.OutData.Add((Distance / 6f, CaptureAmount));
		}

		return true;
	}
}
