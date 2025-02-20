using Sandbox.Diagnostics;
using Sandbox.Events;

namespace KOTH;

public partial class PlayerPawn
{
	[Property] public Action OnJump { get; set; } // TODO : remove
	[Property] public float NoclipSpeed { get; set; } = 1000f;

	//////////////////////////////////////////////////////////////

	[HostSync] public bool IsFrozen { get; set; }
	[Sync] private Angles _rawEyeAngles { get; set; }
	private Angles _smoothEyeAngles;

	[Sync] public bool IsCrouching { get; set; }
	public float CrouchAmount { get; set; }
	[Sync] public bool IsNoclipping { get; set; }
	[Sync] public TimeSince TimeSinceLastInput { get; private set; }
	// [Sync] AnimationHelper.HoldTypes CurrentHoldType { get; set; } = AnimationHelper.HoldTypes.None;

	private Vector3 WishVelocity { get; set; }
	public Vector3 WishMove { get; private set; }
	public bool IsGrounded { get; set; }

	[Sync] private float _eyeHeightOffset { get; set; }
	private float _smoothEyeHeight;
	private Vector3 _previousVelocity;
	private Vector3 _jumpPosition;

	//////////////////////////////////////////////////////////////

	public bool SetMovementVariables(CharacterDefinition CharacterDefinitionIn)
	{
		if (!CharacterDefinitionIn.IsValid())
		{
			return false;
		}

		WeightFactor = CharacterDefinitionIn.WeightKnockbackFactor;
		AirMaxAcceleration = CharacterDefinitionIn.AirMaxAcceleration;
		MaxAcceleration = CharacterDefinitionIn.MaxAcceleration;
		SlowCrouchLerpSpeed = CharacterDefinitionIn.SlowCrouchLerpSpeed;
		CrouchLerpSpeed = CharacterDefinitionIn.CrouchLerpSpeed;
		JumpPower = CharacterDefinitionIn.JumpPower;
		CrouchingFriction = CharacterDefinitionIn.CrouchingFriction;
		WalkFriction = CharacterDefinitionIn.WalkFriction;
		AirAcceleration = CharacterDefinitionIn.AirAcceleration;
		CrouchingAcceleration = CharacterDefinitionIn.CrouchingAcceleration;
		BaseAcceleration = CharacterDefinitionIn.BaseAcceleration;
		WalkSpeed = CharacterDefinitionIn.WalkSpeed;

		return true;
	}

	public float WeightFactor;
	float AirMaxAcceleration;
	float MaxAcceleration;
	float SlowCrouchLerpSpeed;
	float CrouchLerpSpeed;
	float JumpPower;
	float CrouchingFriction;
	float WalkFriction;
	float AirAcceleration;
	float CrouchingAcceleration;
	float BaseAcceleration;
	float WalkSpeed;

	//////////////////////////////////////////////////////////////

	public Vector3 CenterPosition { get => PlayerBoxCollider.Center + WorldPosition; }
	public Angles EyeAngles
	{
		get => _smoothEyeAngles;
		set
		{
			if (!IsProxy) _smoothEyeAngles = value;
			_rawEyeAngles = value;
		}
	}

	//////////////////////////////////////////////////////////////

	private void ApplyAcceleration()
	{
		CharacterController.Acceleration = GetAcceleration();
	}

	// TODO : revisit
	private void UpdateCrouch()
	{
		CrouchAmount = CrouchAmount.LerpTo(IsCrouching ? 1 : 0, Time.Delta * GetCrouchLerpSpeed());
		_smoothEyeHeight = _smoothEyeHeight.LerpTo(_eyeHeightOffset * (IsCrouching ? CrouchAmount : 1), Time.Delta * 10f);
		CharacterController.Height = 64 + _smoothEyeHeight;
	}

	private float GetMaxAcceleration()
	{
		return CharacterController.IsOnGround ? MaxAcceleration : AirMaxAcceleration;
	}

