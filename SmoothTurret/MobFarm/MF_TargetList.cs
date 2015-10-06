using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MF_TargetList : MonoBehaviour {

	public float targetClearTime = 1.5f;
	public float dataClearTime = 1.5f;
	public float clearInterval = .5f;
	public Dictionary<int, TargetData> targetList = new Dictionary<int, TargetData>();

	[HideInInspector] public float lastUpdate; // for target choosing timing

	float lastClear;
	List<int> garbageList = new List<int>();

	void Update () {
		// clear old targets / data
		if ( Time.time >= lastClear + clearInterval ) {
			lastClear = Time.time;
			lastUpdate = Time.time; // to time target choosing during LateUpdate() of targeting script
			
			ClearOld(); // clear targets if not detected recently, remove analyze info if not analyzed recent
		}
	}

	void ClearOld () {
		List<int> keys = new List<int> (targetList.Keys); // to prevent dict error. can't change lastAnalyzed while iterating dict
		foreach ( int i in keys ) {
			if ( targetList[i] == null ) {
				garbageList.Add(i); // mark for removal
				continue;
			}
			if ( targetList[i].transform == null ) {
				garbageList.Add(i); // mark for removal
				continue;
			}
			if ( Time.time >= targetList[i].lastDetected + targetClearTime ) { // not scanned recently
				garbageList.Add(i); // mark for removal
			}
			if ( Time.time >= targetList[i].lastAnalyzed + dataClearTime && targetList[i].lastAnalyzed != null ) { // not analyzed recently, remove analyze info, null indicates as info removed already
				targetList[i].lastAnalyzed = null; // mark so don't need to keep removing info again	
				// info to remove here
				TargetData.RemoveAnalyzeData( this, i );
			}
		}
		// garbage collection -- can't remove a dict entry while iterating through the list
		foreach (int i in garbageList) {
			targetList.Remove(i);
		}
		garbageList.Clear();
	}
}

