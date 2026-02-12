
using UdonSharp;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

namespace myrop.pvp
{
	/// <summary>
	/// This component enables movement smoothing on any component it is attached to
	/// I tried a few different implementations, and none of them gave me a satisfying result, so after a bit of research I found the 
	/// "1€ filter" algorithm, which seemed promissing https://gery.casiez.net/1euro/
	/// Implementation may have errors.
	/// </summary>
	[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
	public class Smoothing : UdonSharpBehaviour
	{
		[Header("Slow movement smoothing (jitter kill)")]
		public float SlowPosLerpSpeed = 12f;   // higher = snappier
		public float SlowRotLerpSpeed = 12f;

		[Header("Fast movement smoothing (responsiveness)")]
		public float FastPosLerpSpeed = 35f;   // higher = less smoothing
		public float FastRotLerpSpeed = 35f;

		[Header("When speeds exceed these, reduce smoothing")]
		public float MaxLinearSpeed = 1.0f;    // m/s
		public float MaxAngularSpeed = 180f;   // deg/s

		private Vector3 _defaultLocalPosition;
		private Quaternion _defaultLocalRotation;
		private bool _init;

		private Vector3 _smoothedPos;
		private Quaternion _smoothedRot;

		private Vector3 _prevRawPos;
		private Quaternion _prevRawRot;

		private void Start()
		{ 
			//We only enable smoothing in VR, so Desktop players can flickshot more easily 
			enabled = Networking.LocalPlayer.IsUserInVR();
		}

		void OnEnable()
		{
			if (!_init)
			{
				_defaultLocalPosition = transform.localPosition;
				_defaultLocalRotation = transform.localRotation;
				_init = true;
			}

			transform.localPosition = _defaultLocalPosition;
			transform.localRotation = _defaultLocalRotation;

			var rawPos = transform.position;
			var rawRot = transform.rotation;

			_smoothedPos = rawPos;
			_smoothedRot = rawRot;

			_prevRawPos = rawPos;
			_prevRawRot = rawRot;
		}

		public override void PostLateUpdate()
		{
			//PvPUtils.Log($"before {transform.localPosition} {transform.position}");
			// capture raw pose (from parent/controller) by restoring local offsets
			transform.localPosition = _defaultLocalPosition;
			transform.localRotation = _defaultLocalRotation;

			float dt = Time.deltaTime;

			var rawPos = transform.position;
			var rawRot = transform.rotation;

			// estimate raw speeds
			float linSpeed = Vector3.Distance(rawPos, _prevRawPos) / dt;
			float angSpeed = Quaternion.Angle(rawRot, _prevRawRot) / dt;

			// map speeds -> blend factor (0 = slow, 1 = fast)
			float fastness = Mathf.Max(
				Mathf.Clamp01(linSpeed / MaxLinearSpeed),
				Mathf.Clamp01(angSpeed / MaxAngularSpeed)
			);

			// pick smoothing speeds based on fastness
			float posT = 1f - Mathf.Exp(-Mathf.Lerp(SlowPosLerpSpeed, FastPosLerpSpeed, fastness) * dt);
			float rotT = 1f - Mathf.Exp(-Mathf.Lerp(SlowRotLerpSpeed, FastRotLerpSpeed, fastness) * dt);

			//PvPUtils.Log($"{_prevRawPos} {rawPos} {transform.position} {_smoothedPos} {posT} {Vector3.Lerp(_smoothedPos, rawPos, posT)}");
			
			_smoothedPos = Vector3.Lerp(_smoothedPos, rawPos, posT);
			_smoothedRot = Quaternion.Slerp(_smoothedRot, rawRot, rotT);

			transform.position = _smoothedPos;
			transform.rotation = _smoothedRot;

			_prevRawPos = rawPos;
			_prevRawRot = rawRot;
		}
	}


}
