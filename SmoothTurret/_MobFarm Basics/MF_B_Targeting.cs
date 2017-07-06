using UnityEngine;
using System.Collections;

[HelpURL("http://mobfarmgames.weebly.com/mf_b_targeting.html")]
public class MF_B_Targeting : MF_AbstractTargeting {

	// to add more priority types, add a name to the enum PriorityType.
	// create a new switch case block below dealing with that new type.
	// if new target information needs to be evaluated, such as health or threat, add these variables to the TargetData class in MF_B_TargetData.
	// populate those variables in the MF_B_Scanner script when the target is scanned.
	// those variables can then be accessed in the ChooseTarget function below.
	
	public enum PriorityType { Closest, Furthest, MostHealth, LeastHealth, MostHealthPercent, LeastHealthPercent };
	
	[Header("Object holding target list:")]
	[Tooltip("If blank: recursively searches self and parents until a target list is found.")]
	public MF_B_TargetList targetListScript;
	[Header("How to choose a target from the list")]
	[SerializeField] protected PriorityType _priority;
	public PriorityType priority {
		get { return _priority; }
		set { _priority = value;
			readyChoose = true; // choose target now if priority changes
		}
	}
	[Tooltip("Keeps the choosen target until it is off the target list. (out of range of scanner, or has died) " +
				"even if another target is better according to the selected priority. Target will switch if a clicked priority is made, unless current target is also a click priority.")]
	public bool keepCurrentTarget; // keep choosen target even if a better one appears, until target is out of range or dies
	[Header("Additional Criteria:")]
	[Split1(true, "If targeting for a weapon, target must be within weapon range.")]
	public bool checkWeapRange;
	[Split2(true, "Target must be within arc limits of the receiving object.")]
	public bool checkArcLimits;

	bool readyChoose;
	
	public override void Awake () {
		base.Awake();
		if ( error == true) { return; }

		priority = _priority;

		// check for selection script and adds itself to list of targeting scripts
	}
	
	void LateUpdate () { // to always run after a target list update 
		if ( error == true ) { return; }

		if ( targetListScript ) { // make sure target list exsists - might be on a killed object

			// don't choose unless there's been an update to target list, unless target is gone
			if ( Time.time == targetListScript.lastUpdate || target == null ) { 
				readyChoose = true;
			}

			// choose target
			if ( readyChoose == true ) {
				readyChoose = false;
				if ( keepCurrentTarget == true && target != null ) { // see if target still exsists in list
					// check for a clicked priority
					if ( targetListScript.targetList.ContainsKey( target.GetInstanceID() ) ) { // make sure target is in list
						if ( targetListScript.targetList[ target.GetInstanceID() ].clickedPriority == false ) { // current target not click priority
							foreach ( int key in targetListScript.targetList.Keys ) { // check if other targets have click priority
								if ( targetListScript.targetList[key].clickedPriority == true ) { // found a click priority, re-evaluate target
									target = ChooseTarget( platformScript, _priority, targetListScript );
									return;
								}
							}
						}
					} else { // target not in list but still exsists in scene, may have gone out of range
						target = ChooseTarget( null );
					}
				}
				if ( target == null || keepCurrentTarget == false ) { // check for new target if always supposed to, or if no target
					target = ChooseTarget( platformScript, _priority, targetListScript );
				}
			}
		}
	}

