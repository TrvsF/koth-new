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
	[Range(3, 24)]
	public int RayCount { get; set; } = 10;

	[Property, Group("Detection")]
	public bool IncludeVertical { get; set; } = true;

	[Property, Group("Detection")]
	public TagSet IgnoreTags { get; set; }

	[Property, Group("Detection")]
	[Range(0.05f, 4f)]
	public float SurfaceOffset { get; set; } = 0.5f;

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

	private int TotalPanels => RayCount + (IncludeVertical ? 2 : 0);

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
		if (LocalPawn != null)
		{ 
			if (LocalPawn.IsDummy)
			{
				return;
			}
		}

		if (Panels is null || Panels.Length != TotalPanels)
		{
			RebuildPanels();
		}

		int Index = 0;

		for (int RayIndex = 0; RayIndex < RayCount; RayIndex++)
		{
			float Yaw = 360f / RayCount * RayIndex;
			PlacePanel(Panels[Index++], WorldPosition, Rotation.FromYaw(Yaw).Forward);
		}

		if (IncludeVertical)
		{
			PlacePanel(Panels[Index++], WorldPosition, Vector3.Up);
			PlacePanel(Panels[Index++], WorldPosition, Vector3.Down);
		}
	}

	private void PlacePanel(ModelRenderer Panel, Vector3 origin, Vector3 dir)
	{
		if (!Panel.IsValid()) return;

		var WorldTrace = Scene.Trace
			.Ray(origin, origin + dir * Range)
			.IgnoreGameObjectHierarchy(GameObject)
			.WithoutTags(IgnoreTags ?? new TagSet())
			.Run();

		Panel.GameObject.Enabled = WorldTrace.Hit;
		if (!WorldTrace.Hit) return;

		Panel.WorldPosition = WorldTrace.HitPosition + WorldTrace.Normal * SurfaceOffset;
		Panel.WorldRotation = Rotation.LookAt(WorldTrace.Normal) * Rotation.From(PanelAlignment);

		float size = MathF.Max(PanelModelSize, 1f);
		float scale = Range * 2.5f / size;
		Panel.WorldScale = new Vector3(scale, scale, 1f);

		var Attribute = Panel.SceneObject?.Attributes;
		if (Attribute is null) return;

		Attribute.Set("FocusPos", origin);
		Attribute.Set("HexSize", HexSize);
		Attribute.Set("Thickness", LineThickness);
		Attribute.Set("Fill", CellFill);
		Attribute.Set("Range", Range);
		Attribute.Set("Falloff", Falloff);
		Attribute.Set("Pulse", Pulse);
		Attribute.Set("Tint", Tint);
		Attribute.Set("Brightness", Brightness);
	}
}
