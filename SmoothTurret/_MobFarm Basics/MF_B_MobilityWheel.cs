using UnityEngine;
using System.Collections;

public class MF_B_MobilityWheel : MF_AbstractMobility {

	[Tooltip("Percentage of the throttle to apply per second. Use to avoin spinning wheels due to abrupt accelleration.")]
	[Range( 0f, 1f )] public float throttleRate;
	public WheelObject[] wheels;
	
	float curThrottle;
	bool error;

	[System.Serializable]
	public class WheelObject {
		public WheelCollider wheelCollider;
		public float torque;
		[Tooltip("How far a wheel may turn in degrees.")]
		[Range( 0, 90 )] public float steerAngleMax;
		[Tooltip("(deg. / sec.)\nHow fast the wheel can turn. Negative values will cause the wheel to turn the opposite direction. This is usefull for rear wheel steering.")]
		public float steerRate;
		[Tooltip("The visual representation of the wheel.")]
		public Transform wheelObject;
	}

	public override void Start () {
		if ( CheckErrors() == true ) { return; }
		base.Start();

		for ( int w=0; w < wheels.Length; w++ ) {
			if ( !wheels[w].wheelObject ) {
				if ( wheels[w].wheelCollider.transform.childCount > 0 ) {
					wheels[w].wheelObject = wheels[w].wheelCollider.transform.GetChild(0);
				}
			}
		}
	}

	// modifies the Vector3 to be same height as unit - allows the vehicle to project the waypoint/target to the height of ground
	public override Vector3 ModifiedPosition ( Vector3 pos ) {
		return new Vector3( pos.x, transform.position.y, pos.z );
	}

	public override void FixedUpdate () {
		if ( error == true ) { return; }
		base.FixedUpdate();

		if (_navTarget) {
			Steer( navTargetAim );
			Move( 1f );
		} else {
			Steer( transform.position + transform.forward );
			Move( 0f );
		}
	}

	public void Steer ( Vector3 location ) {
		Vector3 _navPlaneLoc = ModifiedPosition( location );
		float _angle = MFmath.AngleSigned( transform.forward, _navPlaneLoc - transform.position, transform.up );

		for ( int w=0; w < wheels.Length; w++ ) {
			float _rateFactor = wheels[w].steerRate >= 0 ? 1 : -1; // used for opposite (negative) steering, used for rear wheel steering
			if ( wheels[w].steerAngleMax > 0 ) {
				if ( Mathf.Abs( wheels[w].wheelCollider.steerAngle - ( _angle * _rateFactor ) ) <= Mathf.Abs( wheels[w].steerRate * Time.fixedDeltaTime ) ) { // goal angle is within rate of steering
					wheels[w].wheelCollider.steerAngle = _angle * _rateFactor; // don't overshoot
				} else { // steer towards goal angle
					if ( wheels[w].wheelCollider.steerAngle * _rateFactor > _angle ) {
						wheels[w].wheelCollider.steerAngle -= wheels[w].steerRate * Time.fixedDeltaTime;
					} else {
						wheels[w].wheelCollider.steerAngle += wheels[w].steerRate * Time.fixedDeltaTime;
					}
				}
			}


			// limit steer angles
			if ( wheels[w].wheelCollider.steerAngle > wheels[w].steerAngleMax ) {
				wheels[w].wheelCollider.steerAngle = wheels[w].steerAngleMax;
			}
			if ( wheels[w].wheelCollider.steerAngle < -wheels[w].steerAngleMax ) {
				wheels[w].wheelCollider.steerAngle = -wheels[w].steerAngleMax;
			}

			// position and rotate wheel meshes
			if ( wheels[w].wheelObject ) {
				Vector3 _pos;
				Quaternion _rot;
				wheels[w].wheelCollider.GetWorldPose( out _pos, out _rot );
				wheels[w].wheelObject.position = _pos;
				wheels[w].wheelObject.rotation = _rot;
			}
		}

	}

	public void Move ( float percent ) {
		float _factor = Mathf.Clamp( percent, -1f, 1f ) >= curThrottle ? 1f : -1f;
		curThrottle = Mathf.Clamp( curThrottle + ( _factor * throttleRate * Time.deltaTime ), 	-1f, 1f );

		for ( int w=0; w < wheels.Length; w++ ) {
			if ( wheels[w].torque > 0 ) {
				if ( percent == 0 ) { 
					wheels[w].wheelCollider.brakeTorque = wheels[w].torque;
					wheels[w].wheelCollider.motorTorque = 0f; 
				} else {
					wheels[w].wheelCollider.brakeTorque = 0f;
					wheels[w].wheelCollider.motorTorque = wheels[w].torque * curThrottle; 
				}
			}
		}
	}


	bool CheckErrors () {
		error = false;
		
		if ( wheels.Length > 0 ) {
			for ( int w=0; w < wheels.Length; w++ ) {
				if ( !wheels[w].wheelCollider ) {
					Debug.Log( this+" No wheel collider found on wheel index: " + w ); error = true;
				}
			}
		}
		
		return error;
	}
}
