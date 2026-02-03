using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace myrop.pvp
{
	public class DrawForwardLine : MonoBehaviour
	{
		private void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.red;

			Vector3 start = transform.position;
			Vector3 end = start + transform.forward * 1f;

			Gizmos.DrawLine(start, end);
			Gizmos.DrawSphere(end, 0.05f);
		}
	}
}
