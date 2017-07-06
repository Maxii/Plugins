using UnityEngine;
using System.Collections;

[HelpURL("http://mobfarmgames.weebly.com/mf_abstracttargeting.html")]
public abstract class MF_AbstractTargeting : MonoBehaviour {

	[Tooltip("The current target that is chosen by the script. This is visible for debugging purposes.")]
	public GameObject target;
	[Header("Object receiving target:")]
	[Tooltip("This location will be used to find MF_AbstractPlatform, MF_AbstractNavigation, and MF_AbstractPlatformControl.\n" +
	        "If blank: Defaults to look for MF_AbstractPlatform. Recursively searches self and parents until it is found.\n" +
			"This is used to evaluate any targeting related functions, such as direction limits of a turret. If none is found, those features will be ignored.")]
	public GameObject receivingObject;
	[Split1(true, 85, "Use to suppress automatic searching for MF_AbstractPlatform to find receivingObject.")]
	public bool dontSearchForReceivingObject;
	[Split1(true, 85, "Don't use this script when displaying targeted icons with a selection script.")]
	public bool dontAddToSelectScript;

	[HideInInspector] public MFnum.ScanSource hasPrecision; // for target data requirements
	[HideInInspector] public MFnum.ScanSource hasAngle; // for target data requirements
	[HideInInspector] public MFnum.ScanSource hasRange; // for target data requirements
	[HideInInspector] public MFnum.ScanSource hasVelocity; // for target data requirements
	[HideInInspector] public bool isMarkingTarg; // will be read by platform control script

	[HideInInspector] public MF_AbstractPlatform platformScript;
	[HideInInspector] public MF_AbstractNavigation navScript;
	[HideInInspector] public MF_AbstractPlatformControl receivingControllerScript;

	protected bool error;

	public virtual void Awake () {
		if ( CheckErrors() == true ) { return; }
		if ( receivingObject ) {
			platformScript = receivingObject.GetComponent<MF_AbstractPlatform>();
			navScript = receivingObject.GetComponent<MF_AbstractNavigation>();
			receivingControllerScript = receivingObject.GetComponent<MF_AbstractPlatformControl>();
		}
		if ( dontAddToSelectScript == false ) {
			MF_AbstractSelection selScript = transform.root.GetComponent<MF_AbstractSelection>();
			if ( selScript ) {
				MF_AbstractTargeting[] temp = new MF_AbstractTargeting[ selScript.otherTargScripts.Length + 1 ]; 
				for ( int i=0; i < selScript.otherTargScripts.Length; i++ ) {
					temp[i] = selScript.otherTargScripts[i];
				}
				temp[ selScript.otherTargScripts.Length ] = this;
				selScript.otherTargScripts = temp;
			}
		}
	}

	public virtual void OnEnable () { // reset for object pool support
		target = null;
		hasPrecision = MFnum.ScanSource.None; hasAngle = MFnum.ScanSource.None; hasRange = MFnum.ScanSource.None; hasVelocity = MFnum.ScanSource.None;
		isMarkingTarg = false;
	}

	public virtual void SetMarkedTime ( float time ) {
		// to be overriden by child targeting script
	}

	public virtual bool CheckErrors () {

		// look for defined receiving object
		if ( !receivingObject && dontSearchForReceivingObject == false ) {
			MF_AbstractPlatform ap = UtilityMF.GetComponentInParent<MF_AbstractPlatform>( transform );
			if ( ap ) { receivingObject = ap.gameObject; }
		}

		return error;
	}
}
