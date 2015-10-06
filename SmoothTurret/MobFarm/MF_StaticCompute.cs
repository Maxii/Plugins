using UnityEngine;
using System.Collections;

public class MFcompute {
		
	public static float SmoothRateChange ( float distance, float closureRate, float curRate, float rateAccel, float decelMult, float rateMax ) {
		int _goalFactor = distance >= 0 ? 1 : -1; // which way is goal?
		int _curTurnFactor = curRate >= 0 ? 1 : -1; // which currently turning?
		float _rateDecel = rateAccel * decelMult * 2; // decelerate faster than accelerating (*2 to make it a little more agressive as default)
		
		// if goal is within accelleration, reduce accel to match goal. This minimizes jitter at high acceleration speeds
		// this solution isn't perfect and has trouble dealing with very high rates and low framerates. Working on a way to make this better
		if ( (distance <= _rateDecel * _curTurnFactor * Time.deltaTime && distance >= 0) ||
		    (distance >= _rateDecel * _curTurnFactor * Time.deltaTime && distance <= 0) ) {
			_rateDecel = Mathf.Abs(distance) / Time.deltaTime;
		}
		
		// determine which way to accelerate
		if (_goalFactor * _curTurnFactor > 0) {
			// positive: moving towards goal
			if ( distance * _goalFactor >= MFcompute.DistanceToStop(closureRate, _rateDecel ) * _goalFactor) { 
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

	public static float? BallisticAimAngle ( Vector3 targetLoc, Vector3 exitLoc, float shotSpeed, bool highArc ) {
		int _factor = -Physics.gravity.y > 0 ? 1 : -1;
		float _gravityY = _factor == 1 ? -Physics.gravity.y : Physics.gravity.y; // if reverse gravity, calculate it as normal gravity, but with invert heightDif and angle
		Vector3 _targetLoc = targetLoc;
		float _heightDif = targetLoc.y - exitLoc.y;
		float _targetRangeXZ = Vector2.Distance( new Vector2(exitLoc.x, exitLoc.z), new Vector2(_targetLoc.x, _targetLoc.z) );
		float? _aimRad = null;
		
		float a = Mathf.Sqrt( Mathf.Pow(shotSpeed, 4f) - (_gravityY * ((_gravityY * (_targetRangeXZ*_targetRangeXZ)) + (_factor * 2f * _heightDif * shotSpeed*shotSpeed)) ) ); // invert height if reverse grav
		
		float _rad1 = Mathf.Atan( ( (shotSpeed*shotSpeed) + a ) / (_gravityY * _targetRangeXZ) );
		float _rad2 = Mathf.Atan( ( (shotSpeed*shotSpeed) - a ) / (_gravityY * _targetRangeXZ) );
		
		if ( float.IsNaN(_rad1) == true && float.IsNaN(_rad2) == true ) {
			return null; // no solution
		} else if ( float.IsNaN(_rad1) == true ) {
			_aimRad = _rad2;
		} else if ( float.IsNaN(_rad2) == true ) {
			_aimRad = _rad1;
		} else {
			if ( highArc == true ) {
				_aimRad = Mathf.Max( _rad1, _rad2 ); // pick highest arc
			} else {
				_aimRad = Mathf.Min( _rad1, _rad2 ); // pick lowest arc
			}
		}
		return _aimRad;
	}
	
	public static float? BallisticFlightTime ( Vector3 targetLoc, Vector3 exitLoc, float shotSpeed, float aimRad, bool highArc ) {
		float? _flightTime = null;
		float _speedY = shotSpeed * Mathf.Sin(aimRad);
		int _factor = -Physics.gravity.y > 0 ? 1 : -1;
		float _gravityY = _factor == 1 ? -Physics.gravity.y : Physics.gravity.y; // if reverse gravity, calculate it as normal gravity, but with invert heightDif and angle
		float _heightDif = targetLoc.y - exitLoc.y;
		float _targetRangeXZ = Vector2.Distance( new Vector2(exitLoc.x, exitLoc.z), new Vector2(targetLoc.x, targetLoc.z) );
		
		float a = Mathf.Sqrt( ( (_speedY*_speedY) / (_gravityY*_gravityY) ) - ( ( _factor * 2 * _heightDif ) / _gravityY ) );
		float t1 = ( _speedY / _gravityY ) + a;
		float t2 = ( _speedY / _gravityY ) - a;
		
		float _maxRange = Mathf.Abs( ( (shotSpeed*shotSpeed) * Mathf.Sin( 2 * aimRad ) ) / _gravityY );
		
		if ( t1 < 0 && t2 < 0 ) {
			return null; // no solution
		}
		if ( highArc == true || _targetRangeXZ > _maxRange / 2 ) {
			_flightTime = Mathf.Max( t1, t2 );
		} else {
			if ( t1 < 0 ) {
				_flightTime = t2;
			} else if ( t2 < 0 ) {
				_flightTime = t1;
			} else {
				_flightTime = Mathf.Min( t1, t2 );
			}
		}
		return _flightTime;
	}
		
}











