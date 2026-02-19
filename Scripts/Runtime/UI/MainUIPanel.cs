
using System;
using System.Collections.Generic;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace myrop.pvp
{
	[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
	public class MainUIPanel : UdonSharpBehaviour
	{
		public PvPGameManager PvPGameManagerReference;
		public TextMeshProUGUI JoinedPlayers;

		public GameObject JoinButton;
		public GameObject LeaveButton;
		public GameObject StartButton;
		public GameObject ResetButton;

		[SerializeField]
		private TextMeshProUGUI _lockButtonText;

		[SerializeField]
		private TextMeshProUGUI _ownerText;

		void Start()
		{
			_RefreshUI();
		}

		public void _OnJoinClicked()
		{
			PvPGameManagerReference.SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(PvPGameManagerReference.PlayerRequestedToJoin), Networking.LocalPlayer.playerId);
		}

		public void _OnLeaveClicked()
		{
			PvPGameManagerReference.SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(PvPGameManagerReference.PlayerRequestedToLeave), Networking.LocalPlayer.playerId);
		}

		public void _OnStartClicked()
		{
			if (!Networking.IsOwner(PvPGameManagerReference.gameObject) && PvPGameManagerReference.LockedByOwner)
				return;  //only the owner can start the game if the panel is locked

			PvPGameManagerReference.SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(PvPGameManagerReference.OnStartGameClicked));
		}

		public void _OnResetClicked()
		{
			PvPGameManagerReference.OnResetGameClicked();
		}

		public void _OnToggleLockClicked()
		{
			PvPGameManagerReference._ToggleLocked();
		}

		/*public void _OnOwnerClicked()
		{
			if (PvPGameManagerReference.LockedByOwner)
				return;

			Networking.SetOwner(Networking.LocalPlayer, PvPGameManagerReference.gameObject);

			Scoreboard scoreboard = PvPGameManagerReference.GetScoreboardInstance();
			if (scoreboard != null)
			{
				Networking.SetOwner(Networking.LocalPlayer, scoreboard.gameObject);
			}
		}*/

		private void _RefreshJoinPanel()
		{
			JoinedPlayers.text = "";

			short[] joinedPlayers = PvPGameManagerReference.GetAllJoinedPlayers();
			if (joinedPlayers == null)
				return;

			foreach (int playerId in joinedPlayers)
			{
                VRCPlayerApi player = VRCPlayerApi.GetPlayerById(playerId);
				if (Utilities.IsValid(player))
				{
					JoinedPlayers.text += $"[{player.displayName}] ";
				}
			}
		}

		private void _RefreshButtons()
		{
			bool isLocalPlayerJoined = PvPGameManagerReference.IsLocalPlayerJoined();
			bool isGameStarted = PvPGameManagerReference.IsGameStarted();
			JoinButton.SetActive(!isLocalPlayerJoined);
			LeaveButton.SetActive(isLocalPlayerJoined);

			StartButton.SetActive(!isGameStarted);
			ResetButton.SetActive(isGameStarted);
		}

		private void _RefreshLock()
		{
			if (_lockButtonText != null)
				_lockButtonText.text = PvPGameManagerReference.LockedByOwner ? "Locked" : "Unlocked";

			if (_ownerText != null)
				_ownerText.text = $"Current owner is {Networking.GetOwner(PvPGameManagerReference.gameObject).displayName}";
		}


		public void _RefreshUI()
		{
			_RefreshJoinPanel();
			_RefreshButtons();
			_RefreshLock();
		}
	}
}