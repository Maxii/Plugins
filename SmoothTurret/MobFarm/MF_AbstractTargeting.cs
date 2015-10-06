	using UnityEngine;
using System.Collections;

public abstract class MF_AbstractTargeting : MonoBehaviour {

	public GameObject weaponTarget;
	public GameObject navTarget;
	[Header("Object receiving target:")]
	[Tooltip("If blank: looks on same object, then looks at root object.\nThis is the object that will get the final target, " +
				"and is used to evaluate any targeting related functions, such as direction limits of the turret.")]
	public GameObject receivingObject;

	[HideInInspector] public MF_AbstractPlatform receivingObjectScript;
	[HideInInspector] public bool error;

	public void Start () {
		if ( CheckErrors() == true ) { return; }
	}
	
	bool CheckErrors () {
		string _object = gameObject.name;

		// look for defined receiving object
		if (receivingObject) {
			if ( receivingObject.GetComponent<MF_AbstractPlatform>() ) {
				receivingObjectScript = receivingObject.GetComponent<MF_AbstractPlatform>();
			} else {
				Debug.Log(_object+": Receiving object script not found at defined location."); error = true;
			}
		} else { // if null, choose self to look for turret script
			if ( gameObject.GetComponent<MF_AbstractPlatform>() ) {
				receivingObject = gameObject;
				receivingObjectScript = gameObject.GetComponent<MF_AbstractPlatform>();
			} else { // if null, choose root gameObject to look for turret script
				if ( transform.root.GetComponent<MF_AbstractPlatform>() ) {
					receivingObject = transform.root.gameObject;
					receivingObjectScript = transform.root.GetComponent<MF_AbstractPlatform>();
				} else {
					Debug.Log(_object+": No receiving object script found."); error = true;
				}
			}	
		}
		return error;
	}
}
