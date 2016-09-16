using UnityEngine;
using System.Collections;

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
	[Tooltip("The base object returned from a click. Also should be the location of MF_AbstractStatus, if any.\n" +
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
	[Header("Brackets (Leave blank to use defaults)")]
	[Tooltip("Selection Manager should be the same prefab for all clickable objects in the scene. It will be automatically added to the scene at runtime and all clickable objects will reference it.")]
	public GameObject selectionManager;
	[Tooltip("May specify custom bracket size. If 0: will try to determine size from collider bounds. If no collider found, will use the first collider found on immediate children.")]
	[SerializeField] public float bracketSize;
	
	[HideInInspector] public MF_SelectionManager selectionManagerScript;
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

		// cache instanceID
		if ( !clickObjectBase ) {
			clickObjectBase = gameObject;
		}
		myId = clickObjectBase.GetInstanceID();
	}

	public virtual void OnMouseOver () { }

	// create brackets
	public GameObject MakeBracket( GameObject prefab ) {
		if ( prefab == null ) { return null; }
		GameObject newBracket = (GameObject)Instantiate ( prefab, transform.position, transform.rotation );
		newBracket.transform.SetParent(transform);
		if ( bracketSize == 0 ) { // try to determine size from collider bounds
			bracketSize = UtilityMF.FindColliderBoundsSize( clickObjectBase.transform, true ) * 3f;
			if ( bracketSize == 0 ) { bracketSize = 1; } // if no collider found, set to 1
		}
		newBracket.GetComponent<ParticleSystem>().startSize = bracketSize;
		newBracket.SetActive(false);
		return newBracket;
	}

	public virtual bool CheckErrors () {
		error = false;

		Transform rps;

		if ( selectionManager ) { 
			if ( !selectionManager.GetComponent<MF_SelectionManager>() ) {
				Debug.Log( this+": No MF_SelectionManager script found on defined selection manager."); error = true;
			}
		} else { 
			Debug.Log( this+": No Selection Manager defined."); error = true;
		}

		if ( !targetListScript && NoTargetList == false ) {
			rps = UtilityMF.RecursiveParentComponentSearch( "MF_AbstractTargetList", transform );
			if ( rps != null ) {
				targetListScript = rps.GetComponent<MF_AbstractTargetList>();
			}
		}

		if ( !targetingScript && NoTargetingScript == false ) {
			rps = UtilityMF.RecursiveParentComponentSearch( "MF_AbstractTargeting", transform );
			if ( rps != null ) {
				targetingScript = rps.GetComponent<MF_AbstractTargeting>();
			}
		}

		if ( !navigationScript && NoNavigationScript == false ) {
			rps = UtilityMF.RecursiveParentComponentSearch( "MF_AbstractNavigation", transform );
			if ( rps != null ) {
				navigationScript = rps.GetComponent<MF_AbstractNavigation>();
			}
		}
		
		return error;
	}
}







