
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;


namespace myrop.pvp
{
	[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
	public class PlayerColliderAttacher : UdonSharpBehaviour
	{
		public CapsuleCollider BodyCollider;
		public SphereCollider HeadCollider;
		public GameObject Capsule;

		public PlayerHandlerBase PlayerHandlerInstance;
		private VRCPlayerApi _player;

		void Start()
		{
			_player = Networking.GetOwner(gameObject);

			ApplyColliderScale();
		}

		public override void OnAvatarChanged(VRCPlayerApi player)
		{
			if (_player == null || player.playerId != _player.playerId)
				return;

			ApplyColliderScale();
		}

		public override void OnAvatarEyeHeightChanged(VRCPlayerApi player, float prevEyeHeightAsMeters)
		{
			if (_player == null || player.playerId != _player.playerId)
				return;

			ApplyColliderScale();
		}

		private void ApplyColliderScale()
		{
			float avatarSize = PlayerHandlerInstance.RescaleColliderWithAvatar ? _player.GetAvatarEyeHeightAsMeters() : PlayerHandlerInstance.ColliderHeight;

			float bodySize = avatarSize * 0.8f;
			float headRadius = (avatarSize - bodySize) / 2.0f;

			BodyCollider.center = new Vector3(
				BodyCollider.center.x,
				bodySize / 2.0f,
				BodyCollider.center.z
			);

			BodyCollider.height = bodySize;
			BodyCollider.radius = 0.19f * bodySize;

			HeadCollider.center = new Vector3(
				HeadCollider.center.x,
				bodySize + headRadius,
				HeadCollider.center.z
			);
			HeadCollider.radius = headRadius;

			Capsule.transform.localScale = new Vector3(
				.5f * avatarSize * 0.4f,
				.5f * avatarSize,
				.5f * avatarSize * 0.4f
			);
		}

		public override void PostLateUpdate()
		{
			if (_player == null)
				return;

			transform.position = _player.GetPosition();
		}
	}
}
