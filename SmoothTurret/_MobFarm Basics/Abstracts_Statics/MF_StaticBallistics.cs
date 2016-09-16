using UnityEngine;
using System.Collections;

public class MFball {

	public static float? BallisticAimAngle ( Vector3 targetLoc, Vector3 exitLoc, float shotSpeed, MFnum.ArcType arc ) {
		int _factor = -Physics.gravity.y > 0 ? 1 : -1;
		float _gravityY = _factor == 1 ? -Physics.gravity.y : Physics.gravity.y; // if reverse gravity, calculate it as normal gravity, but with invert heightDif and angle
		float _heightDif = targetLoc.y - exitLoc.y;
		float _targetRangeXZ = Vector2.Distance( new Vector2(exitLoc.x, exitLoc.z), new Vector2(targetLoc.x, targetLoc.z) );
		float? _aimRad = null;
		
		float a = Mathf.Sqrt( Mathf.Pow(shotSpeed, 4f) - (_gravityY * ((_gravityY * (_targetRangeXZ*_targetRangeXZ)) + (_factor * 2f * _heightDif * shotSpeed*shotSpeed)) ) ); // _factor will invert height if reverse grav
		
		float _rad1 = Mathf.Atan( ( (shotSpeed*shotSpeed) + a ) / (_gravityY * _targetRangeXZ) );
		float _rad2 = Mathf.Atan( ( (shotSpeed*shotSpeed) - a ) / (_gravityY * _targetRangeXZ) );
		
		if ( float.IsNaN(_rad1) == true && float.IsNaN(_rad2) == true ) { 
			return null; // no solution
		} else if ( float.IsNaN(_rad1) == true ) { // should this ever happen?
			_aimRad = _rad2;
		} else if ( float.IsNaN(_rad2) == true ) { // should this ever happen?
			_aimRad = _rad1;
		} else {
			if ( arc == MFnum.ArcType.High ) {
				_aimRad = Mathf.Max( _rad1, _rad2 ); // pick highest arc
			} else {
				_aimRad = Mathf.Min( _rad1, _rad2 ); // pick lowest arc
			}
		}
		return _aimRad;
	}
	
	public static float? BallisticFlightTime ( Vector3 targetLoc, Vector3 exitLoc, float shotSpeed, float aimRad, MFnum.ArcType arc ) {
		float? _flightTime = null;
		float _speedY = shotSpeed * Mathf.Sin(aimRad);
		int _factor = -Physics.gravity.y > 0 ? 1 : -1; 
		float _gravityY = _factor == 1 ? -Physics.gravity.y : Physics.gravity.y; // if reverse gravity, calculate it as normal gravity, but with invert heightDif and angle
		float _heightDif = targetLoc.y - exitLoc.y;
		float _halfRange = .5f * (( (shotSpeed*shotSpeed) * Mathf.Sin( 2f * aimRad ) ) / _gravityY); // range used to determine which solution to choose
		
		float a = Mathf.Sqrt( ( (_speedY*_speedY) / (_gravityY*_gravityY) ) - ( ( _factor * 2f * _heightDif ) / _gravityY ) ); // _factor will invert height if reverse grav
		float t1 = ( _speedY / _gravityY ) + a;
		float t2 = ( _speedY / _gravityY ) - a;
		
		if ( t1 < 0 && t2 < 0 ) {
			return null; // no solution
		}
		if ( arc == MFnum.ArcType.High ) { 
			_flightTime = Mathf.Max( t1, t2 );
		} else {
			if ( t1 < 0 ) {
				_flightTime = t2;
			} else if ( t2 < 0 ) {
				_flightTime = t1;
			} else {
				if ( (targetLoc - exitLoc).sqrMagnitude < _halfRange*_halfRange ) { // if target is closer than arc peak, use lowest time
					_flightTime = Mathf.Min( t1, t2 );
				} else { // if target is past arc peak, use highest time
					_flightTime = Mathf.Max( t1, t2 );
				}
			}
		}
		return _flightTime;
	}

	public static float? BallisticIteration ( Vector3 exitLoc, float? shotSpeed, float aimRad, MFnum.ArcType arc, Transform target,
	                                         	Vector3 targetVelocity, Vector3 platformVelocity, Vector3 aimLoc_In, out Vector3 aimLoc_Out ) {
		float? _ballAim = null;
		aimLoc_Out = aimLoc_In;
		// find new flight time
		float? _flightTime = MFball.BallisticFlightTime( aimLoc_In, exitLoc, (float)shotSpeed, aimRad, arc );
		float _effectiveShotSpeed = Vector3.Distance(exitLoc, aimLoc_In) / (float)_flightTime;
		// find intercept based on new _effectiveShotSpeed
		Vector3? _intAim = MFcompute.Intercept( exitLoc, platformVelocity, _effectiveShotSpeed, target.position, targetVelocity );
		if ( _intAim == null ) {
			_ballAim = null;
		} else {
			aimLoc_Out = (Vector3)_intAim; // modify target aim location
			// re-calculate ballistic trajectory based on intercept point
			_ballAim = MFball.BallisticAimAngle( aimLoc_Out, exitLoc, (float)shotSpeed, arc );
		}
		return _ballAim;
	}
}