	Vector3 Gravity = new Vector3(0, 0, 800); // TODO : move me
	private void ApplyMovement()
	{
		CharacterController.ApplyFriction(GetFriction());

		// just do noclip if we're in that mode
		if (DEBUGNoclipCheck())
		{
			return;
		}

		// set our velocity
		if (CharacterController.IsOnGround)
		{
			CharacterController.Velocity = CharacterController.Velocity.WithZ(0);
			CharacterController.Accelerate(WishVelocity);
			CharacterController.Velocity = CharacterController.Velocity.ClampLength(GetWishSpeed());
		}
		else
		{
			CharacterController.Velocity -= Gravity * Time.Delta;
			CharacterController.Accelerate(WishVelocity.ClampLength(GetMaxAcceleration()));
		}

		CharacterController.Move();
	}

	[Rpc.Broadcast]
	public void DoKnockback(Vector3 Knockback)
	{
		CharacterController.Punch(Knockback);
	}

	private bool DEBUGNoclipCheck()
	{
		if (!IsNoclipping)
		{
			return false;
		}

		var vertical = 0f;
		if (Input.Down("Jump")) vertical = 1f;
		if (Input.Down("Duck")) vertical = -1f;

		CharacterController.IsOnGround = false;
		CharacterController.Velocity = WishMove.Normal * EyeAngles.ToRotation() * NoclipSpeed;
		CharacterController.Velocity += Vector3.Up * vertical * NoclipSpeed;
		CharacterController.Move();

		return true;
	}

	TimeSince TimeSinceCrouchPressed = 10f;
	TimeSince TimeSinceCrouchReleased = 10f;

	private float GetCrouchLerpSpeed()
	{
		if (TimeSinceCrouchPressed < 1f && TimeSinceCrouchReleased < 1f)
			return SlowCrouchLerpSpeed;

		return CrouchLerpSpeed;
	}

	private int CrouchOffset = 16;
	private int CrouchOffset2 = 8;
	private void BuildInput()
	{
		IsCrouching = Input.Down("Duck") && !IsNoclipping;

		if (IsCrouching)
		{
			TimeSinceCrouchPressed = 0;
			if (IsGrounded)
			{
				PlayerBoxCollider.Center = new(0, 0, 32 - CrouchOffset2);
				PlayerBoxCollider.Scale = new(32, 32, 64 - CrouchOffset);
			}
			else
			{
				PlayerBoxCollider.Center = new(0, 0, 32 + CrouchOffset2);
				PlayerBoxCollider.Scale = new(32, 32, 64 - CrouchOffset);
			}
		}

		if (Input.Released("Duck"))
		{
			TimeSinceCrouchReleased = 0;
			PlayerBoxCollider.Center = new(0, 0, 32);
			PlayerBoxCollider.Scale = new(32, 32, 64);
		}

		if (Input.Pressed("Noclip") && Game.IsEditor)
		{
			IsNoclipping = !IsNoclipping;
		}

		if (WishMove.LengthSquared > 0.01f || Input.Down("Attack1"))
		{
			TimeSinceLastInput = 0f;
		}

		if (CharacterController.IsOnGround && !IsFrozen)
		{
			if (Input.Pressed("Jump"))
			{
				Jump();
			}
		}
	}

	public void Jump()
	{
		Punch(Vector3.Up * JumpPower);
	}

	public void Punch(Vector3 Vector)
	{
		CharacterController.Punch(Vector);
		BroadcastPlayerJumped();
	}

	public SceneTraceResult TraceBBox(Vector3 start, Vector3 end, float liftFeet = 0.0f, float liftHead = 0.0f)
	{
		var bbox = CharacterController.BoundingBox;
		var mins = bbox.Mins;
		var maxs = bbox.Maxs;

		if (liftFeet > 0)
		{
			start += Vector3.Up * liftFeet;
			maxs = maxs.WithZ(maxs.z - liftFeet);
		}

		if (liftHead > 0)
		{
			end += Vector3.Up * liftHead;
		}

		var tr = Scene.Trace.Ray(start, end)
					.Size(mins, maxs)
					.WithoutTags(CharacterController.IgnoreLayers)
					.IgnoreGameObjectHierarchy(GameObject.Root)
					.Run();
		return tr;
	}

