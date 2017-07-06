using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[HelpURL("http://mobfarmgames.weebly.com/mf_abstractselection.html")]
public abstract class MF_AbstractSelection : MonoBehaviour {
	// abstract so as to accomodate multiple selection type scripts
	// other scripts need to reference some of these variables, and therefore they need a common reference point.
	
	[Tooltip("If blank: recursively searches self and parents until a target list is found.")]
	public MF_AbstractTargetList targetListScript;
	[Tooltip("If blank: recursively searches self and parents until a targeting script is found.")]
	public MF_AbstractTargeting targetingScript;
	[Tooltip("If blank: recursively searches self and parents until a navigation script is found.")]
	public MF_AbstractNavigation navigationScript;
	[Tooltip("Disables automatic target list search. Use in case auto search would find a list you don't want.")]
	public bool NoTargetList;
	[Tooltip("Disables automatic target script search. Use in case auto search would find a script you don't want.")]
	public bool NoTargetingScript;
	[Tooltip("Disables automatic navigation script search. Use in case auto search would find a script you don't want.")]
	public bool NoNavigationScript;
	[Tooltip("The base object returned from a click. Also should be the location of MF_AbstractClassify, if any.\n" +
			"This allows the clicky collider to be anywhere in the object and still return the correct base object.\n" +
	         "If blank: assumes the same object.")] // no way to auto search - no standard criteria to look for
	public GameObject clickObjectBase;
	[Header("Selection Options:")]
	[Tooltip("This object may be clicked to become selected.")]
	public bool selectable = true;
	[Tooltip("This object's target list may be edited when selected by shift or right-clicking other objects.")]
	public bool allowClickTargeting = true;
	[Tooltip("Clicked targets are persistent in the target list. (They won't be cleared due to expired time since last detection.)")]
	public bool clickTargetPersistance = true;
	[Tooltip("Right-clicked targets are given targeting priority.")]
	public bool prioritizeClickTargets = true;
	[Header("Target Options:")]
	[Tooltip("When another object is selected, this object may be clicked to become a target. Does not affect other targeting methods.")]
	public bool clickTargetable = true;
	[Space(8f)]
	[Tooltip("Selection Manager should be the same prefab for all clickable objects in the scene. It will be automatically added to the scene at runtime and all clickable objects will reference it.")]
	public MF_AbstractSelectionManager selectionManager;
	[Header("Marks (Leave blank to use defaults)")]
	public bool showSelected = true;
	public bool showNavigation = true;
	public bool showTargeting = true;
	[Split1("(Diameter in meters)\nMay specify custom mark size. If 0: will try to determine size from collider bounds. If no collider found, will use the first collider found on immediate children.")]
	[SerializeField] public float markSize;

	public Dictionary<int, MF_AbstractSelection> detectingMeList = new Dictionary<int, MF_AbstractSelection>();
	
	[HideInInspector] public MF_AbstractTargeting[] otherTargScripts;
	[HideInInspector] public int myId;
	
	[HideInInspector] public MF_AbstractClassify cScript; 

	protected bool error;

	public virtual void Awake () {
		if (error) { return; }

		// cache instanceID
		if ( !clickObjectBase ) {
			clickObjectBase = gameObject;
		}
		myId = clickObjectBase.GetInstanceID();
		cScript = transform.root.GetComponent<MF_AbstractClassify>();

	}

	public virtual void OnDisable () { // reset for object pool support
		if ( selectionManager && selectionManager.sScript == this ) { selectionManager.sScript = null; } // clear selection
	}

	public virtual void LeftClick() {
		if ( selectionManager.sScript == this ) {
			// clear selection
			selectionManager.sScript = null;
		} else {
			if ( selectable == true ) {
				selectionManager.sScript = this;
			}
		}
	}

	public void OnMouseOver () {
		if ( error ) { return; }
		
		if ( Input.anyKeyDown ) {
			// cache references
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

	void ShiftClick () {
		bool _priority = false;
		if ( Input.GetKey(KeyCode.Mouse1) || Input.GetKey(KeyCode.LeftControl) ) {
			_priority = true;
		}
		if ( selectionManager.sScript && selectionManager.sScript.allowClickTargeting == true ) {
			if ( selectionManager.sScript != this ) { // there's a selected object and it isn't this object
				if ( selectionManager.sScript.targetListScript ) { // does selected have a target list?
					MF_AbstractTargetList _tlScript = selectionManager.sScript.targetListScript; // cache target list script
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
						_tlScript.ClickAdd( myId, clickObjectBase.transform, clickObjectBase.GetComponent<MF_AbstractClassify>(), clickObjectBase.GetComponent<MF_AbstractStats>(),
						                   _priority, selectionManager.sScript.clickTargetPersistance ); 
						
						// other data
					}
				}
			}
		}
	}

	// create brackets
	public GameObject MakeMark( GameObject prefab ) {
		if ( prefab == null ) { return null; }
		GameObject newMark = (GameObject)Instantiate ( prefab, transform.position, transform.rotation );
		newMark.transform.SetParent(transform);
		if ( markSize == 0 ) { // try to determine size from collider bounds
			markSize = UtilityMF.FindColliderBoundsSize( clickObjectBase.transform, true ) * 3f;
			if ( markSize == 0 ) { markSize = 1; } // if no collider found, set to 1
		}
		ParticleSystem.MainModule ps = newMark.GetComponent<ParticleSystem>().main;
		ps.startSize = markSize;
		newMark.SetActive(false);
		return newMark;
	}

	public virtual void Remove ( int key ) {
		if ( detectingMeList.ContainsKey( key ) == true ) {
			detectingMeList.Remove( key );
			DoVisibility();
		}
	}
	
	public virtual void Add ( int key, MF_AbstractSelection script ) {
		if ( detectingMeList.ContainsKey( key ) == false ) {
			detectingMeList.Add( key, script );
			DoVisibility();
		}
	}

	protected virtual void DoVisibility () {}

	public virtual bool CheckErrors () {
		error = false;

		if ( !targetListScript && NoTargetList == false ) {
			targetListScript = UtilityMF.GetComponentInParent<MF_AbstractTargetList>( transform );
		}

		if ( !targetingScript && NoTargetingScript == false ) {
			targetingScript = UtilityMF.GetComponentInParent<MF_AbstractTargeting>( transform );
		}

		if ( !navigationScript && NoNavigationScript == false ) {
			navigationScript = UtilityMF.GetComponentInParent<MF_AbstractNavigation>( transform );
		}
		
		return error;
	}
}







