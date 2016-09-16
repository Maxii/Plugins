using UnityEngine;
using System.Collections;

namespace MFnum {
	public enum ArcType { Low, High }
}

public class ST_Turret : MF_AbstractPlatform {
	
	[Tooltip("Will aim shots as if they are affected by gravity. Projectiles need to be configured to use gravity as well.")]
	public bool useGravity;
	[Tooltip("When using gravity, which arc to use.")]
	public MFnum.ArcType defaultArc;
	[Tooltip("When also using intercept - the number of low arc ballistic computations to run.\nMore = better accuracy, but more cpu usage.")]
	public int ballisticIterations = 1;
	[Tooltip("When also using intercept - the number of high arc ballistic computations to run.\nMore = better accuracy, but more cpu usage.\n" +
		"High arc generally requires more iterations to hit the target.")]
	public int highArcBallisticIterations = 5;
	[Header("Turning Properties: (1-720)")]
	[Tooltip("(deg/sec)\nBest within 1-720 as jitter begins to occur at high rotation/elevation rates. " +
			 "Conider using Constant Turn Rate with high motion rates, as this will eliminate any jitter.\n" +
	         "Also used to adjust pitch for rotation sounds.")]
	[Range(1, 720)]
	public float rotationRateMax = 50f;
	[Tooltip("(deg/sec)\nBest within 1-720 as jitter begins to occur at high rotation/elevation rates. " +
	         "Conider using Constant Turn Rate with high motion rates, as this will eliminate jitter.\n" +
	         "Also used to adjust pitch for elevation sounds.")]
	[Range(1, 720)]
	public float elevationRateMax = 50f;
	[Tooltip("(deg/sec)\nBest within 1-720 as jitter begins to occur at high rotation/elevation rates. " +
	         "Conider using Constant Turn Rate with high motion rates, as this will eliminate jitter.")]
	[Range(1, 720)]
	public float rotationAccel = 50f;
	[Tooltip("(deg/sec)\nBest within 1-720 as jitter begins to occur at high rotation/elevation rates. " +
	         "Conider using Constant Turn Rate with high motion rates, as this will eliminate jitter.")]
	[Range(1, 720)]
	public float elevationAccel = 50f;
	[Tooltip("Multiplier. Causes decelleration to be different than accelleration.")]
	public float decelerateMult = 1f;
	[Tooltip("Best for fast precision turning, or to eliminate any jitter resulting from high-speed rotation/elevation.\n" +
	         "Rotation Accel and Elevation Accel used as rates.")]
	public bool constantTurnRate;
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
	[Header("Aiming Error:")]
	[Tooltip("(deg radius)\nWill cause the turret to point in a random offset from target. Recomend value less than 1.")]
	public float aimError; // 0 = no error, will skip aimError routine if turningAimInaccuracy is also 0
	[Tooltip("How often to generate a new error offset.\n0 = every frame, but only if Aim Error is greater than 0.")]
	public float aimErrorInterval = 0f;
//	[Header("Turret aim inaccuracy due to turn/elevate: ")]
//	[Tooltip("(additive deg radius, per deg of motion/sec)\nRecommend small values, " +
//				"as motion resulting from aiming at the error location causes more motion, causing more error, etc.\n" +
//				"Uses Aim Error Interval and added to any Aim Error. ")]
//	public float turningAimInaccuracy; // additive per degree per second
	[Header("Weapon inaccuracy due to turn/elevate:")]
	[Tooltip("(additive deg radius, per deg of motion/sec)\nWill not affect turret aim; error is sent to weapons. Recommend small values.")]
	public float turningWeapInaccuracy; // additive per degree per second
	[Header("Parts:")]
	[Tooltip("Object that rotates around the y axis.")]
	public Transform rotator;
	[Tooltip("Object that rotates around the x axis.")]
	public Transform elevator;
	public AudioObject rotatorSound;
	public AudioObject elevatorSound;

	[HideInInspector] public Vector3 systAimError;
	[HideInInspector] public float averageRotRateEst;
	[HideInInspector] public float averageEleRateEst;
	[HideInInspector] public float totalTurnWeapInaccuracy;
//	[HideInInspector] public float totalTurnAimInaccuracy;
	[HideInInspector] public MFnum.ArcType curArc;

