using KOTH;
using Sandbox;
using System;
using System.Linq;

public sealed class WallHexProjector : Component
{
	[Property, Group("Detection")]
	[Range(8f, 512f)]
	public float Range { get; set; } = 80f;

	[Property, Group("Detection")]
	[Range(0.05f, 4f)]
	public float SurfaceOffset { get; set; } = 0.5f;

	[Property, Group("Edge Clipping")]
	[Range(2, 32)]
	public int EdgeProbeSteps { get; set; } = 8;

	[Property, Group("Edge Clipping")]
	[Range(0, 8)]
	public int EdgeRefineSteps { get; set; } = 4;

	[Property, Group("Edge Clipping")]
	[Range(0.1f, 16f)]
	public float EdgeProbeDepth { get; set; } = 2f;

	[Property, Group("Edge Clipping")]
	[Range(0.5f, 1f)]
	public float EdgeNormalTolerance { get; set; } = 0.95f;

	[Property, Group("Hex")]
	[Range(0.5f, 128f)]
	public float HexSize { get; set; } = 8f;

	[Property, Group("Hex")]
	[Range(0f, 0.45f)]
	public float LineThickness { get; set; } = 0.07f;

	[Property, Group("Hex")]
	[Range(0f, 1f)]
	public float CellFill { get; set; } = 0.06f;

	[Property, Group("Hex")]
	[Range(0.25f, 8f)]
	public float Falloff { get; set; } = 2f;

	[Property, Group("Hex")]
	[Range(0f, 1f)]
	public float Pulse { get; set; } = 0.35f;

	[Property, Group("Hex")]
	public Color Tint { get; set; } = new Color(0.2f, 0.9f, 1f);

	[Property, Group("Hex")]
	[Range(0f, 10f)]
	public float Brightness { get; set; } = 2f;

	[Property, Group("Setup")]
	public Material HexMaterial { get; set; }

	[Property, Group("Setup")]
	public Model PanelModel { get; set; }

	[Property, Group("Setup")]
	public float PanelModelSize { get; set; } = 100f;

	[Property, Group("Setup")]
	public Angles PanelAlignment { get; set; } = new Angles(90f, 0f, 0f);

	private ModelRenderer[] Panels;
	public PlayerPawn LocalPawn = null;

	protected override void OnStart()
	{
		RebuildPanels();
	}

	protected override void OnDisabled()
	{
		DestroyPanels();
	}

	private int TotalPanels => 12;

	private void DestroyPanels()
	{
		if (Panels is null) return;

		foreach (var Panel in Panels)
		{
			Panel?.GameObject?.Destroy();
		}

		Panels = null;
	}

	private void RebuildPanels()
	{
		DestroyPanels();

		var Model = PanelModel ?? Sandbox.Model.Load("models/dev/plane.vmdl");
		Panels = new ModelRenderer[TotalPanels];

		for (int PanelIndex = 0; PanelIndex < Panels.Length; PanelIndex++)
		{
			var Object = Scene.CreateObject();
			Object.Name = $"HexPanel_{PanelIndex}";
			Object.Flags |= GameObjectFlags.NotSaved | GameObjectFlags.Hidden;

			var Renderer = Object.Components.Create<ModelRenderer>();
			Renderer.Model = Model;
			Renderer.MaterialOverride = HexMaterial;
			Renderer.RenderType = ModelRenderer.ShadowRenderType.Off;

			Panels[PanelIndex] = Renderer;
		}
	}

	protected override void OnUpdate()
	{
		if (LocalPawn == null || !LocalPawn.IsLocallyControlled)
		{
			return;
		}

		if (Panels is null || Panels.Length != TotalPanels)
		{
			RebuildPanels();
		}

		int Index = 0;

		for (int RayIndex = 0; RayIndex < 10; RayIndex++)
		{
			float Yaw = 360f / 10 * RayIndex;
			PlacePanel(Panels[Index++], WorldPosition, Rotation.FromYaw(Yaw).Forward);
		}


		PlacePanel(Panels[Index++], WorldPosition, Vector3.Up);
		PlacePanel(Panels[Index++], WorldPosition, Vector3.Down);
	}

