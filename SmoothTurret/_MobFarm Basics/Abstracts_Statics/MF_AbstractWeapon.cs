using UnityEngine;
using System.Collections;

public abstract class MF_AbstractWeapon : MonoBehaviour {
	
	[Header("Shot settings:")]
	public AudioSource shotSound;
	[Tooltip("(deg radius)\nAdds random inaccuracy to shots.\n0 = perfect accuracy.")]
	public float inaccuracy; // in degrees
	[Header("Set 2 of 3, and the 3rd will be computed at runtime.")]
	[Tooltip("(meters/sec)")]
	public float shotSpeed;
	[Tooltip("(meters)")]
	public float maxRange;
	[Tooltip("(seconds)")]
	public float shotDuration;
	[Header("Fire rate settings:")]
	[Tooltip("(seconds)\nThe minimum time between shots.\n0 = every frame.")]
	public float cycleTime; // min time between shots
	[Tooltip("Number of shots before a Burst Reset Time is triggered. (Seperate from Reload Time.)\n0 = Burst Reset Time is ignored.")]
	public int burstAmount; // 0 = unlimited burst
	[Tooltip("(seconds)\nThis should be more than Cycle Time, and less than Reload Time. " +
	         "Shots cannot fire quicker than Cycle Time, and Reload Time takes precedence over Burst Reset Time if they occur on the same shot.")]
	public float burstResetTime; // should be <= reloadTime
	[Tooltip("Multiple shots per fire command. (Like a shotgun blast.)")]
	public int shotsPerRound = 1;
	[Header("Ammo settings")]
	[Tooltip("Ammo count before a reload is needed.")]
	public int maxAmmoCount; 
	[Tooltip("Weapon doesn't use ammo and doesn't need ammo.")]
	public bool unlimitedAmmo;
	[Tooltip("Triggered when available ammo is 0.")]
	public float reloadTime;
	[Tooltip("Ammo won't be replenished once it is exhusted.")]
	public bool dontReload;
	[Header("Other Settings:")]
	[Tooltip("May be used to selectively activate/deactivate weapons.")]
	public bool active = true;
	[Tooltip("(deg radius)\nIncrease to apparent target size. Used to cause a weapon to begin firing before completely aimed at target.")]
	public float aimTolerance; 
	[Header("Exit point(s) of shots")]
	[Tooltip("If multiple exits, they will be used sequentially. (Usefull for a missile rack, or multi-barrel weapons)")]
	public WeaponExit[] exits;
	
	[HideInInspector] public Vector3 platformVelocity; // set from a control script
	[HideInInspector] public int curAmmoCount;
	[HideInInspector] public int curBurstCount;
	[HideInInspector] public int curExit;
	[HideInInspector] public float curInaccuracy;
	[HideInInspector] public bool bursting;
	[HideInInspector] public float delay;
	
	float lastLosCheck;
	bool losClear;
	protected bool error;
	
	[System.Serializable]
	public class WeaponExit {
		public Transform transform;
		public ParticleSystem flare;
	}

	public virtual void Start () {
		if (CheckErrors() == true) { return; }
		
		curAmmoCount = maxAmmoCount;
		curBurstCount = burstAmount;
		curInaccuracy = inaccuracy;
		
		// find muzzle flash particle systems
		for (int e=0; e < exits.Length; e++) {
			if ( exits[e].transform ) {
				if ( exits[e].flare  ) { // flare is defined
					if ( !exits[e].flare.gameObject.activeInHierarchy ) { // not active in hierarchy - flare is a prefab
						// create flare from prefab
						ParticleSystem myFlare = (ParticleSystem) Instantiate(exits[e].flare, exits[e].transform.position, exits[e].transform.rotation);
						myFlare.transform.parent = exits[e].transform;
						exits[e].flare = myFlare;
					}
					exits[e].flare.Stop(true);
				}
			}
		}
	}
	
	// use this to fire weapon
	public virtual void Shoot () {
		if ( active == false && bursting == false ) { return; }
		if ( ReadyCheck() == true ) {
			DoFire();
		}
	}
	
	// once started, will fire a full weapon burst
	public virtual void ShootBurst () {
		if ( active == false || bursting == true ) { return; }
		if ( burstAmount > 0 ) {
			StartCoroutine(DoBurst());
		} else {
			Shoot();
		}
	}
	
