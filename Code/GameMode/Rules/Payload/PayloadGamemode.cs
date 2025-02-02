using KOTH.World;
using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Events;

namespace KOTH;

public sealed class PayloadGamemode : Component,
	IGameEventHandler<UpdateStateEvent>,
	IGameEventHandler<EnterStateEvent>
{
	[Property] public GameObject PayloadGameobject { get; private set; }
	[Property] public GameObject PayloadPathGameobject { get; private set; }

	PayloadCart PayloadCartComponent { get => PayloadGameobject.GetComponent<PayloadCart>(); }
	PayloadPath PayloadPathComponent { get => PayloadPathGameobject.GetComponent<PayloadPath>(); }

	void IGameEventHandler<EnterStateEvent>.OnGameEvent(EnterStateEvent eventArgs)
	{
		Assert.IsValid(PayloadCartComponent);
		Assert.IsValid(PayloadPathGameobject);

		var StartLocationRotation = PayloadPathComponent.GetStartPositionRotation();
		PayloadGameobject.WorldPosition = StartLocationRotation.Position;
		PayloadGameobject.WorldRotation = StartLocationRotation.Rotation;
	}

	void IGameEventHandler<UpdateStateEvent>.OnGameEvent(UpdateStateEvent eventArgs)
	{
		Assert.True(Networking.IsHost);

		var (IsCapturing, CaptureFactor) = PayloadCartComponent.GetCaptureData();
		Log.Info(IsCapturing);

		if (IsCapturing)
		{
			PayloadGameobject.WorldPosition += Vector3.Forward * PayloadCartComponent.BaseSpeed * CaptureFactor;
		}
	}
}
