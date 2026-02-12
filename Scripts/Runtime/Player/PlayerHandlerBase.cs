

using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace myrop.pvp
{
	[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
	public class PlayerHandlerBase : UdonSharpBehaviour
	{
		public bool RescaleColliderWithAvatar = false;
		public float ColliderHeight = 1.6f;

		public MainUIPanel MainMenuReference;
		public PvPGameManager PvPGameManagerReference;
		public GunBase Gun;
		public PlayerColliderAttacher PlayerColliderAttacherReference;

		public MeshRenderer CapsuleRenderer;
		public Material ImmunityMaterial;
		public Material NormalMaterial;
		public MeshRenderer Healthbar;

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

		private const float IMMUNITY_TIME = 3.0f;

		

		private void Start()
		{
			_isOwner = Networking.IsOwner(gameObject);
			_playerID = Networking.GetOwner(gameObject).playerId;
			Gun.gameObject.SetActive(false);
			PlayerColliderAttacherReference.gameObject.SetActive(false);
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
			PlayerColliderAttacherReference.gameObject.SetActive(true);
			CapsuleRenderer.gameObject.SetActive(PvPGameManagerReference.ShowPlayerCapsule && !_isOwner);
			
			if (Gun != null)
				Gun.gameObject.SetActive(true);
			
			if (!_isOwner)
				return;

			ResetHealth();
			_deathCounter = 0;
			
			_PlaceGun();
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

			Transform player = PlayerColliderAttacherReference.transform;
			
			Gun.transform.position = player.position + player.forward + player.up * ColliderHeight / 2.0f;
			Gun.transform.rotation = player.rotation;
		}

		public void _FinishGame()
		{
			PlayerColliderAttacherReference.gameObject.SetActive(false);

			if (Gun == null) return;

			Gun.gameObject.SetActive(false);
			Gun._Drop();
		}

		private void ResetHealth()
		{
			_health = 100.0f;
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
			LocalAudioManager.PlayAudio(LocalPlayerGotHit, transform.position, ShotImportance.LocalPlayer, 0.7f);

			RequestSerialization();
			OnDeserialization();
		}

		private void TriggerCooldown()
		{
			if (_isImmunity)
				return;

			_isImmunity = true;

			SendCustomEventDelayedSeconds(nameof(_DisableCooldown), IMMUNITY_TIME);

			RequestSerialization();
			OnDeserialization();
		}

		public void _DisableCooldown()
		{
			_isImmunity = false;

			RequestSerialization();
			OnDeserialization();
		}

		private void RespawnLocalPlayer()
		{
			PvPGameManagerReference._TeleportLocalPlayerToRandom();
			_PlaceGun();
			ResetHealth();
			TriggerCooldown();
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
			scoreboard.UpdateInScoreboard(_playerID, _killCounter, _deathCounter, _killCounter - (short)(_deathCounter / 2.0f));

			CapsuleRenderer.material = _isImmunity ? ImmunityMaterial : NormalMaterial;

			Healthbar.material.SetFloat("_Health", _health / 100.0f);
		}
	}
}
