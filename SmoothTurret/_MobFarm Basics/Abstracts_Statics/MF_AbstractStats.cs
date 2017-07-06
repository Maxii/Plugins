using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum DisableTime { Dying, Death, DontDisable }

[HelpURL("http://mobfarmgames.weebly.com/mf_abstractstats.html")]
public abstract class MF_AbstractStats : MonoBehaviour {

	[Split1()]
	public float _shield;
	public virtual float shield {
		get { return _shield; }
		set { _shield = value; }
	}
	[Split2()]
	public float _shieldMax;
	public virtual float shieldMax {
		get { return _shieldMax; }
		set { _shieldMax = value; }
	}

	[Split1()]
	public float _armor;
	public virtual float armor {
		get { return _armor; }
		set { _armor = value; }
	}
	[Split2()]
	public float _armorMax;
	public virtual float armorMax {
		get { return _armorMax; }
		set { _armorMax = value; }
	}

	[Split1()]
	public float _health = 1f;
	public virtual float health {
		get { return _health; }
		set { _health = value; }
	}
	[Split2()]
	public float _healthMax = 1f;
	public virtual float healthMax {
		get { return _healthMax; }
		set { _healthMax = value; }
	}

	[Space(8f)]
	[Split1()]
	public float _energy; // currently unused
	public virtual float energy {
		get { return _energy; }
		set { _energy = value; }
	}
	[Split2()]
	public float _energyMax;
	public virtual float energyMax {
		get { return _energyMax; }
		set { _energyMax = value; }
	}

	[Space(8f)]
	[Split1Attribute()]
	public float size;

	float _monitor;
	public virtual float monitor {
		get { return _monitor; }
		set { _monitor = monitor; }
	}


	[Space(8f)]
	[Split1Attribute("When this unit is killed, when to disable this object.")]
	public DisableTime disableAt;
	[Split1(true, "'Dying' is an intermediate stage prior to death. A value here can give time to perform actions or show various effects before being killed.")]
	public float dyingDuration;
	[Split2(true, "How long to wait before telling all fx to end. A small value here will give fx time to begin.")]
	public float deathDuration = .1f;

	[HideInInspector] public List<MF_FxController> fxScript; // supplied by fx controller
	[HideInInspector] public List<MF_AbstractStats> statsScript; // supplied by other stats scripts down the hierarchy
	[HideInInspector] public float damageID; // used so an explosion doesn't damage the same script multiple times
	[HideInInspector] public AP_Reference poolRefScript; // record location of object pool reference script (if used)

	[HideInInspector] public bool initialized;
	bool doingValidate;
	int totalFxScripts;
	int finished;
	bool didDying;

	public virtual void OnValidate () {
		if ( Application.isPlaying == true ) {
			doingValidate = true;

			shieldMax = _shieldMax;
			armorMax = _armorMax;
			healthMax = _healthMax;
			energyMax = _energyMax;
			shield = _shield;
			armor = _armor;
			health = _health;
			energy = _energy;

			doingValidate = false;
			UpdateStats();
		}
	}

	public void UpdateStats () {
		if ( doingValidate == false && gameObject.activeSelf == true ) {
			if ( fxScript.Count > 0 ) {
				for ( int i=0; i < fxScript.Count; i ++ ) {
					if ( fxScript[i] != null ) { fxScript[i].CheckUnit(); } // send to FxController
				}
			}
		}
	}
		
	public virtual void Awake () {
		MF_AbstractStats sScript = null;
		if ( transform.parent ) { sScript = transform.parent.GetComponentInParent<MF_AbstractStats>(); }
		if ( sScript ) { sScript.statsScript.Add( this ); }
		poolRefScript = gameObject.GetComponent<AP_Reference>();
		if ( size == 0 ) { // Find size of object if not already defined
			size = UtilityMF.FindColliderBoundsSize( transform, true );
		}
	}

	public virtual void OnEnable () {
		doingValidate = true;
		shield = shieldMax;
		armor = armorMax;
		health = healthMax;
		energy = energyMax;
		doingValidate = false;

		didDying = false;
		totalFxScripts = 0;
		finished = 0;

		if ( statsScript.Count > 0 ) {
			for ( int i=0; i < statsScript.Count; i ++ ) {
				if ( statsScript[i] != null ) { statsScript[i].gameObject.SetActive( true ); } // enable other disabled stat script objects
			}
		}
		initialized = true;
//		if ( fxScript.Count > 0 ) {
//			for ( int i=0; i < fxScript.Count; i ++ ) {
//				if ( fxScript[i] != null ) { fxScript[i].InitializeStats( this ); } // send to FxController
//			}
//		}
		UpdateStats();
	}

	public virtual void OnDisable () {
		if ( fxScript.Count > 0 ) {
			for ( int i=0; i < fxScript.Count; i ++ ) {
				if ( fxScript[i] != null ) { fxScript[i].CheckUnit(); } // send to FxController
			}
		}
		initialized = false;
	}

