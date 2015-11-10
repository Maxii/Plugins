using UnityEngine;
using System.Collections;
using MFnum;

public class ST_TurretControl : MonoBehaviour {
	
	public enum ControlType { AI_AutoTarget, AI_NoTarget, Player_Mouse, Player_Click }

	[Header("Turret will get this target:")]
	[Tooltip("Current target of the turret. This will either be supplied by the targeting script, or be assigned directly. However, if the turret is choosing its own targets, this may immediatly be overwritten.")]
	[SerializeField] private GameObject _target;
	[Tooltip("AI_Auto Target: AI will use a scanner to pick targets.\n\n" +
	         "AI_NoTarget: AI will not pick targets, but will aim and fire if it is given a target.\n\n" +
	         "Player_Mouse: Turret will be under mouse control. Will fire on mouse click.\n\n" +
	         "Player_Click: Turret will aim and fire burst at click location.")]
	public ControlType controller;
	[Tooltip("Player to control this turret if Controller is set to Player_Mouse or Player_Click.")]
	[SerializeField] private GameObject _player;
	[Header("Weapon list:")]
	public WeaponData[] weapons;
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

	[HideInInspector] public int curWeapon;

	MF_AbstractTargeting targetingScript;
	MF_AbstractPlatform platformScript;
	ST_Turret turretScript;
	ST_Player playerScript;
	float targetSize;
	float lastLosCheck;
	float lastFire;
	float oldTurnWeapInaccuracy;
	bool? bursting = null;
	int burstEnd;
	bool loaded;
	bool error;

	[System.Serializable]
	public class WeaponData {
		public GameObject weapon;
		[HideInInspector] public MF_BasicWeapon script;
		[HideInInspector] public bool burst;
	}

	// the current target
	public GameObject target {
		get { return _target; }
		set { if ( value != _target ) {
				_target = value;
				if ( _target ) { // in case target became null
					// reset for arc los testing
					turretScript.curArc = turretScript.defaultArc;
					// find target size
					if (checkTargetSize == true && controller != ControlType.Player_Mouse && controller != ControlType.Player_Click ) {
						// try to get target size from collider bounds
						targetSize = UtilityMF.FindColliderBoundsSize( _target, true );
					}
					if ( targetSize == 0 ) { targetSize = targetSizeDefault; }
				}
			}
		}
	}
	// current player
	public GameObject player {
		get { return _player; }
		set { _player = value;
			if ( _player ) {
				playerScript = _player.GetComponent<ST_Player>();
				target = playerScript ? playerScript.aimObject : null;
			}
		}
	}

	void OnValidate () {
		if ( loaded ) {
			target = _target;
			player = _player;
		}
	}