	float timeSinceTarget;
	float curRotRate;
	float curEleRate;
	float _curRotSeperation;
	float _curEleSeperation;
	float lastRotSeperation;
	float lastEleSeperation;
	Quaternion lastRotation;
	Quaternion lastElevation;
	float lastRotRateEst;
	float lastEleRateEst;
	float lastAimError;
	Vector3 targetLoc;
	Vector3 rotatorPlaneTarget;
	Vector3 elevatorPlaneTarget;

	[System.Serializable]
	public class AudioObject {
		public AudioSource audioSource;
		public float pitchMax = 1;
		public float pitchMin;
		[HideInInspector] public float stopTime;
	}

	public override void Start () {
		if (CheckErrors() == true) { return; }
		base.Start();

		lastRotation = rotator.localRotation;
		lastElevation = elevator.localRotation;
		timeSinceTarget = -restDelay;
		curArc = defaultArc;
	}
	
	public override void Update () {
		if (error == true) { return; }
		base.Update(); // AimLocation() called from base

		// find rotation/elevation rates
		float _useTime;
		if (Time.time < .5) { 
//			_useTime = Time.deltaTime; // to avoid smoothDeltaTime returning NaN on the first few frames
			return; // avoids initial movement spurt
		} else {
			_useTime = Time.smoothDeltaTime;
		}
		float _curRotRateEst = Quaternion.Angle( rotator.localRotation, lastRotation ) / _useTime;
		float _curEleRateEst = Quaternion.Angle( elevator.localRotation, lastElevation ) / _useTime;
		// average rotation, then move to lastRotation
		averageRotRateEst = (_curRotRateEst + lastRotRateEst) / 2f;
		lastRotRateEst = _curRotRateEst;
		lastRotation = rotator.localRotation;
		// average elevation, then move to lastElevation
		averageEleRateEst = (_curEleRateEst + lastEleRateEst) / 2f;
		lastEleRateEst = _curEleRateEst;
		lastElevation = elevator.localRotation;
		// find inaccuracy caused by rotation/elevation. (using .03 to minimize influence from spikes)
		if ( averageRotRateEst > rotationAccel * .03f || averageEleRateEst > elevationAccel * .03f ) {
			totalTurnWeapInaccuracy = turningWeapInaccuracy * (averageRotRateEst + averageEleRateEst);
//			totalTurnAimInaccuracy = turningAimInaccuracy * (averageRotRateEst + averageEleRateEst);
		} else { 
			totalTurnWeapInaccuracy = 0f;
//			totalTurnAimInaccuracy = 0f;
		}
		
		// move aim error location
//		if ( aimError + totalTurnAimInaccuracy > 0 ) {
		if ( aimError > 0 ) {
			if ( Time.time >= lastAimError + aimErrorInterval ) {
				systAimError = Random.insideUnitSphere;
				lastAimError = Time.time;
			}
		}

		// find aim locations
		if ( _target ) {
			Vector3 _localTarget;
			timeSinceTarget = Time.time;
			// move apparent location of target due to turret aim error
//			if ( aimError + totalTurnAimInaccuracy > 0 ) {
			if ( aimError > 0 ) {
//				targetAimLocation = targetAimLocation + ((systAimError * (aimError + totalTurnAimInaccuracy) * targetRange) / 57.3f ); // 57.3 is the distance where 1 unit offset appears as 1° offset
				targetAimLocation = targetAimLocation + ((systAimError * aimError * targetRange) / 57.3f ); // 57.3 is the distance where 1 unit offset appears as 1° offset
			}
			
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
				rotatorPlaneTarget = rotator.position + (Quaternion.AngleAxis(restAngle.y, transform.up) * transform.forward * 1000f);
				elevatorPlaneTarget = elevator.position + (Quaternion.AngleAxis(restAngle.x, -rotator.right) * rotator.forward * 1000f);
			}
		}
		
		// turning
		_curRotSeperation = MFmath.AngleSigned(rotator.forward, rotatorPlaneTarget - weaponMount.position, transform.up);
		_curEleSeperation = MFmath.AngleSigned(elevator.forward, elevatorPlaneTarget - weaponMount.position, -rotator.right);
		
