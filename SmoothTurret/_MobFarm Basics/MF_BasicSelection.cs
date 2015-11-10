using UnityEngine;
using System.Collections;

public class MF_BasicSelection : MF_AbstractSelection {

	// if a collider object higher in the tree has a	 rigidbody, the child collider may not register a click. Try attaching a kinematic rigidbody to the clicky collider.
	[Tooltip("Activated if the selected object has this as its weapon target.\n" +
			"If left blank, the default values in the selection manager will be used.")]
	public GameObject targetedMark; // marker to denote selected
	[Tooltip("Activated if the selected object has this in its list of targets.\n" +
	         "If left blank, the default values in the selection manager will be used.")]
	public GameObject targetListMark; // marker to denote selected
	[Tooltip("Activated if this is the selected object.\n" +
	         "If left blank, the default values in the selection manager will be used.")]
	public GameObject selectedMark; // marker to denote active

	GameObject sObject;
	MF_AbstractSelection sScript;

	public override void Start() {
		base.Start();
		if (error) { return; }

		// create instances of brackets from defaults if none provided
		targetedMark = MakeBracket( !targetedMark ? selectionManagerScript.targetedMark : targetedMark );
		targetListMark = MakeBracket( !targetListMark ? selectionManagerScript.targetListMark : targetListMark );
		selectedMark = MakeBracket( !selectedMark ? selectionManagerScript.selectedMark : selectedMark );

	}

	public override void OnMouseOver () {
		if (error) { return; }
		// cache references
		if ( Input.anyKeyDown ) {
			sObject = selectionManagerScript.selectedObject;
			sScript = selectionManagerScript.selectedObjScript;

			if ( Input.GetKey(KeyCode.Mouse0) ) { // left click
				if ( Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftControl) ) { // holding shift or control
					ShiftClick();
				} else { // not holding shift or control
					LeftClick();
				}
			}
			if ( Input.GetKey(KeyCode.Mouse1) ) {
				if ( Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ) { // holding shift
					// nothing
				} else { // not holding shift
					ShiftClick();
				}
			}
		}
	}

	void LeftClick() {
		if ( sObject == clickObjectBase ) 	{ // clicked is already selection
			// clear selection
			selectionManagerScript.selectedObject = null;
			selectionManagerScript.selectedObjScript = null;
		} else {
			if ( selectable == true ) {
				// make clicked the selection
				selectionManagerScript.selectedObject = clickObjectBase;
				selectionManagerScript.selectedObjScript = GetComponent<MF_AbstractSelection>();
			}
		}
	}

	void ShiftClick () {
		bool _priority = false;
		if ( Input.GetKey(KeyCode.Mouse1) || Input.GetKey(KeyCode.LeftControl) ) {
			_priority = true;
		}
		if ( sScript.allowClickTargeting == true ) {
			if ( sObject != null && sObject != clickObjectBase ) { // there's a selected object and it isn't this object
				if (sScript.targetListObject) { // does selected have a target list?
					MF_TargetList _tlScript = sScript.targetListScript; // cache target list script

					// search for clicked target in selected objects target list
					if ( _tlScript.targetList.ContainsKey(myId) == true ) { // found clicked object in target list
						if ( _priority == true ) {
							// don't remove, make priority
							_tlScript.targetList[myId].clickedPriority = true;
						} else {
							// click removes object from target list
							_tlScript.targetList[myId].transform = null; // marks for removal
						}
					} else if ( targetable == true ) { // not found on target list
						// click adds to target list
						// new record
						_tlScript.targetList.Add( myId, new TargetData() );
						_tlScript.targetList[myId].transform = clickObjectBase.transform;
						_tlScript.targetList[myId].script = clickObjectBase.GetComponent<MF_AbstractStatus>();
						_tlScript.targetList[myId].clickedPriority = _priority;
						_tlScript.targetList[myId].targetPersists = sScript.clickTargetPersistance;
						_tlScript.targetList[myId].lastDetected = Time.time;
						_tlScript.targetList[myId].lastAnalyzed = Time.time;
						_tlScript.targetList[myId].sqrMagnitude = (sObject.transform.position - clickObjectBase.transform.position).sqrMagnitude;
						
						// other data
					}
				}
			}
		}
	}

	void Update () {
		if (error) { return; }
		// show/hide selected marker
		if (selectedMark) {
			if ( selectionManagerScript.selectedObject == clickObjectBase.gameObject ) {
				selectedMark.SetActive(true);
			} else {
				selectedMark.SetActive(false);
			}
		}
		// show/hide targeted / target list marker
		// appears as weapon target of selected object
		bool _foundAsTarg = false;
		if ( targetedMark ) {
			if ( selectionManagerScript.selectedObjScript ) { // an object is selected
				if ( selectionManagerScript.selectedObjScript.targetingScript ) { // and has a target list
					if ( selectionManagerScript.selectedObjScript.targetingScript.weaponTarget == clickObjectBase.gameObject ) { // and this object is the weapon target
						_foundAsTarg = true;
					}
				}
			}
			targetedMark.SetActive( _foundAsTarg );
		}

		// appears on target list of selected object
		bool _foundInList = false;
		if ( targetListMark ) {
			if ( selectionManagerScript.selectedObjScript ) { // an object is selected
				if ( selectionManagerScript.selectedObjScript.targetListObject && _foundAsTarg == false ) { // has a target list, not already designated as weap target
					if ( selectionManagerScript.selectedObjScript.targetListScript.targetList.ContainsKey(myId) == true ) {
						_foundInList = true;
					}
				}
			}
			targetListMark.SetActive( _foundInList );
		}
	}
}







