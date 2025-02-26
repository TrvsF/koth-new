using KOTH.World;
using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Events;
using System.ComponentModel.DataAnnotations;

namespace KOTH;

public sealed class PayloadGamemode : Component,
	IGameEventHandler<UpdateStateEvent>,
	IGameEventHandler<EnterStateEvent>
{
	[Property] public GameObject PayloadGameobject { get; set; } = null;
	[Property] public GameObject PayloadPathGameobject { get; set; } = null;

	protected override void OnStart()
	{
		base.OnStart();

		if (PayloadGameobject == null || PayloadPathGameobject == null)
		{
			Log.Warning("tried to start payload gameobject without payload/payloadpath gameobject set");
			Enabled = false;
		}
	}

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

	bool HasCartFinished = false;
	int CurrentSegmentIndex = 0;
	float TargetTransitionFactor = 0;

	void IGameEventHandler<UpdateStateEvent>.OnGameEvent(UpdateStateEvent eventArgs)
	{
		Assert.True(Networking.IsHost);

		if (HasCartFinished)
		{
			Log.Info("the cart has stopped");
			if (GameObject.GetComponent<StateComponent>() is { } ParentState)
			{
				Assert.IsValid(ParentState.DefaultNextState);
				GameMode.Instance.StateMachine.Transition(ParentState.DefaultNextState);
			}
			return;
		}

		var (IsCapturing, CaptureFactor) = PayloadCartComponent.GetCaptureData();

		if (!IsCapturing)
		{
			return;
		}

		var AllSegmentLocations = PayloadPathComponent.AllSegmentPoints;
		var CurrentNodePos = AllSegmentLocations[CurrentSegmentIndex];

		var TargetNodePos = AllSegmentLocations[CurrentSegmentIndex + 1];

		var TotalSegmentDistance = CurrentNodePos.Distance(TargetNodePos);
		var FactorMoved = (PayloadCartComponent.BaseSpeed * CaptureFactor) / TotalSegmentDistance;

		TargetTransitionFactor += FactorMoved;
		if (TargetTransitionFactor > 1)
		{
			Log.Info("next node");

			TargetTransitionFactor = 0;
			++CurrentSegmentIndex;

			if (AllSegmentLocations.Count == CurrentSegmentIndex + 1)
			{
				// CART STOPPED
				HasCartFinished = true;
				return;
			}

			PayloadGameobject.WorldRotation = PayloadPathComponent.GetRotationFromNodeIndexes(CurrentSegmentIndex, CurrentSegmentIndex + 1);
			return;
		}

		var LerpedVector = Vector3.Lerp(CurrentNodePos, TargetNodePos, TargetTransitionFactor);
		PayloadGameobject.WorldPosition = LerpedVector;
	}
}