		// turn opposite if shortest route is through a gimbal limit
		float _bearing = MFmath.AngleSigned(transform.forward, rotatorPlaneTarget - weaponMount.position, transform.up);
		float _aimAngle = MFmath.AngleSigned(transform.forward, rotator.forward, rotator.up);
		float _modAccel = rotationAccel;
		float _modSeperation = _curRotSeperation;
		if (limitLeft > -180 || limitRight < 180) { // is there a gimbal limit?
			if ( (_curRotSeperation < 0 && _bearing > 0 && _aimAngle < 0)  ||  (_curRotSeperation > 0 && _bearing < 0 && _aimAngle > 0) ) { // is shortest turn angle through a limit?
				// reverse turn
				_modAccel *= -1f; // for constant turn rate mode
				_modSeperation *= -1f; // for smooth turn rate mode
			}
		}

		Quaternion _rot;
		if (constantTurnRate == true) { // no variation in rotation speed. more accurate, lightweight, less realistic.
			// apply rotation
			_rot = Quaternion.LookRotation( rotatorPlaneTarget - rotator.position, transform.up );
			rotator.rotation = Quaternion.RotateTowards( rotator.rotation, _rot, Mathf.Min(_modAccel, rotationRateMax) * Time.deltaTime );
			
			// apply elevation
			_rot = Quaternion.LookRotation( elevatorPlaneTarget - weaponMount.position, rotator.up );
			elevator.rotation = Quaternion.RotateTowards( elevator.rotation, _rot, Mathf.Min(elevationAccel, elevationRateMax) * Time.deltaTime );
			
		} else { // accellerate/decellerate to rotation goal
			// find closure rate of angle to target. 
			float _closureTurnRate = (_curRotSeperation - lastRotSeperation) / Time.deltaTime;
			float _closureEleRate = (_curEleSeperation - lastEleSeperation) / Time.deltaTime;
			// store current rate and seperation to become last rate/seperation for next time
			lastRotSeperation = _curRotSeperation;
			lastEleSeperation = _curEleSeperation;

			// apply rotation
			curRotRate = MFcompute.SmoothRateChange(_modSeperation, _closureTurnRate, curRotRate, rotationAccel, decelerateMult, rotationRateMax);
			rotator.rotation = Quaternion.AngleAxis(curRotRate * Time.deltaTime, transform.up) * rotator.rotation;
			
			// apply elevation
			curEleRate = MFcompute.SmoothRateChange(_curEleSeperation, _closureEleRate, curEleRate, elevationAccel, decelerateMult, elevationRateMax);
			elevator.rotation = Quaternion.AngleAxis(curEleRate * Time.deltaTime, -rotator.right) * elevator.rotation; 
		}
		
		CheckGimbalLimits();
		
		// prevent nonsence
		rotator.localEulerAngles = new Vector3( 0f, rotator.localEulerAngles.y, 0f );
		elevator.localEulerAngles = new Vector3( elevator.localEulerAngles.x, 0f, 0f );

