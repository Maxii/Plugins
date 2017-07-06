using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[HelpURL("http://mobfarmgames.weebly.com/mf_fxcontroller.html")]
public class MF_FxController : MonoBehaviour {

	public enum FxState { Alive, Dying, Dead, Shield, Armor, Health, Energy, Monitor, Stage, Trigger }
	public enum FxOp { Equals, LessThan, MoreThan, LessEqual, MoreEqual, Decreases, Increases, Changes }
	public enum WFS { minTime, maxTime, fxDuration }
	public enum Method { Begin, End, Reset }
	public enum DetachTime { ScriptDisable, Dying, Death, DontDetach }

	[Tooltip("When to detach the MF_FxController object from the unit.")]
	public DetachTime detachAt;
	[Tooltip("When detaching, transfer unit rigidbody properties to the rigidbody on this object.")]
	public bool transferMotion;
	public FxItem[] fxList;

	float shield;
	float armor;
	float health;
	float energy;
	float stage;
	float monitor;
	float oldShield;
	float oldArmor;
	float oldHealth;
	float oldEnergy;
	float oldStage;
	float oldMonitor;

	Rigidbody myRigidbody;
	Rigidbody parentRigidbody;
	Transform origParent;
	MF_AbstractStats statsScript;
	MF_AbstractMunition munitionScript; // for monitor value
	MF_AbstractMobility mobilityScript; // for monitor value
	MF_AbstractComponent compScript; // for monitor value
	float startTime;
	float longestDuration;
//	bool initialized;
	bool dying;
	bool dead;
	bool addedSelf;
	bool validate;

	[System.Serializable]
	public class FxItem {
		public GameObject obj;
		public FxState active;
		public FxOp op;
		public float percent;
		public Transform location; // if blank, will assume parent
		public bool alive;
		public bool once;
		public bool duration;
		public float minTime;
		public float maxTime;
		public bool sendValue;
		public bool invertValue;
		[HideInInspector] public GameObject tObj;
		[HideInInspector] public ParticleSystem particle;
		[HideInInspector] public MF_LensFlare flare;
		[HideInInspector] public TrailRenderer trail;
		[HideInInspector] public AudioSource audio;
		[HideInInspector] public float trailOrigTime;
		[HideInInspector] public MF_FxController thisScript;
		[HideInInspector] public MF_AbstractStats statsScript;
		[HideInInspector] public float fxDuration;
		[HideInInspector] public bool beginFlag;
		[HideInInspector] public bool endFlag;
		float partOrigStartLifeMin;
		float partOrigStartLifeMax;
		float partOrigRateT;
		float partOrigRateD;
		WaitForSeconds minWFS;
		WaitForSeconds maxWFS;
		Coroutine trailDecay;
		bool trailDecayRunning;
		bool timerRunning;
		bool onceTriggered;
		bool validated;

		public void Validate () {
			if ( obj ) {
				if ( maxTime < 0f ) { maxTime = 0f; }
				if ( minTime < 0f ) { minTime = 0f; }
				maxTime = maxTime < minTime ? minTime : maxTime;
				minWFS = new WaitForSeconds( minTime );
				maxWFS = new WaitForSeconds( maxTime - minTime );
				if ( obj && validated == false ) {
					if ( location == null ) {
						if ( obj.transform.parent == null ) { // is a prefab
							location = thisScript.transform;
						} else {
							location = obj.transform.parent;
						}
					}
					if ( location ) {
						if ( obj.transform.parent == null ) { // is a prefab
							tObj = (GameObject) Instantiate( obj, location.position, location.rotation, location );
						} else {
							tObj = obj;
							if ( location != thisScript.transform ) {
								tObj.transform.position = location.position;
								tObj.transform.rotation = location.rotation;
								tObj.transform.parent = location;
							}
						}
					}
				}
				// cache references
				if ( validated == false ) { // prevent monitor from 0-ing values and then revalidation occouring thus nuking the original
					particle = tObj.GetComponent<ParticleSystem>();
					flare =  tObj.GetComponent<MF_LensFlare>();
					trail = tObj.GetComponent<TrailRenderer>();
					audio = tObj.GetComponent<AudioSource>();
					if ( trail ) { if ( trailOrigTime == 0 ) { trailOrigTime = trail.time; } }
					if ( particle ) {
						partOrigStartLifeMin = particle.main.startLifetime.constantMin;
						partOrigStartLifeMax = particle.main.startLifetime.constantMax;
						partOrigRateT = particle.emission.rateOverTime.constantMax;
						partOrigRateD = particle.emission.rateOverDistance.constantMax;
					}
				}
				fxDuration = Mathf.Max( particle ? particle.main.duration : 0f, flare ? flare.fadeTime : 0f, trail ? trail.time : 0f, audio ? audio.clip.length : 0f );
			}
			validated = true;
		}

