using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace MFnum {
	public enum ScanSource { None, Analyzer, Detector }
}

public class MF_AbstractTargetList : MonoBehaviour {

//	[Tooltip("Time target can remain on list without being refreshed.\n0 = Never clear.")]
//	public float targetClearTime = 1.5f;
//	[Tooltip("Time additional target info can remain on list without being refreshed.\n0 = Never clear.")]
//	public float dataClearTime = 1.5f;
	[Tooltip("How often to check target list to clear old targets and data. And also used to time target choosing in a targeting script.\n0 = every frame.")]
	public float refreshInterval = .5f;
	[Tooltip("Number of targets currently in the list.")]
	public int targetCount;

	[HideInInspector] public float lastUpdate; // for target choosing timing
	[HideInInspector] public bool iteratingDict; // dict is being iterated by a coroutine
	[HideInInspector] public float nextRefresh;

	protected MF_AbstractSelection selectionScript;
	protected List<int> poiCreateList = new List<int>(); // used to create poi
	protected List<int> garbageList = new List<int>(); // used to remove old entries
	protected static List<int> workingIDsToClean = new List<int>(); // to prevent dict error. can't change lastAnalyzed while iterating dict

	public class AbstractTargetData {
		public Transform transform;
		public MF_AbstractClassify cScript;
		public MF_AbstractStats sScript;
		public bool clickedPriority;
		public bool targetPersists; // won't drop off target list due to time
		public bool dataPersists; // won't loose data due to time
		public float? lastDetected;
		public float? lastAnalyzed; // null = not analyzed (use to test for analysis)
		public float lingerTime;
		public float dataLingerTime;
//		public float? sqrMagnitude;
//		public float? range;

		public float? auxValue1;
		public float? auxValue2;

//		public PoinOfInterest poi;
		public MFnum.ScanSource hasPrecision;
		public MFnum.ScanSource hasAngle;
		public MFnum.ScanSource hasRange;
		public MFnum.ScanSource hasVelocity;
		public MFnum.ScanSource hasFaction;
	}

//	public struct PoinOfInterest { // point of interest
//		public bool isPoi;
//		public float endTime;
//		public float prox;
//	}

	public virtual void Awake () {
		selectionScript = transform.root.GetComponent<MF_AbstractSelection>();
	}

	public virtual void OnEnable () { // reset for object pool support
		lastUpdate = 0f;
		iteratingDict = false;
		nextRefresh = Random.Range( 0.0f, 1.0f ) + refreshInterval; // add random time to stager clear checks that would otherwise be on the same frame
		poiCreateList.Clear();
		garbageList.Clear();
		workingIDsToClean.Clear();
	}

	void Update () {
		if ( iteratingDict == false ) {
			targetCount = TargetCount();
			// clear old targets / data
			if ( Time.time >= nextRefresh ) {
				nextRefresh += refreshInterval;
				lastUpdate = Time.time; // to time target choosing during LateUpdate() of targeting script
				
				ClearOld(); // clear targets if not detected recently, remove analyze info if not analyzed recent

				RefreshMarks();
			}
		}
	}

	public virtual void RefreshMarks () {}

	public virtual int TargetCount () { return 0; } // this is here in the base class because it needs Update(), and Update() isn't used on the child class.

	public virtual void ClearOld () {}

	public virtual void RemoveAnalyzeData ( int key ) {} // remove certian data without removing target entry

	// the following methods are mainly to use with the selection scripts, and are overridden by individual target list scripts
	// they create a wrapper so selection scripts can interface with the various target list types (other packages may use different target list scripts and a common point of access is needed)
	public virtual bool ContainsKey ( int key ) { return false; }

	public virtual void ClickAdd ( int key, Transform transform, MF_AbstractClassify cScript, MF_AbstractStats sScript, bool clickedPriority, bool targetPersists ) {}
	
	public virtual void ClickRemove ( int key ) {}

	public virtual float? GetLastAnalyzed ( int key ) { return null; } // need to be overidden by child class becasue targelList doesn't appear in base

	public virtual float GetDataLingerTime ( int key ) { return 0f; } // need to be overidden by child class becasue targelList doesn't appear in base

	public virtual MFnum.ScanSource GetHasFaction ( int key ) { return MFnum.ScanSource.None; } // need to be overidden by child class becasue targelList doesn't appear in base

	public virtual bool GetPoi ( int key ) { return false; } // need to be overidden by child class becasue targelList doesn't appear in base

	public virtual bool GetJamSource ( int key ) { return false; } // need to be overidden by child class becasue targelList doesn't appear in base

	public virtual void SetClickedPriority ( int key, bool cp ) {} // need to be overidden by child class becasue targelList doesn't appear in base
	
}
