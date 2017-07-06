using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[HelpURL("http://mobfarmgames.weebly.com/mf_b_targetlist.html")]
public class MF_B_TargetList : MF_AbstractTargetList {
	
	public Dictionary<int, TargetData> targetList = new Dictionary<int, TargetData>();

	public class TargetData : AbstractTargetData { 
		// inherit abstract data

		public TargetData () {} // constructor with 0 arguments

		// for creating an entry via clicked target
		public TargetData ( Transform transform, MF_AbstractClassify cScript, MF_AbstractStats sScript, bool clickedPriority, bool targetPersists ) {
			this.transform = transform;
			this.cScript = cScript;
			this.sScript = sScript;
			this.clickedPriority = clickedPriority;
			this.targetPersists = targetPersists;
			this.lastDetected = Time.time;
			this.lastAnalyzed = Time.time;
//			this.sqrMagnitude = sqrMagnitude;
		} 
	}

	public override void OnEnable () { // reset for object pool support
		base.OnEnable();
		targetList.Clear();
	}

	public void OnDisable () { // reset for object pool support
		foreach ( int key in targetList.Keys ) {
			// remove
			RemoveAsDetecting ( key );
		}
	}

	public override int TargetCount () {
		return targetList.Count;
	}
	
	public override bool ContainsKey ( int key ) {
		return targetList.ContainsKey( key );
	}
	
	public override void ClickAdd ( int key, Transform transform, MF_AbstractClassify cScript, MF_AbstractStats sScript, bool clickedPriority, bool targetPersists ) {
		targetList.Add( key, new TargetData( transform, cScript, sScript, clickedPriority, targetPersists ) ); 
	}
	
	public override void ClickRemove ( int key ) {
		targetList[key].transform = null; // mark for removal
	}
	
	public override float? GetLastAnalyzed ( int key ) {
		return targetList[key].lastAnalyzed;
	}

	public override float GetDataLingerTime ( int key ) {
		return targetList[key].dataLingerTime;
	}
	
	public override void SetClickedPriority ( int key, bool cp ) {
		targetList[key].clickedPriority = cp;
	}

	public override MFnum.ScanSource GetHasFaction ( int key ) {
		return targetList[key].hasFaction;
	}

	public override void RemoveAnalyzeData ( int key ) {
		// remove certian data without removing target entry
		TargetData _tlKey = targetList[key];
		if ( _tlKey.hasPrecision != MFnum.ScanSource.Detector ) { _tlKey.hasPrecision = MFnum.ScanSource.None; }
		if ( _tlKey.hasAngle != MFnum.ScanSource.Detector ) { _tlKey.hasAngle = MFnum.ScanSource.None; }
		if ( _tlKey.hasRange != MFnum.ScanSource.Detector ) { _tlKey.hasRange = MFnum.ScanSource.None; }
		if ( _tlKey.hasVelocity != MFnum.ScanSource.Detector ) { _tlKey.hasVelocity = MFnum.ScanSource.None; }

		targetList[key] = _tlKey;
	} 

	public override void ClearOld () {
		iteratingDict = true;

		workingIDsToClean = new List<int> (targetList.Keys); 
		for ( int i=0; i < workingIDsToClean.Count; i++ ) {
			int key = workingIDsToClean[i];
			if ( targetList[ key ] == null ) {
				garbageList.Add( key ); // mark for removal
				continue;
			}
			if ( targetList[ key ].transform == null ) {
				garbageList.Add( key ); // mark for removal
				continue;
			}
			if ( targetList[ key ].transform.gameObject.activeSelf == false ) {
				garbageList.Add( key ); // mark for removal
				continue;
			}
			if ( targetList[ key ].targetPersists == false ) {
				if ( Time.time > targetList[ key ].lingerTime ) { // not scanned recently
					garbageList.Add( key ); // mark for removal
				}
			}
			if ( targetList[ key ].dataPersists == false ) {
				if ( Time.time > targetList[ key ].dataLingerTime && targetList[ key ].lastAnalyzed != null ) { // not analyzed recently, remove analyze info, null indicates as info removed already
					targetList[ key ].lastAnalyzed = null; // mark so don't need to keep removing info again	
					RemoveAnalyzeData( key );
				}
			}
		}
		// garbage collection -- can't remove a dict entry while iterating through the list
		for ( int i=0; i < garbageList.Count; i++ ) {
			int key = garbageList[i];
			RemoveAsDetecting( key );
			targetList.Remove( key );
		}
		garbageList.Clear();

		iteratingDict = false;
	}

	// update selection marks
	void RemoveAsDetecting ( int key ) {
		TargetData _tlKey = targetList.ContainsKey(key) == true ? targetList[key] : null;
		if ( _tlKey != null && _tlKey.cScript && _tlKey.cScript.selectionScript && selectionScript ) {
			_tlKey.cScript.selectionScript.Remove( selectionScript.myId ); // remove myself as a detecting unit from target
		}
	}
	
}

