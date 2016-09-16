using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MF_AbstractTargetList : MonoBehaviour {

	[Tooltip("Time target can remain on list without being refreshed.\n0 = Never clear.")]
	public float targetClearTime = 1.5f;
	[Tooltip("Time additional target info can remain on list without being refreshed.\n0 = Never clear.")]
	public float dataClearTime = 1.5f;
	[Tooltip("How often to check clear times.\n0 = every frame. (Also used to clear null enteries)")]
	public float clearInterval = .5f;
	[Tooltip("Number of targets currently in the list.")]
	public int targetCount;

	[HideInInspector] public float lastUpdate; // for target choosing timing
	[HideInInspector] public bool iteratingDict; // dict is being iterated by a coroutine

	protected List<int> garbageList = new List<int>();
	
	float lastClear;

	public class AbstractTargetData {
		public Transform transform;
		public MF_AbstractStatus script;
		public bool clickedPriority;
		public bool targetPersists; // won't drop off target list due to time
		public bool dataPersists; // won't loose data due to time
		public float? lastDetected;
		public float? lastAnalyzed; // null = not analyzed (use to test for analysis)
		public float? sqrMagnitude;
		public float? range;
		public float? auxValue1;
		public float? auxValue2;
	}

	void Start () {
		lastClear = Random.Range( -clearInterval * 1.0f, 0.0f ); // add random time to stager clear checks that would otherwise be on the same frame
	}

	void Update () {
		if ( iteratingDict == false ) {
			targetCount = TargetCount();
			// clear old targets / data
			if ( Time.time >= lastClear + clearInterval ) {
				lastClear = Time.time;
				lastUpdate = Time.time; // to time target choosing during LateUpdate() of targeting script
				
				ClearOld(); // clear targets if not detected recently, remove analyze info if not analyzed recent
			}
		}
	}

	public virtual int TargetCount () { return 0; } // this is here in the base class because it needs Update(), and Update() isn't used on the child class.

	public virtual void ClearOld () {}

	public virtual void RemoveAnalyzeData ( int key ) {} // remove certian data without removing target entry

	// the following methods are mainly to use with the selection scripts, and are overridden by individual target list scripts
	// they create a wrapper so selection scripts can interface with the various target list types (other packages may use different target list scripts and a common point of access is needed)
	public virtual bool ContainsKey ( int key ) { return false; }

	public virtual void ClickAdd ( int key, Transform transform, MF_AbstractStatus script, bool clickedPriority, bool targetPersists, float? sqrMagnitude ) {}
	
	public virtual void ClickRemove ( int key ) {}

	public virtual float? GetLastAnalyzed ( int key ) { return null; }

	public virtual void SetClickedPriority ( int key, bool cp ) {}
	
}
