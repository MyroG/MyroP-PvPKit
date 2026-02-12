
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace myrop.pvp
{
	[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
	public class PlayerScaleEnforcer : UdonSharpBehaviour
	{
		public PvPGameManager GameManager;

		[Header("Avatar height settings")]
		public float MinHeightInMeters = 1.6f;
		public float MaxHeightInMeters = 1.8f;


		private float _savedEyeHeight;

		private void Start()
		{
			GameManager.SetPlayerScaleEnforcerInstance(this);
		}

		public override void OnPlayerJoined(VRCPlayerApi player)
		{
			if (!player.isLocal) return;

			_savedEyeHeight = player.GetAvatarEyeHeightAsMeters();
		}

		public void _ApplyLocalPlayerScale()
		{
			VRCPlayerApi player = Networking.LocalPlayer;

			float currentEyeHeight = player.GetAvatarEyeHeightAsMeters();
			float clampedScale = Mathf.Clamp(currentEyeHeight, MinHeightInMeters, MaxHeightInMeters);

			if (currentEyeHeight != clampedScale)
			{
				player.SetAvatarEyeHeightByMeters(clampedScale);
			}
		}

		public override void OnAvatarEyeHeightChanged(VRCPlayerApi player, float prevEyeHeightAsMeters)
		{
			if (!player.isLocal) return;

			if (GameManager.IsGameStartedAndLocalPlayerIngame())
			{
				_ApplyLocalPlayerScale();
			}
			else
			{
				_savedEyeHeight = player.GetAvatarEyeHeightAsMeters();
			}			
		}

		public override void OnAvatarChanged(VRCPlayerApi player)
		{
			if (!player.isLocal) return;

			_savedEyeHeight = player.GetAvatarEyeHeightAsMeters();

			if (GameManager.IsGameStartedAndLocalPlayerIngame())
			{
				_ApplyLocalPlayerScale();
			}
		}

		public void _RevertAvatarScale()
		{
			Networking.LocalPlayer.SetAvatarEyeHeightByMeters(_savedEyeHeight);
		}
	}
}
