using UnityEngine;
using System.Collections;

public abstract class MF_AbstractSelection : MonoBehaviour {
	// abstract so as to accomodate multiple selection type scripts
	// other scripts need to reference some of these variables, and therefore they need a common reference point.
	
	[Tooltip("If blank: recursively searches self and parents until a target list is found.")]
	public GameObject targetListObject;
	[Tooltip("If blank: recursively searches self and parents until a targeting script is found.")]
	public GameObject targetingObject;
	[Tooltip("Disables automatic target list search. Use in case auto search would find a list you don't want.")]
	public bool NoTargetList;
	[Tooltip("Disables automatic target script search. Use in case auto search would find a script you don't want.")]
	public bool NoTargetingScript;
	[Tooltip("The base object returned from a click. Also should be the location of MF_AbstractStatus, if any.\n" +
			"This allows the clicky collider to be anywhere in the object and still return the correct base object.\n" +
	         "If blank: assumes the same object.")] // no way to auto search - no standard criteria to look for
	public GameObject clickObjectBase;
	[Header("Selection Options:")]
	[Tooltip("This object may be clicked to become selected.")]
	public bool selectable = true;
	[Tooltip("This object's target list may be edited by shift-clicking other objects.")]
	public bool allowClickTargeting = true;
	[Tooltip("Clicked targets are persistent in the target list. (They won't be cleared due to expired time since last detection.)")]
	public bool clickTargetPersistance = true;
	[Tooltip("Right-clicked targets are given target priority.")]
	public bool prioritizeClickTargets = true;
	[Header("Target Options:")]
	[Tooltip("When another object is selected, this object may be clicked to become a target. Does not affect other targeting methods.")]
	public bool targetable = true;
	[Header("Brackets (Leave blank to use defaults)")]
	[Tooltip("Selection Manager should be the same prefab for all clickable objects in the scene. It will be automaticaly added to the scene at runtime and all clickable objects will reference it.")]
	public GameObject selectionManager;
	[Tooltip("May specify custom bracket size. If 0: will try to determine size from collider bounds. If no collider found, will use the first collider found on immediate children.")]
	[SerializeField] public float bracketSize;
	
	[HideInInspector] public MF_SelectionManager selectionManagerScript;
	[HideInInspector] public MF_TargetList targetListScript;
	[HideInInspector] public MF_AbstractTargeting targetingScript;
	[HideInInspector] public int myId;

	protected bool error;

	void Awake () {
		if (CheckErrors() == true) { return; }
		// create the SelectionManager if not already instantiated, then make that the reference for all selection scripts
		GameObject _sm = GameObject.Find( selectionManager.name+"(Clone)" );
		if ( _sm ) { // found the SelectionManager
			selectionManager = _sm;
		} else { // create the SelectionManager
			selectionManager = (GameObject)Instantiate( selectionManager, Vector3.zero, Quaternion.identity );
		}
		selectionManagerScript = selectionManager.GetComponent<MF_SelectionManager>();
	}

	public virtual void Start () {
		if (error) { return; }
		// cache scripts
		if (targetListObject) {
			targetListScript = targetListObject.GetComponent<MF_TargetList>();
		}
		if (targetingObject) {
			targetingScript = targetingObject.GetComponent<MF_AbstractTargeting>();
		}

		// cache instanceID
		if ( !clickObjectBase ) {
			clickObjectBase = gameObject;
		}
		myId = clickObjectBase.GetInstanceID();
	}

	public virtual void OnMouseOver () { }

	// create brackets
	public GameObject MakeBracket( GameObject prefab ) {
		GameObject newBracket = (GameObject)Instantiate ( prefab, transform.position, transform.rotation );
		newBracket.transform.SetParent(transform);
		if ( bracketSize == 0 ) { // try to determine size from collider bounds
			bracketSize = UtilityMF.FindColliderBoundsSize( clickObjectBase, true ) * 3f;
		}
		newBracket.GetComponent<ParticleSystem>().startSize = bracketSize;
		newBracket.SetActive(false);
		return newBracket;
	}

	public virtual bool CheckErrors () {
		error = false;
		string _object = gameObject.name;

		Transform rps;

		if ( selectionManager ) { 
			if ( !selectionManager.GetComponent<MF_SelectionManager>() ) {
				Debug.Log(_object+": No MF_SelectionManager script found on defined selection manager."); error = true;
			}
		} else { 
			Debug.Log(_object+": No Selection Manager defined."); error = true;
		}

		if ( targetListObject ) {
			if ( !targetListObject.GetComponent<MF_TargetList>() ) {
				Debug.Log(_object+": Target list not found on defined object: "+targetListObject); error = true;
			}
		} else {
			if ( NoTargetList == false ) {
				rps = UtilityMF.RecursiveParentSearch( "MF_TargetList", transform );
				if ( rps != null ) {
					targetListObject = rps.gameObject;
				} else {
					// no error
				}
			}
		}

		if ( targetingObject ) {
			if ( !targetingObject.GetComponent<MF_AbstractTargeting>() ) {
				Debug.Log(_object+": Targeting script not found on defined object: "+targetingObject); error = true;
			}
		} else {
			if ( NoTargetingScript == false ) {
				rps = UtilityMF.RecursiveParentSearch( "MF_AbstractTargeting", transform );
				if ( rps != null ) {
					targetingObject = rps.gameObject;
				} else {
					// no error
				}
			}
		}
		
		return error;
	}
}







