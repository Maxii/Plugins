using UnityEngine;
using System.Collections;

public class ST_Turret : MF_AbstractPlatform {
	[Header("Aiming Options:")]
	[Tooltip("Turret will aim ahead to hit a moving target.")]
	public bool useIntercept = true;
	[Tooltip("Will aim shots as if they are affected by gravity.")]
	public bool useGravity;
	[Tooltip("When using gravity, will use the solution with the highest arc.")]
	public bool useHighArc;
	[Header("Turning Properties: (1-1000)")]
	[Tooltip("(deg/sec)\nBest within 1-1000 as jitter begins to occur at high rotation/elevation rates. " +
			 "Conider using Constant Turn Rate with high motion rates, as this will eliminate any jitter.\n" +
	         "Also used to adjust pitch for rotation sounds.")]
	[Range(1, 1000)]
	public float rotationRateMax = 50f;
	[Tooltip("(deg/sec)\nBest within 1-1000 as jitter begins to occur at high rotation/elevation rates. " +
	         "Conider using Constant Turn Rate with high motion rates, as this will eliminate jitter.\n" +
	         "Also used to adjust pitch for elevation sounds.")]
	[Range(1, 1000)]
	public float elevationRateMax = 50f;
	[Tooltip("(deg/sec)\nBest within 1-1000 as jitter begins to occur at high rotation/elevation rates. " +
	         "Conider using Constant Turn Rate with high motion rates, as this will eliminate jitter.")]
	[Range(1, 1000)]
	public float rotationAccel = 50f;
	[Tooltip("(deg/sec)\nBest within 1-1000 as jitter begins to occur at high rotation/elevation rates. " +
	         "Conider using Constant Turn Rate with high motion rates, as this will eliminate jitter.")]
	[Range(1, 1000)]
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
	public Vector2 restAngle;
	[Header("Aiming Error:")]
	[Tooltip("(deg radius)\nWill cause the turret to point in a random offset from target. Recomend value less than 1.")]
	public float aimError; // 0 = no error, will skip aimError routine if turningAimInaccuracy is also 0
	[Tooltip("How often to generate a new error offset.\n0 = every frame, but only if Aim Error or Turning Aim Inaccuracy are greater than 0.")]
	public float aimErrorInterval = 0f;
	[Header("Turret aim inaccuracy due to turn/elevate: ")]
	[Tooltip("(additive deg radius, per deg of motion/sec)\nRecommend small values, " +
				"as motion resulting from aiming at the error location causes more motion, causing more error, etc.\n" +
				"Uses Aim Error Interval and added to any Aim Error. ")]
	public float turningAimInaccuracy; // additive per degree per second
	[Header("Weapon inaccuracy due to turn/elevate:")]
	[Tooltip("(additive deg radius, per deg of motion/sec)\nWill not affect turret aim; error is sent to weapons. Recommend small values.")]
	public float turningWeapInaccuracy; // additive per degree per second
	[Header("Parts:")]
	public GameObject rotator = null;
	public GameObject elevator = null;
	public AudioSource rotatorSound = null;
	public AudioSource elevatorSound = null;
	[Tooltip("Sound will play slower or faster with motion variation. Normal play speed is at 1/2 of Max Rotation/Elevation.\n" +
	         "0 = no pitch variation."	)]
	public float soundPitchRange = .5f;
	[Header("Object holding turret:")]
	[Tooltip("Will impart any velocity to fired shots. Rigidbody is not necessary. If turret platform/vehicle/parent is stationary, may leave empty:")]
	public GameObject platform;

	[HideInInspector] public float targetRange;
	[HideInInspector] public Vector3 platformVelocity;
	[HideInInspector] public Vector3 systAimError;
	[HideInInspector] public float averageRotRateEst;
	[HideInInspector] public float averageEleRateEst;
	[HideInInspector] public float totalTurnWeapInaccuracy;
	[HideInInspector] public float totalTurnAimInaccuracy;
	
