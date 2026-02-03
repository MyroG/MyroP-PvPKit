
using ArchiTech.ProTV;
using myrop.pvp;
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

namespace myrop.pvp
{
	[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
	public class UiScoreboard : UdonSharpBehaviour
	{
		public int NumberRowsMax;

		public UiScoreboardRow[] Rows;

		void Start()
		{
			for (int i = 0; i < Rows.Length; i++)
			{
				Rows[i].gameObject.SetActive(false);
			}
		}

		public void UpdateScoreboard(short[] serializedScoreboard)
		{
			for (int serializedScoreboardIndex = 0 , rowIndex = 0; serializedScoreboardIndex < serializedScoreboard.Length; serializedScoreboardIndex += 4, rowIndex++)
			{
				short[] serializedScoreboardRow = new short[4];
				Array.Copy(serializedScoreboard, serializedScoreboardIndex, serializedScoreboardRow, 0, 4);

				if (rowIndex >= NumberRowsMax)
				{
					Rows[rowIndex].gameObject.SetActive(false);
				}
				else
				{
					Rows[rowIndex].gameObject.SetActive(true);
					Rows[rowIndex].SetScore(serializedScoreboardRow);
				}
			}
			/*int indexSerializedScoreboard = 0;
			for (int rowIndex = 0; rowIndex < Rows.Length; rowIndex++)
			{
				int score = UNINITIALIZED;
				DataList playerList = new DataList();

				while (indexSerializedScoreboard < serializedScoreboard.Length && serializedScoreboard[indexSerializedScoreboard] != SEPARATOR)
				{
					if (score == UNINITIALIZED)
					{
						score = serializedScoreboard[indexSerializedScoreboard];
					}
					else
					{
						playerList.Add(serializedScoreboard[indexSerializedScoreboard]);
					}

					indexSerializedScoreboard++;
				}

				indexSerializedScoreboard++;

				if (rowIndex >= NumberRowsMax || score == UNINITIALIZED)
				{
					Rows[rowIndex].gameObject.SetActive(false);
				}
				else
				{
					Rows[rowIndex].gameObject.SetActive(true);
					Rows[rowIndex].SetScore(score, playerList);
				}
			}*/
		}
	}
}