using UnityEngine;
using System.Collections;

public class MFcompute {
		
	public static float SmoothRateChange ( float distance, float closureRate, float curRate, float rateAccel, float decelMult, float rateMax ) {
		int _goalFactor = distance >= 0 ? 1 : -1; // which way is goal?
		int _curTurnFactor = curRate >= 0 ? 1 : -1; // which way currently turning?
		
		// if goal is within accelleration, reduce accel to match goal. This minimizes jitter at high acceleration speeds
		if ( (distance <= rateAccel * _curTurnFactor * Time.deltaTime && distance >= 0) 	||		(distance >= rateAccel * _curTurnFactor * Time.deltaTime && distance <= 0) ) {
			rateAccel = Mathf.Abs(distance) / Time.deltaTime;
		}

		float _rateDecel = rateAccel * decelMult * 2; // decelerate faster than accelerating (*2 to make it a little more agressive as default)
		
		// determine which way to accelerate
		if (_goalFactor * _curTurnFactor > 0) {
			// positive: moving towards goal
			if ( distance * _goalFactor >= MFcompute.DistanceToStop(closureRate, _rateDecel ) * _goalFactor ) { 
				// turn faster
				curRate = Mathf.Clamp(   curRate + (rateAccel * _curTurnFactor * Time.deltaTime)   , -rateMax, rateMax);
			} else {
				if ( distance * _goalFactor <= MFcompute.DistanceToStop(closureRate, _rateDecel ) * _goalFactor * .8 ) { 
					//turn slower
					curRate = Mathf.Clamp(   curRate - (_rateDecel * _curTurnFactor * Time.deltaTime)   , -rateMax, rateMax);
				} // else don't change
			}
		} else {
			// negative: moving away from goal, start heading towards goal
			curRate = Mathf.Clamp(   curRate + (_rateDecel * _goalFactor * Time.deltaTime)   , -rateMax, rateMax);
		}
		return curRate;
	}
	
	public static float DistanceToStop ( float speed, float accel ) {
		int _speedFactor = speed >= 0 ? 1 : -1;
		float _x = Mathf.Abs(speed) - (accel * 2f * Time.deltaTime); // new speed if choosing to decelerate now (with an extra frame, otherwise it's a little too conservative)
		return ((_x + accel) * _x) / (accel * 2f) * -_speedFactor;
	}

	// safe terminal velocity approximation for rigidbodies. Won't return a speed less than current speed.
	public static float FindTerminalVelocity ( float thrust, Rigidbody rb ) {
		return Mathf.Clamp( thrust / Mathf.Clamp( rb.mass * rb.drag, 1f, Mathf.Infinity ), 		rb.velocity.magnitude / Time.fixedDeltaTime, 	Mathf.Infinity );
	}
	
	//first-order intercept
	public static Vector3? Intercept( Vector3 shooterPosition, Vector3 shooterVelocity, float shotSpeed, Vector3 targetPosition, Vector3 targetVelocity ) {
		Vector3 targetRelativeVelocity = targetVelocity - shooterVelocity;
		float? t = InterceptTime( shotSpeed, targetPosition - shooterPosition, targetRelativeVelocity);
		return targetPosition + (t * targetRelativeVelocity);
	}
	//first-order intercept using relative target position
	public static float? InterceptTime ( float shotSpeed, Vector3 targetRelativePosition, Vector3 targetRelativeVelocity ) {
		float velocitySquared = targetRelativeVelocity.sqrMagnitude;
		if (velocitySquared < 0.001f) {
			return 0f;
		}
		float a = velocitySquared - shotSpeed*shotSpeed;
		
		//handle similar velocities
		if (Mathf.Abs(a) < 0.001f) {
			float t = -targetRelativePosition.sqrMagnitude/( 2f * Vector3.Dot( targetRelativeVelocity, targetRelativePosition ));
			return Mathf.Max( t, 0f ); //don't shoot back in time
		}
		
		float b = 2f * Vector3.Dot( targetRelativeVelocity, targetRelativePosition );
		float c = targetRelativePosition.sqrMagnitude;
		float determinant = b*b - 4f*a*c;
		
		if (determinant > 0f) { //determinant > 0; two intercept paths (most common)
			determinant = Mathf.Sqrt(determinant);
			float t1 = ( -b + determinant ) / (2f * a);
			float t2 = ( -b - determinant ) / (2f * a);
			if ( t1 > 0f ) {
				if ( t2 > 0f ) {
					return Mathf.Min(t1, t2); //both are positive
				} else {
					return t1; //only t1 is positive
				}
			} else {
				return Mathf.Max( t2, 0f ); //don't shoot back in time
			}
		} else if ( determinant < 0f ) { //determinant < 0; no intercept path
			return null;
		} else { //determinant = 0; one intercept path, pretty much never happens
			return Mathf.Max( -b / (2f * a), 0f ); //don't shoot back in time
		}
	}
}











