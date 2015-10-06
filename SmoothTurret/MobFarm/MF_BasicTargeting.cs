using UnityEngine;
using System.Collections;

public class MF_BasicTargeting : MF_AbstractTargeting {

	// to add more priority types, add a name to the enum PriorityType.
	// create a new switch case block below dealing with that new type.
	// if new target information needs to be evaluated, such as health or threat, add these variables to the TargetData class in MF_BasicTargetData.
	// populate those variables in the MF_BasicScanner script when the target is scanned.
	// those variables can then be accessed in the ChooseTarget function below.
	
	public enum PriorityType { Closest, Furthest };
	
	[Header("Object holding target list:")]
	[Tooltip("If blank: looks on same object, then looks at root object, then assumes none. If none, no target will be found.")]
	public GameObject targetListObject;
	[Header("How to choose a target from the list")]
	public PriorityType priority;
	[Tooltip("Keeps the choosen target until it is off the target list. (Out of range of scanner, or has died.) " +
				"Even if another target is a better according to the selected priority.")]
	public bool keepCurrentTarget; // keep choosen target even if a better one appears, until target is out of range or dies
	[Header("Additional Criteria:")]
	[Tooltip("If targeting for a weapon, target must be within weapon range.")]
	public bool checkWeapRange;
	[Tooltip("Target must be within arc limits of the receiving object.")]
	public bool checkArcLimits;

	MF_TargetList targetListScript;
	bool readyChoose;
	PriorityType priorityToggle;
	ST_TurretControl receivingControllerScript;
	
	new void Start () {
		base.Start();
		if (CheckErrors() == true) { return; }

		// look for defined target list
		if (targetListObject) {
			if ( targetListObject.GetComponent<MF_TargetList>() ) {
				targetListScript = targetListObject.GetComponent<MF_TargetList>();
			}
		} else { // if null, choose self to look for target list
			if ( gameObject.GetComponent<MF_TargetList>() ) {
				targetListScript = gameObject.GetComponent<MF_TargetList>();
			} else { // if null, choose root gameObject to look for target list
				if ( transform.root.GetComponent<MF_TargetList>() ) {
					targetListScript = transform.root.GetComponent<MF_TargetList>();
				}
			}	
		}

		if ( receivingObject.GetComponent<ST_TurretControl>() ) {
			receivingControllerScript = receivingObject.GetComponent<ST_TurretControl>();
		}
	}
	
	void LateUpdate () { // to always run after a target list update 
		if ( error == true ) { return; }
		// don't choose unless there's been an update to target list, unless target is gone
		if ( Time.time == targetListScript.lastUpdate || weaponTarget == null ) { 
			readyChoose = true;
		}
		// choose target now if priority changes
		if ( priority != priorityToggle ) {
			priorityToggle = priority;
			readyChoose = true;
		}

		// choose target
		if (targetListScript) { // make sure target list exsists
			if ( readyChoose == true ) {
				readyChoose = false;
				if ( (keepCurrentTarget == true) && (weaponTarget != null) ) { // see if target still exsists in list
					if ( !targetListScript.targetList.ContainsKey( weaponTarget.GetInstanceID() ) ) { // target not in list
						weaponTarget = null;
					}
				}
				if ( (weaponTarget == null) || (keepCurrentTarget == false) ) { // check for new target if always supposed to or if no target
					weaponTarget = ChooseTarget( receivingObjectScript, priority, targetListScript );
				}
			}
		}
	}
	
	
	GameObject ChooseTarget ( MF_AbstractPlatform receivingObjectScript, PriorityType priority, MF_TargetList targetListScript ) {
		GameObject _bestTarget = null;
		float? _bestValue = null;

		foreach ( int key in targetListScript.targetList.Keys ) { // iterate through target list
			if ( targetListScript.targetList[key] == null ) { continue; } // skip null entries
			if ( targetListScript.targetList[key].transform == null ) { continue; } // skip missing objects
			if ( checkArcLimits == true && receivingObjectScript.TargetWithinLimits( targetListScript.targetList[key].transform ) == false ) { continue; } // check arc limits
			if (receivingControllerScript && checkWeapRange == true ) {
				if ( receivingControllerScript.weapons.Length > 0 ) {
					if ( receivingControllerScript.weapons[ receivingControllerScript.curWeapon ].script.RangeCheck( targetListScript.targetList[key].transform ) == false ) { continue; } // check weapon range
				}
			}

			switch ( priority ) {
			case PriorityType.Closest :
				if ( _bestValue == null ) { _bestValue = Mathf.Infinity; } // initialize _bestValue
				if ( targetListScript.targetList[key].sqrMagnitude < _bestValue ) {
					_bestTarget = targetListScript.targetList[key].transform.gameObject;
					_bestValue = targetListScript.targetList[key].sqrMagnitude;
				}
				break;
			case PriorityType.Furthest :
				if ( _bestValue == null ) { _bestValue = -Mathf.Infinity; } // initialize _bestValue
				if ( targetListScript.targetList[key].sqrMagnitude > _bestValue ) {
					_bestTarget = targetListScript.targetList[key].transform.gameObject;
					_bestValue = targetListScript.targetList[key].sqrMagnitude;
				}
				break;

			// add additional priority types

			default :
				break;
			}
		}
		return _bestTarget;
	}
	
	
	bool CheckErrors () {
//		string _object = gameObject.name;

		return error;
	}

}
