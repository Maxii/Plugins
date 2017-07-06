using UnityEngine;
using System.Collections;

[HelpURL("http://mobfarmgames.weebly.com/mf_selectionrepeater.html")]
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

		if ( !selectionScript ) {
			selectionScript = UtilityMF.GetComponentInParent<MF_AbstractSelection>( transform );
			if ( selectionScript == null ) { Debug.Log( this+": No selection script found."); error = true; }
		}

		return error;
	}
}
