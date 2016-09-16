using UnityEngine;
using System.Collections;

public class MF_B_Selection : MF_AbstractSelection {
	
	// Selection script should be placed on the same object with a collider and rigidbody, or on an object with a rigidbody that has a compound collider (colliders on children)
	// If the object uses a compound collider, then this script should be on the rigidbody object
	[Tooltip("Activated if the selected object has detected this unit.\n" +
	         "If left blank, the default values in the selection manager will be used.")]
	public GameObject detectedMark; // marker to denote selected
	[Tooltip("Activated if the selected object has analyzed this unit.\n" +
	         "If left blank, the default values in the selection manager will be used.")]
	public GameObject analyzedMark; // marker to denote selected
	[Tooltip("Activated if the selected object has this as its weapon target.\n" +
	         "If left blank, the default values in the selection manager will be used.")]
	public GameObject weaponTargetMark; // marker to denote selected
	[Tooltip("Activated if the selected object has a navigation target.\n" +
	         "If left blank, the default values in the selection manager will be used.")]
	public GameObject navTargetMark; // marker to denote selected
	[Tooltip("Activated if this is the selected object.\n" +
	         "If left blank, the default values in the selection manager will be used.")]
	public GameObject selectedMark; // marker to denote active
	
	GameObject sObject;
	MF_AbstractSelection sScript;
	
	public override void Start() {
		base.Start();
		if (error) { return; }
		
		// create instances of brackets from defaults if none provided
		detectedMark = MakeBracket( !detectedMark ? selectionManagerScript.detectedMark : detectedMark );
		analyzedMark = MakeBracket( !analyzedMark ? selectionManagerScript.analyzedMark : analyzedMark );
		weaponTargetMark = MakeBracket( !weaponTargetMark ? selectionManagerScript.weaponTargetMark : weaponTargetMark );
		navTargetMark = MakeBracket( !navTargetMark ? selectionManagerScript.navTargetMark : navTargetMark );
		selectedMark = MakeBracket( !selectedMark ? selectionManagerScript.selectedMark : selectedMark );
	}
	
	public override void OnMouseOver () {
		if (error) { return; }

		if ( Input.anyKeyDown ) {
			// cache references
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
		if ( sScript && sScript.allowClickTargeting == true ) {
			if ( sObject != null && sObject != clickObjectBase ) { // there's a selected object and it isn't this object
				if (sScript.targetListScript) { // does selected have a target list?
					MF_AbstractTargetList _tlScript = sScript.targetListScript; // cache target list script
					
					// search for clicked target in selected objects target list
					if ( _tlScript.ContainsKey( myId ) == true ) { // found clicked object in target list
						if ( _priority == true ) {
							// don't remove, make priority
							_tlScript.SetClickedPriority( myId, true );
						} else {
							// click removes object from target list
							_tlScript.ClickRemove( myId ); // marks for removal
						}
					} else if ( clickTargetable == true ) { // not found on target list
						// click adds to target list
						// new record
						_tlScript.ClickAdd( myId, clickObjectBase.transform, clickObjectBase.GetComponent<MF_AbstractStatus>(),
						              _priority, sScript.clickTargetPersistance, (sObject.transform.position - clickObjectBase.transform.position).sqrMagnitude ); 
						
						// other data
					}
				}
			}
		}
	}
	
	void Update () {
		if (error) { return; }
		// show/hide selected bracket
		if (selectedMark) {
			if ( selectionManagerScript.selectedObject == clickObjectBase.gameObject ) {
				selectedMark.SetActive(true);
			} else {
				selectedMark.SetActive(false);
			}
		}
		if (navTargetMark) {
			bool _showNav = false;
			if ( selectionManagerScript.selectedObject == clickObjectBase.gameObject ) {
				if ( selectionManagerScript.selectedObjScript.navigationScript ) { // and has a nav script
					if ( selectionManagerScript.selectedObjScript.navigationScript.navTarget ) {
						navTargetMark.transform.position = selectionManagerScript.selectedObjScript.navigationScript.navTarget.transform.position;
						_showNav = true;
					}
				}
			}
			navTargetMark.SetActive( _showNav );
		}
		// show/hide other brackets
		// appears as weapon target of selected object
		bool _foundAsWeapTarg = false;
		if ( weaponTargetMark ) {
			if ( selectionManagerScript.selectedObjScript ) { // an object is selected
				if ( selectionManagerScript.selectedObjScript.targetingScript ) { // and has a target list
					if ( selectionManagerScript.selectedObjScript.targetingScript.target == clickObjectBase.gameObject ) { // and this object is the weapon target
						_foundAsWeapTarg = true;
					}
				}
			}
			weaponTargetMark.SetActive( _foundAsWeapTarg );
		}
		// appears as analyzed on target list of selected object
		bool _foundAsAnalyzed = false;
		if ( analyzedMark ) {
			if ( selectionManagerScript.selectedObjScript ) { // an object is selected
				if ( selectionManagerScript.selectedObjScript.targetListScript && _foundAsWeapTarg == false ) { // has a target list, not already designated as weap target
					if ( selectionManagerScript.selectedObjScript.targetListScript.ContainsKey( myId ) == true ) {
						if ( selectionManagerScript.selectedObjScript.targetListScript.GetLastAnalyzed( myId ) + selectionManagerScript.selectedObjScript.targetListScript.dataClearTime >= Time.time ) {
							_foundAsAnalyzed = true;
						}
					}
				}
			}
			analyzedMark.SetActive( _foundAsAnalyzed );
		}
		// appears as detected on target list of selected object
		bool _foundAsDetected = false;
		if ( detectedMark ) {
			if ( selectionManagerScript.selectedObjScript ) { // an object is selected
				if ( selectionManagerScript.selectedObjScript.targetListScript && _foundAsWeapTarg == false && _foundAsAnalyzed == false ) { // has a target list, not already designated as weap target or analyzed
					if ( selectionManagerScript.selectedObjScript.targetListScript.ContainsKey( myId ) == true ) {
						_foundAsDetected = true;
					}
				}
			}
			detectedMark.SetActive( _foundAsDetected );
		}
	}
}



