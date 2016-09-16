using UnityEngine;
using System.Collections;
using MFnum;

public class ST_TurretControl : MF_AbstractPlatformControl {
	
	public enum ControlType { AI_AutoTarget, None, Player_Mouse, Player_Click }

	[Header("Turret will get this target:")]
	[Tooltip("Current target of the turret. This will either be supplied by the targeting script, or be assigned directly. However, if the turret is choosing its own targets, this may immediately be overwritten.")]
	[SerializeField] private Transform _target;
	[Tooltip("AI_Auto Target: AI will use a scanner to pick targets.\n\n" +
	         "None: Turret is deactivated.\n\n" +
	         "Player_Mouse: Turret will be under mouse control. Will fire on mouse click.\n\n" +
	         "Player_Click: Turret will aim and fire burst at click location.")]
	public ControlType controller;
	[Tooltip("Player to control this turret if Controller is set to Player_Mouse or Player_Click.")]
	public ST_Player playerScript;
	[Header("Multiple weapons on a single turret:")]
	[Tooltip("(meters)\n0 = don't angle weapons")]
	public float fixedConvergeRange; // 0 = don't converge
	[Tooltip("Will angle weapons slightly to converge fire baed on target range.")]
	public bool dynamicConverge;
	[Tooltip("(deg/sec)\nWhen using Dynamic Converge.")]
	public float convergeSlewRate = 2f;
	[Tooltip("(meters)\nWhen using Dynamic Converge, limits how far weapons will angle inwards. This is the minimum range shots will converge at.")]
	public float minConvergeRange;
	[Tooltip("For multiple weapons per turret.\nWill alternate their fire instead of firing all at once.")]
	public bool alternatingFire;
	[Header("Options:")]
	[Tooltip("Upon firing, weapons will finish a full burst according to their burst fire setting.")]
	public bool fullBurst;
	[Tooltip("Uses Raycast to check line of sight before firing. If using gravity, LoS will checked along an approximated ballistic arc.")]
	public bool checkLOS;
	[Tooltip("When checking LOS:\nTrue: Always use direct line.\nFalse: Will use a ballistic LoS approximation when using gravity.")]
	public bool alwaysDirectLos;
	[Tooltip("When using ballistics, a blocked LoS using the default arc will set the weapon to try the other arc. This will reset after every shot, or every burst if full burst is used." +
		"If Use Direct Los is true, this won't have any effect.")]
	public bool losMayChangeArc;
	[Tooltip("Increase time between Raycast LoS checks to improve performance.\n0 = before every shot.")]
	public float losCheckInterval; 
	[Tooltip("Will limit inaccuracy imparted to weapons due to any turning/acceleration. If this is less than a weapon's inaccuracy, it will have no effect.")]
	public float maxInaccuracy;
	[Tooltip("Try to determine target size from collider.")]
	public bool checkTargetSize;
	[Tooltip("(meters)\nDefault target size to fire at if none found or provided.")]
	public float targetSizeDefault;

	ST_Turret turretScript;
	float targetSize;
	float lastLosCheck;
	bool losClear;
	float lastFire;
	float oldTurnWeapInaccuracy;
	bool? bursting = null;
	int burstEnd;
	bool loaded;

