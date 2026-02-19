
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace myrop.pvp
{
	public enum EGunMode
	{
		SemiAuto,
		Automatic
	}

	public enum ECrosshair
	{
		No,
		DesktopVR,
		Desktop,
		VR
	}

	[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
	public class GunBase : UdonSharpBehaviour
	{
		[Header("References")]
		public PvPGameManager GameManager;
		public GunNetworked GunNetworkedReference;
		public VRCPickup PickupReference;		
		public Transform Barrel;
		public UIGunAmmo AmmoDisplay;
		public HitMarkerManager HitMarkerManager;
		public ParticleSystem MuzzleFlash;

		[Header("Gun settings")]
		public EGunMode GunMode;
		[Range(0, 20)]
		public int FireRatePerSecond;
		public float MaxRange = 1000.0f;
		public float Damage = 100.0f;
		[Header("Ammo (-1 means infinity)")]
		public int MaxAmmoInMag = -1;
		public int MaxReserveAmmo = -1;
		[Range(0, 10)]
		public float ReloadTime = 2.0f;

		[Header("Accuracy")]
		[SerializeField]
		[Range(0, 45)]
		private float _minSpreadDeg = 0.0f;
		[SerializeField]
		[Range(0, 45)]
		private float _maxSpreadDeg = 0.0f;

		[Range(0, 90)]
		public float SpreadRecoverySpeedWhileShooting = 1.0f;
		[Range(0, 5)]
		public float SpreadRecoverySpeedWhileIdle = 1.0f;
		[Range(0, 90)]
		public float SpreadLossPerShot = 0.0f;
		private float _currentAngleSpread = 0.0f;
		private float _currentRecoveryCoefficient = 0.0f;

		[Header("Recoil animation")]
		public Transform GunMesh;
		public Transform GunblowbackOrigin;
		public Transform GunblowbackMax;
		private Vector3 _defaultGunPosition;

		public ECrosshair Crosshair = ECrosshair.DesktopVR;

		

		[Header("Audio")]
		public AudioManager LocalAudioManager;
		public AudioManager RemoteAudioManager;
		public AudioClip ShotClose;
		public AudioClip ShotFar;
		public AudioClip PlayerHit;
		public AudioClip Reload;
		public float VolumeLocalShooting = 0.7f;
		public float VolumeLocalHit = 1;
		public float VolumeRemoteShooting = 1;

		[Header("Defines where the ammo UI should be placed")]
		public Transform PathStart;
		public Transform PathEnd;

		private bool _isTriggerPressed;
		private bool _isReloading;
		private bool _isWaitingForNextBullet; //After a bullet was shot, we need to wait until the next bullet can be shot

		public const int HIT_LAYER_MASK = 1 << 0; //Default

		private bool _isLooping = false;

		private float _lastShotTime;
		private Smoothing _smoothingModule;

		public ImpactDebug ImpactDebugReference;

		private void Start()
		{
			_defaultGunPosition = GunMesh.transform.localPosition;
			if (Networking.LocalPlayer.IsUserInVR())
			{
				PickupReference.orientation = VRC_Pickup.PickupOrientation.Any;
				PickupReference.ExactGrip = null;
				PickupReference.ExactGun = null;
			}
		}


		public void RegisterSmoothingModule(Smoothing smoothing)
		{
			_smoothingModule = smoothing;
		}

		public void ResetAmmo()
		{
			GunNetworkedReference.CurrentAmmo = MaxAmmoInMag;
			GunNetworkedReference.ReserveAmmo = MaxReserveAmmo;
		}

		public override void OnPickup()
		{
			_isTriggerPressed = false;

			if (AmmoDisplay != null)
			{
				AmmoDisplay.gameObject.SetActive(true);
				AmmoDisplay.AttachToGun(this);
				GunNetworkedReference.RefreshUI();
			}

			if (!_isLooping)
			{
				_isLooping = true;
				_CustomLoop();
			}
			
			if (_smoothingModule != null)
			{
				//We only enable smoothing in VR, so Desktop players can flickshot more easily 
				_smoothingModule.enabled = Networking.LocalPlayer.IsUserInVR();
			}
		}

		public void SetMinSpreadDeg(float deg)
		{
			_minSpreadDeg = Mathf.Clamp(deg, 0.0f, 45.0f);
		}

		public void SetMaxSpreadDeg(float deg)
		{
			_maxSpreadDeg = Mathf.Clamp(deg, 0.0f, 45.0f);
		}

		public override void OnDrop()
		{
			_isTriggerPressed = false;

			if (AmmoDisplay != null)
			{
				AmmoDisplay.gameObject.SetActive(false);
			}

			if (_smoothingModule != null)
			{
				_smoothingModule.enabled = false;
			}
		}

		public override void OnPickupUseDown()
		{
			_isTriggerPressed = true;
			_lastShotTime = Time.time;
			_AttemptToShoot();
		}

		public float GetCurrentSpread()
		{
			return _currentAngleSpread;
		}

		public float GetCurrentSpreadNormalized()
		{
			return _currentAngleSpread;
		}

		public bool IsHeld()
		{
			return PickupReference.IsHeld;
		}

		public override void OnPickupUseUp()
		{
			_isTriggerPressed = false;
		}

		public void _Drop()
		{
			if (PickupReference.IsHeld)
				PickupReference.Drop();
		}

		public void _ChamberNextBullet()
		{
			_isWaitingForNextBullet = false;

			if (_isTriggerPressed && GunMode == EGunMode.Automatic) //We don't want to shoot the next bullet immediately in semi auto mode
			{
				_AttemptToShoot();
			}
		}

		public void _FinishedReloading()
		{
			_isReloading = false;

			int ammoMissing = MaxAmmoInMag - GunNetworkedReference.CurrentAmmo;
			int ammoToLoad = Mathf.Min(ammoMissing, GunNetworkedReference.ReserveAmmo < 0 ? ammoMissing : GunNetworkedReference.ReserveAmmo);

			GunNetworkedReference.CurrentAmmo += ammoToLoad;

			if (GunNetworkedReference.ReserveAmmo >= 0)
				GunNetworkedReference.ReserveAmmo -= ammoToLoad;
		}

		/// <summary>
		/// Attempting to shoot, the shot is taken if a bullet is chambered or the gun is not reloading
		/// </summary>
		private void _AttemptToShoot()
		{
			PvPUtils.Log($"_AttemptToShoot : Local player attempts to shoot, _isWaitingForNextBullet={_isWaitingForNextBullet}");

			if (_isWaitingForNextBullet) //The next bullet isn't chambered yet, we need to wait a bit
				return;

			if (GunNetworkedReference.CurrentAmmo == 0 ||  _isReloading)  //We don't shoot if no ammo or reloading
				return;

			_isWaitingForNextBullet = true;

			_Shoot();
			_currentRecoveryCoefficient = 0.0f;
			float nextShotTime = _lastShotTime + 1.0f / FireRatePerSecond;

			SendCustomEventDelayedSeconds(nameof(_ChamberNextBullet), nextShotTime - Time.time);

			_lastShotTime = nextShotTime;

			if (GunNetworkedReference.CurrentAmmo > 0)
			{
				GunNetworkedReference.CurrentAmmo--;
			}
		}

		private Vector3 GetSpreadDirection(Quaternion barrelRot, float angleDeg)
		{
			Vector2 random = Random.insideUnitCircle * Mathf.Tan(angleDeg * 0.5f * Mathf.Deg2Rad);

			Vector3 forward = barrelRot * Vector3.forward;
			Vector3 right = barrelRot * Vector3.right;
			Vector3 up = barrelRot * Vector3.up;

			Vector3 dir = forward
						+ right * random.x
						+ up * random.y;

			return dir.normalized;
		}

		private void _Shoot()
		{
			PvPUtils.Log($"_Shoot : Local player shoots, _currentAngleSpread={_currentAngleSpread}");

			if (Physics.Raycast(GetRaycastStart(), GetSpreadDirection(GetRaycastRotation(), _currentAngleSpread), out RaycastHit hit, MaxRange, HIT_LAYER_MASK))
			{
				if (hit.collider == null || hit.collider.gameObject == null)
				{
					//This can happen if we hit a blacklisted object
					OnShotMissed(hit);
					return;
				}
				
				if (ImpactDebugReference != null)
				{
					ImpactDebugReference.Place(hit.point);
				}

				HitDetector hitDetector = hit.collider.gameObject.GetComponent<HitDetector>();

				if (hitDetector != null)
				{
					PvPUtils.Log($"_Shoot : Local player hit a player");

					if (!hitDetector.IsInvulnerable())
					{
						VRCPlayerApi otherPlayer = Networking.GetOwner(hitDetector.gameObject);
						OnShotHit(hit, otherPlayer, hitDetector.DamageMultiplicator);

						//damage
						float damage = Damage * hitDetector.DamageMultiplicator;
						hitDetector.PlayerHandler.SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(PlayerHandlerBase.ReceiveDamage), damage, Networking.LocalPlayer.playerId);

						//hit, we slightly delay the event to ensure it doesn't play at the same time as the gunshot, it would sound weird
						SendCustomEventDelayedSeconds(nameof(_LocalPlayerHitEnemy), 0.05f);

						if (HitMarkerManager != null)
							HitMarkerManager.Play(hit.point);
					}
				}
				else
				{
					OnShotMissed(hit);
				}
			}

			//accuracy
			_currentAngleSpread += SpreadLossPerShot;
			_currentAngleSpread = Mathf.Clamp(_currentAngleSpread, _minSpreadDeg, _maxSpreadDeg);

			//audio
			LocalAudioManager.PlayAudio(ShotClose, transform.position, 2, VolumeLocalHit);

			//muzzleflash
			MuzzleFlash.Stop();
			MuzzleFlash.Play();

			if (GunNetworkedReference != null)
				GunNetworkedReference._QueueRemotePlayerShotEvent();
		}

		public void _LocalPlayerHitEnemy()
		{
			LocalAudioManager.PlayAudio(PlayerHit, transform.position, 0, VolumeLocalHit);
		}

		private bool IsAimWithHead()
		{
			return GameManager.DesktopAimWithHead && !Networking.LocalPlayer.IsUserInVR();
		}

		public Vector3 GetRaycastStart()
		{
			if (IsAimWithHead())
				return Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
			else
				return Barrel.position;
		}

		public Quaternion GetRaycastRotation()
		{
			if (IsAimWithHead())
				return Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
			else
				return Barrel.rotation;
		}

		public Vector3 GetRaycastDirection()
		{
			if (IsAimWithHead())
				return Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation * Vector3.forward;
			else
				return Barrel.forward;
		}


		public void _CustomLoop()
		{
			if (!_isTriggerPressed)
			{
				// If the trigger is not pressed, the spread recovers with an increasing recovery rate
				_currentRecoveryCoefficient += SpreadRecoverySpeedWhileShooting * SpreadRecoverySpeedWhileIdle;
			}
			else
			{
				// If the trigger is pressed, the spread decreases slowly
				_currentRecoveryCoefficient = SpreadRecoverySpeedWhileShooting;
			}
			_currentAngleSpread -= Time.deltaTime * _currentRecoveryCoefficient;
			_currentAngleSpread = Mathf.Clamp(_currentAngleSpread, _minSpreadDeg, _maxSpreadDeg);

			//Gun blowback
			Vector3 offset = GunblowbackOrigin.localPosition - _defaultGunPosition;
			float lerp = Mathf.InverseLerp(_minSpreadDeg, _maxSpreadDeg, _currentAngleSpread);
			GunMesh.localPosition = Vector3.Lerp(GunblowbackOrigin.localPosition
				, GunblowbackMax.localPosition
				, lerp) - offset;
			GunMesh.localRotation = Quaternion.Lerp(GunblowbackOrigin.localRotation
				, GunblowbackMax.localRotation
				, lerp);

			//reloading
			if (!_isReloading && !_isTriggerPressed && MaxAmmoInMag > 0)
			{
				float dot = Vector3.Dot(transform.forward, Vector3.down);

				bool isPointingDown = dot > 0.8f;

				if (isPointingDown && GunNetworkedReference.CurrentAmmo != MaxAmmoInMag && GunNetworkedReference.ReserveAmmo != 0)
				{
					_isReloading = true;
					SendCustomEventDelayedSeconds(nameof(_FinishedReloading), ReloadTime);

					//audio
					LocalAudioManager.PlayAudio(Reload, transform.position, 1, VolumeLocalHit);
				}
			}

			if (_currentAngleSpread == 0 && !PickupReference.IsHeld)
			{
				_isLooping = false;
			}
			else
			{
				SendCustomEventDelayedFrames(nameof(_CustomLoop), 1);
			}
		}

		public bool ShowCrosshair()
		{
			if (Crosshair == ECrosshair.No)
				return false;

			if (Crosshair == ECrosshair.DesktopVR)
				return true;

			bool isVR = Networking.LocalPlayer.IsUserInVR();
			return Crosshair == ECrosshair.VR
				? isVR
				: !isVR;
		}

		public virtual void OnStart() { }
		public virtual void OnShotMissed(RaycastHit hit) { }

		public virtual void OnShotHit(RaycastHit hit, VRCPlayerApi playerHitted, float damageMultiplicator) { }
	}
}
