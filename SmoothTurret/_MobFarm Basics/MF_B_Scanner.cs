using UnityEngine;
using System.Collections;

namespace MFnum {
	public enum MatchType { Relation, Faction }
}

[HelpURL("http://mobfarmgames.weebly.com/mf_b_scanner.html")]
public class MF_B_Scanner : MF_AbstractComponent {

	[Header("Location of Target List:")]
	[Tooltip("If blank: recursively searches parents until a target list is found.")]
	public MF_B_TargetList targetListScript;
	[Header("Targeting Settings:")]
	public MatchBlock match;
	[Header("Detector Settings:")]
	//public bool detectorActive = true;
	[Tooltip("True: Always resolves a target to its root object.\nFalse: Each object with the proper tag/layer can be a target" +
		"(Use to target seperate parts of an object heirarchy)")]
	public bool targetRootObject = true;
	[Split1(true, "(meters)")]
	public float detectorRange = 100f;
	[Split2(true, "(seconds)\nHow often to refresh the target list. 0 = every frame.")]
	public float detectorInterval = 1f;
	[Split1(true, "How long after non-detection a target will remain on the target list. (Detector Interval + Linger Adjust)")]
	public float lingerAdjust = .4f;
	[Split1()]
	public bool requireLos;
	[Split2(40, "Starts the los raycast check some distance from the scanner. Use this to avoid other geometry that would block the raycast.")]
	public float losMinRange;
	
	[HideInInspector] float nextDetect;

	MF_AbstractClassify cScript;
	MF_AbstractSelection selectionScript;
	int instanceID;
	LayerMask mask;
	int targetCount;
	bool loaded;
	bool error;

	[System.Serializable]
	public class MatchBlock {
		[Tooltip("Targets can be matched by Tags (no collider needed) or Layers (collider required)")]
		public MFnum.ScanMethodType scanMethod;
		[Tooltip("Faction: Will target specific factions.\n" +
		         "Relation: Will target specific relations as defined in this unit's status script.\n\n" +
		         "Scanner will ignore faction location information in a target status script, and only explicitly use tags or layers.")]
		public MFnum.MatchType matchBy;
		public MFnum.Relation targetableRelation;
		public MFnum.FactionType[] targetableFactions;
	}
	
	void Awake () {
		if ( CheckErrors() == true ) { return; }

		selectionScript = transform.root.GetComponent<MF_AbstractSelection>();
		instanceID = cScript.gameObject.GetInstanceID();

		loaded = true;
		BuildLayermask();

	}

	protected override void OnEnable () { // reset for object pool support
		base.OnEnable();
		nextDetect = Random.Range( -detectorInterval * 1.0f, 0.0f ); // add random time to stagger scan pulses that would otherwise be on the same frame
	}

	void Update () {
		if ( error == true ) { return; }
		// detector
		if ( Time.time >= nextDetect ) {
			nextDetect += detectorInterval;
			DoScanner();
//			targetListScript.lastUpdate = Time.time; // for target choosing timing
		}	
	}
	
