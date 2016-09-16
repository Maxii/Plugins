using UnityEngine;
using System.Collections;

public class MF_B_Turret : MF_AbstractPlatform {

	[Header("Turning Properties:")]
	public float rotationRate = 50f;
	public float elevationRate = 50f;
	[Header("Turn Arc Limits: (-180 to 180)")]
	[Range(-180, 0)] public float limitLeft = -180f;
	[Range(0, 180)] public float limitRight = 180f;
	[Header("Elevation Arc Limits: (-90 to 90)")]
	[Range(0, 90)] public float limitUp = 90f;
	[Range(-90, 0)] public float limitDown = -10f;
	[Header("Rest Angle: (x = elevation, y = rotation)")]
	[Tooltip("Position when turret has no target.")]
	public Vector2 restAngle;
	[Tooltip("Time without a target before turret will move to rest position.")]
	public float restDelay;

	[Header("Parts:")]
	[Tooltip("Object that rotates around the y axis.")]
	public Transform rotator;
	[Tooltip("Object that rotates around the x axis.")]
	public Transform elevator;
	public AudioObject rotatorSound;
	public AudioObject elevatorSound;

	float timeSinceTarget;
	Vector3 rotatorPlaneTarget;
	Vector3 elevatorPlaneTarget;
	float rotAudioStopTime;
	float eleAudioStopTime;

	[System.Serializable]
	public class AudioObject {
		public AudioSource audioSource;
		public float pitchMax = 1f;
		public float pitchMin = .75f;
		[HideInInspector] public float stopTime;
	}
	
	public override void Start () {
		if ( CheckErrors() == true ) { return; }
		base.Start();

		timeSinceTarget = -restDelay;
	}
	
	public override void Update () {
		if (error == true) { return; }
		base.Update(); // AimLocation() called from base
		
		// find aim locations
		if ( _target ) {
			Vector3 _localTarget;
			timeSinceTarget = Time.time;
			// find target's location in rotation plane
			_localTarget = rotator.InverseTransformPoint( targetAimLocation ); 
			rotatorPlaneTarget = rotator.TransformPoint( new Vector3(_localTarget.x, 0f, _localTarget.z) );
			// find target's location in elevation plane as if rotator is already facing target, as rotation will eventualy bring it to front. (don't elevate past 90/-90 degrees to reach target)
			Vector3 _cross = Vector3.Cross( rotator.up, targetAimLocation - weaponMount.position );
			Vector3 _level = Vector3.Cross( _cross, rotator.up ); // find direction towards target but level with local plane
			float _angle = Vector3.Angle( _level, targetAimLocation - weaponMount.position ); 
			if ( _localTarget.y < elevator.localPosition.y + weaponMount.localPosition.y ) { _angle *= -1; } // should angle be negative?
			elevatorPlaneTarget = weaponMount.position + (Quaternion.AngleAxis( _angle, -rotator.right ) * rotator.forward * 1000f);
			
			//			Debug.DrawRay( weaponMount.position, _cross, Color.red, .01f );
			//			Debug.DrawRay( weaponMount.position, _level, Color.green, .01f );
			//			Debug.DrawRay( weaponMount.position, targetAimLocation - weaponMount.position, Color.cyan, .01f );
			//			Debug.Log( _angle );
			
		} else { // no target
			targetAimLocation = Vector3.zero;
			if ( Time.time >= timeSinceTarget + restDelay ) {
				// set rotation and elevation goals to the rest position
				rotatorPlaneTarget = rotator.position + (Quaternion.AngleAxis( restAngle.y, transform.up ) * transform.forward * 1000f);
				elevatorPlaneTarget = elevator.position + (Quaternion.AngleAxis( restAngle.x, -rotator.right ) * rotator.forward * 1000f);
			}
		}
		
		// turning
		float _curRotSeperation = MFmath.AngleSigned(rotator.forward, rotatorPlaneTarget - weaponMount.position, transform.up);
		// turn opposite if shortest route is through a gimbal limit
		float _bearing = MFmath.AngleSigned(transform.forward, rotatorPlaneTarget - weaponMount.position, transform.up);
		float _aimAngle = MFmath.AngleSigned(transform.forward, rotator.forward, rotator.up);
		float _modRotRate = rotationRate;
		if (limitLeft > -180 || limitRight < 180) { // is there a gimbal limit?
			if ( (_curRotSeperation < 0 && _bearing > 0 && _aimAngle < 0)  ||  (_curRotSeperation > 0 && _bearing < 0 && _aimAngle > 0) ) { // is shortest turn angle through a limit?
				// reverse turn
				_modRotRate *= -1f;
			}
		}
		
		Quaternion _rot;
		Quaternion _lastRot = rotator.localRotation;
		Quaternion _lastEle = elevator.localRotation;
		// apply rotation
		_rot = Quaternion.LookRotation( rotatorPlaneTarget - rotator.position, transform.up );
		rotator.rotation = Quaternion.RotateTowards( rotator.rotation, _rot, _modRotRate * Time.deltaTime );
		// apply elevation
		_rot = Quaternion.LookRotation( elevatorPlaneTarget - weaponMount.position, rotator.up );
		elevator.rotation = Quaternion.RotateTowards( elevator.rotation, _rot, elevationRate * Time.deltaTime );

		CheckGimbalLimits();

		// find rate for sounds
		float _rotRate = Quaternion.Angle( _lastRot, rotator.localRotation ) / Time.deltaTime;
		float _eleRate = Quaternion.Angle( _lastEle, elevator.localRotation ) / Time.deltaTime;

		// prevent nonsence
		rotator.localEulerAngles = new Vector3( 0f, rotator.localEulerAngles.y, 0f );
		elevator.localEulerAngles = new Vector3( elevator.localEulerAngles.x, 0f, 0f );
		
		// turn sounds	
		if (rotatorSound.audioSource) {
			TurnSound( rotatorSound, _rotRate, rotationRate );
		}
		if (elevatorSound.audioSource) {
			TurnSound( elevatorSound, _eleRate, elevationRate );
		}
	}
	