	MF_BasicScanner scannerScript;
	float curRotRate;
	float curEleRate;
	float _curRotSeperation;
	float _curEleSeperation;
	float lastRotSeperation;
	float lastEleSeperation;
	Vector3 lastTargetPosition;
	Vector3 lastPlatformPosition;
	Quaternion lastRotation;
	Quaternion lastElevation;
	float lastRotRateEst;
	float lastEleRateEst;
	float lastAimError;
	Vector3? _interceptAim;
	Vector3 targetLoc;
	bool error;
	
	void Start () {
		if (CheckErrors() == true) { return; }
		lastRotation = rotator.transform.localRotation;
		lastElevation = elevator.transform.localRotation;
	}
	
	void Update () {
		if (error == true) { return; }
		
		// find rotation/elevation rates
		float _useTime;
		if (Time.time < 1) { // to avoid smoothDeltaTime returning NaN on the first few frames
			_useTime = Time.deltaTime;
		} else {
			_useTime = Time.smoothDeltaTime;
		}
		float _curRotRateEst = Quaternion.Angle( rotator.transform.localRotation, lastRotation ) / _useTime;
		float _curEleRateEst = Quaternion.Angle( elevator.transform.localRotation, lastElevation ) / _useTime;
		// average rotation, then move to lastRotation
		averageRotRateEst = (_curRotRateEst + lastRotRateEst) / 2f;
		lastRotRateEst = _curRotRateEst;
		lastRotation = rotator.transform.localRotation;
		// average elevation, then move to lastElevation
		averageEleRateEst = (_curEleRateEst + lastEleRateEst) / 2f;
		lastEleRateEst = _curEleRateEst;
		lastElevation = elevator.transform.localRotation;
		// find inaccuracy caused my rotation/elevation. (using .03 to minimize influence from spikes)
		if ( averageRotRateEst > rotationAccel * .03f || averageEleRateEst > elevationAccel * .03f ) {
			totalTurnWeapInaccuracy = turningWeapInaccuracy * (averageRotRateEst + averageEleRateEst);
			totalTurnAimInaccuracy = turningAimInaccuracy * (averageRotRateEst + averageEleRateEst);
		} else { 
			totalTurnWeapInaccuracy = 0f;
			totalTurnAimInaccuracy = 0f;
		}
		
		// move aim error location
		if ( aimError + totalTurnAimInaccuracy > 0 ) {
			if ( Time.time >= lastAimError + aimErrorInterval ) {
				systAimError = Random.insideUnitSphere;
				lastAimError = Time.time;
			}
		}

		// used in various calculations
		if (target) {
			targetRange = Vector3.Distance( exitLoc, target.transform.position );
		}
		
		// intercept and ballistics
		if (target) {
			Vector3 _targetVelocity = Vector3.zero;
			targetLoc = target.transform.position;
			if ( useIntercept == true ) { // gather velocity information
				if (platform) { // check if turret/platform has been provided
					if (platform.rigidbody) { // if has a rigidbody, use velocity
						platformVelocity = platform.rigidbody.velocity;
					} else { // otherwise compute velocity from change in position
						platformVelocity = (platform.transform.position - lastPlatformPosition) / Time.deltaTime;
						lastPlatformPosition = platform.transform.position;
					}	
				} else { // otherwise assume turret is stationary
					platformVelocity = Vector3.zero;
				}
				if (target.rigidbody) { // if target has a rigidbody, use velocity
					_targetVelocity = target.rigidbody.velocity;
				} else { // otherwise compute velocity from change in position
					_targetVelocity = (targetLoc - lastTargetPosition) / Time.deltaTime;
					lastTargetPosition = targetLoc;
				}
			}

			if ( useGravity == true && Physics.gravity.y != 0 ) {
				int _factor = -Physics.gravity.y > 0 ? 1 : -1;
				// find initial aim angle
				float? _ballAim = MFcompute.BallisticAimAngle( targetLoc, exitLoc, shotSpeed, useHighArc );
				if ( _ballAim == null ) {
					target = null;
				} else {
					if ( useIntercept == true ) {
						_ballAim = BallisticIteration ( exitLoc, shotSpeed, (float)_ballAim, _targetVelocity );
						if ( useHighArc == true ) { // iterate for better high arc accuracy
							int bi = 0;
							int biMax = 4;
							while ( target && bi++ < biMax ) {
								_ballAim = BallisticIteration ( exitLoc, shotSpeed, (float)_ballAim, _targetVelocity );
							}
						}
					}
					if (target) {
						targetLocation = ((Quaternion.AngleAxis( _factor * (float)_ballAim * Mathf.Rad2Deg, Vector3.Cross((targetLoc - exitLoc), Vector3.up) ) * 
						                   (new Vector3(targetLoc.x, exitLoc.y, targetLoc.z) - exitLoc) ).normalized * targetRange ) + exitLoc;
					}
				}
			} else {
				if ( useIntercept == true ) {
					// point at linear intercept position
					_interceptAim = MFcompute.Intercept(exitLoc, platformVelocity, shotSpeed, targetLoc, _targetVelocity);
					if ( _interceptAim == null ) {
						target = null;
					} else {
						targetLocation = (Vector3)_interceptAim;
					}
				} else { // point at target position
					targetLocation = target.transform.position;
				}
			}
		}
		
		// find aim locations
		Vector3 _localTarget;
		Vector3 _rotatorPlaneTarget;
		float _xzDist;
		Vector3 _elevatorPlaneTarget;
		if (target) {
			// move apparent location of target due to turret aim error
			if ( aimError + totalTurnAimInaccuracy > 0 ) {
				Quaternion errorRotation = Quaternion.Euler( systAimError * (aimError + totalTurnAimInaccuracy) );
				targetLocation = exitLoc + (errorRotation * (targetLocation - exitLoc)).normalized * targetRange; // **** keep this normalized ??
			}
			
			// find target's location in rotation plane
			_localTarget = rotator.transform.InverseTransformPoint( targetLocation ); 
			_rotatorPlaneTarget = rotator.transform.TransformPoint( new Vector3(_localTarget.x, 0f, _localTarget.z) );
			// find target's location in elevation plane as if rotator is already facing target, as rotation will eventualy bring it to front. (don't elevate past 90/-90 degrees to reach target)
			_xzDist = Vector3.Distance( rotator.transform.position, _rotatorPlaneTarget );
			_elevatorPlaneTarget = rotator.transform.TransformPoint( new Vector3(0f, _localTarget.y, _xzDist / transform.localScale.z) ); 
			
		} else { // no target
			// set rotation and elevation goals to the rest position
			_rotatorPlaneTarget = rotator.transform.position + (Quaternion.AngleAxis(restAngle.y, rotator.transform.up) * transform.forward * 1000f);
			_elevatorPlaneTarget = elevator.transform.position + (Quaternion.AngleAxis(restAngle.x, -elevator.transform.right) * rotator.transform.forward * 1000f);
		}
		
		// turning
		_curRotSeperation = MFmath.AngleSigned(rotator.transform.forward, _rotatorPlaneTarget - weaponMount.transform.position, rotator.transform.up);
		_curEleSeperation = MFmath.AngleSigned(elevator.transform.forward, _elevatorPlaneTarget - weaponMount.transform.position, -elevator.transform.right);
		
		// turn opposite if shortest route is through a gimbal limit
		float _bearing = MFmath.AngleSigned(transform.forward, _rotatorPlaneTarget - weaponMount.transform.position, rotator.transform.up);
		float _aimAngle = MFmath.AngleSigned(transform.forward, rotator.transform.forward, rotator.transform.up);
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
			_rot = Quaternion.LookRotation( _rotatorPlaneTarget - rotator.transform.position, rotator.transform.up );
			rotator.transform.rotation = Quaternion.RotateTowards( rotator.transform.rotation, _rot, Mathf.Min(_modAccel, rotationRateMax) * Time.deltaTime );
			
			// apply elevation
			_rot = Quaternion.LookRotation( _elevatorPlaneTarget - weaponMount.transform.position, rotator.transform.up );
			elevator.transform.rotation = Quaternion.RotateTowards( elevator.transform.rotation, _rot, Mathf.Min(elevationAccel, elevationRateMax) * Time.deltaTime );
			
		} else { // accellerate/decellerate to rotation goal
			// find closure rate of angle to target. 
			float _closureTurnRate = (_curRotSeperation - lastRotSeperation) / Time.deltaTime;
			float _closureEleRate = (_curEleSeperation - lastEleSeperation) / Time.deltaTime;
			// store current rate and seperation to become last rate/seperation for next time
			lastRotSeperation = _curRotSeperation;
			lastEleSeperation = _curEleSeperation;
			
			// apply rotation
			curRotRate = MFcompute.SmoothRateChange(_modSeperation, _closureTurnRate, curRotRate, rotationAccel, decelerateMult, rotationRateMax);
			rotator.transform.rotation = Quaternion.AngleAxis(curRotRate * Time.deltaTime, rotator.transform.up) * rotator.transform.rotation;
			
			// apply elevation
			curEleRate = MFcompute.SmoothRateChange(_curEleSeperation, _closureEleRate, curEleRate, elevationAccel, decelerateMult, elevationRateMax);
			elevator.transform.rotation = Quaternion.AngleAxis(curEleRate * Time.deltaTime, -elevator.transform.right) * elevator.transform.rotation; 
		}
		
