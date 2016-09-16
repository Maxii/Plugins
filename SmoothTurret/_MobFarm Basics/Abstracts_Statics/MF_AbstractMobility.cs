using UnityEngine;
using System.Collections;

public class MF_AbstractMobility : MonoBehaviour {

	[Tooltip("The current navigation target.")]
	[SerializeField] protected Transform _navTarget; 
	public Transform navTarget {
		get { return _navTarget; }
		set { if ( value != _navTarget ) {
				_navTarget = value;
				navRigidbody = _navTarget ? _navTarget.root.GetComponent<Rigidbody>() : null; 
			}
		}
	}
	[Tooltip("Maneuver to intercept the Nav Target.")]
	public bool useIntercept = true;
	[Tooltip("Maneuver to intercept Nav Target with a weapon. Designate the weapon platform to use. This weapon platform must also use intercept.")]
	public MF_AbstractPlatform interceptForPlatform; 

	[HideInInspector] public Vector3 navTargetAim;
	[HideInInspector] public Rigidbody myRigidbody;
	[HideInInspector] public Vector3 velocity;

	Rigidbody navRigidbody;
	Vector3 myLastPosition;
	Vector3 navLastPosition;

	public virtual void Start () {
		myRigidbody = GetComponent<Rigidbody>();
	}

	public virtual void FixedUpdate () {

		velocity = UnitVelocity();

		if (_navTarget) {
			navTargetAim = _navTarget.position;
			if ( useIntercept == true ) {
				if ( interceptForPlatform && interceptForPlatform.target == _navTarget ) {
					// intercept for this target already computed by platform
					navTargetAim = interceptForPlatform.targetAimLocation;
				} else {
					float _vel = velocity.magnitude; // use my speed
					if ( interceptForPlatform ) {

						_vel = interceptForPlatform.shotSpeed ?? 0f;
					} // use platform shot speed
					// _navTarget velocity
					Vector3 _navVelocity = Vector3.zero;
					if (navRigidbody) {
						_navVelocity = navRigidbody.velocity;
					} else {
						_navVelocity = navLastPosition - _navTarget.position;
						navLastPosition = _navTarget.position;
					}

					navTargetAim = MFcompute.Intercept( transform.position, velocity, _vel, _navTarget.position, _navVelocity ) ?? _navTarget.position;
				}
			}
		}
	}

	public Vector3 UnitVelocity () {
		Vector3 _vel;
		if ( myRigidbody ) {
			_vel = myRigidbody.velocity;
		} else {
			_vel = myLastPosition - transform.position;
			myLastPosition = transform.position;
		}
		return _vel;
	}

	// allows different pobility scripts to return a modified location for targets/waypoints for evaluation
	// used mainly for navigation constrained away from a potential waypoint ( e.g. ground vehicle attempting to reach a waypoint in the air )
	public virtual Vector3 ModifiedPosition ( Vector3 pos ) {
		return pos;
	}
	
}
