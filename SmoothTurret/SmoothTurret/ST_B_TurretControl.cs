using UnityEngine;
using System.Collections;

[HelpURL("http://mobfarmgames.weebly.com/mf_b_turretcontrol.html")]
public class ST_B_TurretControl : MF_AbstractPlatformControl {
	
	public enum ControlType { AI_AutoTarget, None }
	
	[Header("Turret will get this target:")]
	[Tooltip("Current target of the turret. This will either be supplied by the targeting script, or be assigned directly. However, if the turret is choosing its own targets, this may immediately be overwritten.")]
	[SerializeField] private Transform _target;
	[Tooltip("AI_Auto Target: AI will use a scanner to pick targets.\n\n" +
	         "None: AI will not pick targets, but will aim and fire if it is given a target.")]
	public ControlType controller;
	[Header("Options:")]
	[Split1(true, "For multiple weapons per turret.\nWill alternate their fire instead of firing all at once.")]
	public bool alternatingFire;
	[Split2("Upon firing, weapons will finish a full burst according to their burst fire setting.")]
	public bool fullBurst;
	[Split1(true, "Uses Raycast to check line of sight before firing.")]
	public bool checkLOS;
	[Split2(true, "Increase time between Raycast LoS checks to improve performance.\n0 = before every shot.")]
	public float losCheckInterval; 
	[Split1(true, "Try to determine target size from collider.")]
	public bool checkTargetSize;
	[Split2(true, "(meters)\nDefault target size to fire at if none found or provided.")]
	public float targetSizeDefault;
	
//	MF_AbstractPlatform platformScript;
	float targetSize;
	float lastLosCheck;
	bool losClear;
	float lastFire;
	bool? bursting = null;
	bool loaded;

	// the current target
	public Transform target {
		get { return _target; }
		set { if ( value != _target ) {
				_target = value;
				if ( _target ) { // in case target became null
					// find target size
					targetSize = 0; // reset
					if (checkTargetSize == true ) {
						// try to get target size from collider bounds
						targetSize = UtilityMF.FindColliderBoundsSize( _target, true );
					}
					if ( targetSize == 0 ) { targetSize = targetSizeDefault; }
				}
			}
		}
	}
	
	void OnValidate () {
		if ( loaded ) {
			Transform _t = _target;
			_target = null;
			target = _t;
		}
	}
	
	public override void Awake () {
		base.Awake(); // will CheckErrors() from base
		if ( error == true )  { return; }

		platformScript = GetComponent<MF_AbstractPlatform>();
		
		loaded = true;
		OnValidate();
	}

	public override void OnEnable () { // reset for object pool support
		base.OnEnable();
		lastLosCheck = 0;
		lastFire = 0f;
		bursting = null;
	}

	void OnDisable () { // reset for object pool support
		target = null;
	}
	
	void Update () {
		if (error == true) { return; }
		
		// AI control type
		DoControlType();
		
		// pass target to turret
		platformScript.target = _target; 
		
		// fullBurst control
		if ( fullBurst == true ) {
			DoFullBurst();
		}
		
		// check aim and fire
		if ( _target ) {
			DoAimFire();
		}
	}
	
	void DoControlType() {
		switch ( controller ) {

		case ControlType.AI_AutoTarget : // AI will aim, getting targets from targeting script
			if ( targetingScript ) { // make sure targeting script exsists, assign weapon target
				if ( targetingScript.target ) {
					target = targetingScript.target.transform;
				} else {
					target = null;
				}
			}
			break;
			
		case ControlType.None : // Turret is deactivated
			target = null;
			break;
			
		default :
			break;
		}
	}
	
	void DoAimFire() {
		if ( weapons.Length > 0 ) {
			// if weapon is not active, find one that is active
			int l = 0;
			while ( weapons[curWeapon].script.active == false && l++ <= weapons.Length ) {
				curWeapon = MFmath.Mod(curWeapon+1, weapons.Length);
			}
			
			if ( l >= weapons.Length ) {
				return; // no weapons active
			}

			// set shot speed for turret aim intercept
			platformScript.shotSpeed = weapons[curWeapon].script.shotSpeed;
			platformScript.exitLoc = weapons[curWeapon].script.exits[ weapons[curWeapon].script.curExit ].transform.position;
		}

		// check target data vs weapon requirements, set platform checkData
		if ( CheckData() == false ) { return; }

		// make sure all weapons fire if a burst has already begun
		if ( bursting == true ) {
			Shoot();
			return;
		}

		float combAimTime = Mathf.Max( aimTime, weapons[curWeapon].script.aimTime );
		
		if ( controller == ControlType.AI_AutoTarget ) { // AI will shoot
			if ( weapons.Length > 0 ) {
				
				if ( platformScript.AimCheck( targetSize, weapons[curWeapon].script.aimTolerance ) == true ) { // check if turret is aimed at target
					curAimTime = Mathf.Clamp( curAimTime + Time.deltaTime, 		0f, combAimTime ); // add to aimTime
					if ( curAimTime >= combAimTime ) {
						if ( weapons[curWeapon].script.ReadyCheck() == true ) { // early out if weapon isn't ready
							Shoot();
						}
					}
				} else {
					curAimTime = Mathf.Clamp( curAimTime - Time.deltaTime, 0f, combAimTime ); // subtract from aimTime
				}
			}
		}
	}
	