	// the current target
	public Transform target {
		get { return _target; }
		set { if ( value != _target ) {
				_target = value;
				if ( _target ) { // in case target became null
					// reset for arc los testing
					if ( turretScript ) { // in case target is set by another script before turretScript is defined
						turretScript.curArc = turretScript.defaultArc;
					}
					// find target size
					targetSize = 0; // reset
					if (checkTargetSize == true && controller != ControlType.Player_Mouse && controller != ControlType.Player_Click ) {
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

	public override void Start () {
		base.Start();
		if ( error == true ) { return; }

		turretScript = GetComponent<ST_Turret>();

		// cache scripts for all weapons
		if ( weapons.Length > 0 ) {
			for (int wd=0; wd < weapons.Length; wd++) {
				if (weapons[wd].weapon) {
					weapons[wd].script = weapons[wd].weapon.GetComponent<MF_AbstractWeapon>();
				}
			}
			//set fixed converge angle of weapons relative to weaponMount forward direction, to converge at given range. 0 = no fixed convergence
			if (fixedConvergeRange > 0) {
				for (int w=0; w < weapons.Length; w++) {
					if (weapons[w].weapon) {
						weapons[w].weapon.transform.rotation = Quaternion.LookRotation( turretScript.weaponMount.transform.position +
						                                                              	  ( turretScript.weaponMount.transform.forward * fixedConvergeRange ) -
						                                                                  weapons[w].weapon.transform.position, turretScript.weaponMount.transform.up );
					}
				}
			}
		}

		loaded = true;
		OnValidate();
	}

	void Update () {
		if (error == true) { return; }

		// player or AI control
		DoControlType();

		// pass target to turret
		turretScript.target = _target; 
		
		// set angle of weapons to converge based on current target range
		if ( dynamicConverge == true && weapons.Length > 0 ) {
			DoConverge();
		}
		
		// set weapons inaccuracy due to turret rotation/elevation
		if ( weapons.Length > 0 ) {
			DoInaccuracy();
		}

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
			
		case ControlType.Player_Mouse :  // player will move aim object and hold mouse to fire
			turretScript.playerControl = true; // used to suspend useIntercept
			if ( playerScript ) { // make sure player is defined
				if ( playerScript.turretControl == true ) {
					target = playerScript.aimObject.transform;
					if ( weapons.Length > 0 ) {
						if ( Input.GetMouseButton(0) ) {
							Shoot();
						}
					}
				}
			} else { // no player defined
				target = null;
			}
			break;
			
		case ControlType.Player_Click : // player clicks turret to activate/deactivate it, then clicks anywhere to place a target. Turret aims and fires a burst.
			turretScript.playerControl = true; // used to suspend useIntercept
			if ( playerScript ) { // make sure player is defined
				if ( playerScript.turretControl == true ) {
					if ( playerScript.fireObject.activeSelf == false ) {
						target = playerScript.aimObject.transform;
					}
					if ( weapons.Length > 0 ) {
						if ( Input.GetMouseButtonDown(0) ) {
							// place target object
							if ( weapons[curWeapon].script.RangeCheck( _target ) == true ) { // check range
								playerScript.fireObject.transform.position = playerScript.aimObject.transform.position;
								playerScript.fireObject.SetActive(true);
								target = playerScript.fireObject.transform;
							}
						}
					}
				}
			} else { // no player defined
				target = null;
			}
			break;
			
		case ControlType.AI_AutoTarget : // AI will aim, getting targets from targeting script
			turretScript.playerControl = false;
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
			turretScript.playerControl = false;
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
			
			// set shot speed for turret aim intercept / ballistics
			turretScript.shotSpeed = weapons[curWeapon].script.shotSpeed;
			turretScript.exitLoc = weapons[curWeapon].script.exits[ weapons[curWeapon].script.curExit ].transform.position;
			
			if ( l >= weapons.Length ) {
				return; // no weapons active
			}
		}

		// make sure all weapons fire if a burst has already begun
		if ( bursting == true ) {
			Shoot();
			return;
		}

		if ( controller == ControlType.AI_AutoTarget ) { // AI will shoot
			if ( weapons.Length > 0 ) {
				if ( turretScript.AimCheck( targetSize, weapons[curWeapon].script.aimTolerance ) == true ) { // check if turret is aimed at target
					if ( weapons[curWeapon].script.ReadyCheck() == true ) { // early out if weapon isn't ready
						Shoot();
					}
				}
			}
		}
		
		if ( controller == ControlType.Player_Click ) {
			if ( weapons.Length > 0 ) {
				if ( target == playerScript.fireObject.transform ) { // player clicked a location
					if ( turretScript.AimCheck( targetSize, weapons[curWeapon].script.aimTolerance ) == true ) { // check if turret is aimed at target
						
						if ( weapons[curWeapon].script.ReadyCheck() == true ) { // early out if weapon isn't ready
							Shoot();
							if ( fullBurst == false ) {
								target = playerScript.aimObject.transform;
							}
						}
					}
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
				turretScript.curArc = turretScript.defaultArc; // reset default arc
				bursting = null;
				// remove fire point as target under player_click control
				if ( controller == ControlType.Player_Click ) {
					if (playerScript) {
						target = playerScript.aimObject.transform;
					}
				}
			}
		}
	}

	void DoConverge() {
		float _convergeRange = Mathf.Clamp(    Vector3.Distance(turretScript.weaponMount.transform.position, turretScript.targetAimLocation),    minConvergeRange, Mathf.Infinity );
		for (int w = 0; w < weapons.Length; w++) {
			if (weapons[w] != null) {
				Quaternion _rotGoal;
				if ( _target ) { // slew to target
					_rotGoal = Quaternion.LookRotation( turretScript.weaponMount.transform.position +
					                                   (turretScript.weaponMount.transform.forward * _convergeRange) -
					                                   weapons[w].weapon.transform.position, turretScript.weaponMount.transform.up );
				} else { // reset converge to none 
					_rotGoal = Quaternion.LookRotation( weapons[w].weapon.transform.position +
					                                   (turretScript.weaponMount.transform.forward * 1000f) -
					                                   weapons[w].weapon.transform.position, turretScript.weaponMount.transform.up );
				}
				weapons[w].weapon.transform.rotation = Quaternion.RotateTowards( weapons[w].weapon.transform.rotation, _rotGoal, convergeSlewRate * Time.deltaTime );
			}
		}
	}

	void DoInaccuracy() {
		if ( turretScript.turningWeapInaccuracy > 0 || oldTurnWeapInaccuracy != turretScript.turningWeapInaccuracy ) { // catch setting 0 after being > 0, otherwise the weaps wont get updated
			oldTurnWeapInaccuracy = turretScript.turningWeapInaccuracy;
			if ( oldTurnWeapInaccuracy == 0) {
				turretScript.totalTurnWeapInaccuracy = 0f;
			}
			for (int wi=0; wi < weapons.Length; wi++) {
				weapons[wi].script.curInaccuracy = Mathf.Clamp(   weapons[wi].script.inaccuracy + turretScript.totalTurnWeapInaccuracy,   
				                                               weapons[wi].script.inaccuracy, Mathf.Max(maxInaccuracy, weapons[wi].script.inaccuracy) );
			}
		}
	}

	void Shoot() {
		// check line of sight
		if ( checkLOS == false || controller == ControlType.Player_Mouse || controller == ControlType.Player_Click ) { // always clear when not using LOS or under player control
			losClear = true;
		} else {
			if (Time.time >= lastLosCheck + losCheckInterval) {
				RaycastHit _hit;
				losClear = false;
				Vector3 _startPos = weapons[curWeapon].script.exits[ weapons[curWeapon].script.curExit ].transform.position;

				bool _directLos = ( turretScript.useGravity == false || alwaysDirectLos == true ) ? true : false;
				if ( _directLos == false ) { // use ballistic approximation los line
					float? _shotSpeed = turretScript.shotSpeed;
					Vector3 _startDir = weapons[curWeapon].script.exits[ weapons[curWeapon].script.curExit ].transform.forward;
					Vector3 _startDirHoriz = new Vector3( _startDir.x, 0f, _startDir.z );

					int _factor = -Physics.gravity.y > 0 ? 1 : -1;
					float _aimAngle = MFmath.AngleSigned( _startDirHoriz, _startDir, Vector3.Cross(_startDir, Vector3.up) );
					if ( _factor * _aimAngle > 0 ) { // aiming up, find apex, use los to apex and then to target
						float _ballRange = (float)( (_shotSpeed*_shotSpeed) * Mathf.Sin( _factor * 2 * _aimAngle * Mathf.Deg2Rad ) ) / (_factor * -Physics.gravity.y);
						// compare range
						if ( (_startPos - _target.position).sqrMagnitude <= _ballRange*_ballRange*.25 ) { // squaring 1/2 _ballRange
							_directLos = true; // if target before apex, just use direct los
						} else { // continue to do ballistic los

							Vector3 _midPos = _startPos + ( _startDirHoriz.normalized * _ballRange * .5f );
							float _ballPeak = (float)( (_startDir.y * _shotSpeed) * (_startDir.y * _shotSpeed) ) / (-Physics.gravity.y * 2f );
							Vector3 _apexPos = _midPos + ( Vector3.up * _ballPeak );

//							//Debug.DrawLine( _startPos, _startPos + ((_startDirHoriz).normalized * _ballRange), Color.cyan, .1f );
//							Debug.DrawLine( _startPos, target.transform.position, Color.green, .1f );
//							Debug.DrawLine( _startPos, _apexPos, Color.red, .1f ); 
//							Debug.DrawLine( _apexPos, target.transform.position, Color.red, .1f );

							// check los
							if (Physics.Linecast(_startPos, _apexPos, out _hit) ) { // check exit to apex
								// recursively search parent of hit collider for target
								losClear = UtilityMF.RecursiveParentTransformSearch( _target, _hit.transform );
							} else if (Physics.Linecast(_apexPos, _target.position, out _hit) ) { // check apex to target
								// recursively search parent of hit collider for target
								losClear = UtilityMF.RecursiveParentTransformSearch( _target, _hit.transform );
							}
							
							if ( losClear == false ) {
								if ( losMayChangeArc == true ) {
									turretScript.curArc = turretScript.defaultArc == MFnum.ArcType.Low ? MFnum.ArcType.High : MFnum.ArcType.Low;
								}
							}
						}
					} else { // aiming downwards (already past apex) just use direct los
						_directLos = true;
					}
				}
				if ( _directLos == true ) { // use direct los line
					if (Physics.Raycast(_startPos, _target.position - _startPos, out _hit, weapons[curWeapon].script.maxRange)) {
						// recursively search parent of hit collider for target
						losClear = UtilityMF.RecursiveParentTransformSearch( _target, _hit.transform );
					}
				}
				lastLosCheck = Time.time;
			}
		}
		
		// fire weapons
		if (losClear == true) {
			if ( weapons[curWeapon].script.RangeCheck( _target ) == true ) {
				if ( alternatingFire == true && weapons.Length > 1 ) { // alternate fire bewteen weapons
					weapons[curWeapon].script.platformVelocity = turretScript.velocity; // send velocity to current weapon script

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
					}
					if ( fullBurst == false ) {
						// based on weapons[0] cycle rate. for best results, all weapons should have the same cycle time
						if ( Time.time >= lastFire + (weapons[0].script.cycleTime / weapons.Length) ) { 
							weapons[curWeapon].script.Shoot(); 
							turretScript.curArc = turretScript.defaultArc; // reset default arc 
							curWeapon = MFmath.Mod(curWeapon+1, weapons.Length);
							lastFire = Time.time;
						}
					}

				} else { // fire all weapons at once
					for (int sw=0; sw < weapons.Length; sw++) {
						weapons[sw].script.platformVelocity = turretScript.velocity; // send velocity to all weapon scripts

						if ( fullBurst == true && bursting != null ) { 
							bursting = true;
							if ( weapons[sw].burst == false ) {
								weapons[sw].burst = true;
								weapons[sw].script.ShootBurst(); 
							}
						} else { 
							weapons[sw].script.Shoot(); 
							turretScript.curArc = turretScript.defaultArc; // reset default arc 
						}
					}
				}
			}
		}
	}

	public override bool CheckErrors () {
		base.CheckErrors();

		if ( !GetComponent<ST_Turret>() ) { Debug.Log( this+": No turret script found." ); error = true; }

		return error;
	}
}


