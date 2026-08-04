using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Events;
using static Sandbox.PhysicsContact;

namespace KOTH;

public sealed class EngiePlayer : Component
{
	public PlayerPawn OwnerPawn { get => GameObject.Root.GetComponent<PlayerPawn>(); }

	public TurretComponent ActiveTurretComponent { get; private set; }
	public bool IsTurretInWorld { get => ActiveTurretComponent.IsValid(); }
	public bool IsPreviewingTurret { get => TurretPreviewObject.IsValid(); }

	private GameObject TurretPreviewObject = null;

	protected override void OnStart()
	{
		base.OnStart();

		if (IsProxy)
		{
			return;
		}

		foreach (var GameObject in Scene.GetAllObjects(true))
		{
			if (GameObject.Network.Owner != PlayerState.Local.Connection)
			{
				continue;
			}

			if (GameObject.GetComponent<TurretComponent>() is {} Turret)
			{
				ActiveTurretComponent = Turret;
				break;
			}
		}
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
	}

	EEquipmentSlot GoneSlot = EEquipmentSlot.Undefined;

	private Vector3 GetTurretSpawnLocation()
	{
		Assert.IsValid(OwnerPawn);
		return OwnerPawn.CenterPosition + (OwnerPawn.AimRay.Forward * 128);
	}

	private void SpawnTurret()
	{
		var Turret = GameMode.Instance.ClassList.TurretPrefab.Clone(GetTurretSpawnLocation(), OwnerPawn.Boom.WorldRotation);
		ActiveTurretComponent = Turret.Components.Get<TurretComponent>();
		ActiveTurretComponent.OwnerState = PlayerState.Local; // !

		if (!OwnerPawn.Inventory.CurrentWeaponGameObject.IsValid())
		{
			Log.Error("failed to spawn turret bc of some bullshit");
			return;
		}

		GoneSlot = OwnerPawn.CurrentEquipment.Slot;
		ActiveTurretComponent.SetFromWeaponGameObject(OwnerPawn.Inventory.CurrentWeaponGameObject);
		OwnerPawn.Inventory.RemoveWeapon(OwnerPawn.CurrentEquipment);

		Turret.NetworkSpawn();
	}

	private void DestroyTurret()
	{
		if (GoneSlot == EEquipmentSlot.Primary)
		{
			OwnerPawn.Inventory.Give(OwnerPawn.PlayerPawnDefinition.CharacterDefinition.PrimaryWeapon, false);
		}
		else if (GoneSlot == EEquipmentSlot.Secondary)
		{
			OwnerPawn.Inventory.Give(OwnerPawn.PlayerPawnDefinition.CharacterDefinition.SecondaryWeapon, false);
		}
		GoneSlot = EEquipmentSlot.Undefined;

		ActiveTurretComponent.GameObject.Destroy();
		ActiveTurretComponent = null;
	}

	private void CreateTurretPreview()
	{
		TurretPreviewObject = GameMode.Instance.ClassList.TurretPrefab.Clone(GetTurretSpawnLocation(), OwnerPawn.Boom.WorldRotation);
		TurretPreviewObject.NetworkMode = NetworkMode.Never;

		foreach (var Component in TurretPreviewObject.Components.GetAll())
		{
			if (Component is SkinnedModelRenderer { } SkinnedModelRenderer)
			{
				SkinnedModelRenderer.Tint = SkinnedModelRenderer.Tint.WithAlpha(0.8f);
				continue;
			}

			Component.Enabled = false;
		}
	}

	private void DestroyTurretPreview()
	{
		if (!TurretPreviewObject.IsValid())
		{
			return;
		}

		TurretPreviewObject.Destroy();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if (IsProxy || !OwnerPawn.IsValid() || !OwnerPawn.IsLocallyControlled)
		{
			return;
		}

		if (TurretPreviewObject.IsValid())
		{
			TurretPreviewObject.WorldPosition = GetTurretSpawnLocation();
			TurretPreviewObject.WorldRotation = OwnerPawn.Boom.WorldRotation;
		}

		///////////////////////////////////////////////////

		bool RequestBuilding = Input.Pressed("use");		

		if (RequestBuilding)
		{
			if (IsTurretInWorld)
			{
				DestroyTurret();
			}

			if (!IsPreviewingTurret)
			{
				CreateTurretPreview();
			}
			else if (!IsTurretInWorld)
			{
				DestroyTurretPreview();
				SpawnTurret();
			}
		}

		//////////////////////////////////////////////////

		//bool RequestWeaponEquip = Input.Down("attack2");

		//if (RequestWeaponEquip)
		//{
		//	if (!Scene.IsValid())
		//	{
		//		Log.Warning($"the scene isn't fucking valid.. on engie component {this} attached to player {OwnerPawn}");
		//	}

		//	var TraceResults = Scene.Trace.Ray(OwnerPawn.CenterPosition, OwnerPawn.CenterPosition + (OwnerPawn.AimRay.Forward * 256f)) // magic
		//	.UseHitboxes()
		//	.IgnoreGameObjectHierarchy(OwnerPawn.GameObject.Root)
		//	.Size(Vector3.One)
		//	.RunAll();

		//	foreach (var TraceElement in TraceResults)
		//	{
		//		if (!TraceElement.Hit)
		//		{
		//			continue;
		//		}

		//		if (TraceElement.GameObject.Root.Components.Get<TurretComponent>(FindMode.EnabledInSelfAndDescendants) is { } HitTurret)
		//		{
		//			if (HitTurret.OwnerState.PlayerPawn == OwnerPawn)
		//			{
		//				HitTurret.SetFromWeaponGameObject(OwnerPawn.Inventory.CurrentWeaponGameObject);
		//				// TODO
		//				// unequip weapon
		//				// cooldown etc
		//			}
		//		}
		//	}
		//}
	}

}
