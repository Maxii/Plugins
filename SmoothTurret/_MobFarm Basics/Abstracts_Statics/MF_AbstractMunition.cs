using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[HelpURL("http://mobfarmgames.weebly.com/mf_abstractmunition.html")]
public class MF_AbstractMunition : MonoBehaviour {

	[Tooltip("Time after munition death to begin stopping all fx. A small value will give time for fx to begin.")]
	public float deathDuration = .1f;
	[Space(8f)]
	[Split1()]
	public int _stage; // mainly for missiles, but could adapted for other purposes
	public virtual int stage {
		get { return _stage; }
		set { _stage = value; }
	}
	[Split2()]
	public int stageStart;

	[HideInInspector] public float duration;
	[HideInInspector] public float monitor;
	[HideInInspector] public AP_Reference poolRefScript; // record location of object pool reference script (if used)
	[HideInInspector] public MF_FxController fxScript;

	protected float damageID;
	protected float startTime;

	protected virtual void OnValidate () {
		if ( Application.isPlaying == true ) {
			stage = _stage;
			UpdateStats();
		}
	}

	protected virtual void Awake () {
		poolRefScript = GetComponent<AP_Reference>();
		damageID = GetInstanceID();
	}

	protected virtual void OnEnable () {
		stage = stageStart;
		startTime = Time.time;
		monitor = 0f;
	}

	public void UpdateStats () {
		if ( fxScript ) { fxScript.CheckUnit(); }
	}

	public virtual void DoDeath () {
		if ( fxScript ) { fxScript.DoDeath( true ); } // send to FxController
		gameObject.SetActive( false );
		Invoke( "DoDeathEnd", deathDuration );
	}
	public virtual void DoDeathEnd () {
		// fx controller will end fx. will call FxFinished when done
		if ( fxScript ) {
			fxScript.DoDeathEnd(); // send to FxController
		} else { // not using fx
			DoDestroy();
		}
	}
	public virtual void FxFinished () { // MF_FxController finished
		DoDestroy();
	}

	public virtual void DoDestroy () { // ready to destroy or despawn
		if ( poolRefScript == null || MF_AutoPool.Despawn( poolRefScript ) == false ) {
			Destroy( gameObject );
		}
	}
}
