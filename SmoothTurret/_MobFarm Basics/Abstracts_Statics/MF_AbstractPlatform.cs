using UnityEngine;
using System.Collections;

public abstract class MF_AbstractPlatform : MonoBehaviour {

	[Tooltip("Current target - this is usually provided by another script.")]
	[SerializeField] protected Transform _target;
	public Transform target {
		get { return _target; }
		set { if ( value != _target ) {
				_target = value;
				targetRigidbody = _target ? _target.root.GetComponent<Rigidbody>() : null; 
			}
		}
	}
	[Tooltip("Parent object for weapons.")]
	public Transform weaponMount;
	[Tooltip("Toggle whether to show Aim Object.")]
	public bool aimObjectActive;
	[Tooltip("Object that shows where the platform is pointed.")]
	public GameObject aimObject;
	[Tooltip("Targeting range to use if clicking on empty space.")]
	public float emptyAimRange = 50;
	[Tooltip("Aim Object always appears Empty Aim Range. (Better performance)")]
	public bool fixedAimObjectRange;
	[Header("Aiming Options:")]
	[Tooltip("If true, will impart unit’s velocity to fired shots and be used in other calculations. Uncheck if the unit is stationary.")]
	public bool useRootVelocity = true;
	[Tooltip("Assign a specific transform to be used for velocity.\n" +
		"If blank, will assume the root object.")]
	public Transform velocityRoot;
	[Tooltip("Platform will calculate aim to hit a moving target.")]
	public bool useIntercept = true;

	[HideInInspector] public float? shotSpeed = null; // for ballistics / intrcept, nullable to indicate no projectile
	[HideInInspector] public Vector3 exitLoc; // will be set by a control script when needed. used for ballistics / intrcept
	[HideInInspector] public Vector3 targetAimLocation;
	[HideInInspector] public float targetRange;
	[HideInInspector] public Vector3 lastTargetPosition;
	[HideInInspector] public Rigidbody targetRigidbody;
	[HideInInspector] public Vector3 velocity;
	[HideInInspector] public bool playerControl;

	Rigidbody myRigidbody;
	Vector3 lastPlatformPosition;
	MF_AbstractMobility mobilityScript;
	protected bool error;

	public virtual void Start () {
		if ( CheckErrors() == true ) { return; }

		if ( !velocityRoot ) {
			velocityRoot = transform.root;
		}
		mobilityScript = velocityRoot.GetComponent<MF_AbstractMobility>();
		myRigidbody = velocityRoot.GetComponent<Rigidbody>();
		exitLoc = transform.position;

		if ( aimObject ) { // aimObject is defined
			if ( !aimObject.activeInHierarchy ) { // not active in hierarchy - aimObject is a prefab
				// create aimObject from prefab
				GameObject ao = (GameObject)Instantiate( aimObject, weaponMount.position, weaponMount.rotation );
				ao.transform.parent = weaponMount;
				ao.transform.localPosition = new Vector3( 0f, 0f, emptyAimRange );
				aimObject = ao;
			}
		}
	}

	public virtual void Update () {
		if ( error == true ) { return; }	

		// unit velocity - will also be read by control script and used in MF_AbstractWeapon
		if ( useRootVelocity == true ) {
			if ( mobilityScript ) {
				velocity = mobilityScript.velocity;
			} else {
				velocity = UnitVelocity();
			}
		}

		// used in various calculations
		// **** Maybe find a way to remove this if using settings that don't require it 
		if ( _target ) {
			targetRange = Vector3.Distance( exitLoc, _target.position );
		}

		targetAimLocation = AimLocation();

		if ( aimObject ) {
			if ( aimObject.activeInHierarchy != aimObjectActive ) {
				aimObject.SetActive( aimObjectActive );
			}

			if ( aimObjectActive == true ) {
				if ( fixedAimObjectRange == false ) { // move aimObject based on raycast
					Ray _ray = new Ray( weaponMount.position, weaponMount.forward );
					RaycastHit _hit;
					Vector3 _objPos;
					
					if ( Physics.Raycast( _ray, out _hit, Mathf.Infinity ) ) { // hit collider
						_objPos = _hit.point;
					} else { // hit nothing
						_objPos = _ray.origin + (_ray.direction * emptyAimRange);
					}
					// move aimObject to aim location
					aimObject.transform.position = _objPos;
				}
			}
		}
	}

	public virtual Vector3 AimLocation () {
		// adjust aim location to hit target 
		Vector3 _aimLoc = Vector3.zero;
		if ( _target ) {
			_aimLoc = _target.position;

			if ( useIntercept == true && shotSpeed != null ) { 
				Vector3 _targetVelocity = Vector3.zero;
				// target velocity
				if ( targetRigidbody ) { // if target has a rigidbody, use velocity
					_targetVelocity = targetRigidbody.velocity;
				} else { // otherwise compute velocity from change in position
					_targetVelocity =  (_aimLoc - lastTargetPosition ) / Time.deltaTime;
					lastTargetPosition = _aimLoc;
				}
				
				// point at linear intercept position
				Vector3? interceptAim = MFcompute.Intercept( exitLoc, velocity, (float)shotSpeed, _aimLoc, _targetVelocity );
				if ( interceptAim == null ) {
					target = null;
				} else {
					_aimLoc = (Vector3)interceptAim;
				}
			}
		}
		return _aimLoc;
	}

	// check if the platform is aimed at the target
	public virtual bool AimCheck ( float targetSize, float aimTolerance ) {
		if (error == true) { return false; }

		bool _ready = false;
		if ( _target ) {
			if ( targetRange == 0 ) { // assume target range hasn't been evaluated yet. This can happen due to timing of other scripts upon gaining an intital target
				targetRange = Vector3.Distance( weaponMount.position, _target.position );
			}
			float _targetFovRadius = Mathf.Clamp(   (Mathf.Atan( (targetSize / 2f) / targetRange ) * Mathf.Rad2Deg) + aimTolerance,    0, 180 );
			if ( Vector3.Angle( weaponMount.forward, targetAimLocation - weaponMount.position ) <= _targetFovRadius ) {
				_ready = true;
			}
		}
		return _ready;
	}

	public virtual bool TargetWithinLimits ( Transform targ ) {
		return true;
	}

	// used if velocity information is needed, and not using MF_AbstractMobility
	Vector3 UnitVelocity () {
		Vector3 _vel;
		if ( myRigidbody ) {
			_vel = myRigidbody.velocity;
		} else {
			_vel = lastPlatformPosition - transform.position;
			lastPlatformPosition = transform.position;
		}
		return _vel;
	}

	bool CheckErrors () {
		error = false;

		if ( weaponMount == false ) { Debug.Log( this+": Weapon mount part hasn't been defined." ); error = true; }
		
		return error;
	}
}
