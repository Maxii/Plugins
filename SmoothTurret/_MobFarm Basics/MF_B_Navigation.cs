using UnityEngine;
using System.Collections;

[HelpURL("http://mobfarmgames.weebly.com/mf_b_navigation.html")]
public class MF_B_Navigation : MF_AbstractNavigation {

	[Tooltip("Will stop when the last waypoint in the list is reached.")]
	public bool stopAtLastWpt;
	[Tooltip("The location of a targeting script. This is used to determine which target to follow when using NavMode.Target or NavMode.TargetOrWaypoint.\n" +
		"If blank, it will check the same game object.")]
	public MF_AbstractTargeting targetingScript;
	[Tooltip("The location of a mobility script. If blank, it will check the same game object, then recursively check parents until one is found.")]
	public MF_AbstractMobility mobilityScript;

	bool reachedLastWpt;
	bool error;

	public void OnValidate () {
		waypointGroup = _waypointGroup;
		curWpt = Mathf.Clamp( curWpt, 	0, Mathf.Max( 0, waypoints.Length - 1 ) ); // check for out of range curWpt
		if ( curWpt < waypoints.Length - 1 ) {
			reachedLastWpt = false;
		}
	}
	
	void Awake () {
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
	
	void DoNav ( Transform goal, float prox ) {
		if ( mobilityScript ) {
			mobilityScript.navTarget = goal;
			mobilityScript.goalProx = prox;
		}
	}
	
	void NavTarget () {
		if ( targetingScript ) {
			navTarget = targetingScript.target ? targetingScript.target.transform : null;
			if (navTarget) {
				DoNav( navTarget, targetProx );
			} else {
				DoNav( null, 0f );
			}
		}
	}
	
	void NavWaypoint () {
		Transform nav = null;
		if ( reachedLastWpt == true && stopAtLastWpt == true) { // stop if at last waypoint
			DoNav( null, 0f );
		} else {
			if ( waypoints.Length > 0 ) {
				if ( waypoints[curWpt] ) {
					// next waypoint
					Vector3 _modLoc = mobilityScript.ModifiedPosition( waypoints[curWpt].position );
					if ( ( transform.position - _modLoc ).sqrMagnitude <= waypointProx*waypointProx ) { // at waypoint
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
					nav = waypoints[curWpt];
				}
			}
		} 

		DoNav( nav, 0f );
	}
	
	bool CheckErrors () {
		error = false;

		if ( !mobilityScript ) {
			mobilityScript = UtilityMF.GetComponentInParent<MF_AbstractMobility>( transform );
		}

		if ( !targetingScript ) {
			targetingScript = GetComponent<MF_AbstractTargeting>();
		}
		
		return error;
	}
}

