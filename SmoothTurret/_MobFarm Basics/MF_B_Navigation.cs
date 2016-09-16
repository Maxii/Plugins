using UnityEngine;
using System.Collections;

public class MF_B_Navigation : MF_AbstractNavigation {

	[Tooltip("Will stop when the last waypoint in the list is reached.")]
	public bool stopAtLastWpt;
	[Tooltip("Will stop when within Goal Prox of the target.")]
	public bool stopAtTarget;
	
	public MF_AbstractTargeting targetingScript;
	public MF_AbstractMobility mobilityScript;

	bool reachedLastWpt;
	bool error;

	public void OnValidate () {
		waypointGroup = _waypointGroup;
		curWpt = Mathf.Clamp( curWpt, 	0, waypoints.Length - 1 ); // check for out of range curWpt
		if ( curWpt < waypoints.Length - 1 ) {
			reachedLastWpt = false;
		}
	}
	
	void Start () {
		if ( CheckErrors() == true ) { return; }

		if ( randomWpt ) {
			curWpt = Random.Range( 0, waypoints.Length );
		} else {
			curWpt = 0;
		}
	}
	
	void Update () {
		if ( error == true ) { return; }
		
		switch ( navMode ) {
			
		case ( NavType.Waypoint ) :
			NavWaypoint();
			break;
			
		case ( NavType.Target ) :
			NavTarget();
			break;
			
		case ( NavType.TargetOrWaypoint ) :
			if ( targetingScript && targetingScript.target ) {
				NavTarget();
			} else {
				NavWaypoint();
			}
			break;
			
		default : break;
		}
	}
	
	void DoNav ( Transform goal ) {
		if (mobilityScript) {
			mobilityScript.navTarget = goal;
		}
	}
	
	void NavTarget () {
		if ( targetingScript ) {
			navTarget = targetingScript.target ? targetingScript.target.transform : null;
			if (navTarget) {
				if ( stopAtTarget == true && ( transform.position - navTarget.position ).sqrMagnitude <= goalProx*goalProx ) {
					DoNav( null );
				} else {
					DoNav( navTarget );
				}
			} else {
				DoNav( null );
			}
		}
	}
	
	void NavWaypoint () {
		Transform _nav = null;
		if ( reachedLastWpt == true && stopAtLastWpt == true) { // stop if at last waypoint
			DoNav( null );
		} else {
			if ( waypoints.Length > 0 ) {
				if ( waypoints[curWpt] ) {
					// next waypoint
					Vector3 _modLoc = mobilityScript.ModifiedPosition( waypoints[curWpt].position );
					if ( ( transform.position - _modLoc ).sqrMagnitude <= goalProx*goalProx ) { // at waypoint
						if ( stopAtLastWpt == true && curWpt == waypoints.Length - 1 ) {
							reachedLastWpt = true;
						} else {
							if ( randomWpt == true ) {
								curWpt = Random.Range( 0, waypoints.Length );
							} else {
								curWpt = MFmath.Mod( curWpt +1, waypoints.Length);
							}
						}
					}
					navTarget = waypoints[curWpt];
					_nav = waypoints[curWpt];
				}
			}
		} 
		DoNav( _nav );
	}
	
	bool CheckErrors () {
		error = false;
		Transform rps = null;

		if ( !mobilityScript ) {
			rps = UtilityMF.RecursiveParentComponentSearch( "MF_AbstractMobility", transform );
			if ( rps != null ) {
				mobilityScript = rps.GetComponent<MF_AbstractMobility>();
			}
		}

		if ( !targetingScript ) {
			if  (GetComponent<MF_AbstractTargeting>()) {
				targetingScript = GetComponent<MF_AbstractTargeting>();
			}
		}
		
		return error;
	}
}

