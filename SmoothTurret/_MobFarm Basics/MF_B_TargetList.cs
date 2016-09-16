using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MF_B_TargetList : MF_AbstractTargetList {
	
	public Dictionary<int, TargetData> targetList = new Dictionary<int, TargetData>();

	public class TargetData : AbstractTargetData { 
		// inherit abstract data

		public TargetData () {} // constructor with 0 arguments

		// for creating an entry via clicked target
		public TargetData ( Transform transform, MF_AbstractStatus script, bool clickedPriority, bool targetPersists, float? sqrMagnitude ) {
			this.transform = transform;
			this.script = script;
			this.clickedPriority = clickedPriority;
			this.targetPersists = targetPersists;
			this.lastDetected = Time.time;
			this.lastAnalyzed = Time.time;
			this.sqrMagnitude = sqrMagnitude;
		} 
	}

	public override int TargetCount () {
		return targetList.Count;
	}
	
	public override bool ContainsKey ( int key ) {
		return targetList.ContainsKey( key );
	}
	
	public override void ClickAdd ( int key, Transform transform, MF_AbstractStatus script, bool clickedPriority, bool targetPersists, float? sqrMagnitude ) {
		targetList.Add( key, new TargetData( transform, script, clickedPriority, targetPersists, sqrMagnitude ) ); 
	}
	
	public override void ClickRemove ( int key ) {
		targetList[key].transform = null; // mark for removal
	}
	
	public override float? GetLastAnalyzed ( int key ) {
		return targetList[key].lastAnalyzed;
	}
	
	public override void SetClickedPriority ( int key, bool cp ) {
		targetList[key].clickedPriority = cp;
	}

	public override void ClearOld () {
		iteratingDict = true;
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
			if ( targetClearTime != 0 ) {
				if ( targetList[i].targetPersists == false ) {
					if ( Time.time >= targetList[i].lastDetected + targetClearTime ) { // not scanned recently
						garbageList.Add(i); // mark for removal
					}
				}
			}
			if ( dataClearTime != 0 ) {
				if ( targetList[i].dataPersists == false ) {
					if ( Time.time >= targetList[i].lastAnalyzed + dataClearTime && targetList[i].lastAnalyzed != null ) { // not analyzed recently, remove analyze info, null indicates as info removed already
						targetList[i].lastAnalyzed = null; // mark so don't need to keep removing info again	
						RemoveAnalyzeData( i );
					}
				}
			}
		}
		// garbage collection -- can't remove a dict entry while iterating through the list
		foreach (int i in garbageList) {
			targetList.Remove(i);
		}
		garbageList.Clear();
		iteratingDict = false;
	}
}

