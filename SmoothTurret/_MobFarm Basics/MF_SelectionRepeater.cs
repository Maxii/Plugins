using UnityEngine;
using System.Collections;

public class MF_SelectionRepeater : MonoBehaviour {

	// allows objects with more than one collider to have multiple clickable colliders. Clicks are sent to a single selection script.

	public MF_AbstractSelection selectionScript;

	bool error;

	void Start () {
		if ( CheckErrors() == true ) { return; }
	}

	// send OnMouseOver to the designated selection script
	void OnMouseDown () {
		if ( error == true ) { return; }

		if (selectionScript) {
			selectionScript.OnMouseOver();
		}
	}

	public virtual bool CheckErrors () {
		error = false;
		
		Transform rps;
		if ( !selectionScript ) {
			rps = UtilityMF.RecursiveParentComponentSearch( "MF_AbstractSelection", transform );
			if ( rps != null ) {
				selectionScript = rps.GetComponent<MF_AbstractSelection>();
			} else {
				Debug.Log( this+": No selection script found."); error = true;
			}
		}

		return error;
	}
}
