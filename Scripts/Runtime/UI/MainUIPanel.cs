
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
	[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
	public class MainUIPanel : UdonSharpBehaviour
	{
		public PvPGameManager PvPGameManagerReference;
		public TextMeshProUGUI JoinedPlayers;

		public GameObject JoinButton;
		public GameObject LeaveButton;
		public GameObject StartButton;
		public GameObject ResetButton;



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
			PvPGameManagerReference.SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(PvPGameManagerReference.OnStartGameClicked));
		}

		public void _OnResetClicked()
		{
			PvPGameManagerReference.SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(PvPGameManagerReference.OnResetGameClicked));
		}

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


		public void _RefreshUI()
		{
			_RefreshJoinPanel();
			_RefreshButtons();
		}
	}
}