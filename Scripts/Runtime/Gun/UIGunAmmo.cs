
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace myrop.pvp
{
	[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
	public class UIGunAmmo : UdonSharpBehaviour
	{
		public TextMeshProUGUI Mag;
		public TextMeshProUGUI Reserve;

		private GunBase _gun;

		private void Start()
		{
			gameObject.SetActive(false);
		}

		public void AttachToGun(GunBase gunBase)
		{
			_gun = gunBase;
		}

		public void SetAmmo(int mag, int magCapacity, int reserve)
		{
			if (mag < 0 || magCapacity < 0)
			{
				//Infinite amount of ammo, we don't need to render anything
				Mag.text = "";
				Reserve.text = "";
			}
			else if (reserve < 0)
			{
				//Infinite amount of reserve
				Mag.text = $"<size=70>{mag}</size>/{magCapacity}";
				Reserve.text = "";
			}
			else
			{
				Mag.text = $"<size=70>{mag}</size>/{magCapacity}";
				Reserve.text = $"<size=70>{reserve}</size>";
			}
		}

		/// <summary>
		/// Placing the UI between PathStart and PathEnd
		/// </summary>
		public override void PostLateUpdate()
		{
			if (_gun == null)
			{
				return;
			}
			Vector3 pathStart = _gun.PathStart.position;
			Vector3 pathEnd = _gun.PathEnd.position;
			Vector3 headPosition = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;

			Vector3 se = pathEnd - pathStart;
			float seLenSq = se.sqrMagnitude;

			if (seLenSq < 1e-8f)
			{
				transform.position = pathStart;
				return;
			}

			// Project C onto segment, clamp to [0,1]
			float t = Vector3.Dot(headPosition - pathStart, se) / seLenSq;
			t = Mathf.Clamp01(t);

			// Closest point on the segment to C
			transform.position = pathStart + se * t;

			transform.LookAt(headPosition);
			transform.forward = -transform.forward;
		}
	}
}