	private void DoScanner () {
		GameObject[] _targets = null;
		
		// generate initial target list	

		// tags
		if ( match.scanMethod == MFnum.ScanMethodType.Tags ) {
			_targets = new GameObject[0];

			MFnum.FactionType[] _factions = new MFnum.FactionType[0];
			if ( match.matchBy == MFnum.MatchType.Faction ) {
				_factions = match.targetableFactions;
			} else { // if by relation
				if ( cScript ) { // gather factions in appropriate relation type
					if ( match.targetableRelation == MFnum.Relation.Enemy ) {
						_factions = cScript.factions.enemies;
					} else if ( match.targetableRelation == MFnum.Relation.Ally ) {
						_factions = cScript.factions.allies;
					} else if ( match.targetableRelation == MFnum.Relation.Neutral ) {
						_factions = cScript.factions.neutral;
					}
				}
			}
			for ( int f=0; f < _factions.Length; f++ ) { // for each targetable faction
				if ( _factions[f] == MFnum.FactionType.None ) { continue; } // no faction
				GameObject[] _thisFaction = GameObject.FindGameObjectsWithTag( _factions[f].ToString() ); // find all targets with this targetable tag
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
				// **** avoids a bug where unity leaves an invisible empty object when playing a scene if a prefab is selected in the project view.
				// **** but this also then requires each target to inherit MF_AbstractStatus (the 'ghost' objects don't have a script, so checking for one is the easiest way to weed them out)
				// **** alternately, just make sure you click off prefabs when playing a scene
//				if ( _targets[t].transform.root.GetComponent<MF_AbstractStatus>() == null ) {
//					_targets[t] = null;
//					continue;
//				}
			}
		}

		// layers
		if ( match.scanMethod == MFnum.ScanMethodType.Layers ) {
			Collider[] _colliders = Physics.OverlapSphere( transform.position, detectorRange, mask );
			_targets = new GameObject[ _colliders.Length ];
			for (int c=0; c < _colliders.Length; c++) {
				_targets[c] = _colliders[c].transform.root.gameObject;
			}
		}

		for (int d=0; d < _targets.Length; d++) {
			targetCount = 0;
			if ( _targets[d] == gameObject ) { continue; } // skip self
			if ( _targets[d] == gameObject.activeSelf == false ) { continue; } // skip disabled 
			if ( _targets[d] == null ) { continue; } // skip null 

			int key = 0;
			if ( targetRootObject == true ) {
				_targets[d] = _targets[d].transform.root.gameObject; // make sure accessing root level
				key = _targets[d].transform.root.gameObject.GetInstanceID();
			} else {
				key = _targets[d].GetInstanceID();
			}

			if ( requireLos == true && ( transform.position - _targets[d].transform.position).sqrMagnitude > losMinRange * losMinRange ) {
				RaycastHit _hit;
				Vector3 _targDir = transform.position - _targets[d].transform.position;
				Physics.Linecast( transform.position - (_targDir.normalized * losMinRange), _targets[d].transform.position, out _hit );
				if ( _hit.transform.root != _targets[d].transform ) {
					if ( targetListScript.targetList.ContainsKey(key) == true ) {
						targetListScript.targetList[key].transform = null;
					}
					continue; // los blocked
				}
			}
			targetCount++;
			if ( targetCount > 0 ) { monitor = 1; } else { monitor = 0; }
			SendCheckUnit();

			// add to targetList
			if ( targetListScript.targetList.ContainsKey(key) == false) { // don't try to overwrite exsisting key
				MF_AbstractClassify _cScript = _targets[d].GetComponent<MF_AbstractClassify>();
				MF_AbstractStats _sScript = _targets[d].GetComponent<MF_AbstractStats>();
				// new record
				targetListScript.targetList.Add( key, new MF_B_TargetList.TargetData() );
				targetListScript.targetList[key].transform = _targets[d].transform;
				targetListScript.targetList[key].cScript = _cScript;
				targetListScript.targetList[key].sScript = _sScript;
				targetListScript.targetList[key].hasPrecision = MFnum.ScanSource.Detector;
				targetListScript.targetList[key].hasAngle = MFnum.ScanSource.Detector;
				targetListScript.targetList[key].hasRange = MFnum.ScanSource.Detector;
				targetListScript.targetList[key].hasVelocity = MFnum.ScanSource.Detector;
				targetListScript.targetList[key].hasFaction = MFnum.ScanSource.Detector;
				if ( _cScript ) { // add to detectingMeList
					if ( _cScript.selectionScript && selectionScript ) {
						_cScript.selectionScript.Add( instanceID, selectionScript );
					}
				}
			}
//			if ( targetListScript.targetList[key].poi.isPoi == true ) { continue; } // skip points of interest
			// update record
			targetListScript.targetList[key].lastDetected = Time.time;
			targetListScript.targetList[key].lastAnalyzed = Time.time;
			targetListScript.targetList[key].lingerTime = Time.time + detectorInterval + lingerAdjust;
			targetListScript.targetList[key].dataLingerTime = Time.time + detectorInterval + lingerAdjust;
//			targetListScript.targetList[key].sqrMagnitude = ( transform.position - _targets[d].transform.position ).sqrMagnitude;

			// other data gathered by scanner

		}
	}

	// build the layermask of targetable factions. Only needed for layer faction method
	public void BuildLayermask () {
		if ( loaded == false ) { return; }
		string[] _layerNames = new string[0];
		
		if ( match.matchBy == MFnum.MatchType.Faction || match.matchBy == MFnum.MatchType.Relation ) {
			MFnum.FactionType[] _factions = new MFnum.FactionType[0];
			
			if ( match.matchBy == MFnum.MatchType.Faction ) {
				_factions = match.targetableFactions;
			} else { // if by relation
				if ( cScript ) { // gather factions in appropriate relation type
					if 		( match.targetableRelation == MFnum.Relation.Enemy ) { _factions = cScript.factions.enemies; }
					else if ( match.targetableRelation == MFnum.Relation.Ally ) { _factions = cScript.factions.allies; }
					else if ( match.targetableRelation == MFnum.Relation.Neutral ) { _factions = cScript.factions.neutral; }
				}
			}
			_layerNames = new string[ _factions.Length ]; // array to hold layer names
			
			for (int f=0; f < _factions.Length; f++) { // for each targetable faction
				if ( _factions[f] == MFnum.FactionType.None ) { continue; } // no faction
				_layerNames[f] = _factions[f].ToString(); // convert enum to string
			}
		}
		
		mask = LayerMask.GetMask(_layerNames); // final layermask
	}
	
	private bool CheckErrors() {

		if ( !targetListScript ) {
			targetListScript = UtilityMF.GetComponentInParent<MF_B_TargetList>( transform );
			if ( targetListScript == null ) { Debug.Log( this+": Target list location not found."); error = true; }
		}

		cScript = UtilityMF.GetComponentInParent<MF_AbstractClassify>( transform );
		if ( cScript == null ) { Debug.Log( this+": Classify script not found."); error = true; }

		return error;
	}

}