		public void Enable () {
			if ( tObj && validated ) {
				if ( !particle && !flare && !trail && !audio ) { tObj.SetActive( false ); }
				if ( location != thisScript.transform ) {
					tObj.transform.position = location.position;
					tObj.transform.rotation = location.rotation;
					tObj.transform.parent = location;
				}
				if ( particle ) {
					var m = particle.main.startLifetime;
					var e = particle.emission;
					m.constantMin = partOrigStartLifeMin;
					m.constantMax = partOrigStartLifeMax;
					e.rateOverTime = partOrigRateT;
					e.rateOverDistance = partOrigRateD;
					particle.Stop(); particle.Clear();
				}
				if ( flare ) { flare.flare.brightness = 0f; }
				if ( trail ) { trail.time = 0f; trail.Clear(); }
				if ( audio ) { audio.Stop(); }
				onceTriggered = false;
				trailDecayRunning = false;
				timerRunning = false;
				beginFlag = false;
				endFlag = true;
			}
		}
			
		public void Begin () {
			if ( tObj && validated && ( once == false || onceTriggered == false ) ) {
				beginFlag = true;
				if ( timerRunning == true ) { return; }
				endFlag = false;
				onceTriggered = true;
				tObj.SetActive( true );
				if ( particle ) { particle.Play(); }
				if ( flare ) { flare.FadeIn(); }
				if ( trail ) {
					if ( trailDecay != null && trailDecayRunning ) {
						thisScript.StopCoroutine( trailDecay );
					}
					trailDecayRunning = false;
					trail.Clear();
					trail.time = trailOrigTime;
				}
				if ( audio ) { audio.Play(); }

				if ( duration == true && thisScript.gameObject.activeInHierarchy == true ) {
					if ( thisScript ) { thisScript.StartCoroutine( Timer() ); }
				}
			}
		}

		public void End () { // stop playing, still need to let it finish
			if ( tObj && validated ) {
				endFlag = true;
				if ( timerRunning == true ) { return; }
				beginFlag = false;
				if ( particle ) { particle.Stop(); }
				if ( flare ) { flare.FadeOut(); }
				if ( trail && trailDecayRunning == false && thisScript.gameObject.activeInHierarchy == true ) { trailDecay = thisScript.StartCoroutine( TrailDecay() ); }
				if ( audio && audio.loop == true ) { audio.Stop(); }
				if ( !particle && !flare && !trail && !audio ) { tObj.SetActive( false ); }
			}
		}

		public void Monitor ( float value )  {
			if ( sendValue == true && validated ) {
				if ( invertValue == true ) { value = 1f - value; }

				if ( particle ) {
					var m = particle.main.startLifetime;
					var e = particle.emission;
					m.constantMin = partOrigStartLifeMin * value;
					m.constantMax = partOrigStartLifeMax * value;
					e.rateOverTime = partOrigRateT * value;
					e.rateOverDistance = partOrigRateD * value;
				}
				if ( flare ) { flare.multiplier = value; }
				if ( trail && trailDecayRunning == false ) { trail.time = trailOrigTime * value; }
				if ( audio ) { audio.volume = value; }
			}
		}

		IEnumerator TrailDecay () {
			trailDecayRunning = true;
			while ( trail.time > 0f ) {
				trail.time -= trailOrigTime * Time.deltaTime;
				yield return null;
			}
			trailDecayRunning = false;
		}

