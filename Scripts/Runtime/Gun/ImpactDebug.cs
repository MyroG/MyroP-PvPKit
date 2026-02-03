
using Newtonsoft.Json.Linq;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace myrop.pvp
{
	[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
	public class ImpactDebug : UdonSharpBehaviour
	{
		public GameObject ImpactPrefab;
		public int MaxNumberImpacts;

		private GameObject[] _spawnedPrefabs;
		private int _index;
		void Start()
		{
			_spawnedPrefabs = new GameObject[MaxNumberImpacts];
			for(int i = 0; i < MaxNumberImpacts; i++)
			{
				_spawnedPrefabs[i] = Instantiate(ImpactPrefab);
				_spawnedPrefabs[i].gameObject.SetActive(false);
			}
			_index = 0;
		}

		public void Place(Vector3 position)
		{
			int max = _spawnedPrefabs.Length;
			if (_spawnedPrefabs == null || _index >= max)
			{
				return;
			}

			_spawnedPrefabs[_index].transform.position = position;
			_spawnedPrefabs[_index].SetActive(true);
			_index++;

			_index = ((_index % max) + max) % max;
		}
	}
}
