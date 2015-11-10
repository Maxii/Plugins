	using UnityEngine;
using System.Collections;

public abstract class MF_AbstractTargeting : MonoBehaviour {

	public GameObject weaponTarget;
	public GameObject navTarget;
	[Header("Object receiving target:")]
	[Tooltip("If blank: recursively searches parents until a platform script is found.\nThis is the object that will get the final target, " +
				"and is used to evaluate any targeting related functions, such as direction limits of a turret.")]
	public GameObject receivingObject;

	[HideInInspector] public MF_AbstractPlatform receivingObjectScript;
	[HideInInspector] public bool error;

	public void Start () {
		if ( CheckErrors() == true ) { return; }
		receivingObjectScript = receivingObject.GetComponent<MF_AbstractPlatform>();
	}
	
	bool CheckErrors () {
		string _object = gameObject.name;
		Transform rps;

		// look for defined receiving object
		if ( receivingObject ) {
			if ( !receivingObject.GetComponent<MF_AbstractPlatform>() ) {
				Debug.Log(_object+": Receiving object script not found on defined object: "+receivingObject); error = true;
			}
		} else {
			rps = UtilityMF.RecursiveParentSearch( "MF_AbstractPlatform", transform );
			if ( rps != null ) {
				receivingObject = rps.gameObject;
			} else {
				Debug.Log(_object+": No receiving object script found."); error = true;
			}
		}

		return error;
	}
}
