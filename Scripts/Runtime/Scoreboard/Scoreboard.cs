
using UdonSharp;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace myrop.pvp
{
	[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
	public class Scoreboard : UdonSharpBehaviour
	{
		public PvPGameManager GameManager;

		[UdonSynced]
		private short[] _serializedScoreboard;

		/// <summary>
		/// Key : Player ID
		/// Value : int array of 3 elements : Kill, death, score
		/// </summary>
		private DataDictionary _playerToScore;

		public UiScoreboard[] Scoreboards;

		void Start()
		{
			_serializedScoreboard = new short[0];

			GameManager.SetScoreboardInstance(this);
		}

		public void Init(short[] playersInGame)
		{
			_playerToScore = new DataDictionary();

			for (int i = 0; i < playersInGame.Length; i++)
			{
				int playerID = playersInGame[i];
				_playerToScore[playerID] = new DataToken(new int[3]);
			}

			SyncScoreboard();
		}

		/// <summary>
		/// _playerToScore uses the format 
		///      Key : Player ID
		///      Value : int array of 3 elements : Kill, death, score
		///      
		/// The GenerateScoreboard() transforms the _playerToScore dictionary into :
		///      Key : Score
		///      Value : DataList (where each element of that DataList contains PlayerID, Kill, Death as int array)
		///      
		/// The convertion is done to make sorting by score easier, since DataDictionary keys can easily be sorted 
		/// </summary>
		/// <returns>The generated scoreboard, so the converted _playerToScore dictionary</returns>
		private DataDictionary GenerateScoreboard()
		{
			DataDictionary scoreboard = new DataDictionary();

			if (_playerToScore == null)
				return scoreboard;

			DataList allPlayerIDs = _playerToScore.GetKeys();

			for (int indexPlayerId = 0;  indexPlayerId < allPlayerIDs.Count; indexPlayerId++)
			{
				int playerID = allPlayerIDs[indexPlayerId].Int;
				int[] playerStats = (int[]) _playerToScore[playerID].Reference;
				int kills = playerStats[0];
				int deaths = playerStats[1];
				int score = playerStats[2];

				//Now, we store all that data in the new "scoreboard" dataDictionary

				//If "score" doesn't exist in that DataDictionary, then we create a new DataList, otherwise we directly append a new element to that DataList
				if (!scoreboard.ContainsKey(score))
				{
					scoreboard[score] = new DataToken(new DataList());
				}

				int[] convertedStats = new int[3];
				convertedStats[0] = playerID;
				convertedStats[1] = kills;
				convertedStats[2] = deaths;

				scoreboard[score].DataList.Add(new DataToken(convertedStats)); //Oh god I hope one day we get generic/template support in U#
			}

			return scoreboard;
		}


		/// <summary>
		/// Scoreboard row convertion, see param info for more details
		/// </summary>
		/// <param name="score">Score of that scoreboard row</param>
		/// <param name="allPlayersWithThatScore">DataList, where each element of that DataList contains [PlayerID, Kill, Death] as int[3] array</param>
		/// <returns>[player1_ID], [player1_Kill], [player1_Death], [player1_Score], [player2_ID], [player2_Kill], [player2_Death],. [player2_Score]...</returns>
		private DataList SerializeScore(int score, DataList allPlayersWithThatScore)
		{
			DataList serialized = new DataList();
			
			for (int i = 0; i < allPlayersWithThatScore.Count; i++)
			{
				int[] playerStats = (int[]) allPlayersWithThatScore[i].Reference;
				
				foreach (int stat in playerStats)
				{
					serialized.Add(stat);
				}
				serialized.Add(score);
			}
			return serialized;
		}

		/// <summary>
		/// Syncronizes the scoreboard
		/// </summary>
		private void SyncScoreboard()
		{
			if (!Networking.IsOwner(gameObject))
				return;

			DataDictionary generatedScoreboard = GenerateScoreboard();

			DataList serializedScoreboardAsDataList = new DataList();

			//(1) We first build the "serializedScoreboardAsDataList" Datalist...
			DataList sortedByScore = generatedScoreboard.GetKeys();
			sortedByScore.Sort();

			for (int indexDT = sortedByScore.Count - 1; indexDT >= 0; indexDT--)
			{
				int score = sortedByScore[indexDT].Int;

				DataList serializedScore = SerializeScore(score, generatedScoreboard[score].DataList);
				serializedScoreboardAsDataList.AddRange(serializedScore);
			}

			//(2) ... Then we convert that DataList into a short array for syncronisation
			_serializedScoreboard = new short[serializedScoreboardAsDataList.Count];

			for (int i = 0; i < _serializedScoreboard.Length; i++)
			{
				_serializedScoreboard[i] = (short)serializedScoreboardAsDataList[i].Int;
			}

			RequestSerialization();
			OnDeserialization();
		}

		/// <summary>
		/// Updates the stats of the player in the scoreboard
		/// </summary>
		/// <param name="playerId"></param>
		/// <param name="kill"></param>
		/// <param name="death"></param>
		/// <param name="score"></param>
		public void UpdateInScoreboard(int playerId, int kill, int death, int score)
		{
			PvPUtils.Log($"UpdateInScoreboard : playerId={playerId} K={kill} D={death} S={score}");
			if (_playerToScore == null 
				|| !_playerToScore.ContainsKey(playerId))
				return;

			int[] storedStats = ((int[])(_playerToScore[playerId].Reference));
			storedStats[0] = kill;
			storedStats[1] = death;
			storedStats[2] = score;

			SyncScoreboard();
		}

		public override void OnDeserialization()
		{
			// Debug();
			foreach (var scoreboard in Scoreboards)
			{
				scoreboard.UpdateScoreboard(_serializedScoreboard);
			}
		}

		private void Debug()
		{
			string output = "";
			foreach (var value in _serializedScoreboard)
			{
				output += $"[{value.ToString()}]";
			}
			PvPUtils.Log($"Serialized scoreboard : {output}");
		}
	}
}
