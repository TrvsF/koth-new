using KOTH.World;
using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Events;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace KOTH;

public record OnPayloadCapturePointEvent(int PointIndex) : IGameEvent;

public sealed class PayloadGamemode : Component,
	ITeamSpawnTime,
	IGameEventHandler<UpdateStateEvent>,
	IGameEventHandler<EnterStateEvent>,
	IGameEventHandler<LeaveStateEvent>
{
	[Property] public GameObject PayloadGameobject { get; set; } = null;
	[Property] public GameObject PayloadPathGameobject { get; set; } = null;

	[Property] public List<GameObject> CTSpawns { get; set; } = null;

	[Property] public GameObject TActiveSpawn { get; set; } = null;
	[Property] public GameObject CTActiveSpawn { get; set; } = null;

	[Property] public SoundEvent PayloadMoveSound { get; set; }
	[Property] public float SetupTime { get; set; } = 30f;

	protected override void OnStart()
	{
		base.OnStart();

		if (PayloadGameobject == null || PayloadPathGameobject == null)
		{
			Log.Warning("tried to start payload gameobject without payload/payloadpath gameobject set");
			Enabled = false;
		}
	}

	public PayloadCart PayloadCartComponent { get => PayloadGameobject.GetComponent<PayloadCart>(); }
	public PayloadPath PayloadPathComponent { get => PayloadPathGameobject.GetComponent<PayloadPath>(); }

	public float TSpawnTime => TActiveSpawn.GetComponent<SpawnZone>().SpawnTime;
	public float CTSpawnTime => CTActiveSpawn.GetComponent<SpawnZone>().SpawnTime;

	public List<SpawnDoor> SpawnDoors => TActiveSpawn.GetComponent<SpawnZone>().SpawnDoors;

	private RealTimeSince TimeSinceStart = 0;
	private bool IsSetupTime = true;
	private bool HasCartFinished = false;
	private int CurrentPathSegmentIndex = 0;
	private FSound PayloadPushSound;

	[Sync] private int CurrentSegmentIndex { get; set; } = 0;
	[Sync] private float TargetTransitionFactor { get; set; } = 0;

	protected override void OnEnabled()
	{
		PayloadPushSound = new FSound(PayloadMoveSound, PayloadCartComponent.WorldPosition, PayloadCartComponent, true);
	}

	void IGameEventHandler<LeaveStateEvent>.OnGameEvent(LeaveStateEvent eventArgs)
	{
		PayloadCartComponent.HighlightOutline.Destroy();
		PayloadCartComponent.HighlightOutline = null;
	}

	void IGameEventHandler<EnterStateEvent>.OnGameEvent(EnterStateEvent eventArgs)
	{
		Assert.IsValid(PayloadCartComponent);
		Assert.IsValid(PayloadPathGameobject);
		Assert.True(CTSpawns.Count > 0);

		var StartLocationRotation = PayloadPathComponent.GetStartPositionRotation();
		PayloadGameobject.WorldPosition = StartLocationRotation.Position;
		PayloadGameobject.WorldRotation = StartLocationRotation.Rotation;

		PayloadCartComponent.HighlightOutline = PayloadGameobject.AddComponent<HighlightOutline>();
		PayloadCartComponent.HighlightOutline.ObscuredColor = Color.Blue.WithAlpha(0.2f);
		PayloadCartComponent.HighlightOutline.Color = Color.Cyan.WithAlpha(0.05f);
		PayloadCartComponent.HighlightOutline.Width = .25f;

		TimeSinceStart = 0;
		IsSetupTime = true;
		HasCartFinished = false;
		CurrentSegmentIndex = 0;
		TargetTransitionFactor = 0;
		CurrentPathSegmentIndex = 0;

		foreach (var Door in SpawnDoors)
		{
			Door.Open = false;
		}

		SetCTSpawn(0);
	}

	void IGameEventHandler<UpdateStateEvent>.OnGameEvent(UpdateStateEvent eventArgs)
	{
		Assert.True(Networking.IsHost);

		if (IsSetupTime)
		{
			if (TimeSinceStart < SetupTime)
			{
				return;
			}

			IsSetupTime = false;
			foreach (var Door in SpawnDoors)
			{
				Door.Open = true;
			}
		}

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
			if (AudioComponent.Instance.IsPlayingSound(PayloadPushSound))
			{
				AudioComponent.Instance.StopSound(PayloadPushSound);
			}

			return;
		}

		AudioComponent.Instance.PlaySound(PayloadPushSound);

		var AllSegmentLocations = PayloadPathComponent.AllSegmentPoints;
		var CurrentNodePos = AllSegmentLocations[CurrentSegmentIndex];

		var TargetNodePos = AllSegmentLocations[CurrentSegmentIndex + 1];

		var TotalSegmentDistance = CurrentNodePos.Distance(TargetNodePos);
		var FactorMoved = (PayloadCartComponent.BaseSpeed * CaptureFactor) / TotalSegmentDistance;

		TargetTransitionFactor += FactorMoved;
		if (TargetTransitionFactor > 1)
		{
			TargetTransitionFactor = 0;
			++CurrentSegmentIndex;

			var PriorPathsCovered = 0;
			for (int Index = CurrentPathSegmentIndex; Index >= 0; --Index)
			{
				PriorPathsCovered += PayloadPathComponent.PathSegments[Index].SegmentPoints.Count;
			}

			if (CurrentSegmentIndex >= PriorPathsCovered)
			{
				OnCapture(CurrentPathSegmentIndex);
				++CurrentPathSegmentIndex;
			}

			if (AllSegmentLocations.Count == CurrentSegmentIndex + 1)
			{
				// CART STOPPED
				BroadcastOnCapture(-1);
				HasCartFinished = true;
				return;
			}

			PayloadGameobject.WorldRotation = PayloadPathComponent.GetRotationFromNodeIndexes(CurrentSegmentIndex, CurrentSegmentIndex + 1);
			return;
		}

		var LerpedVector = Vector3.Lerp(CurrentNodePos, TargetNodePos, TargetTransitionFactor);
		PayloadGameobject.WorldPosition = LerpedVector;
	}

	private void SetCTSpawn(int SpawnIndex)
	{
		if (CTSpawns.Count < SpawnIndex)
		{
			return;
		}

		foreach (var Spawn in CTSpawns)
		{
			Spawn.Enabled = false;
		}

		CTSpawns[SpawnIndex].Enabled = true;
		CTActiveSpawn = CTSpawns[SpawnIndex].GetComponentInChildren<SpawnZone>().GameObject;
	}

	private void OnCapture(int Index)
	{
		BroadcastOnCapture(Index);

		var SpawnIndex = Index + 1;
		SetCTSpawn(SpawnIndex);

		GameMode.Instance.StateMachine.NextStateTime += 120;
	}

	[Rpc.Broadcast]
	private void BroadcastOnCapture(int Index)
	{
		Game.TakeScreenshot();

		// HACK : shitty screenshot hack TODO : remove
		if (Index == -1)
		{
			return;
		}

		Scene.Dispatch(new OnPayloadCapturePointEvent(Index));
	}

	////////////////////////////////////////////////////////////////////////////////////////

	public float GetUIData(out List<(float Distance, float CaptureAmount)> SegmentDistances)
	{
		SegmentDistances = new();

		var AllSegmentLocations = PayloadPathComponent.AllSegmentPoints;
		var TotalSegmentsEvaluated = 0;

		foreach (var Path in PayloadPathComponent.PathSegments)
		{
			var SegmentPointsInPath = Path.SegmentPoints.Count;

			if (CurrentSegmentIndex + 1 >= TotalSegmentsEvaluated + SegmentPointsInPath)
			{
				SegmentDistances.Add((Path.GetDistance(), 1f));
				TotalSegmentsEvaluated += SegmentPointsInPath;
				continue;
			}

			if (TotalSegmentsEvaluated <= CurrentSegmentIndex && TotalSegmentsEvaluated + SegmentPointsInPath > CurrentSegmentIndex)
			{
				var TotalPathDistance = Path.GetDistance();
				var CoveredDistance = 0f;

				var PathIndex = CurrentSegmentIndex - TotalSegmentsEvaluated;
				for (int NodeIndex = 0; NodeIndex < Path.SegmentPoints.Count; ++NodeIndex)
				{
					if (PathIndex == NodeIndex)
					{
						break;
					}

					CoveredDistance += Path.SegmentPoints[NodeIndex].Distance(Path.SegmentPoints[NodeIndex + 1]);
				}

				var CoveredFactor = CoveredDistance / TotalPathDistance;
				CoveredFactor += (TargetTransitionFactor * AllSegmentLocations[CurrentSegmentIndex].Distance(AllSegmentLocations[CurrentSegmentIndex + 1])) / TotalPathDistance;
				
				SegmentDistances.Add((TotalPathDistance, CoveredFactor));

				TotalSegmentsEvaluated += SegmentPointsInPath;
				continue;
			}

			SegmentDistances.Add((Path.GetDistance(), 0));
			TotalSegmentsEvaluated += SegmentPointsInPath;
		}

		return 0;
	}
}