	private SceneTraceResult RunTrace(Vector3 Start, Vector3 End)
	{
		return Scene.Trace
			.Ray(Start, End)
			.IgnoreGameObjectHierarchy(GameObject)
			.Run();
	}

	private void PlacePanel(ModelRenderer Panel, Vector3 Origin, Vector3 Direction)
	{
		if (!Panel.IsValid()) return;

		var WorldTrace = RunTrace(Origin, Origin + Direction * Range);

		Panel.GameObject.Enabled = WorldTrace.Hit;
		if (!WorldTrace.Hit) return;

		var HitPosition = WorldTrace.HitPosition;
		var SurfaceNormal = WorldTrace.Normal;
		var PanelRotation = Rotation.LookAt(SurfaceNormal) * Rotation.From(PanelAlignment);

		// The plane model is flat on its local Z, so local X and Y lie along the surface.
		var AxisX = PanelRotation.Forward;
		var AxisY = PanelRotation.Left;

		float HalfExtent = Range * 1.25f;
		float PositiveX = HalfExtent;
		float NegativeX = HalfExtent;
		float PositiveY = HalfExtent;
		float NegativeY = HalfExtent;

		PositiveX = FindSurfaceExtent(HitPosition, SurfaceNormal, AxisX, HalfExtent);
		NegativeX = FindSurfaceExtent(HitPosition, SurfaceNormal, -AxisX, HalfExtent);
		PositiveY = FindSurfaceExtent(HitPosition, SurfaceNormal, AxisY, HalfExtent);
		NegativeY = FindSurfaceExtent(HitPosition, SurfaceNormal, -AxisY, HalfExtent);

		float Width = PositiveX + NegativeX;
		float Height = PositiveY + NegativeY;

		if (Width < 0.01f || Height < 0.01f)
		{
			Panel.GameObject.Enabled = false;
			return;
		}

		var Center = HitPosition
			+ AxisX * ((PositiveX - NegativeX) * 0.5f)
			+ AxisY * ((PositiveY - NegativeY) * 0.5f);

		float Size = MathF.Max(PanelModelSize, 1f);

		Panel.WorldPosition = Center + SurfaceNormal * SurfaceOffset;
		Panel.WorldRotation = PanelRotation;
		Panel.WorldScale = new Vector3(Width / Size, Height / Size, 1f);

		var Attribute = Panel.SceneObject?.Attributes;
		if (Attribute is null) return;

		Attribute.Set("FocusPos", Origin);
		Attribute.Set("HexSize", HexSize);
		Attribute.Set("Thickness", LineThickness);
		Attribute.Set("Fill", CellFill);
		Attribute.Set("Range", Range);
		Attribute.Set("Falloff", Falloff);
		Attribute.Set("Pulse", Pulse);
		Attribute.Set("Tint", Tint);
		Attribute.Set("Brightness", Brightness);
	}

	private float FindSurfaceExtent(Vector3 Origin, Vector3 Normal, Vector3 Direction, float MaxDistance)
	{
		int Steps = Math.Max(EdgeProbeSteps, 1);
		float StepSize = MaxDistance / Steps;
		float Inside = 0f;
		float Outside = -1f;

		for (int Step = 1; Step <= Steps; Step++)
		{
			float Distance = StepSize * Step;

			if (!HasSurfaceAt(Origin + Direction * Distance, Normal))
			{
				Outside = Distance;
				break;
			}

			Inside = Distance;
		}

		if (Outside < 0f) return MaxDistance;

		for (int RefineIndex = 0; RefineIndex < EdgeRefineSteps; RefineIndex++)
		{
			float Middle = (Inside + Outside) * 0.5f;

			if (HasSurfaceAt(Origin + Direction * Middle, Normal))
			{
				Inside = Middle;
			}
			else
			{
				Outside = Middle;
			}
		}

		return Inside;
	}

	private bool HasSurfaceAt(Vector3 Point, Vector3 Normal)
	{
		var ProbeTrace = RunTrace(Point + Normal * EdgeProbeDepth, Point - Normal * EdgeProbeDepth);

		if (ProbeTrace.StartedSolid || !ProbeTrace.Hit) return false;

		return Vector3.Dot(ProbeTrace.Normal, Normal) >= EdgeNormalTolerance;
	}
}
