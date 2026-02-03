
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace myrop.pvp
{
	[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
	public class HitDetector : UdonSharpBehaviour
	{
		public PlayerHandlerBase PlayerHandler;

		public float DamageMultiplicator = 1.0f;

		private void Start()
		{
		}
	}
}
