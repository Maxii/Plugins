using UnityEngine;
using System.Collections;

public class MF_BasicWeapon : MonoBehaviour {

	public enum GravityUsage { Default, Gravity, No_Gravity }

	[Header("Shot settings:")]
	public GameObject shot;
	public AudioSource shotSound;
	[Tooltip("(deg radius)\nAdds random inaccuracy to shots.\n0 = perfect accuracy.")]
	public float inaccuracy; // in degrees
	[Header("Set 2 of 3, and the 3rd will be computed")]
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
	public int maxAmmoCount; 
	public bool unlimitedAmmo;
	[Tooltip("Triggered when Max Ammo Count is 0.")]
	public float reloadTime;
	public bool dontReload;
	[Header("Other Settings:")]
	[Tooltip("May be used to selectively activate/deactivate weapons.")]
	public bool active = true;
	[Tooltip("(deg radius)\nIncrease to apparent target size. Used to cause a weapon to begin firing before completely aimed at target.")]
	public float aimTolerance; 
	[Tooltip("True: Change all shots to use gravity.\nFalse: Will use gravity only if shot rigidbody uses gravity.")]
	public GravityUsage gravity;
	[Header("Exit point(s) of shots")]
	[Tooltip("If multiple exits, they will be used sequentially. (Usefull for a missile rack, or multi-barrel weapons)")]
	public GunExit[] exits;

	[HideInInspector] public Vector3 platformVelocity;
	[HideInInspector] public int curAmmoCount;
	[HideInInspector] public int curBurstCount;
	[HideInInspector] public int curExit;
	[HideInInspector] public float curInaccuracy;
	[HideInInspector] public bool bursting;
	[HideInInspector] public float delay;

	float lastLosCheck;
	bool losClear;
	bool usingGravity;
	bool error;

	[System.Serializable]
	public class GunExit {
		public Transform transform;
		[HideInInspector] public GameObject flare;
		[HideInInspector] public ParticleSystem particleComponent;
	}

	void OnValidate() {
		Initialize();
	}

	public void Initialize () { // call this method if shot prefab changes, or if shotSpeed, maxRange, or shotDurarion change
		curInaccuracy = inaccuracy; // initialize

		usingGravity = false; // remains false if forceing no gravity or if projectil doesn't use it
		if ( shot.GetComponent<Rigidbody>() ) { // verify rigidbody
			if ( gravity == GravityUsage.Gravity ) { // force gravity
				usingGravity = true;
			} else if ( shot.GetComponent<Rigidbody>().useGravity == true && gravity == GravityUsage.Default ) { // use gravity if projectile does
				usingGravity = true;
			}
		}
		
		// compute missing value: shotSpeed, maxRange, shotDuration
		if (shotSpeed <= 0) {
			shotSpeed = maxRange / shotDuration;
		} else if (maxRange <= 0) {
			maxRange = shotSpeed * shotDuration;
		} else { // compute shotDuration even if all 3 are set to keep math consistant
			shotDuration = maxRange / shotSpeed;
		}
	}
	
	public void Start () {
		if (CheckErrors() == true) { return; }
		
		curAmmoCount = maxAmmoCount;
		curBurstCount = burstAmount;
		
		// find muzzle flash particle systems
		for (int e=0; e < exits.Length; e++) {
			if ( exits[e].transform ) {
				for ( int f=0; f < exits[e].transform.childCount; f++ ) {
					if ( exits[e].transform.GetChild(f).GetComponent<ParticleSystem>() ) {
						exits[e].flare = exits[e].transform.GetChild(f).gameObject;
						exits[e].particleComponent = exits[f].flare.GetComponent<ParticleSystem>();
						exits[e].particleComponent.Stop(true);
						break;
					}
				}
			}
		}
	}

	// use this to fire weapon
	public void Shoot () {
		if ( active == false && bursting == false ) { return; }
		if ( ReadyCheck() == true ) {
			DoFire();
		}
	}

	// once started, will fire a full weapon burst
	public void ShootBurst () {
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
			// out of ammo
		} else {
			if ( Time.time >= delay ) {
				// reset burst and ammo
				if (curAmmoCount <= 0) {
					curAmmoCount = maxAmmoCount;
					curBurstCount = burstAmount;
					curExit = 0;
				}
				if (curBurstCount <= 0) {
					curBurstCount = burstAmount;
				}
				return true;
			}
		}
		return false;
	}
	
	// use this only if already checked if weapon is ready to fire
	public virtual void DoFire () {
		if (error == true) { return; }
		if (active == false) { return; }
		
		// fire weapon
		// create shot
		for (int spr=0; spr < shotsPerRound; spr++) {
			GameObject myShot = (GameObject) Instantiate(shot, exits[curExit].transform.position, exits[curExit].transform.rotation);
			Vector2 errorV2 = Random.insideUnitCircle * curInaccuracy;
			myShot.transform.rotation *= Quaternion.Euler(errorV2.x, errorV2.y, 0);
			Rigidbody _rb = myShot.GetComponent<Rigidbody>();
			_rb.velocity = platformVelocity + (myShot.transform.forward * shotSpeed);
			_rb.useGravity = usingGravity;
			MF_BasicProjectile shotScript = myShot.GetComponent<MF_BasicProjectile>();
			shotScript.duration = shotDuration;
		}
		// flare
		if (exits[curExit].flare) {
			exits[curExit].particleComponent.Play();
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
		if (curBurstCount <= 0) { 
			bursting = false;
			if ( burstAmount != 0 ) { // 0 = unlimited burst, don't use burstResetTime
				delay = Time.time + Mathf.Max(cycleTime, burstResetTime); // burstResetTime cannot be < cycleTime
			}
		}
		if (curAmmoCount <= 0 ) { 
			delay = Time.time + Mathf.Max(cycleTime, reloadTime); // reloadTime cannot be < cycleTime
		}
	}
	
	// check if the weapons is aimed at the target location - does not account for shot intercept point. Use the AimCheck in the SmoothTurret script for intercept. This is here to use the weapon script without a turret.
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
		if (active == false) { return false; }
		float _sqRange = (exits[curExit].transform.position - target.position).sqrMagnitude;
		if ( usingGravity == true ) { // ballistic range **** does not account for height ?
			float _ballRange = (shotSpeed*shotSpeed) / -Physics.gravity.y;
			if ( _sqRange <= (_ballRange*_ballRange) ) {
				return true;
			}
		} else { // standard range
			if ( _sqRange <= (maxRange*maxRange) ) {
				return true;
			}
		}
		return false;
	}
	
	public virtual bool CheckErrors () {
		error = false;
		string _object = gameObject.name;
		if (shot == false) { Debug.Log(_object+": Weapon shot object hasn't been defined."); error = true; }
		if (shotsPerRound <= 0) { Debug.Log(_object+": Weapon shotsPerRound must be > 0."); error = true; }
		if (exits.Length <= 0) { Debug.Log(_object+": Weapon must have at least 1 exit defined."); error = true; }
		for (int e=0; e < exits.Length; e++) {
			if (exits[e].transform == false) { Debug.Log(_object+": Weapon exits index "+e+" hasn't been defined."); error = true; }
		}
		int _e1 = 0;
		if (shotSpeed <= 0) { _e1++; }
		if (maxRange <= 0) { _e1++; }
		if (shotDuration <= 0) { _e1++; }
		if (_e1 > 1) {
			maxRange = 1f; // prevent div 0 error if another script accesses maxRange
			Debug.Log(_object+": 2 of 3 need to be > 0: shotSpeed, maxRange, shotDuration");
			error = true;
		}
		
		return error;
	}
}
