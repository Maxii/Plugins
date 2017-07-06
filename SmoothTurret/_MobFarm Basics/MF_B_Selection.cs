using UnityEngine;
using System.Collections;

[HelpURL("http://mobfarmgames.weebly.com/mf_b_selection.html")]
public class MF_B_Selection : MF_AbstractSelection {
	
	// Selection script should be placed on the same object with a collider and rigidbody, or on an object with a rigidbody that has a compound collider (colliders on children)
	// If the object uses a compound collider, then this script should be on the rigidbody object
	[Tooltip("Activated if this is the selected object.\n" +
	         "If left blank, the default values in the selection manager will be used.")]
	public GameObject selectedMark; // marker to denote active
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

	[Space(8f)]
	[Tooltip("This object reperesents a point of interest. (No need for Selected, Detected, or Analyzed marks.)")]
	public bool isPointOfInterest;
	[Tooltip("Activated if the selected object is a point of interest.\n" +
	         "If left blank, the default values in the selection manager will be used")]
	public GameObject poiMark; 

	MF_B_SelectionManager mfbSelectionManager;

	public override void Awake () {
		if (CheckErrors() == true) { return; }

		// create the SelectionManager if not already instantiated, then make that the reference for all selection scripts
		GameObject _sm = GameObject.Find( selectionManager.gameObject.name+"(Clone)" );
		if ( _sm ) { // found the SelectionManager
			mfbSelectionManager = _sm.GetComponent<MF_B_SelectionManager>();
		} else { // create the SelectionManager
			mfbSelectionManager = (MF_B_SelectionManager)Instantiate( selectionManager, Vector3.zero, Quaternion.identity );
		}
		selectionManager = mfbSelectionManager;

		base.Awake();
		// create instances of brackets from defaults if none provided
		if ( isPointOfInterest == false ) {
			selectedMark = MakeMark( !selectedMark ? mfbSelectionManager.selectedMark : selectedMark );
			detectedMark = MakeMark( !detectedMark ? mfbSelectionManager.detectedMark : detectedMark );
			analyzedMark = MakeMark( !analyzedMark ? mfbSelectionManager.analyzedMark : analyzedMark );
			navTargetMark = MakeMark( !navTargetMark ? mfbSelectionManager.navTargetMark : navTargetMark );
		} else {
			poiMark = MakeMark( !poiMark ? mfbSelectionManager.poiMark : poiMark );
		}
		weaponTargetMark = MakeMark( !weaponTargetMark ? mfbSelectionManager.weaponTargetMark : weaponTargetMark );
	}

	void OnEnable () {
		Update();
	}

	void Update () {
		DoMarks();
	}
	
	void DoMarks () {
		if ( error ) { return; }

		MF_AbstractSelection sScript = selectionManager.sScript;

		bool _foundAsSelected = false;
		bool _foundAsWeapTarget = false;
		bool _foundNav = false;
		bool _foundAsPoi = false;
		bool _foundAsAnalyzed = false;

		if ( selectedMark ) {
			bool _showingSelected = false;
			if ( selectionManager.sScript == this ) { // selected unit
				_foundAsSelected = true;
				if ( showSelected == true ) {
					_showingSelected = true;
				}
			}
			selectedMark.SetActive( _showingSelected );
		}
		
		if ( navTargetMark && navigationScript ) {
			if ( showNavigation == true && navigationScript.navTarget ) {
				if ( _foundAsSelected == true ) {
					navTargetMark.transform.position = navigationScript.navTarget.transform.position;
					_foundNav = true;
				}
			}
			navTargetMark.SetActive( _foundNav );
		}

		if ( weaponTargetMark ) {
			if ( sScript && sScript != this && sScript.showTargeting == true ) {
				if ( detectingMeList.ContainsKey( sScript.myId ) == true ) { // make sure selected unit is still detecting me (timing when going out of range - targeting script nulls target after)
					if ( sScript.targetingScript && sScript.targetingScript.target == gameObject ) {
						_foundAsWeapTarget = true;
					}
					for ( int i=0; i < sScript.otherTargScripts.Length; i++ ) { // see if any of the selected unit's targeting scripts are targeting me
						if ( sScript.otherTargScripts[i].target == gameObject ) {
							_foundAsWeapTarget = true;
							break;
						}
					}
				}
			}
			weaponTargetMark.SetActive( _foundAsWeapTarget );
		}
		
		_foundAsPoi = SetScanMark ( poiMark, false, MFnum.MarkType.PoI );
		_foundAsAnalyzed = SetScanMark ( analyzedMark, _foundAsPoi, MFnum.MarkType.Analyzed );
		SetScanMark ( detectedMark, ( _foundAsAnalyzed || _foundAsPoi ), MFnum.MarkType.Detected );
	}
	
	bool SetScanMark ( GameObject mark, bool exception, MFnum.MarkType type ) {
		MF_AbstractSelection sScript = selectionManager.sScript;
		bool set = false;

		if ( mark ) { // make sure mark exsists
			if ( exception == false ) {
				if ( sScript && sScript != this && sScript.showTargeting == true ) { // self is not selected
					if ( detectingMeList.ContainsKey( sScript.myId ) ) { // search if selected unit is detecting me
						if ( type == MFnum.MarkType.Analyzed ) { // check if analyzed
							if ( sScript.targetListScript.ContainsKey( myId ) ) {
								if ( sScript.targetListScript.GetDataLingerTime( myId ) >= Time.time ) {
									set = true;
								}
							} else {
								set = true;
							}
						} else if ( type == MFnum.MarkType.Detected ) { // check detection only
							set = true;
						} else if ( type == MFnum.MarkType.PoI ) { // check if poi
							set = sScript.targetListScript.GetPoi( myId );
						}
					} 
				}
			}
			mark.SetActive( set );
		}
		return set;
	}

	public override bool CheckErrors () {
		error = base.CheckErrors();
		
		if ( !selectionManager ) { 
			Debug.Log( this+": No Selection Manager defined."); error = true;
		} else {
			if ( selectionManager.GetComponent<MF_B_SelectionManager>() == null ) { Debug.Log( this+": No MF_B_SelectionManager script found."); error = true; }
		}
		
		return error;
	}
}



