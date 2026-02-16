
using myrop.pvp;
using System;
using System.Linq;
using System.Reflection;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using static UnityEngine.UI.CanvasScaler;

namespace myrop.pvp
{
	[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
	public class PvPGameManager : UdonSharpBehaviour
	{
		public Transform[] SpawnPoints;

		public Transform RespawnPoint;

		public int RoundLengthInSeconds;

		public bool ShowPlayerCapsule = true;

		[UdonSynced] 
		public bool LockedByOwner = true;

		[UdonSynced]
		private double _startNetworkingTime;

		[UdonSynced]
		private bool _isGameStarted;
		private bool _clearPlayerListNext;
		private bool _gameStartedSaved;

		[UdonSynced]
		private short[] _joinedPlayers;

		private const int START_TIME_DELAY = 5;

		public MainUIPanel MainUIPanelReference;


		/// <summary>
		/// Player object of the locasl player only
		/// </summary>
		private PlayerHandlerBase _localPlayerObject;

		/// <summary>
		/// Player objects of each player (player ID -> Player Object), technically this could be a DataList, but this makes it easier to access Player object using the player ID
		/// </summary>
		private DataDictionary/*<int, PlayerHandlerBase>*/ _playerObjects;

		private Scoreboard _scoreboardInstance;
		private PlayerScaleEnforcer _playerScaleEnforcerInstance;

		void Start()
		{
			_localPlayerObject = PvPUtils.FindPlayerHandlerOf(Networking.LocalPlayer);
			_joinedPlayers = new short[0];
		}


		public void SetPlayerScaleEnforcerInstance(PlayerScaleEnforcer playerScaleEnforcer)
		{
			_playerScaleEnforcerInstance = playerScaleEnforcer;
		}

		public void SetScoreboardInstance(Scoreboard scoreboardInstance)
		{
			_scoreboardInstance = scoreboardInstance;
		}

		public Scoreboard GetScoreboardInstance()
		{
			return _scoreboardInstance;
		}

		public PlayerHandlerBase GetPlayerObjectOfLocalPlayer()
		{
			return _localPlayerObject;
		}

		public DataDictionary GetAllPlayerObjects()
		{
			return _playerObjects;
		}

		public PlayerHandlerBase GetPlayerObjectOf(int fromPlayerID)
		{
			if (_playerObjects == null)
				return null;

			if (!_playerObjects.ContainsKey(fromPlayerID))
				return null;

			return (PlayerHandlerBase) _playerObjects[fromPlayerID].Reference;
		}


		public override void OnPlayerRestored(VRCPlayerApi player)
		{
			if (_playerObjects == null)
				_playerObjects = new DataDictionary();

			_playerObjects[player.playerId] = PvPUtils.FindPlayerHandlerOf(player);
		}

		public override void OnPlayerLeft(VRCPlayerApi player)
		{
			if (_playerObjects == null)
				return;

			_playerObjects.Remove(player.playerId);
		}


		/// <summary>
		/// Called when Start is pressed
		/// </summary>
		[NetworkCallable(maxEventsPerSecond: 1)]
		public void OnStartGameClicked()
		{
			PvPUtils.Log($"Start button got clicked...");

			if (_isGameStarted)
			{
				PvPUtils.LogWarning($"Game already started, we do nothing");
				return; //in case the reset button is pressed during the start delay
			}

			if (_joinedPlayers.Length == 0)
			{
				PvPUtils.LogWarning($"0 players, we do nothing");
				return;
			}

			if (!Networking.IsOwner(gameObject))
				return;  //Only the owner can start

			_startNetworkingTime = Networking.GetServerTimeInSeconds() + START_TIME_DELAY;
			_isGameStarted = true;

			RequestSerialization();
			OnDeserialization();
		}

		[NetworkCallable(maxEventsPerSecond: 1)]
		public void OnResetGameClicked()
		{
			if (!Networking.IsOwner(gameObject))
				return;   //I think it makes sense if only the owner can reset the game

			_isGameStarted = false;
			_clearPlayerListNext = true;

			RequestSerialization();
			OnDeserialization();
		}

		public void _GameStarted()
		{
			PvPUtils.Log($"Attempting to start the game...");

			if (!_isGameStarted || _playerObjects == null)
			{
				PvPUtils.LogWarning($"Game already started, we do nothing");
				return; //in case the reset button is pressed during the start delay
			}

			if (_joinedPlayers.Length > SpawnPoints.Length)
			{
				PvPUtils.LogError($"Too many joined players, game cannot be started");
				return; //we cannot start the game if there are more players than spawn points, this should never happen, but it's an extra security
			}

			_scoreboardInstance.Init(_joinedPlayers);

			for (int i = 0; i < _joinedPlayers.Length; i++)
			{
				int playerId = _joinedPlayers[i];
				VRCPlayerApi player = VRCPlayerApi.GetPlayerById(playerId);
				if (!Utilities.IsValid(player))
					continue;

				PlayerHandlerBase po = (PlayerHandlerBase)_playerObjects[playerId].Reference;
				if (!Utilities.IsValid(po))
					continue;

				po._StartGame();

				if (player.isLocal)
				{
					player.TeleportTo(SpawnPoints[i].position, SpawnPoints[i].rotation);
					if (_playerScaleEnforcerInstance != null)
					{
						_playerScaleEnforcerInstance._ApplyLocalPlayerScale();
					}
				}
			}
		}

		public void _TeleportLocalPlayerToRandom()
		{
			PvPUtils.Log("Teleporting the local player to a random location...");
			if ( SpawnPoints.Length ==0)
            {
				return;
            }
            Transform spawn = SpawnPoints[UnityEngine.Random.Range(0, SpawnPoints.Length)];
			Networking.LocalPlayer.TeleportTo(spawn.position, spawn.rotation);
		}

		public void _ResetGame()
		{
			PvPUtils.Log($"Attempting to reset the game...");

			if (_isGameStarted || _playerObjects == null)
			{
				PvPUtils.LogWarning($"Game already stopped, we do nothing");
				return; //in case the reset button is pressed during the start delay
			}

			for (int i = 0; i < _joinedPlayers.Length; i++)
			{
				int playerId = _joinedPlayers[i];
				VRCPlayerApi player = VRCPlayerApi.GetPlayerById(playerId);
				if (!Utilities.IsValid(player))
					continue;

				PlayerHandlerBase po = (PlayerHandlerBase)_playerObjects[playerId].Reference;
				if (!Utilities.IsValid(po))
					continue;

				po._FinishGame();

				if (player.isLocal)
				{
					player.TeleportTo(RespawnPoint.position, RespawnPoint.rotation);
					if (_playerScaleEnforcerInstance != null)
					{
						_playerScaleEnforcerInstance._RevertAvatarScale();
					}
				}
			}
		}

		public override void OnPostSerialization(SerializationResult result)
		{
			if (!_clearPlayerListNext)
				return;
			PvPUtils.Log($"Clearing player list...");

			//We need to :
			//- First sync _isGameStarted = false;
			//- Then sync  _joinedPlayers = []
			//Otherwise, remote players will get _isGameStarted = false and  _joinedPlayers = [] at the same time, meaning that their game won't reset
			//Since they are already removed from the joined player list
			//I am setting _joinedPlayers = new short[0]; in OnPostSerialization to ensure it gets synced at a separate networking tick
			_clearPlayerListNext = false;
			_joinedPlayers = new short[0];

			RequestSerialization();
			OnDeserialization();
		}

		public override void OnOwnershipTransferred(VRCPlayerApi player)
		{
			MainUIPanelReference._RefreshUI();
		}

		public override void OnDeserialization()
		{
			MainUIPanelReference._RefreshUI();

			if (_isGameStarted == _gameStartedSaved)
			{
				PvPUtils.Log($"In OnDeserialization, no changes detected, leaving...");
				return;
			}

			_gameStartedSaved = _isGameStarted;

			if (_isGameStarted)
			{
				PvPUtils.Log($"Starting in {_startNetworkingTime - Networking.GetServerTimeInSeconds()} seconds...");

				SendCustomEventDelayedSeconds(nameof(_GameStarted), (float)(_startNetworkingTime - Networking.GetServerTimeInSeconds()));
			}
			else
			{
				_ResetGame();
			}
		}

		[NetworkCallable(maxEventsPerSecond: 5)]
		public void PlayerRequestedToJoin(int playerId)
		{
			PvPUtils.Log($"Player {playerId} requested to join");

			if (!Networking.IsOwner(gameObject) || _isGameStarted)
				return;

			if (_joinedPlayers.Length >= SpawnPoints.Length)
			{
				PvPUtils.LogError($"Player {playerId} couldn't join : Too many registed players ({SpawnPoints.Length})");
				return;
			}

			InsertPlayer((short) playerId);
		}

		[NetworkCallable(maxEventsPerSecond: 1)]
		public void PlayerRequestedToLeave(int playerId)
		{
			PvPUtils.Log($"Player {playerId} requested to leave");

			if (!Networking.IsOwner(gameObject) || _isGameStarted)
				return;

			RemovePlayer(playerId);
		}

		private void InsertPlayer(short playerId)
		{
			if (_joinedPlayers != null && _joinedPlayers.Length >= 1)
			{
				//quick check if the player is already in it
				for (int i = 0; i < _joinedPlayers.Length; i++)
				{
					if (_joinedPlayers[i] == playerId)
					{
						return;
					}
				}
			}

			short[] newArr = new short[_joinedPlayers.Length + 1];

			Array.Copy(_joinedPlayers, newArr, _joinedPlayers.Length);
			newArr[_joinedPlayers.Length] = playerId;

			_joinedPlayers = newArr;

			RequestSerialization();
			OnDeserialization();
		}

		private void RemovePlayer(int playerId)
		{
			if (_joinedPlayers == null)
				return;

			int index = -1;

			for (int i = 0; i < _joinedPlayers.Length; i++)
			{
				if (_joinedPlayers[i] == playerId)
				{
					index = i;
					break;
				}
			}

			if (index == -1) return;

			short[] newArr = new short[_joinedPlayers.Length - 1];

			Array.Copy(_joinedPlayers, 0, newArr, 0, index);
			Array.Copy(_joinedPlayers, index + 1, newArr, index, _joinedPlayers.Length - index - 1);

			_joinedPlayers = newArr;

			RequestSerialization();
			OnDeserialization();
		}

		public short[] GetAllJoinedPlayers()
		{
			return _joinedPlayers;
		}

		public bool IsLocalPlayerJoined()
		{
			if (_joinedPlayers == null)
				return false;

			for (int i = 0; i < _joinedPlayers.Length; i++)
			{
				if (_joinedPlayers[i] == Networking.LocalPlayer.playerId)
				{
					return true;
				}
			}
			return false;
		}

		private void Update()
		{
			if (!Networking.IsOwner(gameObject) || !_isGameStarted)
				return;

            if (Networking.GetServerTimeInSeconds() > _startNetworkingTime + RoundLengthInSeconds)
            {
				PvPUtils.Log($"End reached, resetting the game");

				OnResetGameClicked();
			}
        }

		public bool IsGameStarted()
		{
			return _isGameStarted;
		}

		public bool IsGameStartedAndLocalPlayerIngame()
		{
			return IsGameStarted() && IsLocalPlayerJoined();
		}

		public void _ToggleLocked()
		{
			if (!Networking.IsOwner(gameObject))
			{
				return;
			}

			LockedByOwner = !LockedByOwner;

			RequestSerialization();
			OnDeserialization();
		}
	}
}