	GameObject ChooseTarget ( GameObject targ ) {
		hasPrecision = MFnum.ScanSource.None; hasAngle = MFnum.ScanSource.None; hasRange = MFnum.ScanSource.None; hasVelocity = MFnum.ScanSource.None;
		return targ;
	}
	GameObject ChooseTarget ( MF_AbstractPlatform receivingObjectScript, PriorityType priority, MF_B_TargetList targetListScript ) {
		int? _bestKey = null;
		float? _bestValue = null;
		bool _priorityFound = false;

		foreach ( int key in targetListScript.targetList.Keys ) { // iterate through target list
			if ( targetListScript.targetList[key] == null ) { continue; } // skip null entries
			if ( targetListScript.targetList[key].transform == null ) { continue; } // skip missing objects
			// if ( targetListScript.targetList[key].poi.isPoi == true ) { continue; } // skip points of interest
			if ( checkArcLimits == true && receivingObjectScript ) {
				if ( receivingObjectScript.TargetWithinLimits( targetListScript.targetList[key].transform ) == false ) { continue; } // check arc limits
			}
			if (receivingControllerScript && checkWeapRange == true ) {
				if ( receivingControllerScript.weapons.Length > 0 ) {
					if ( receivingControllerScript.weapons[ receivingControllerScript.curWeapon ].script.RangeCheck( targetListScript.targetList[key].transform ) == false ) { continue; } // check weapon range
				}
			}

			if ( _priorityFound == true ) {
				if ( targetListScript.targetList[key].clickedPriority == false ) {
					continue;
				}
			} else { // _priorityFound == false
				if ( targetListScript.targetList[key].clickedPriority == true ) { // found first priority target
					_bestKey = key;
					_bestValue = ( transform.position - targetListScript.targetList[key].transform.position ).sqrMagnitude;
					_priorityFound = true;
					continue;
				}
			}

			float? _value = null;
			switch ( priority ) {
			case PriorityType.Closest :
				if ( _bestValue == null ) { _bestValue = Mathf.Infinity; } // initialize _bestValue
				_value = ( transform.position - targetListScript.targetList[key].transform.position ).sqrMagnitude;
				if ( _value < _bestValue ) {
					_bestKey = key;
					_bestValue = _value;
				}
				break;
			case PriorityType.Furthest :
				if ( _bestValue == null ) { _bestValue = -Mathf.Infinity; } // initialize _bestValue
				_value = ( transform.position - targetListScript.targetList[key].transform.position ).sqrMagnitude;
				if ( _value > _bestValue ) {
					_bestKey = key;
					_bestValue = _value;
				}
				break;
			case PriorityType.MostHealth :
				if ( _bestValue == null ) { _bestValue = -Mathf.Infinity; } // initialize _bestValue
				_value = targetListScript.targetList[key].sScript.health;
				if ( _value > _bestValue ) {
					_bestKey = key;
					_bestValue = _value;
				}
				break;
			case PriorityType.LeastHealth :
				if ( _bestValue == null ) { _bestValue = Mathf.Infinity; } // initialize _bestValue
				_value = targetListScript.targetList[key].sScript.health;
				if ( _value < _bestValue ) {
					_bestKey = key;
					_bestValue = _value;
				}
				break;
			case PriorityType.MostHealthPercent :
				if ( _bestValue == null ) { _bestValue = -Mathf.Infinity; } // initialize _bestValue
				_value = targetListScript.targetList[key].sScript.health / targetListScript.targetList[key].sScript.healthMax;
				if ( _value > _bestValue ) {
					_bestKey = key;
					_bestValue = _value;
				}
				break;
			case PriorityType.LeastHealthPercent :
				if ( _bestValue == null ) { _bestValue = Mathf.Infinity; } // initialize _bestValue
				_value = targetListScript.targetList[key].sScript.health / targetListScript.targetList[key].sScript.healthMax;
				if ( _value < _bestValue ) {
					_bestKey = key;
					_bestValue = _value;
				}
				break;

			// add additional priority types

			default :
				break;
			}
		}

		if ( _bestKey != null ) {
			MF_B_TargetList.TargetData tlk = targetListScript.targetList[ (int)_bestKey ];
			hasPrecision = tlk.hasPrecision;
			hasAngle = tlk.hasAngle;
			hasRange = tlk.hasRange;
			hasVelocity = tlk.hasVelocity;
			return tlk.transform.gameObject;
		} else {
			hasPrecision = MFnum.ScanSource.None; hasAngle = MFnum.ScanSource.None; hasRange = MFnum.ScanSource.None; hasVelocity = MFnum.ScanSource.None;
			return null;
		}
	}
	
	
	public override bool CheckErrors () {
		base.CheckErrors();

		if ( !targetListScript ) {
			targetListScript = UtilityMF.GetComponentInParent<MF_B_TargetList>( transform );
			if ( targetListScript == null ) { Debug.Log( this+": No target list found."); error = true; }
		}

		return error;
	}

}
