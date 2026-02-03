
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

namespace myrop.pvp
{
	/// <summary>
	/// A simple script that disables the pickup if the player is not the owner of it
	/// </summary>
	[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
	public class DisablePickupForNonOwner : UdonSharpBehaviour
	{
		public VRCPickup PickupReference;

		void Start()
		{
			_Apply();
		}

		private void _Apply()
		{
			PickupReference.pickupable = Networking.GetOwner(PickupReference.gameObject) == Networking.LocalPlayer;
		}

		public override void OnOwnershipTransferred(VRCPlayerApi player)
		{
			_Apply();
		}
	}
}