	// generalized damage applied to stats in order of: shield > armor > health
	public virtual float DoDamage ( float damage ) {
		return DoDamage ( damage, MFnum.StatType.General, MFnum.DamageType.General, Vector3.zero, null, 1f, 1f, 1f, 1f, 1f, 0f );
	}
	// apply generalized damage to specific stat
	public virtual float DoDamage ( float damage, MFnum.StatType apply ) { 
		return DoDamage ( damage, apply, MFnum.DamageType.General, Vector3.zero, null, 1f, 1f, 1f, 1f, 1f, 0f );
	}
	// detailed damage applied to stats in order of: shield > armor > health
	public virtual float DoDamage ( float damage, MFnum.DamageType damType, Vector3 loc, AudioSource audio, float multS, float multA, float multH, float multE, float multPen, float addReduce ) {
		return DoDamage ( damage, MFnum.StatType.General, damType, loc, audio, multS, multA, multH, multE, multPen, addReduce );
	}
	// detailed damage applied to specific stat
	public virtual float DoDamage ( float damage, MFnum.StatType apply, MFnum.DamageType damType, Vector3 loc, AudioSource audio, float multS, float multA, float multH, float multE, float multPen, float addReduce ) {
		float d = damage;
		if ( apply == MFnum.StatType.General ) {
			d = ApplyDamage ( MFnum.StatType.Shield, d, damType, loc, audio, multS, multA, multH, multE, multPen, addReduce );
			d = ApplyDamage ( MFnum.StatType.Armor, d, damType, loc, audio, multS, multA, multH, multE, multPen, addReduce );
			d = ApplyDamage ( MFnum.StatType.Health, d, damType, loc, audio, multS, multA, multH, multE, multPen, addReduce );
			// energy not in succession for normal damage
		} else if ( apply == MFnum.StatType.Shield ) {
			d = ApplyDamage ( MFnum.StatType.Shield, damage, damType, loc, audio, multS, multA, multH, multE, multPen, addReduce );
		} else if ( apply == MFnum.StatType.Armor ) {
			d = ApplyDamage ( MFnum.StatType.Armor, damage, damType, loc, audio, multS, multA, multH, multE, multPen, addReduce );
		} else if ( apply == MFnum.StatType.Health ) {
			d = ApplyDamage ( MFnum.StatType.Health, damage, damType, loc, audio, multS, multA, multH, multE, multPen, addReduce );
		} else if ( apply == MFnum.StatType.Energy ) {
			d = ApplyDamage ( MFnum.StatType.Energy, damage, damType, loc, audio, multS, multA, multH, multE, multPen, addReduce );
		}
		return d;
	}

	public virtual float ApplyDamage ( MFnum.StatType stat, float damage, MFnum.DamageType damType, Vector3 loc, AudioSource audio, float multS, float multA, float multH, float multE, float multPen, float addReduce ) {
		// indended to be overridden - insert rules for how damage is handled
		return damage;
	}

	public virtual void DoDying () {
		if ( didDying == true ) { return; } // don't call more than once
		didDying = true;
		if ( statsScript.Count > 0 ) {
			for ( int i=0; i < statsScript.Count; i ++ ) {
				if ( statsScript[i] != null ) { statsScript[i].DoDying(); } // send to other stats scripts
			}
		}
		if ( fxScript.Count > 0 ) { // fx controller can detach and perform fx
			for ( int i=0; i < fxScript.Count; i ++ ) {
				if ( fxScript[i] != null ) { fxScript[i].DoDying( disableAt == DisableTime.Dying ); } // send to FxController
			}
		}
		if ( disableAt == DisableTime.Dying ) {
			gameObject.SetActive( false );
		}
		Invoke( "DoDeath", dyingDuration );
	}
	public virtual void DoDeath () {
		if ( fxScript.Count > 0 ) { // fx controller can detach and perform fx
			for ( int i=0; i < fxScript.Count; i ++ ) {
				if ( fxScript[i] != null ) { fxScript[i].DoDeath( disableAt == DisableTime.Death ); } // send to FxController
			}
		}
		if ( disableAt == DisableTime.Death ) {
			gameObject.SetActive( false );
		}
		Invoke( "DoDeathEnd", deathDuration );
	}
	public virtual void DoDeathEnd () {
		if ( fxScript.Count > 0 ) { // fx controller will end fx. will call FxFinished when done
			for ( int i=0; i < fxScript.Count; i ++ ) {
				if ( fxScript[i] != null ) {
					totalFxScripts++;
					fxScript[i].DoDeathEnd();
				} // send to FxController
			}
		} else { // not using fx
			DoDestroy();
		}
	}
	public virtual void FxFinished () { 
		// wait unitl all fx controllers have checked in as finished
		finished++;
		if ( finished >= totalFxScripts ) { DoDestroy(); }
	}

	public virtual void DoDestroy () {
		// destroy or despawn object
		if ( poolRefScript == null || MF_AutoPool.Despawn( poolRefScript ) == false ) {
			if ( transform.parent != null ) { // part of larger object
				gameObject.SetActive( false );
			} else {
				Destroy( gameObject );
			}
		}
	}

}