		IEnumerator Timer () {
			timerRunning = true;
			yield return minWFS;
			if ( endFlag == true ) {
				timerRunning = false;
				this.End();
				yield break;
			}
			timerRunning = false;
			yield return maxWFS;
			this.End();
		}
	}
	// -------------------------------------- end of FxItem

	void OnValidate () {
		if ( Application.isPlaying == true ) {
			if ( dying == true || dead == true ) { return; } // don't change while effects are possibly detached
			if ( validate == false ) { // prevent Add() running more than once
				statsScript = UtilityMF.GetComponentInParent<MF_AbstractStats>( transform );
				if ( statsScript ) { statsScript.fxScript.Add( this ); }
			}

			float dur = 0f;
			for ( int i=0; i < fxList.Length; i++ ) {
				fxList[i].thisScript = this;
				fxList[i].statsScript = statsScript;
				fxList[i].Validate();
				dur = Mathf.Max( fxList[i].fxDuration, fxList[i].duration == true ? fxList[i].minTime : 0f );
				longestDuration = Mathf.Max( longestDuration, dur );
			}
			validate = true;
			CheckUnit();
		}
	}

	void Awake () {
		OnValidate();
		origParent = transform.parent;
		myRigidbody = GetComponent<Rigidbody>();
		if ( origParent ) {
			parentRigidbody = origParent.root.GetComponent<Rigidbody>();
			munitionScript = transform.parent.GetComponent<MF_AbstractMunition>();
			mobilityScript = transform.parent.GetComponent<MF_AbstractMobility>();
			compScript = transform.parent.GetComponent<MF_AbstractComponent>();
		}
		if ( munitionScript ) { munitionScript.fxScript = this; }
		if ( mobilityScript ) { mobilityScript.fxScript.Add( this ); }
		if ( compScript ) { compScript.fxScript.Add( this ); }

	}

	void OnEnable() {
		dying = false;
		dead = false;
		for ( int i=0; i < fxList.Length; i++ ) {
			fxList[i].Enable();
		}

//		if ( statsScript == null ) { statsScript = script; } // in case awake or validate hasn't been called yet
		if ( statsScript ) {
			shield = statsScript.shield / statsScript.shieldMax;
			armor = statsScript.armor / statsScript.armorMax;
			health = statsScript.health / statsScript.healthMax;
			energy = statsScript.energy / statsScript.energyMax;
		}
		if ( munitionScript ) { stage = (float) munitionScript.stage; }
		monitor = GetMonitorValue();
		oldShield = shield;
		oldArmor = armor;
		oldHealth = health;
		oldEnergy = energy;
		oldStage = stage;
		oldMonitor = monitor;

		CheckUnit();
	}

//	public void InitializeStats ( MF_AbstractStats script ) { // sent from stats script, need to time after stats are reset
//		if ( statsScript == null ) { statsScript = script; } // in case awake or validate hasn't been called yet
//		shield = statsScript.shield / statsScript.shieldMax;
//		armor = statsScript.armor / statsScript.armorMax;
//		health = statsScript.health / statsScript.healthMax;
//		energy = statsScript.energy / statsScript.energyMax;
//		stage = (float) statsScript.stage;
//		monitor = GetMonitorValue();
//		oldShield = shield;
//		oldArmor = armor;
//		oldHealth = health;
//		oldEnergy = energy;
//		oldStage = stage;
//		oldMonitor = monitor;
//		initialized = true;
//	}

	void OnDisable () {
		CancelInvoke();
//		initialized = false;
	}
		
	void DoDetach () {
		// seperate fx from unit
		transform.parent = null;
		for ( int i=0; i < fxList.Length; i++ ) {
			if ( fxList[i].tObj ) {
				if ( fxList[i].location != transform ) { // not already child of fx controller
					fxList[i].tObj.transform.parent = transform; // make it a child of fx controller
				}
			}
		}
		if ( transferMotion == true && parentRigidbody && myRigidbody ) {
			myRigidbody.isKinematic = false;
			myRigidbody.mass = parentRigidbody.mass;
			myRigidbody.drag = parentRigidbody.drag;
			myRigidbody.angularDrag = parentRigidbody.angularDrag;
			myRigidbody.useGravity = parentRigidbody.useGravity;
			myRigidbody.velocity = parentRigidbody.velocity;
			myRigidbody.angularVelocity = parentRigidbody.angularVelocity;
		}
	}

