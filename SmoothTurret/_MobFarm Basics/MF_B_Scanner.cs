using UnityEngine;
using System.Collections;

public class MF_B_Scanner : MonoBehaviour {

	[Header("Location of Target List:")]
	[Tooltip("If blank: recursively searches parents until a target list is found.")]
	public MF_B_TargetList targetListScript;
	[Header("Targeting Settings:")]
	[Tooltip("Targets can be matched by Tags (no collider needed) or Layers (collider required)")]
	public MFnum.FactionMethodType factionMethod;
	public MFnum.FactionType[] targetableFactions;
	[Header("Detector Settings:")]
	//public bool detectorActive = true;
	[Tooltip("True: Always resolves a target to its root object.\nFalse: Each object with the proper tag/layer can be a target" +
		"(Use to target seperate parts of an object heirarchy)")]
	public bool targetRootObject = true;
	[Tooltip("(meters)")]
	public float detectorRange = 100f;
	[Tooltip("(seconds)\nHow often to refresh the target list. 0 = every frame.")]
	public float detectorInterval = 1f;
	public bool requireLos;
	[Tooltip("Starts the los raycast check some distance from the scanner. Use this to avoid other geometry that would block the raycast.")]
	public float losMinRange;
	
	[HideInInspector] float lastDetect;

	LayerMask mask;
	bool error;
	
	void Start () {
		if ( CheckErrors() == true ) { return; }

		lastDetect = Random.Range( -detectorInterval * 1.0f, 0.0f ); // add random time to stagger scan pulses that would otherwise be on the same frame
		
		// **** eventually need to be able to rebuild this on the fly !!
		// build the layermask of targetable factions. Only needed for layer faction method
		string[] _layerNames = new string[ targetableFactions.Length ]; // array to hold layer names
		for ( int f=0; f < targetableFactions.Length; f++ ) { // for each targetable faction
			if ( targetableFactions[f] == MFnum.FactionType.None ) { continue; } // no faction
			_layerNames[f] = targetableFactions[f].ToString(); // convert enum to string
		}
		mask = LayerMask.GetMask( _layerNames ); // final layermask
	}
	
	void Update () {
		if ( error == true ) { return; }
		// detector
		if ( Time.time >= lastDetect + detectorInterval ) {
			lastDetect = Time.time;
			DoScanner();
			targetListScript.lastUpdate = Time.time; // for target choosing timing
		}	
	}
	
	private void DoScanner () {
		GameObject[] _targets = null;
		
		// generate initial target list	

		// tags
		if ( factionMethod == MFnum.FactionMethodType.Tags ) {
			_targets = new GameObject[0];
			for (int f=0; f < targetableFactions.Length; f++) { // for each targetable faction
				if ( targetableFactions[f] == MFnum.FactionType.None ) { continue; } // no faction
				GameObject[] _thisFaction = GameObject.FindGameObjectsWithTag( targetableFactions[f].ToString() ); // find all targets with this targetable tag
				// combine exsisting array
				int _origLength = _targets.Length;
				System.Array.Resize<GameObject>( ref _targets, _targets.Length + _thisFaction.Length );
				System.Array.Copy( _thisFaction, 0, _targets, _origLength, _thisFaction.Length );
			}
			for (int t=0; t < _targets.Length; t++) {;
				if ( ( transform.position - _targets[t].transform.position ).sqrMagnitude > detectorRange * detectorRange ) {
					_targets[t] = null; // out of range
					continue;
				}
				// **** avoids a bug where unity leaves an invisible empty object after editing a prefab when playing a scene without first clicking off the edited prefab.
				// **** but this also then requires each target to inherit MF_AbstractStatus (the 'ghost' objects don't have a script, so checking for one is the easiest way to weed them out)
				// **** alternately, just make sure you click off prefabs when playing a scene
//				if ( _targets[t].transform.root.GetComponent<MF_AbstractStatus>() == null ) {
//					_targets[t] = null;
//					continue;
//				}
			}
		}

		// layers
		if ( factionMethod == MFnum.FactionMethodType.Layers ) {
			Collider[] _colliders = Physics.OverlapSphere( transform.position, detectorRange, mask );
			_targets = new GameObject[ _colliders.Length ];
			for (int c=0; c < _colliders.Length; c++) {
				_targets[c] = _colliders[c].gameObject;
			}
		}

		for (int d=0; d < _targets.Length; d++) {
			if ( _targets[d] == gameObject ) { continue; } // skip self
			if ( _targets[d] == null ) { continue; } // skip null 

			int key;
			if ( targetRootObject == true ) {
				_targets[d] = _targets[d].transform.root.gameObject; // make sure accessing root level
				key = _targets[d].transform.root.gameObject.GetInstanceID();
			} else {
				key = _targets[d].GetInstanceID();
			}

			if ( requireLos == true && ( transform.position - _targets[d].transform.position).sqrMagnitude > losMinRange * losMinRange ) {
				RaycastHit _hit;
				Vector3 _targDir = transform.position - _targets[d].transform.position;
				Physics.Raycast( transform.position - (_targDir.normalized * losMinRange), -_targDir, out _hit, detectorRange );
				if ( _hit.transform.root != _targets[d].transform ) {
					if ( targetListScript.targetList.ContainsKey(key) == true ) {
						targetListScript.targetList[key] = null;
					}
					continue;
				}
			}

			// add to targetList
			if ( targetListScript.targetList.ContainsKey(key) == false) { // don't try to overwrite exsisting key
				// new record
				targetListScript.targetList.Add( key, new MF_B_TargetList.TargetData() );
				targetListScript.targetList[key].transform = _targets[d].transform;
				targetListScript.targetList[key].script = _targets[d].GetComponent<MF_AbstractStatus>();
			}
			// update record
			targetListScript.targetList[key].lastDetected = Time.time;
			targetListScript.targetList[key].lastAnalyzed = Time.time;
			targetListScript.targetList[key].sqrMagnitude = ( transform.position - _targets[d].transform.position ).sqrMagnitude;

			// other data gathered by detector

		}
	}
	
	private bool CheckErrors() {
		Transform rps;

		if ( !targetListScript ) {
			rps = UtilityMF.RecursiveParentComponentSearch( "MF_B_TargetList", transform );
			if ( rps != null ) {
				targetListScript = rps.GetComponent<MF_B_TargetList>();
			} else {
				Debug.Log( this+": Target list location not found."); error = true;
			}
		}
		return error;
	}

}