	[Broadcast]
	public void BroadcastPlayerJumped()
	{
		AnimationHelper?.TriggerJump();
		OnJump?.Invoke();
	}

	public TimeSince TimeSinceGroundedChanged { get; private set; }

	const float MinimumFallDamage = 15f;
	private void GroundedChanged(bool WasOnGround, bool IsOnGround)
	{
		if (!IsLocallyControlled)
			return;

		TimeSinceGroundedChanged = 0;

		if (WasOnGround && !IsOnGround)
		{
			_jumpPosition = WorldPosition;
		}

		if (!WasOnGround && IsOnGround && !IsNoclipping)
		{
			var Velocity = MathF.Abs(_previousVelocity.z);
			var FallDamage = Velocity * 0.0225f; // 15/0.0225 = 666.6

			if (FallDamage > MinimumFallDamage)
			{
				PlayFallSound();
				FDamageRequest DamageRequest = new()
				{
					TargetDamageComponent = DamageComponent,
					AttackerPlayerPawn = this,
					TargetPlayerPawn = this,
					DamageOrigin = WorldPosition,
					TargetOrigin = CenterPosition,
					BaseDamage = FallDamage,
					BaseKnockbackStrength = 0,
					DirectImpact = true,
					DamageType = EDamageType.Melee,
				};
				Scene.Dispatch(new DamageRequestEvent(DamageRequest));
			}
		}
	}

	[Property, Group("Effects")] public SoundEvent LandSound { get; set; }

	[Broadcast]
	private void PlayFallSound()
	{
		var handle = Sound.Play(LandSound, WorldPosition);
		// handle.ListenLocal = IsViewer;
	}

	private void BuildWishInput()
	{
		WishMove = 0f;

		if (IsFrozen)
			return;

		WishMove += Input.AnalogMove;
	}

	private void BuildWishVelocity()
	{
		WishVelocity = 0f;

		var rot = EyeAngles.WithPitch(0f).ToRotation();

		var wishDirection = WishMove.Normal * rot;
		wishDirection = wishDirection.WithZ(0);
		WishVelocity = wishDirection * GetWishSpeed();
	}

	private float GetFriction()
	{
		if (!CharacterController.IsOnGround) return 0.1f;
		if (IsCrouching) return CrouchingFriction;
		return WalkFriction;
	}

	private float GetAcceleration()
	{
		if (!CharacterController.IsOnGround) return AirAcceleration;
		else if (IsCrouching) return CrouchingAcceleration;

		return BaseAcceleration;
	}

	float GetEyeHeightOffset()
	{
		if (IsCrouching) return -16f;
		if (!IsAlive) return -48f;
		return 0f;
	}

	const float CrouchFactor = 0.75f;
	private float GetWishSpeed()
	{
		if (IsCrouching) return WalkSpeed * CrouchFactor;
		return WalkSpeed;
	}

	Vector3 LastVelocity = Vector3.Zero;
	private void DebugUpdate()
	{
		DebugText.Update();
		DebugText.Write($"Player", Color.White, 20);
		DebugText.Write($"Velocity: {CharacterController.Velocity}");
		var Speed = CharacterController.Velocity.Length;
		var HSpeed = CharacterController.Velocity.WithZ(0).Length;
		var VSpeed = CharacterController.Velocity.z;
		DebugText.Write($"Speed: {Speed}", Speed >= LastVelocity.Length ? Color.Green : Color.Red);
		DebugText.Write($"HSpeed: {HSpeed}", HSpeed >= LastVelocity.WithZ(0).Length ? Color.Green : Color.Red);
		DebugText.Write($"VSpeed: {VSpeed}", VSpeed >= LastVelocity.z ? Color.Green : Color.Red);

		LastVelocity = CharacterController.Velocity;
	}
}
