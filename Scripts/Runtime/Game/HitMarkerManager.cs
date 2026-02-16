using System;
using System.Collections.Generic;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace myrop.pvp
{
	/// <summary>
	/// Spawns Hit markers
	/// Pretty much a copy/paste of the AudioManager script
	/// Ideally, to avoid copy/paste, HitMarkerManager should inherit from something like PoolManager<ParticleSystem>, but u# doesn't fully support generics yet
	/// </summary>
	[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
	public class HitMarkerManager : UdonSharpBehaviour
	{
		[SerializeField]
		private Transform Parent;

		[Header("Pooling")]
		public int PoolSize = 8;
		public ParticleSystem ParticleSystemSettings;
		private ParticleSystem[] _pool;

		private VRCPlayerApi _localPlayer;

		/// <summary>
		/// key : Priority score, lower is higher priority (float)
		/// Value : The particle system
		/// </summary>
		private DataDictionary _activeParticleSystems;

		void Start()
		{
			_activeParticleSystems = new DataDictionary();
			_localPlayer = Networking.LocalPlayer;

			_pool = new ParticleSystem[PoolSize];
			for (int i = 0; i < _pool.Length; i++)
			{
				GameObject particleSystemGo = Instantiate(ParticleSystemSettings.gameObject);
				ParticleSystem particleSystem = particleSystemGo.GetComponent<ParticleSystem>();
				_pool[i] = particleSystem;
			}

			Reparent();

			_CustomLoop();
		}

		public void SetNewParent(Transform parent)
		{
			Parent = parent;
			Reparent();
		}

		private void Reparent()
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
			//We remove all particle systems from _activeParticleSystems that aren't played anymore
			DataList keys = _activeParticleSystems.GetKeys();
			for (int i = 0; i < keys.Count; i++)
			{
				ParticleSystem src = (ParticleSystem)_activeParticleSystems[keys[i]].Reference;
				if (src != null && !src.isPlaying)
				{
					_activeParticleSystems.Remove(keys[i]);
				}
			}

			SendCustomEventDelayedSeconds(nameof(_CustomLoop), 0.05f);
		}

		public void Play(Vector3 pos)
		{
			// if at max audio sources, try stealing the worst one
			ParticleSystem src;
			if (_activeParticleSystems.Count >= PoolSize)
			{
				float worstScore = FindWorstActiveScore();

				// disable particle system
				ParticleSystem worstParticleSystem = (ParticleSystem)_activeParticleSystems[worstScore].Reference;
				worstParticleSystem.Stop();
				_activeParticleSystems.Remove(worstScore);

				src = worstParticleSystem;
			}
			else
			{
				src = Next();
			}

			if (src != null)
			{
				src.Play();
				src.gameObject.transform.position = pos;
				_activeParticleSystems[GetScore()] = src;
			}
		}

		/// <summary>
		/// Returns the worst active Score, which is the highest score value
		/// </summary>
		/// <returns></returns>
		private float FindWorstActiveScore()
		{
			DataList keys = _activeParticleSystems.GetKeys();
			keys.Sort();
			return keys[keys.Count - 1].Float;
		}

		private float GetScore()
		{
			return -Time.time;
		}

		/// <summary>
		/// Returns the next available particle system, basically an particle system that isn't playing
		/// Returns null if there's none available
		/// </summary>
		/// <returns></returns>
		ParticleSystem Next()
		{
			// find a free particle system
			for (int i = 0; i < _pool.Length; i++)
			{
				if (!_pool[i].isPlaying)
					return _pool[i];
			}
			return null;
		}
	}
}