	// fx to occour before upon death condition, such as breakup and pre-explosions
	public void DoDying ( bool disable ) { // called from stats script
		if ( detachAt == DetachTime.ScriptDisable && disable == true ) {
			DoDetach();
		} else if ( detachAt == DetachTime.Dying ) {
			DoDetach();
		}
		dying = true;
		CheckUnit();
	}
	// fx such as main explosion
	public void DoDeath( bool disable ) {
		if ( detachAt == DetachTime.ScriptDisable && disable == true ) {
			DoDetach();
		} else if ( detachAt == DetachTime.Death ) {
			DoDetach();
		}
		dying = false;
		dead = true;
		CheckUnit();
	}
	// time for all effects to end
	public void DoDeathEnd() {
		for ( int i=0; i < fxList.Length; i++ ) {
			fxList[i].End();
		}
		Invoke( "DoReset", longestDuration );
	}
	// reset and reattach all fx
	public void DoReset () {
		dying = false;
		dead = false;
		for ( int i=0; i < fxList.Length; i++ ) {
			if ( fxList[i].tObj ) {
				if ( fxList[i].location ) {
					if ( fxList[i].location != transform ) {
						fxList[i].tObj.transform.position = fxList[i].location.position;
						fxList[i].tObj.transform.rotation = fxList[i].location.rotation;
						fxList[i].tObj.transform.parent = fxList[i].location;
					}
				} else { // original parent is gone, destroy instead
					Destroy( fxList[i].tObj );
				}
			}
		}
		if ( origParent ) {
			if ( myRigidbody ) { myRigidbody.isKinematic = true; }
			transform.position = origParent.position;
			transform.rotation = origParent.rotation;
			transform.parent = origParent;
			// let scripts know fx have all finished
			if ( statsScript ) { statsScript.FxFinished(); } 
			if ( munitionScript ) { munitionScript.FxFinished(); }
		} else {
			Destroy( gameObject );
		}
	}

	public void CheckUnit () {
		if ( gameObject.activeInHierarchy == false ) { return; }
		if ( statsScript && statsScript.initialized == false ) { return; }
		if ( statsScript ) {
			oldShield = shield;
			oldArmor = armor;
			oldHealth = health;
			oldEnergy = energy;
			oldStage = stage;
			shield = statsScript.shield / statsScript.shieldMax;
			armor = statsScript.armor / statsScript.armorMax;
			health = statsScript.health / statsScript.healthMax;
		}
		if ( munitionScript ) { stage = (float) munitionScript.stage; }
		oldMonitor = monitor;
		monitor = GetMonitorValue(); // can be from a different script
		FxItem item = null;

		for ( int i=0; i < fxList.Length; i++ ) {
			item = fxList[i];
			if ( item.active == FxState.Trigger ) { continue; } // skip trigger
			bool result = false;
			bool dontCheckBegin = false; // checks for: will not run begin while already begun
			bool dontDoEnd = false; // checks for: will not run end while ended
			if ( item.active == FxState.Alive || item.active == FxState.Dying || item.active == FxState.Dead ) { 
				result = GetStateBool( item.active );
			} else if ( item.op == FxOp.Increases || item.op == FxOp.Decreases || item.op == FxOp.Changes ) {
				dontCheckBegin = true; dontDoEnd = true;
				if ( fxList[i].alive == true && GetUnitActive() == false ) {
					result = false ;
				} else {
					result = GetStatChange( item.active, item.op );
				}
			} else { // all stat vales, monitor, and stage
				if ( fxList[i].alive == true && GetUnitActive() == false ) {
					result = false ;
				} else {
					result = Evaluate( GetStatValue( item.active ), item.op, item.percent );
				}
			}
				
			if ( fxList[i].sendValue == true ) {
				float sValue = 0f;
				if ( item.active == FxState.Shield ) { sValue = shield; }
				else if ( item.active == FxState.Armor ) { sValue = armor; }
				else if ( item.active == FxState.Health ) { sValue = health; }
				else if ( item.active == FxState.Energy ) { sValue = energy; }
				else if ( item.active == FxState.Monitor ) { sValue = monitor; }
				fxList[i].Monitor( sValue );
			}
				
			if ( result == true ) {
				// change if don't need to check, or check is ok to change
				if ( dontCheckBegin == true || fxList[i].beginFlag == false ) { item.Begin(); }
			} else {
				// change if don't need to check, or check is ok to change
				if ( dontDoEnd == false && fxList[i].endFlag == false ) { item.End(); }
			}
		}
	}

