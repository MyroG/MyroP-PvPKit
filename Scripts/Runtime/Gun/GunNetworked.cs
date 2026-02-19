
using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace myrop.pvp
{
	[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
	public class GunNetworked : UdonSharpBehaviour
	{
		public GunBase Gun;

		[UdonSynced]
		[HideInInspector]
		private int _currentAmmo;
		public int CurrentAmmo
		{
			get => _currentAmmo;
			set
			{
				_currentAmmo = value;
				RequestSerialization();
				RefreshUI();
			}
		}

		[UdonSynced]
		[HideInInspector]
		private int _reserveAmmo;
		public int ReserveAmmo
		{
			get => _reserveAmmo;
			set
			{
				_reserveAmmo = value;
				RequestSerialization();
				RefreshUI();
			}
		}

		public void _QueueRemotePlayerShotEvent()
		{
			if (NetworkCalling.GetQueuedEvents((IUdonEventReceiver)this, nameof(RemotePlayerShot)) == 0) //To prevent events getting queued
				SendCustomNetworkEvent(NetworkEventTarget.Others, nameof(RemotePlayerShot));
		}

		[NetworkCallable(maxEventsPerSecond: 5)]
		public void RemotePlayerShot()
		{
			int id = Networking.GetOwner(gameObject).playerId;
			Random.InitState(id);
			float dist = Vector3.Distance(Networking.LocalPlayer.GetPosition(), transform.position);
			Gun.RemoteAudioManager.PlayAudio(dist < Gun.RemoteAudioManager.MidDist ? Gun.ShotClose : Gun.ShotFar, transform.position, 5, Gun.VolumeRemoteShooting, Random.Range(-0.2f, 0.2f));

			//muzzle flash
			Gun.MuzzleFlash.Stop();
			Gun.MuzzleFlash.Play();
		}

		public void RefreshUI()
		{
			if (Gun.AmmoDisplay != null)
			{
				Gun.AmmoDisplay.SetAmmo(_currentAmmo, Gun.MaxAmmoInMag, _reserveAmmo);
			}
		}
	}
}
