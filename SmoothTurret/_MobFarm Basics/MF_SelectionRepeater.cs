using UnityEngine;
using System.Collections;

public class MF_SelectionRepeater : MonoBehaviour {

	// allows objects with more than one collider to have multiple clickable colliders. Click are sent to a single selection script.

	public GameObject selectScriptObject;

	MF_AbstractSelection selectionScript;
	bool error;

	void Start () {
		if ( CheckErrors() == true ) { return; }
	}

	// send click to the main selection script of this object
	void OnMouseDown () {
		if (error) { return; }
		selectionScript.OnMouseOver();
	}

	bool CheckErrors () {
		error = false;
		string _object = transform.root.name;
		
		Transform rps;

		if ( selectScriptObject ) {
			if ( selectScriptObject.GetComponent<MF_AbstractSelection>() ) {
				selectionScript = selectScriptObject.GetComponent<MF_AbstractSelection>();
			} else {
				Debug.Log(_object+": Selection script not found on defined object: "+selectScriptObject); error = true;
			}
		} else {
			rps = UtilityMF.RecursiveParentSearch( "MF_AbstractSelection", transform );
			if ( rps != null ) {
				selectionScript = rps.GetComponent<MF_AbstractSelection>();
			} else {
				Debug.Log(_object+": Selection script not found."); error = true;
			}
		}
		return error;
	}
}
