
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

namespace myrop.pvp
{
	[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
	public class UiScoreboardRow : UdonSharpBehaviour
	{
		public TextMeshProUGUI Name;
		public TextMeshProUGUI Kill;
		public TextMeshProUGUI Death;
		public TextMeshProUGUI Score;
		void Start()
		{

		}

		public void SetScore(short[] serializedRow)
		{
			Name.text = "";
			Kill.text = "0";
			Death.text = "0";
			Score.text = "0";

			if (serializedRow.Length != 4)
			{
				return;
			}

			VRCPlayerApi player = VRCPlayerApi.GetPlayerById(serializedRow[0]);
			bool hasLocalPlayer = Utilities.IsValid(player) ? player.isLocal : false;

			int kill = serializedRow[1];
			int death = serializedRow[2];
			int score = serializedRow[3];

			WriteText(Name, Utilities.IsValid(player) ? player.displayName : "[PlayerLeft]", hasLocalPlayer);
			WriteText(Kill, kill.ToString(), hasLocalPlayer);
			WriteText(Death, death.ToString(), hasLocalPlayer);
			WriteText(Score, score.ToString(), hasLocalPlayer);
		}

		private void WriteText(TextMeshProUGUI textfield, string text, bool isLocalPlayer)
		{
			if (isLocalPlayer)
			{
				textfield.text = $"<b>{text}</b>";
			}
			else
			{
				textfield.text = $"{text}";
			}
		}
	}
}