	public void TriggerFx ( int tValue ) { // seperate control for effects. positive tValue turns on, negative tValue turns off
		bool tValueNegative = tValue < 0 ? true : false;
		tValue = Mathf.Abs( tValue );
		FxItem item = null;
		for ( int i=0; i < fxList.Length; i++ ) {
			item = fxList[i];
			bool result = false;
			bool dontCheckBegin = false; // checks for: will not run begin while already begun
			bool dontDoEnd = false; // checks for: will not run end while ended
			if ( item.active == FxState.Trigger ) {	
				if ( item.op != FxOp.Increases && item.op != FxOp.Decreases && item.op != FxOp.Changes ) { // increase, decrease, change: don't work with trigger
					result = Evaluate( (float) tValue, item.op, item.percent );
				}
			}
			if ( result == true ) {
				// change if don't need to check, or check is ok to change
				if ( tValueNegative == false ) {
					if ( dontCheckBegin == true || fxList[i].beginFlag == false ) { item.Begin(); }
				} else {
					if ( dontDoEnd == false && fxList[i].endFlag == false ) { item.End(); }
				}
			}
		}
	}

	bool GetUnitActive () {
		if ( origParent ) {
			return origParent.gameObject.activeInHierarchy;
		} else {
			return gameObject.activeInHierarchy;
		}
	}

	public bool GetStateBool ( FxState state ) {
		if ( state == FxState.Alive ) { return GetUnitActive(); }
		if ( state == FxState.Dying ) { return dying; }
		if ( state == FxState.Dead ) { return dead; }
		return false;
	}

	public bool GetStatChange ( FxState state, FxOp op ) {
		if ( state == FxState.Shield ) { return Evaluate( shield, op, oldShield ); }
		if ( state == FxState.Armor ) { return Evaluate( armor, op, oldArmor ); }
		if ( state == FxState.Health ) { return Evaluate( health, op, oldHealth ); }
		if ( state == FxState.Energy ) { return Evaluate( energy, op, oldEnergy ); }
		if ( state == FxState.Stage ) { return Evaluate( stage, op, oldStage ); }
		if ( state == FxState.Monitor ) { return Evaluate( monitor, op, oldMonitor ); }
		return false;
	}

	public float GetStatValue ( FxState state ) {
		if ( state == FxState.Shield ) { return shield; }
		if ( state == FxState.Armor ) { return armor; }
		if ( state == FxState.Health ) { return health; }
		if ( state == FxState.Energy ) { return energy; }
		if ( state == FxState.Stage ) { return stage; }
		if ( state == FxState.Monitor ) { return monitor; }
		return 0f;
	}

	public bool Evaluate ( float a, FxOp op, float b ) {
		if ( op == FxOp.Equals ) { return a == b; }
		if ( op == FxOp.LessThan ) { return a < b; }
		if ( op == FxOp.MoreThan ) { return a > b; }
		if ( op == FxOp.LessEqual ) { return a <= b; }
		if ( op == FxOp.MoreEqual ) { return a >= b; }
		if ( op == FxOp.Increases ) { return a > b; }
		if ( op == FxOp.Decreases ) { return a < b; }
		if ( op == FxOp.Changes ) { return a != b; }
		return false;
	}

	float GetMonitorValue () {
		if ( munitionScript ) { return munitionScript.monitor; }
		else if ( mobilityScript ) { return mobilityScript.monitor; }
		else if ( compScript ) { return compScript.monitor; }
		else if ( statsScript ) { return statsScript.monitor; } // make sure last in progression
		return 0f;
	}

}