		CheckGimbalLimits();
		
		// prevent nonsence
		rotator.transform.localEulerAngles = new Vector3( 0f, rotator.transform.localEulerAngles.y, 0f );
		elevator.transform.localEulerAngles = new Vector3( elevator.transform.localEulerAngles.x, 0f, 0f );
		
		// turn sounds	
		if (rotatorSound) {
			TurnSound( rotatorSound, soundPitchRange, averageRotRateEst, rotationRateMax, rotationAccel );
		}
		if (elevatorSound) {
			TurnSound( elevatorSound, soundPitchRange, averageEleRateEst, elevationRateMax, elevationAccel );
		}
	}

	void TurnSound ( AudioSource sound, float pitchRange, float avgRateEst, float rateMax, float accel ) {
		if ( avgRateEst > accel * .03f ) {
			if ( sound.isPlaying == false ) {
				sound.PlayDelayed(.1f);
			}
			sound.pitch = 1f + (( (avgRateEst - (rateMax * .5f)) / rateMax ) * (pitchRange * 2f)); 
		} else {
			sound.Stop();
		}
	}
	
	// checks if turret has moved outside of its gimbal limits. If so, it puts it back to the appropriate limit
	void CheckGimbalLimits () {
		if (error == true) { return; }
		if (rotator.transform.localEulerAngles.y > limitRight && rotator.transform.localEulerAngles.y <=180) {
			rotator.transform.localEulerAngles = new Vector3( rotator.transform.localEulerAngles.x, limitRight, rotator.transform.localEulerAngles.z );
			curRotRate = 0f;
		}
		if (rotator.transform.localEulerAngles.y < 360+limitLeft && rotator.transform.localEulerAngles.y >= 180) {
			rotator.transform.localEulerAngles = new Vector3( rotator.transform.localEulerAngles.x, 360+limitLeft, rotator.transform.localEulerAngles.z );
			curRotRate = 0f;
		}
		if (elevator.transform.localEulerAngles.x > -limitDown && elevator.transform.localEulerAngles.x <=180) {
			elevator.transform.localEulerAngles = new Vector3( -limitDown, elevator.transform.localEulerAngles.y, elevator.transform.localEulerAngles.z );
			curEleRate = 0f;
		}
		if (elevator.transform.localEulerAngles.x < 360-limitUp && elevator.transform.localEulerAngles.x >= 180) {
			elevator.transform.localEulerAngles = new Vector3( 360-limitUp, elevator.transform.localEulerAngles.y, elevator.transform.localEulerAngles.z );
			curEleRate = 0f;
		}
	}
	
	// tests if a given target is within the gimbal limits of this turret 
	public override bool TargetWithinLimits ( Transform target ) {
		if (error == true) { return false; }
		if (target) {
			// find target's location in rotation plane
			Vector3 _localTarget = rotator.transform.InverseTransformPoint( target.position ); 
			Vector3 _rotatorPlaneTarget = rotator.transform.TransformPoint( new Vector3(_localTarget.x, 0f, _localTarget.z) );
			
			float _angle = MFmath.AngleSigned(transform.forward, _rotatorPlaneTarget - transform.position, rotator.transform.up);
			if (_angle > limitRight || _angle < limitLeft) {
				return false;
			}
			
			// find target's location in elevation plane as if rotator is already facing target, as rotation will eventualy bring it to front. (don't elevate past 90/-90 degrees to reach target)
			float _xzDist = Vector3.Distance( rotator.transform.position, _rotatorPlaneTarget );
			Vector3 _elevatorPlaneTarget = rotator.transform.TransformPoint( new Vector3( 0f, _localTarget.y, _xzDist / transform.localScale.z ) ); 
			
			_angle = MFmath.AngleSigned(rotator.transform.forward, _elevatorPlaneTarget - elevator.transform.position, -elevator.transform.right);
			if (_angle < limitDown || _angle > limitUp) {
				return false;
			}   
			return true;
		} else {
			return false;
		}
	}
	
	// check if the turret is aimed at the target
	public bool AimCheck ( float targetSize, float aimTolerance ) {
		if (error == true) { return false; }
		// note that large values of aim error and small targets can cause this check to fail. (as it should) Compensate with higher aimTolerance
		bool _ready = false;
		if (target) {
			if ( targetRange == 0 ) { // assume target range hasn't been evaluated yet. This can happen due to timing of other scripts upon gaining an intital target
				targetRange = Vector3.Distance( weaponMount.transform.position, target.transform.position );
			}
			float _targetFovRadius = Mathf.Clamp(   (Mathf.Atan( (targetSize / 2f) / targetRange ) * Mathf.Rad2Deg) + aimTolerance,    0, 180 );
			if ( Vector3.Angle(weaponMount.transform.forward, targetLocation - weaponMount.transform.position) <= _targetFovRadius ) {
				_ready = true;
			}
		}
		return _ready;
	}

	float? BallisticIteration ( Vector3 exitLoc, float shotSpeed, float aimRad, Vector3 targetVelocity ) {
		float? _ballAim = null;
		// find new flight time
		float? _flightTime = MFcompute.BallisticFlightTime( targetLoc, exitLoc, shotSpeed, aimRad, useHighArc );
		float _effectiveShotSpeed = Vector3.Distance(transform.position, targetLoc) / (float)_flightTime;
		// find intercept based on new _effectiveShotSpeed
		Vector3? _intAim = MFcompute.Intercept(exitLoc, platformVelocity, _effectiveShotSpeed, target.transform.position, targetVelocity);
		if ( _intAim == null ) {
			target = null;
		} else {
			targetLoc = (Vector3)_intAim;
			// re-calculate ballistic trajectory based on intercept point
			_ballAim = MFcompute.BallisticAimAngle( targetLoc, exitLoc, shotSpeed, useHighArc );
			if ( _ballAim == null ) {
				target = null;
			}
		}
		return _ballAim;
	}
	
	bool CheckErrors () {
		error = false;
		string _object = gameObject.name;
		if (rotator == false) { Debug.Log(_object+": Turret rotator part hasn't been defined."); error = true; }
		if (elevator == false) { Debug.Log(_object+": Turret elevator part hasn't been defined."); error = true; }
		if (weaponMount == false) { Debug.Log(_object+": Turret weapon mount part hasn't been defined."); error = true; }
		if (decelerateMult <= 0) { Debug.Log(_object+": Turret decelerateMult must be > 0"); error = true; }
		if (turningWeapInaccuracy < 0) { Debug.Log(_object+": Turret turningWeapInaccuracy must be >= 0"); error = true; }
		if (transform.localScale.x != transform.localScale.z) { Debug.Log(_object+" Turret x and z transform scale must be equal."); error = true; }
		if (rotator) {
			if (rotator.transform.localScale.x != 1) { Debug.Log(_object+" rotator part transform x scale must = 1."); error = true; }
			if (rotator.transform.localScale.y != 1) { Debug.Log(_object+" rotator part transform y scale must = 1."); error = true; }
			if (rotator.transform.localScale.z != 1) { Debug.Log(_object+" rotator part transform z scale must = 1."); error = true; }
		}
		
		return error;
	}
}