	void Start () {
		if (CheckErrors() == true) { return; }

		turretScript = GetComponent<ST_Turret>();
		platformScript = GetComponent<MF_AbstractPlatform>();
		targetingScript = GetComponent<MF_AbstractTargeting>();

		// cache scripts for all weapons
		if ( weapons.Length > 0 ) {
			for (int wd=0; wd < weapons.Length; wd++) {
				if (weapons[wd].weapon) {
					weapons[wd].script = weapons[wd].weapon.GetComponent<MF_BasicWeapon>();
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
		turretScript.target = target; 
		
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
		if ( target ) {
			DoAimFire();
		}
	}

	void DoControlType() {
		switch (controller) {
			
		case ControlType.Player_Mouse :  // player will move aim object and hold mouse to fire
			turretScript.playerControl = true; // used to suspend useIntercept
			if ( _player && playerScript ) { // make sure player is defined
				if ( playerScript.turretControl == true ) {
					target = playerScript.aimObject;
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
			if ( _player && playerScript ) { // make sure player is defined
				if ( playerScript.turretControl == true ) {
					if ( playerScript.fireObject.activeSelf == false ) {
						target = playerScript.aimObject;
					}
					if ( weapons.Length > 0 ) {
						if ( Input.GetMouseButtonDown(0) ) {
							// place target object
							if ( weapons[curWeapon].script.RangeCheck(target.transform) == true ) { // check range
								playerScript.fireObject.transform.position = playerScript.aimObject.transform.position;
								playerScript.fireObject.SetActive(true);
								target = playerScript.fireObject;
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
			target = targetingScript ? targetingScript.weaponTarget : null; // make sure targeting script exsists, assign weapon target
			break;
			
		case ControlType.AI_NoTarget : // AI will aim if given a target, but targets must be supplied some other way
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
		
		if ( controller == ControlType.AI_AutoTarget || controller == ControlType.AI_NoTarget ) { // AI will shoot
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
				if ( target == playerScript.fireObject ) { // player clicked a location
					if ( turretScript.AimCheck( targetSize, weapons[curWeapon].script.aimTolerance ) == true ) { // check if turret is aimed at target
						
						if ( weapons[curWeapon].script.ReadyCheck() == true ) { // early out if weapon isn't ready
							Shoot();
							if ( fullBurst == false ) {
								target = playerScript.aimObject;
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
					if (player) {
						target = playerScript.aimObject;
					}
				}
			}
		}
	}

	void DoConverge() {
		float _convergeRange = Mathf.Clamp(    Vector3.Distance(turretScript.weaponMount.transform.position, turretScript.targetLocation),    minConvergeRange, Mathf.Infinity );
		for (int w = 0; w < weapons.Length; w++) {
			if (weapons[w] != null) {
				Quaternion _rotGoal;
				if (target) { // slew to target
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
		bool _losClear = false;
		if (checkLOS == false || controller == ControlType.Player_Mouse || controller == ControlType.Player_Click ) { // always clear when not using LOS or under player control
			_losClear = true;
		} else {
			if (Time.time >= lastLosCheck + losCheckInterval) {
				RaycastHit _hit;
				_losClear = false;
				Vector3 _startPos = weapons[curWeapon].script.exits[ weapons[curWeapon].script.curExit ].transform.position;

				bool _directLos = ( turretScript.useGravity == false || alwaysDirectLos == true ) ? true : false;
				if ( _directLos == false ) { // use ballistic approximation los line
					float _shotSpeed = platformScript.shotSpeed;
					Vector3 _startDir = weapons[curWeapon].script.exits[ weapons[curWeapon].script.curExit ].transform.forward;
					Vector3 _startDirHoriz = new Vector3( _startDir.x, 0f, _startDir.z );

					int _factor = -Physics.gravity.y > 0 ? 1 : -1;
					float _aimAngle = MFmath.AngleSigned( _startDirHoriz, _startDir, Vector3.Cross(_startDir, Vector3.up) );
					if ( _factor * _aimAngle > 0 ) { // aiming up, find apex, use los to apex and then to target
						float _ballRange = ( (_shotSpeed*_shotSpeed) * Mathf.Sin( _factor * 2 * _aimAngle * Mathf.Deg2Rad ) ) / (_factor * -Physics.gravity.y);
						// compare range
						if ( (_startPos - target.transform.position).sqrMagnitude <= _ballRange*_ballRange*.25 ) { // squaring 1/2 _ballRange
							_directLos = true; // if target before apex, just use direct los
						} else { // continue to do ballistic los

							Vector3 _midPos = _startPos + ( _startDirHoriz.normalized * _ballRange * .5f );
							float _ballPeak = ( (_startDir.y * _shotSpeed) * (_startDir.y * _shotSpeed) ) / (-Physics.gravity.y * 2f );
							Vector3 _apexPos = _midPos + ( Vector3.up * _ballPeak );

//							//Debug.DrawLine( _startPos, _startPos + ((_startDirHoriz).normalized * _ballRange), Color.cyan, .1f );
//							Debug.DrawLine( _startPos, target.transform.position, Color.green, .1f );
//							Debug.DrawLine( _startPos, _apexPos, Color.red, .1f ); 
//							Debug.DrawLine( _apexPos, target.transform.position, Color.red, .1f );

							// check los
							if (Physics.Linecast(_startPos, _apexPos, out _hit) ) { // check exit to apex
								// recursively search parent of hit collider for target
								_losClear = UtilityMF.RecursiveParentTransformSearch( target.transform, _hit.transform ) ?? false;
							} else if (Physics.Linecast(_apexPos, target.transform.position, out _hit) ) { // check apex to target
								// recursively search parent of hit collider for target
								_losClear = UtilityMF.RecursiveParentTransformSearch( target.transform, _hit.transform ) ?? false;
							}
							
							if ( _losClear == false ) {
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
					if (Physics.Raycast(_startPos, target.transform.position - _startPos, out _hit, weapons[curWeapon].script.maxRange)) {
						// recursively search parent of hit collider for target
						_losClear = UtilityMF.RecursiveParentTransformSearch( target.transform, _hit.transform ) ?? false;
					}
				}
				lastLosCheck = Time.time;
			}
		}
		
		// fire weapons
		if (_losClear == true) {
			if ( weapons[curWeapon].script.RangeCheck(target.transform) == true ) {
				weapons[curWeapon].script.platformVelocity = turretScript.platformVelocity;
				if ( alternatingFire == true && weapons.Length > 1 ) { // alternate fire bewteen weapons
					if ( fullBurst == true && bursting != null ) { 
						// based on weapons[0] cycle rate. for best results, all weapons should have the same cycle time
						float _delay;
						if ( bursting == false ) {
							// begining of burst has to wait a hair longer to be in sync with the last weapon (1 frame?)
							_delay = weapons[ weapons.Length-1 ].script.delay - (weapons[0].script.cycleTime / weapons.Length);
						} else {
							_delay = lastFire + (weapons[0].script.cycleTime / weapons.Length);
						}
						if ( Time.time >= _delay ) { 
							bursting = true;
							if ( weapons[curWeapon].burst == false ) {
								weapons[curWeapon].burst = true;
								weapons[curWeapon].script.ShootBurst(); 
								curWeapon = MFmath.Mod(curWeapon+1, weapons.Length);
								lastFire = Time.time;
							}
						}
					} else { 
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

	bool CheckErrors () {
		error = false;
		string _object = gameObject.name;
		if ( weapons.Length > 0 ) {
			for (int cw=0; cw < weapons.Length; cw++) {
				if (weapons[cw].weapon == false) { Debug.Log(_object+": TurretControl weapon index "+cw+" hasn't been defined."); error = true; }
			}
		}
		if ( !GetComponent<ST_Turret>() ) { Debug.Log(_object+": No turret script found."); error = true; }
		if ( _player ) {
			if ( !_player.GetComponent<ST_Player>() ) { Debug.Log(_object+": Defined player has no player script."); error = true; }
		}
		return error;
	}
}


