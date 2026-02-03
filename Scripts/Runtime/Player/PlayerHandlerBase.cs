

using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.Jobs;
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

		[UdonSynced]
		private float _health;

		[UdonSynced]
		private int _killCounter;

		[UdonSynced]
		private int _deathCounter;

		private Scoreboard _scoreboard;
		private int _playerID;

		private void Start()
		{
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
		public override void OnDeserialization()
		{
			Scoreboard scoreboard = GetScoreboard();
			if (scoreboard == null )
				return;
			scoreboard.UpdateInScoreboard(_playerID, _killCounter, _deathCounter, _killCounter - (short)(_deathCounter / 2.0f));
		}

		public void _StartGame()
		{
			PlayerColliderAttacherReference.gameObject.SetActive(true);
			ResetHealth();
			_deathCounter = 0;
			//Since the gun gets placed in front of the player collider, we need to make sure that the collider gets attached to the player first before we spawn the gun
			//So delaying the event by one or two frames should be good enough
			_PlaceGun();

			RequestSerialization();
			OnDeserialization();
		}

		/// <summary>
		/// Places a gun in front of the player, twice, delayed by two frames
		/// </summary>
		private void _PlaceGun()
		{
			//The reason we do it twice, is because VRCObjectsSync is weird, moving a VRCObjectSynced object doesn't always work...
			SendCustomEventDelayedFrames(nameof(_PlaceGunOnce), 2);
			SendCustomEventDelayedFrames(nameof(_PlaceGunOnce), 2);
		}

		/// <summary>
		/// Places a gun in front of the player instantly
		/// If the gun is already held, then it doesn't get respawned
		/// </summary>
		public void _PlaceGunOnce()
		{
			if (Gun == null) return;

			Gun.gameObject.SetActive(true);

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
			PvPUtils.Log($"ReceiveDamage {damageReceived} from player ID {fromPlayerID}, IsOwner is {Networking.IsOwner(gameObject)}");

			if (!Networking.IsOwner(gameObject))
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

			RequestSerialization();
			OnDeserialization();
		}

		private void RespawnLocalPlayer()
		{
			PvPGameManagerReference._TeleportLocalPlayerToRandom();
			_PlaceGun();
			ResetHealth();			
		}

		/// <summary>
		/// Called when the local player killed "playerKilled"
		/// </summary>
		/// <param name="playerKilled"></param>
		/// <exception cref="NotImplementedException"></exception>
		[NetworkCallable]
		public void KilledPlayer(int playerKilled)
		{
			PvPUtils.Log($"local player {Networking.LocalPlayer.playerId} killed player ID {playerKilled}, IsOwner={Networking.IsOwner(gameObject)}, _killCounter={_killCounter}");

			if (!Networking.IsOwner(gameObject))
				return;

			if (playerKilled == Networking.LocalPlayer.playerId)
				return; //...let's not increment the kill counter if suicide

			_killCounter++;

			RequestSerialization();
			OnDeserialization();
		}
	}
}
