
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace myrop.pvp
{
	public static class PvPUtils
	{
		private const bool ENABLE_LOGIN = true;
		public static void Log(string message)
		{
			if (ENABLE_LOGIN)
			{
				Debug.Log($"[PvP] {message}");
			}
		}

		public static void LogError(string message)
		{
			if (ENABLE_LOGIN)
			{
				Debug.LogError($"[PvP] {message}");
			}
		}

		public static void LogWarning(string message)
		{
			if (ENABLE_LOGIN)
			{
				Debug.LogWarning($"[PvP] {message}");
			}
		}

		public static PlayerHandlerBase FindPlayerHandlerOf(VRCPlayerApi player)
		{
			var objects = Networking.GetPlayerObjects(player);
			for (int i = 0; i < objects.Length; i++)
			{
				if (!Utilities.IsValid(objects[i])) continue;
				PlayerHandlerBase foundScript = objects[i].GetComponentInChildren<PlayerHandlerBase>();
				if (Utilities.IsValid(foundScript)) return foundScript;
			}
			return null;
		}
	}
}