	void DoFullBurst() {
		// reset burst control
		if ( bursting == null ) { 
			for ( int i=0; i < weapons.Length; i++ ) {
				weapons[i].burst = false;
			}
			bursting = false;
		} 
		// check burst end
		if ( bursting == true ) { 
			for ( int cb=0; cb < weapons.Length; cb++ ) { // check all weapons bursting status
				if ( weapons[cb].burst == false ) {
					break; // found a weapon that hasn't started burst
				}
				if ( weapons[cb].script.bursting == true && weapons[cb].script.active == true ) {
					break; // found a weapon not finished bursting
				}
				// end bursting
				bursting = null;
			}
		}
	}
	
	void Shoot() {
		// check line of sight
		losClear = true;
		if ( checkLOS == true ) {
			if (Time.time >= lastLosCheck + losCheckInterval) {
				RaycastHit _hit;
				Vector3 _startPos = weapons[curWeapon].script.exits[ weapons[curWeapon].script.curExit ].transform.position;
				if ( Physics.Linecast(_startPos, _target.position, out _hit) ) { 
					// recursively search parent of hit collider for target
					losClear = UtilityMF.RecursiveParentTransformSearch( _target, _hit.transform );
				}
				lastLosCheck = Time.time;
			}
		}
		
		// fire weapons
		if (losClear == true) {
			if ( weapons[curWeapon].script.RangeCheck( _target ) == true ) {

				// if targeting script can mark targets, mark when firing
				if ( targetingScript && targetingScript.isMarkingTarg == true ) {
					if ( weapons.Length > 0 ) {
						targetingScript.SetMarkedTime( weapons[ curWeapon ].script.GetTimeOfFlight( _target ) );
					}
				}

				if ( alternatingFire == true && weapons.Length > 1 ) { // alternate fire bewteen weapons
					weapons[curWeapon].script.platformVelocity = platformScript.velocity; // send velocity to weapon script

					if ( fullBurst == true && bursting != null ) { 
						// based on weapons[0] cycle rate. for best results, all weapons should have the same cycle time
						float _delay = lastFire + (weapons[0].script.cycleTime / weapons.Length);
						if ( weapons[curWeapon].script.ReadyCheck() == true ) { // needed to keep weapon cycle time in sync with delay in this script. (Make sure ShootBurst() is ready to be called)
							if ( Time.time >= _delay ) { 
								bursting = true;
								if ( weapons[curWeapon].burst == false ) {
									weapons[curWeapon].burst = true;
									weapons[curWeapon].script.ShootBurst(); 
									curWeapon = MFmath.Mod(curWeapon+1, weapons.Length);
									lastFire = Time.time;
								}
							}
						}
					} else { 
						// based on weapons[0] cycle rate. for best results, all weapons should have the same cycle time
						if ( Time.time >= lastFire + (weapons[0].script.cycleTime / weapons.Length) ) { 
							weapons[curWeapon].script.Shoot(); 
							curWeapon = MFmath.Mod(curWeapon+1, weapons.Length);
							lastFire = Time.time;
						}
					}
					
				} else { // fire all weapons at once
					for (int sw=0; sw < weapons.Length; sw++) {
						weapons[sw].script.platformVelocity = platformScript.velocity; // send velocity to weapon script
						if ( fullBurst == true && bursting != null ) { 
							bursting = true;
							if ( weapons[sw].burst == false ) {
								weapons[sw].burst = true;
								weapons[sw].script.ShootBurst( _target ); 
							}
						} else { 
							weapons[sw].script.Shoot( _target ); 
						}
					}
				}
			}
		}
	}
	
	public override bool CheckErrors () {
		base.CheckErrors();

//		if ( !GetComponent<MF_AbstractPlatform>() ) { Debug.Log( this+": No platform script found." ); error = true; }

		return error;
	}
}