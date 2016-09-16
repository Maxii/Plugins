	using UnityEngine;
using System.Collections;

public abstract class MF_AbstractTargeting : MonoBehaviour {

	[Tooltip("The current target that is chosen by the script. This is visible for debugging purposes.")]
	public GameObject target;
	[Header("Object receiving target:")]
	[Tooltip("This location will be used to find MF_AbstractPlatform, MF_AbstractNavigation, and MF_AbstractWeaponControl.\n" +
	        "If blank: Defaults to look for MF_AbstractPlatform. Recursively searches self and parents until it is found.\n" +
			"This is used to evaluate any targeting related functions, such as direction limits of a turret. If none is found, those features will be ignored.")]
	public GameObject receivingObject;
	[Tooltip("Use to suppress automatic searching for MF_AbstractPlatform to find receivingObject.")]
	public bool dontSearchForReceivingObject;

	[HideInInspector] public MF_AbstractPlatform platformScript;
	[HideInInspector] public MF_AbstractNavigation navScript;
	[HideInInspector] public MF_AbstractPlatformControl receivingControllerScript;

	protected bool error;

	public virtual void Start () {
		if ( CheckErrors() == true ) { return; }
		if ( receivingObject ) {
			platformScript = receivingObject.GetComponent<MF_AbstractPlatform>();
			navScript = receivingObject.GetComponent<MF_AbstractNavigation>();
			receivingControllerScript = receivingObject.GetComponent<MF_AbstractPlatformControl>();
		}
	}
	
	public virtual bool CheckErrors () {
		Transform rps;

		// look for defined receiving object
		if ( !receivingObject && dontSearchForReceivingObject == false ) {
			rps = UtilityMF.RecursiveParentComponentSearch( "MF_AbstractPlatform", transform );
			if ( rps != null ) {
				receivingObject = rps.gameObject;
			}
		}

		return error;
	}
}
