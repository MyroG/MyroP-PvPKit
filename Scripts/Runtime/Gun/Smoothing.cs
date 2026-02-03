
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace myrop.pvp
{
	/// <summary>
	/// This component enables movement smoothing on any component it is attached to
	/// </summary>
	[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
	public class Smoothing : UdonSharpBehaviour
	{
		public float SmoothingSpeed = 15.0f;

		[Header("Smoothing gets disabled when the object is faster than the values set below")]
		public float MaxLinearSpeed = 1.0f;
		public float MaxAngularSpeed = 1.0f;

		private Vector3 _previousPosition;
		private Quaternion _previousRotation;

		private Vector3 _defaultLocalPosition;
		private Quaternion _defaultLocalRotation;

		private bool _isFirstEnable = true;

		private void Start()
		{
			//We only enable smoothing in VR, so Desktop players can flickshot more easily
			enabled = Networking.LocalPlayer.IsUserInVR();
		}

		void OnEnable()
		{
			if (_isFirstEnable)
			{ 
				_defaultLocalPosition = transform.localPosition;
				_defaultLocalRotation = transform.localRotation;
				_isFirstEnable = false;
			}

			transform.localPosition = _defaultLocalPosition;
			transform.localRotation = _defaultLocalRotation;

			_previousPosition = transform.position;
			_previousRotation = transform.rotation;
		}

		public override void PostLateUpdate()
		{
			transform.localPosition = _defaultLocalPosition;
			transform.localRotation = _defaultLocalRotation;

			if (ShouldSmoothMovements())
			{
				float t = Time.deltaTime * SmoothingSpeed;

				transform.position = Vector3.Lerp(_previousPosition, transform.position, t);
				transform.rotation = Quaternion.Slerp(_previousRotation, transform.rotation, t);
			}

			_previousPosition = transform.position;
			_previousRotation = transform.rotation;
		}

		private bool ShouldSmoothMovements()
		{
			return true;
			float dt = Time.deltaTime;
			if (dt <= 0f) return false;

			float linearSpeed =
				Vector3.Distance(transform.position, _previousPosition) / dt;
			float angularSpeed =
				Quaternion.Angle(transform.rotation, _previousRotation) / dt;

			Debug.Log($"{transform.position} {_previousPosition} {linearSpeed}");
			return linearSpeed < MaxLinearSpeed && angularSpeed < MaxAngularSpeed;
		}
	}
}
