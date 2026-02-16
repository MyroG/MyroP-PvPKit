
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using static VRC.SDKBase.VRCPlayerApi;

namespace myrop.pvp
{
	[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
	public class HitDetector : UdonSharpBehaviour
	{
		public PlayerHandlerBase PlayerHandler;
		public TrackingDataType AttachTo;
		public Vector3 Offset;
		public float ScaleMultiplicator = 1f;
		public float DamageMultiplicator = 1.0f;
		public bool VisibleMesh;
		public Material ImmunityMaterial;
		public Material NormalMaterial;

		private VRCPlayerApi _player;
		private MeshRenderer _meshRenderer;
		private Vector3 _defaultScale;

		private bool _isSpawnInvulnerable;    // cannot receive damage, this is a synced variable by the local player (synced in another class)
		private bool _isSpawnDamageDisabled;    // cannot deal damage, to prevent spawn kill
		private int _numberQueuedEvents;

		private void Start()
		{
			_defaultScale = transform.localScale;
			_meshRenderer = GetComponent<MeshRenderer>();
			if (!VisibleMesh && _meshRenderer != null)
				_meshRenderer.enabled = false;
			_player = Networking.GetOwner(PlayerHandler.gameObject);
		}

		public override void PostLateUpdate()
		{
			transform.position = _player.GetTrackingData(AttachTo).position + Offset * ScaleMultiplicator;
			transform.rotation = _player.GetTrackingData(AttachTo).rotation;
			transform.localScale = _defaultScale * ScaleMultiplicator * _player.GetAvatarEyeHeightAsMeters();
		}

		public void SetSpawnInvulnerability(bool spawnInvulnerability)
		{
			_isSpawnInvulnerable = spawnInvulnerability;
			_SetImmunityMaterial();
		}

		public void EnableSpawnDamageCooldown(float cooldownTime)
		{
			_isSpawnDamageDisabled = true;
			_numberQueuedEvents++;
			_SetImmunityMaterial();

			SendCustomEventDelayedSeconds(nameof(_DisableSpawnDamageCooldown), cooldownTime);
		}

		public void _DisableSpawnDamageCooldown()
		{
			_numberQueuedEvents--;
			if (_numberQueuedEvents != 0)
				return;
			_isSpawnDamageDisabled = false;
			_SetImmunityMaterial();
		}

		public void _SetImmunityMaterial()
		{
			bool isImmune = _isSpawnInvulnerable && _isSpawnDamageDisabled;
			if (_meshRenderer != null && VisibleMesh)
			{
				_meshRenderer.material = isImmune ? ImmunityMaterial : NormalMaterial;
			}
		}
	}
}
