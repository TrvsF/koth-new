
namespace KOTH;

public partial class ViewModel : Component, IEquipment
{
	public Equipment Equipment { get; set; }
	PlayerPawn Owner => Equipment.IsValid() ? Equipment.Owner : null;

	[Property, Group("Components")] public SkinnedModelRenderer Arms { get; set; }
	[Property, Group("GameObjects")] public GameObject Muzzle { get; set; }
	[Property, Group("GameObjects")] public GameObject EjectionPort { get; set; }
	[Property, Group("Components")] public SkinnedModelRenderer ModelRenderer { get; set; }
	[Property, Group("Configuration")] public bool UseMovementInertia { get; set; } = true;

	private float YawInertiaScale => 2f;
	private float PitchInertiaScale => 2f;
	private bool activateInertia = false;
	private float lastPitch;
	private float lastYaw;
	private float YawInertia;
	private float PitchInertia;

	protected override void OnAwake()
	{
		ModelRenderer?.Set("b_deploy_skip", true);
	}

	protected override void OnStart()
	{
		// Somehow?
		if (Owner.IsValid())
			Owner.OnJump += OnPlayerJumped;

		// Somehow this can happen?
		if (!Equipment.IsValid())
			return;
	}

	void OnPlayerJumped()
	{
		ModelRenderer?.Set("b_jump", true);
	}

	void ApplyAnimationTransform()
	{
		if (!ModelRenderer.IsValid()) return;
		if (!ModelRenderer.Enabled) return;

		var bone = ModelRenderer.SceneModel.GetBoneLocalTransform("camera");
		var camera = Equipment.Owner.Camera.GameObject;

		var scale = GameSettingsSystem.Current.ViewBob / 100f;

		camera.LocalPosition += bone.Position * scale;
		camera.LocalRotation *= bone.Rotation * scale;
	}

	void ApplyInertia()
	{
		var PlayerCameraObject = Equipment.Owner.Camera.GameObject;
		var inRot = PlayerCameraObject.WorldRotation;

		// Need to fetch data from the camera for the first frame
		if (!activateInertia)
		{
			lastPitch = inRot.Pitch();
			lastYaw = inRot.Yaw();
			YawInertia = 0;
			PitchInertia = 0;
			activateInertia = true;
		}

		var newPitch = PlayerCameraObject.WorldRotation.Pitch();
		var newYaw = PlayerCameraObject.WorldRotation.Yaw();

		PitchInertia = Angles.NormalizeAngle(newPitch - lastPitch);
		YawInertia = Angles.NormalizeAngle(lastYaw - newYaw);

		lastPitch = newPitch;
		lastYaw = newYaw;
	}

	private Vector3 lerpedWishMove;

	private Vector3 localPosition;
	private Rotation localRotation;

	private Vector3 lerpedLocalPosition;
	private Rotation lerpedlocalRotation;

	protected void ApplyVelocity()
	{
		var moveVel = Owner.CharacterController.Velocity;
		var moveLen = moveVel.Length;

		var wishMove = Owner.WishMove.Normal * 1f;

		if (Owner.IsCrouching) moveLen *= 0.5f;

		lerpedWishMove = lerpedWishMove.LerpTo(wishMove, Time.Delta * 7.0f);
		ModelRenderer?.Set("move_bob", moveLen.Remap(0, 300, 0, 1, true));

		if (UseMovementInertia)
			YawInertia += lerpedWishMove.y * 10f;

		ModelRenderer?.Set("aim_yaw_inertia", YawInertia * YawInertiaScale);
		ModelRenderer?.Set("aim_pitch_inertia", PitchInertia * PitchInertiaScale);
	}

	private float FieldOfViewOffset = 0f;
	private float TargetFieldOfView = 90f;

	void ApplyAnimationParameters()
	{
		ModelRenderer.Set("b_grounded", Owner.IsGrounded);

		// Handedness
		ModelRenderer.Set("b_twohanded", true);

		// Weapon state
		ModelRenderer.Set("b_empty", !Equipment.Components.Get<AmmoComponent>(FindMode.EnabledInSelfAndDescendants)?.HasAmmo ?? false);
	}

	public bool PlayDeployEffects
	{
		set
		{
			ModelRenderer?.Set("b_deploy", value);
			ModelRenderer?.Set("b_deploy_skip", !value);
		}
	}

	protected override void OnUpdate()
	{
		// Reset every frame
		localRotation = Rotation.Identity;
		localPosition = Vector3.Zero;

		if (!Owner.IsValid() || !Owner.CharacterController.IsValid())
			return;

		ApplyAnimationParameters();

		ApplyVelocity();
		ApplyAnimationTransform();
		ApplyInertia();

		var baseFov = GameSettingsSystem.Current.FieldOfView;

		TargetFieldOfView = TargetFieldOfView.LerpTo(baseFov + FieldOfViewOffset, Time.Delta * 10f);
		FieldOfViewOffset = 0;

		lerpedlocalRotation = Rotation.Lerp(lerpedlocalRotation, localRotation, Time.Delta * 10f);
		lerpedLocalPosition = lerpedLocalPosition.LerpTo(localPosition, Time.Delta * 10f);

		LocalRotation = lerpedlocalRotation;
		LocalPosition = lerpedLocalPosition;
	}
}