	void TurnSound ( AudioObject ao, float rate, float rateMax ) {
		if ( rate > .1f ) { // avoid near 0 vaues
			ao.audioSource.pitch = ao.pitchMin + (( rate / rateMax ) * (ao.pitchMax - ao.pitchMin));
			if ( ao.audioSource.isPlaying == false ) {
				ao.audioSource.Play();
			}
			ao.stopTime = Time.time + .1f; // continuously refresh stop time
		} else {
			if ( Time.time >= ao.stopTime ) { // make sure audio has been stopped for at least .1 seconds
				ao.audioSource.Stop();
			}
		}
	}

	// checks if turret has moved outside of its gimbal limits. If so, it puts it back to the appropriate limit
	void CheckGimbalLimits () {
		if (error == true) { return; }
		if (rotator.localEulerAngles.y > limitRight && rotator.localEulerAngles.y <=180) {
			rotator.localEulerAngles = new Vector3( rotator.localEulerAngles.x, limitRight, rotator.localEulerAngles.z );
		}
		if (rotator.localEulerAngles.y < 360+limitLeft && rotator.localEulerAngles.y >= 180) {
			rotator.localEulerAngles = new Vector3( rotator.localEulerAngles.x, 360+limitLeft, rotator.localEulerAngles.z );
		}
		if (elevator.localEulerAngles.x > -limitDown && elevator.localEulerAngles.x <=180) {
			elevator.localEulerAngles = new Vector3( -limitDown, elevator.localEulerAngles.y, elevator.localEulerAngles.z );
		}
		if (elevator.localEulerAngles.x < 360-limitUp && elevator.localEulerAngles.x >= 180) {
			elevator.localEulerAngles = new Vector3( 360-limitUp, elevator.localEulerAngles.y, elevator.localEulerAngles.z );
		}
	}
	
	// tests if a given target is within the gimbal limits of this turret 
	public override bool TargetWithinLimits ( Transform targ ) {
		if (error == true) { return false; }
		if ( targ ) {
			// find target's location in rotation plane
			Vector3 _localTarget = rotator.InverseTransformPoint( targ.position ); 
			Vector3 _rotatorPlaneTarget = rotator.TransformPoint( new Vector3(_localTarget.x, 0f, _localTarget.z) );
			
			float _testAngle = MFmath.AngleSigned( transform.forward, _rotatorPlaneTarget - transform.position, transform.up );
			if (_testAngle > limitRight || _testAngle < limitLeft) {
				return false;
			}
			
			// find target's location in elevation plane as if rotator is already facing target, as rotation will eventualy bring it to front. (don't elevate past 90/-90 degrees to reach target)
			Vector3 _cross = Vector3.Cross( rotator.up, targ.position - weaponMount.position );
			Vector3 _level = Vector3.Cross( _cross, rotator.up ); // find direction towards target but level with local plane
			float _angle = Vector3.Angle( _level, targ.position - weaponMount.position ); 
			if ( _localTarget.y < elevator.localPosition.y + weaponMount.localPosition.y ) { _angle *= -1; } // should angle be negative?
			elevatorPlaneTarget = weaponMount.position + (Quaternion.AngleAxis( _angle, -rotator.right ) * rotator.forward * 1000f);
			
			_testAngle = MFmath.AngleSigned( rotator.forward, elevatorPlaneTarget - elevator.position, -rotator.right );
			if (_testAngle < limitDown || _testAngle > limitUp) {
				return false;
			}  
			return true;
		} else {
			return false;
		}
	}
	
	bool CheckErrors () {
		error = false;

		if (rotator == false) { Debug.Log( this+": Turret rotator part hasn't been defined." ); error = true; }
		if (elevator == false) { Debug.Log( this+": Turret elevator part hasn't been defined." ); error = true; }
		if (transform.localScale.x != transform.localScale.z) { Debug.Log( this+" Turret transform x and z scales must be equal." ); error = true; }
		if (rotator) {
			if (rotator.localScale.x != 1) { Debug.Log( this+" rotator part transform xyz scales must = 1." ); error = true; }
			if (rotator.localScale.y != 1) { Debug.Log( this+" rotator part transform xyz scales must = 1." ); error = true; }
			if (rotator.localScale.z != 1) { Debug.Log( this+" rotator part transform xyz scales must = 1." ); error = true; }
		}
		
		return error;
	}
}