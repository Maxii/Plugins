using UnityEngine;
using System.Collections;

public class MF_BasicScanner : MonoBehaviour {

	[Header("Location of Target List:")]
	[Tooltip("If blank: looks on same object, then looks at root object.")]
	public GameObject targetListObject;
	[Header("Targeting Settings:")]
	[Tooltip("Targets can be matched by Tags (no collider needed) or Layers (collider required)")]
	public FactionMethodType factionMethod;
	public FactionType[] targetableFactions;
	[Header("Detector Settings:")]
	//public bool detectorActive = true;
	[Tooltip("(meters)")]
	public float detectorRange;
	[Tooltip("(seconds)\nHow often to refresh the target list. 0 = every frame.")]
	public float detectorInterval;
	public bool requireLos;
	public float losMinRange;
	
	[HideInInspector] float lastDetect;
	
	MF_TargetList targetListScript;
	LayerMask mask;
	bool error;
	
	void Start () {
		if ( CheckErrors() == true ) { return; }
		
		targetListScript = targetListObject.GetComponent<MF_TargetList>();
		lastDetect = Random.Range( -detectorInterval * 1.0f, 0.0f ); // add random time to stager scan pulses that would otherwise be on the same frame
		
		// **** need to be able to rebuild this on the fly !!
		// build the layermask of targetable factions. Only needed for layer faction method
		string[] _layerNames = new string[ targetableFactions.Length ]; // array to hold layer names
		for (int f=0; f < targetableFactions.Length; f++) { // for each targetable faction
			_layerNames[f] = targetableFactions[f].ToString(); // convert enum to string
		}
		mask = LayerMask.GetMask(_layerNames); // final layermask
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
		if ( factionMethod == FactionMethodType.Tags ) {
			_targets = new GameObject[0];
			for (int f=0; f < targetableFactions.Length; f++) { // for each targetable faction
				GameObject[] _thisFaction = GameObject.FindGameObjectsWithTag( targetableFactions[f].ToString() ); // find all targets with this targetable tag
				// combine exsisting array
				int _origLength = _targets.Length;
				System.Array.Resize<GameObject>( ref _targets, _targets.Length + _thisFaction.Length );
				System.Array.Copy( _thisFaction, 0, _targets, _origLength, _thisFaction.Length );
			}
			for (int t=0; t < _targets.Length; t++) {
				if ( (transform.position - _targets[t].transform.position).sqrMagnitude > detectorRange * detectorRange ) {
					_targets[t] = null; // out of range
					continue;
				}
				// **** avoids a bug where unity leaves an invisible empty object after editing a prefab when playing a scene without clicking off the edited prefab.
				if ( _targets[t].transform.root.GetComponent<MF_AbstractStatus>() == null ) {
					_targets[t] = null;
					continue;
				}
			}
		}

		// layers
		if ( factionMethod == FactionMethodType.Layers ) {
			Collider[] _colliders = Physics.OverlapSphere( transform.position, detectorRange, mask );
			_targets = new GameObject[ _colliders.Length ];
			for (int c=0; c < _colliders.Length; c++) {
				_targets[c] = _colliders[c].gameObject;
			}
		}

		for (int d=0; d < _targets.Length; d++) {
			if ( _targets[d] == gameObject ) { continue; } // skip self
			if ( _targets[d] == null ) { continue; } // skip null 

			_targets[d] = _targets[d].transform.root.gameObject; // make sure accessing root level
			int key = _targets[d].GetInstanceID();

			if ( requireLos == true && (transform.position - _targets[d].transform.position).sqrMagnitude > losMinRange * losMinRange ) {
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
				targetListScript.targetList.Add( key, new TargetData() );
				targetListScript.targetList[key].transform = _targets[d].transform;
				targetListScript.targetList[key].script = _targets[d].GetComponent<MF_AbstractStatus>();;
			}
			// update record
			targetListScript.targetList[key].lastDetected = Time.time;
			targetListScript.targetList[key].lastAnalyzed = Time.time;
			targetListScript.targetList[key].sqrMagnitude = (transform.position - _targets[d].transform.position).sqrMagnitude;

			// other data gathered by detector

		}
	}
	
	private bool CheckErrors() {
		string _object = gameObject.name;
		if ( targetListObject ) {
			if ( !targetListObject.GetComponent<MF_TargetList>() ) {
				Debug.Log(_object+": Target list not found on defined object: "+targetListObject); error = true;
			}
		} else {
			if ( GetComponent<MF_TargetList>() ) {
				targetListObject = gameObject;
			} else {
				if ( transform.root.GetComponent<MF_TargetList>() ) {
					targetListObject = transform.root.gameObject;
				} else {
					Debug.Log(_object+": Target list location not defined."); error = true;
				}
			}
		}
		return error;
	}

}
