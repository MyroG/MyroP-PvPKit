using System.Collections.Generic;
using System.Linq;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace myrop.pvp
{
	public enum ShotImportance { LocalPlayer = 1, Other = 4}

	[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
	public class AudioManager : UdonSharpBehaviour
	{
		[SerializeField]
		private Transform Parent;
		
		[Header("Pooling")]
		public int PoolSize = 8;
		public AudioSource AudioSourceSettings;
		private AudioSource[] _pool;

		[Header("Distance Rules (meters)")]
		public float nearDist = 25f;
		public float midDist = 60f;
		public float maxAudibleDist = 120f;

		[Header("Per-shooter Throttle")]
		public float minIntervalPerShooter = 0.07f;
		private float _timeLastAudioSourcePlayed;

		private VRCPlayerApi _localPlayer;

		/// <summary>
		/// key : Priority score, lower is higher priority (float)
		/// Value : The Audio source
		/// </summary>
		private DataDictionary _activeAudioSources;

		void Start()
		{
			_activeAudioSources = new DataDictionary();
			_localPlayer = Networking.LocalPlayer;

			_pool = new AudioSource[PoolSize];
			for (int i = 0; i < _pool.Length; i++)
			{
				GameObject audioSourceGo = Instantiate(AudioSourceSettings.gameObject);
				AudioSource audioSource = audioSourceGo.GetComponent<AudioSource>();
				_pool[i] = audioSource;
			}

			ReparentAudioSources();

			_CustomLoop();
		}

		public void SetNewParent(Transform parent)
		{
			Parent = parent;
			ReparentAudioSources();
		}

		private void ReparentAudioSources()
		{
			if (_pool == null)
				return;

			for (int i = 0; i < _pool.Length; i++)
			{
				_pool[i].transform.parent = Parent == null ? transform : Parent;
			}
		}

		public void _CustomLoop()
		{
			//We remove all audio sources from _activeAudioSources that aren't played anymore
			DataList keys = _activeAudioSources.GetKeys();
			for (int i = 0;i < keys.Count;i++)
			{
				AudioSource src = (AudioSource) _activeAudioSources[keys[i]].Reference;
				if (src != null && !src.isPlaying)
				{
					_activeAudioSources.Remove(keys[i]);
				}
			}

			SendCustomEventDelayedSeconds(nameof(_CustomLoop), 0.05f);
		}

		public void PlayAudio(AudioClip clip, Vector3 pos, ShotImportance importance, float volume = 1f, float pitchJitter = 0.0f)
		{
			if (!clip) return;

			float dist = Vector3.Distance(_localPlayer.GetPosition(), pos);
			
			if (dist > maxAudibleDist) 
				return; //No Audio when too far away

			if (Time.time - _timeLastAudioSourcePlayed < minIntervalPerShooter)
				return; //prevents audio spam

			// if at max audio sources, try stealing the worst one
			if (_activeAudioSources.Count >= PoolSize)
			{
				float worstScore = FindWorstActiveScore();

				// disable audio source
				AudioSource worstAudioSource = (AudioSource) _activeAudioSources[worstScore].Reference;
				worstAudioSource.Stop();
				_activeAudioSources.Remove(worstScore);
			}

			AudioSource src = NextSource();

			if (src != null)
			{
				src.transform.position = pos;
				src.clip = clip;
				src.volume = volume;
				src.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
				src.Play();
				_timeLastAudioSourcePlayed = Time.time;
				_activeAudioSources[GetScore(dist, importance)] = src;
			}
		}

		/// <summary>
		/// Returns the worst active Score, which is the highest score value
		/// </summary>
		/// <returns></returns>
		private float FindWorstActiveScore()
		{
			DataList keys = _activeAudioSources.GetKeys();
			keys.Sort();
			return keys[keys.Count - 1].Float;
		}

		/// <summary>
		/// Returns "score" if the score is unique, otherwise we generate a new score value
		/// </summary>
		/// <param name="score"></param>
		/// <returns></returns>
		private float GetScore(float dist, ShotImportance importance)
		{
			/* Smaller score = higher priority
			 * I was thinking that:
			 * - More recent audio sources should be prioritized, so I substract Time.time
			 * - Further away audio sources should not be prioritized
			 * */
			float score = (float)System.Convert.ToInt32(importance) * dist - (Time.time / 2.0f);

			if (_activeAudioSources.ContainsKey(score))
			{
				//Very unlikely to happen, but just in case, if the key "score" already exists, then the new score is "score + previousScore / 2" 
				DataList keys = _activeAudioSources.GetKeys();
				keys.Sort();
				int indexScore = keys.IndexOf(score);
				if (indexScore == 0)
				{
					//the score is already the best possible score
					return score - 10.0f;
				}
				else
				{
					return (score + keys[indexScore - 1].Float) / 2.0f;
				}
			}
			else
			{
				return score;
			}
		}

		/// <summary>
		/// Returns the next available Audio Source
		/// Returns null if there's none available
		/// </summary>
		/// <returns></returns>
		AudioSource NextSource()
		{
			// find a free Audio Source
			for (int i = 0; i < _pool.Length; i++)
			{
				if (!_pool[i].isPlaying) 
					return _pool[i];
			}
			return null;
		}
	}
}