	IEnumerator DoBurst () {
		if ( PrivateReadyCheck() == true ) { // handle single shots and burstReset
			DoFire();
			
			// fire until burst runs out
			bursting = true;
			while ( curBurstCount > 0 ) {
				
				yield return null;
				
				if ( PrivateReadyCheck() == true ) {
					DoFire();
				}
			}
			bursting = false;
		}
	}
	
	// use to determine if weapon is ready to fire. (not realoding, waiting for cycleTime, etc.) Seperate function to allow other scripts to check ready status.
	public virtual bool ReadyCheck () {
		if ( active == false || bursting == true ) { return false; }
		return PrivateReadyCheck();
	}
	
	private bool PrivateReadyCheck () {
		if ( curAmmoCount <= 0 && dontReload == true && unlimitedAmmo == false) {
			return false; // out of ammo, can't reload, and requires ammo
		} else if ( maxAmmoCount <= 0 && unlimitedAmmo == false ) {
			return false; // can't hold ammo, and requires ammo
		} else {
			if ( Time.time >= delay ) {
				// reset burst and ammo
				if ( curAmmoCount <= 0 && unlimitedAmmo == false ) {
					curAmmoCount = maxAmmoCount;
					curBurstCount = burstAmount;
					curExit = 0;
				}
				if ( curBurstCount <= 0 ) {
					curBurstCount = burstAmount;
				}
				return true;
			} else {
				return false; // not at delay time yet
			}
		}
	}
	
	// use this only if already checked if weapon is ready to fire
	public virtual void DoFire () {
		if (error == true) { return; }
		if (active == false) { return; }

		// flare
		if (exits[curExit].flare) {
			exits[curExit].flare.Play();
		}
		// audio
		if (shotSound) {
			shotSound.Play();
		}
		
		if (unlimitedAmmo == false) { curAmmoCount--; } // only use ammo if not unlimited
		if (curBurstCount > 0) { curBurstCount--; } // don't go below 0
		curExit = MFmath.Mod(curExit+1, exits.Length); // next exit
		
		// find next delay
		delay = Time.time + cycleTime;
		if ( curBurstCount <= 0 ) { 
			bursting = false;
			if ( burstAmount != 0 ) { // 0 = unlimited burst, don't use burstResetTime
				delay = Time.time + Mathf.Max(cycleTime, burstResetTime); // burstResetTime cannot be < cycleTime
			}
		}
		if ( curAmmoCount <= 0 && unlimitedAmmo == false ) { 
			delay = Time.time + Mathf.Max(cycleTime, reloadTime); // reloadTime cannot be < cycleTime
		}
	}
	
	// check if the weapons is aimed at the target location - does not account for shot intercept point. Use the AimCheck in the turret script for intercept. This is here to use the weapon script without a turret.
	public virtual bool AimCheck ( Transform target, float targetSize ) {
		if (active == false) { return false; }
		float _targetRange = Vector3.Distance(exits[curExit].transform.position, target.position);
		float _targetFovRadius = Mathf.Clamp(   (Mathf.Atan( (targetSize / 2) / _targetRange ) * Mathf.Rad2Deg) + aimTolerance,    0, 180 );
		if ( Vector3.Angle(exits[curExit].transform.forward, target.position - exits[curExit].transform.position) <= _targetFovRadius ) {
			return true;
		} else {
			return false;
		}
	}

	public virtual bool RangeCheck ( Transform target ) {
		return RangeCheck ( target, 1f );
	}
	public virtual bool RangeCheck ( Transform target, float mult ) {
		if (active == false) { return false; }
		return true;
	}
	
	public virtual bool CheckErrors () {
		error = false;
		if (shotsPerRound <= 0) { Debug.Log( this+": Weapon shotsPerRound must be > 0."); error = true; }
		if (exits.Length <= 0) { Debug.Log( this+": Weapon must have at least 1 exit defined."); error = true; }
		for (int e=0; e < exits.Length; e++) {
			if (exits[e].transform == false) { Debug.Log( this	+": Weapon exits index "+e+" hasn't been defined."); error = true; }
		}
		return error;
	}
}
