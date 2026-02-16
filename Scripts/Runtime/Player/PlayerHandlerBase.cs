

using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace myrop.pvp
{
	[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
	public class PlayerHandlerBase : UdonSharpBehaviour
	{
		public MainUIPanel MainMenuReference;
		public PvPGameManager PvPGameManagerReference;
		public GunBase Gun;
		public MeshRenderer Healthbar;

		public HitDetector[] Colliders;

		[Header("Audio")]
		public AudioManager LocalAudioManager;
		public AudioClip LocalPlayerGotHit;

		[UdonSynced]
		private float _health;

		[UdonSynced]
		private int _killCounter;

		[UdonSynced]
		private int _deathCounter;

		[UdonSynced]
		private bool _isImmunity;

		private Scoreboard _scoreboard;


		private int _playerID;
		private bool _isOwner;
		private VRCPlayerApi _owner;

		/// <summary>
		/// The time it takes someone is immune
		/// </summary>
		private const float IMMUNITY_TIME = 3.0f;

		

		private void Start()
		{
			_owner = Networking.GetOwner(gameObject);
			_isOwner = Networking.IsOwner(gameObject);
			_playerID = _owner.playerId;
			
			if (Gun != null)
				Gun.gameObject.SetActive(false);

			Healthbar.gameObject.SetActive(false);
			SetColliderEnabledState(false);
		}

		private Scoreboard GetScoreboard()
		{
			if (_scoreboard == null)
			{
				_scoreboard = PvPGameManagerReference.GetScoreboardInstance();
			}
			return _scoreboard;
		}

		public void _StartGame()
		{
			SetColliderEnabledState(true);
			
			if (Gun != null)
				Gun.gameObject.SetActive(true);

			Healthbar.gameObject.SetActive(true);

			if (!_isOwner)
				return;

			ResetHealth();
			ResetScore();
			
			_PlaceGun();

			if (Gun != null)
				Gun.ResetAmmo();
			TriggerCooldown();
			RequestSerialization();
			OnDeserialization();
		}

		/// <summary>
		/// Places a gun in front of the player, twice, delayed by two frames
		/// </summary>
		private void _PlaceGun()
		{
			//Since the gun gets placed in front of the player collider, we need to make sure that the collider gets attached to the player first before we spawn the gun
			//So delaying the event by a few frames should be good enough (Another way would be to manually move those colliders also, but I don't want to create
			//too many dependencies

			//The reason we do it twice, is because VRCObjectsSync is weird, moving a VRCObjectSynced object doesn't always work...
			SendCustomEventDelayedFrames(nameof(_PlaceGunOnce), 3);
			SendCustomEventDelayedFrames(nameof(_PlaceGunOnce), 3);
		}

		/// <summary>
		/// Places a gun in front of the player instantly
		/// If the gun is already held, then it doesn't get respawned
		/// </summary>
		public void _PlaceGunOnce()
		{
			if (Gun == null) return;

			if (!Networking.IsOwner(Gun.gameObject))
				return;

			if (Gun.PickupReference.IsHeld)
				return;

			Vector3 position = _owner.GetPosition();
			Quaternion rotation = _owner.GetTrackingData(VRCPlayerApi.TrackingDataType.AvatarRoot).rotation;

			Vector3 forward = rotation * Vector3.forward;
			Vector3 up = rotation * Vector3.up;

			Gun.transform.position = position + forward + up * (_owner.GetAvatarEyeHeightAsMeters() * 0.7f);
			Gun.transform.rotation = rotation;
		}

		public void _FinishGame()
		{
			SetColliderEnabledState(false);
			Healthbar.gameObject.SetActive(false);

			if (Gun == null) return;

			Gun.gameObject.SetActive(false);
			
			Gun._Drop();
		}

		private void ResetHealth()
		{
			_health = 100.0f;
		}

		private void ResetScore()
		{
			_killCounter = 0;
			_deathCounter = 0;
		}


		[NetworkCallable]
		public void ReceiveDamage(float damageReceived, int fromPlayerID)
		{
			PvPUtils.Log($"ReceiveDamage {damageReceived} from player ID {fromPlayerID}, IsOwner is {Networking.IsOwner(gameObject)}, immunity {_isImmunity}");

			if (!_isOwner)
				return;

			if (_isImmunity) 
				return;

			_health -= damageReceived;

			if (_health <= 0)
			{
				PvPUtils.Log($"ReceiveDamage health is now {_health}");
				RespawnLocalPlayer();

				//We need to tell "fromPlayerID" that "fromPlayerID" killed the local player
				PlayerHandlerBase otherPlayerObject = PvPGameManagerReference.GetPlayerObjectOf(fromPlayerID);
				if (otherPlayerObject != null)
				{
					otherPlayerObject.SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(KilledPlayer), Networking.GetOwner(gameObject).playerId);
				}
				_deathCounter++;
			}

			//audio
			LocalAudioManager.PlayAudio(LocalPlayerGotHit, transform.position, 1, 0.7f);

			RequestSerialization();
			OnDeserialization();
		}

		private void TriggerCooldown()
		{
			if (_isImmunity)
				return;

			PvPUtils.Log($"Cooldown triggered");

			_isImmunity = true;

			SendCustomEventDelayedSeconds(nameof(_DisableCooldown), IMMUNITY_TIME);

			//During the cooldown, we also do not want to deal damage to any other player
			DataDictionary allPlayerObject = PvPGameManagerReference.GetAllPlayerObjects();
			if (allPlayerObject != null)
			{
				DataList keys = allPlayerObject.GetKeys();
				for(int i = 0;i < keys.Count; i++)
				{
					PlayerHandlerBase otherPlayer = (PlayerHandlerBase) allPlayerObject[keys[i]].Reference;
					if (otherPlayer != null)
					{
						foreach(HitDetector hitDetector in otherPlayer.Colliders)
						{
							hitDetector.EnableSpawnDamageCooldown(IMMUNITY_TIME);
						}
					}
				}
			}

			RequestSerialization();
			OnDeserialization();
		}

		public void _DisableCooldown()
		{
			_isImmunity = false;

			PvPUtils.Log($"Cooldown end, player is not immune anymore");

			RequestSerialization();
			OnDeserialization();
		}

		private void RespawnLocalPlayer()
		{
			PvPGameManagerReference._TeleportLocalPlayerToRandom();
			_PlaceGun();
			ResetHealth();
			TriggerCooldown();

			if (Gun != null)
				Gun.ResetAmmo();
		}

		/// <summary>
		/// Called when the local player killed "playerKilled"
		/// </summary>
		/// <param name="playerKilled"></param>
		/// <exception cref="NotImplementedException"></exception>
		[NetworkCallable]
		public void KilledPlayer(int playerKilled)
		{
			PvPUtils.Log($"local player {Networking.LocalPlayer.playerId} killed player ID {playerKilled}, IsOwner={_isOwner}, _killCounter={_killCounter}");

			if (!_isOwner)
				return;

			if (playerKilled == Networking.LocalPlayer.playerId)
				return; //...let's not increment the kill counter if suicide

			_killCounter++;

			RequestSerialization();
			OnDeserialization();
		}

		public override void OnDeserialization()
		{
			Scoreboard scoreboard = GetScoreboard();
			if (scoreboard == null)
				return;
			scoreboard.UpdateInScoreboard(_playerID, _killCounter, _deathCounter, _killCounter * 2 - _deathCounter );

			ApplyColliderMaterials();

			Healthbar.material.SetFloat("_Health", _health / 100.0f);
		}

		private void ApplyColliderMaterials()
		{
			if (Colliders == null)
				return;

			for (int i = 0; i < Colliders.Length; i++)
			{
				HitDetector hitDetector = Colliders[i];
				if (hitDetector == null) 
					continue;

				hitDetector.SetSpawnInvulnerability(_isImmunity);
			}
		}

		private void SetColliderEnabledState(bool isEnabled)
		{
			if (_isOwner)
				isEnabled = false; //We do not want to show the colliders for the local player

			for (int i = 0; i < Colliders.Length; i++)
			{
				HitDetector hitDetector = Colliders[i];
				if (hitDetector == null)
					continue;

				hitDetector.gameObject.SetActive(isEnabled);
			}
		}

		public override void PostLateUpdate()
		{
			Healthbar.transform.position = _owner.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position + Vector3.up * 0.65f;
		}
	}
}