		// turn sounds	
		if (rotatorSound.audioSource) {
			TurnSound( rotatorSound, averageRotRateEst, rotationRateMax );
		}
		if (elevatorSound.audioSource) {
			TurnSound( elevatorSound, averageEleRateEst, elevationRateMax );
		}
	}

	void TurnSound ( AudioObject ao, float rate, float rateMax ) {
		if ( rate > rateMax * .02f ) { // avoid near 0 values
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
			curRotRate = 0f;
		}
		if (rotator.localEulerAngles.y < 360+limitLeft && rotator.localEulerAngles.y >= 180) {
			rotator.localEulerAngles = new Vector3( rotator.localEulerAngles.x, 360+limitLeft, rotator.localEulerAngles.z );
			curRotRate = 0f;
		}
		if (elevator.localEulerAngles.x > -limitDown && elevator.localEulerAngles.x <=180) {
			elevator.localEulerAngles = new Vector3( -limitDown, elevator.localEulerAngles.y, elevator.localEulerAngles.z );
			curEleRate = 0f;
		}
		if (elevator.localEulerAngles.x < 360-limitUp && elevator.localEulerAngles.x >= 180) {
			elevator.localEulerAngles = new Vector3( 360-limitUp, elevator.localEulerAngles.y, elevator.localEulerAngles.z );
			curEleRate = 0f;
		}
	}
	
	// tests if a given target is within the gimbal limits of this turret 
	public override bool TargetWithinLimits ( Transform targ ) {
		if ( error == true ) { return false; }
		if ( targ ) {
			// find target's location in rotation plane
			Vector3 _localTarget = rotator.InverseTransformPoint( targ.position ); 
			Vector3 rotatorPlaneTarget = rotator.TransformPoint( new Vector3(_localTarget.x, 0f, _localTarget.z) );

			float _testAngle = MFmath.AngleSigned( transform.forward, rotatorPlaneTarget - transform.position, transform.up );
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

	public override Vector3 AimLocation() {
		// intercept and ballistics
		if ( _target ) {
			Vector3 _targetVelocity = Vector3.zero;
			targetLoc = _target.position;
			if ( useIntercept == true && shotSpeed != null ) { 
				// target velocity
				if (targetRigidbody) { // if target has a rigidbody, use velocity
					_targetVelocity = targetRigidbody.velocity;
				} else { // otherwise compute velocity from change in position
					_targetVelocity = ( targetLoc - lastTargetPosition ) / Time.deltaTime;
					lastTargetPosition = targetLoc;
				}
			}
			
			if ( useGravity == true && Physics.gravity.y != 0 && shotSpeed != null ) { // ballistic aim
				int _factor = -Physics.gravity.y > 0 ? 1 : -1;
				// find initial aim angle
				float? _ballAim = MFball.BallisticAimAngle( targetLoc, exitLoc, (float)shotSpeed, curArc );
				if ( _ballAim == null ) {
					target = null;
				} else {
					if ( useIntercept == true && playerControl == false ) { // ballistic + intercept
						// iterate for better ballistic accuracy when also using intercept
						int bi = 0;
						int biMax;
						if ( curArc == MFnum.ArcType.High ) {
							biMax = highArcBallisticIterations;
						} else {
							biMax = ballisticIterations;
						}
						while ( _target && bi++ < biMax ) {
							//							_ballAim = BallisticIteration ( exitLoc, (float)shotSpeed, (float)_ballAim, _targetVelocity );
							_ballAim = MFball.BallisticIteration ( exitLoc, (float)shotSpeed, (float)_ballAim, curArc, _target, _targetVelocity, velocity, targetLoc, out targetLoc );
							if ( _ballAim == null ) { target = null; } // no solution, release target
						}
					}
					if ( _target ) { // target can become null in balistic iteration if no solution
						Vector3 _cross = -Vector3.Cross( ( targetLoc - exitLoc ), Vector3.up );
						Quaternion _eleAngleDir = Quaternion.AngleAxis( _factor * (float)_ballAim * Mathf.Rad2Deg, -_cross );
						targetAimLocation = exitLoc + (( _eleAngleDir * Vector3.Cross( _cross, Vector3.up ) ).normalized * targetRange );
					}
				}
			} else { // no gravity
				if ( useIntercept == true && playerControl == false && shotSpeed != null ) {
					// point at linear intercept position
					Vector3? _interceptAim = MFcompute.Intercept( exitLoc, velocity, (float)shotSpeed, targetLoc, _targetVelocity );
					if ( _interceptAim == null ) {
						target = null;
					} else {
						targetAimLocation = (Vector3)_interceptAim;
					}
				} else { // point at target position
					targetAimLocation = _target.position;
				}
			}
		} else {
			targetAimLocation = Vector3.zero;
		}
		return targetAimLocation;
	}

	bool CheckErrors () {
		error = false;

		if (rotator == false) { Debug.Log( this+": Turret rotator part hasn't been defined."); error = true; }
		if (elevator == false) { Debug.Log( this+": Turret elevator part hasn't been defined."); error = true; }
		if (weaponMount == false) { Debug.Log( this+": Turret weapon mount part hasn't been defined."); error = true; }
		if (decelerateMult <= 0) { Debug.Log( this+": Turret decelerateMult must be > 0"); error = true; }
		if (turningWeapInaccuracy < 0) { Debug.Log( this+": Turret turningWeapInaccuracy must be >= 0"); error = true; }
		if (!Mathf.Approximately( transform.localScale.x, transform.localScale.z ) ) { Debug.Log( this+" Turret transform x and z scales must be equal."); error = true; }
		if (rotator) {
			if (rotator.localScale.x != 1) { Debug.Log( this+" rotator part transform xyz scales must = 1."); error = true; }
			if (rotator.localScale.y != 1) { Debug.Log( this+" rotator part transform xyz scales must = 1."); error = true; }
			if (rotator.localScale.z != 1) { Debug.Log( this+" rotator part transform xyz scales must = 1."); error = true; }
		}
		
		return error;
	}
}
