
using Cinemachine.Utility;
using System.Threading;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Rendering;
using VRC.SDKBase;
using VRC.Udon;

namespace myrop.pvp
{
	[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
	public class Crosshair : UdonSharpBehaviour
	{
		public GameObject CrosshairObject;

		public Transform[] Branches;

		private Vector3[] _neutralBranchesPositions;

		[Tooltip("Max render distance of the crosshair, make sure to set that value below the far clipping plane of the main camera")]
		public float MaxRenderDistanceCrosshair = 100.0f;

		[Range(0f, 1f)]
		public float CrosshairSizeOnScreen = 1.0f;
		private PlayerHandlerBase _localPlayerHandler;

		void Start()
		{
			_localPlayerHandler = PvPUtils.FindPlayerHandlerOf(Networking.LocalPlayer);

			_neutralBranchesPositions = new Vector3[Branches.Length];
			for(int i = 0; i <  Branches.Length; i++)
			{
				_neutralBranchesPositions[i] = Branches[i].localPosition;
			}
		}

		private void Update()
		{
			if (_localPlayerHandler == null)
			{
				return;
			}

			GunBase gun = _localPlayerHandler.Gun;
			if (gun == null || !gun.IsHeld() || !gun.ShowCrosshair())
			{
				CrosshairObject.SetActive(false);
				return;
			}
			CrosshairObject.SetActive(true);


			float crosshairDistanceFromPlayer = 0.0f;
			//Rendering the crosshair in the scene
			if (Physics.Raycast(gun.Barrel.position, gun.Barrel.forward, out RaycastHit hit, MaxRenderDistanceCrosshair, GunBase.HIT_LAYER_MASK))
			{
				CrosshairObject.transform.position = hit.point;
				crosshairDistanceFromPlayer = hit.distance;
			}
			else
			{
				CrosshairObject.transform.position = gun.Barrel.position + gun.Barrel.forward * MaxRenderDistanceCrosshair;
				crosshairDistanceFromPlayer = MaxRenderDistanceCrosshair;
			}

			//Precision
			float speadAngle = gun.GetCurrentSpread();
			float crossHairPrecision = crosshairDistanceFromPlayer * Mathf.Tan(0.5f * speadAngle * Mathf.Deg2Rad);
			float crossHairSize = CrosshairSizeOnScreen * crosshairDistanceFromPlayer;

			CrosshairObject.transform.localScale = Vector3.one * crossHairSize;
			CrosshairObject.transform.LookAt(Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position);	

			if (crossHairSize != 0)
			{
				for (int i = 0; i < Branches.Length; i++)
				{
					Branches[i].localPosition =
						_neutralBranchesPositions[i] +
						(_neutralBranchesPositions[i].normalized * crossHairPrecision / crossHairSize);
				}
			}
		}
	}
}
