using UnityEngine;
using System.Collections;

[HelpURL("http://mobfarmgames.weebly.com/mf_b_mobilityspacearcade.html")]
public class MF_B_MobilitySpaceArcade : MF_AbstractMobility {

	public enum ThrustSource { Thruster, Custom }

	[Tooltip("Allows the vehile to bank.\nIf blank, will assign this object's first child.")]
	public Transform rollTransform;
	[Split1("(deg. / sec.)\nHow fast the vehicle can turn")]
	public float turnRate;
	[Split2("If true, will reduce thrust from engines the more the goal is angled away from within 10° of the front. At 90° away, thrust will be 0.")]
	public bool slowDownForTurns;
	[Split1("(deg. / sec.)\nHow fast the vehicle will bank.")]
	public float bankRate;
	[Split2("The maximum angle the vehicle will attempt to bank.")]
	public float bankAngleMax;
	[Split1("The minimum throttle setting as a percentage. Typically 0.")]
	public float throttleMin = 0f;
	[Split2("The maximum throttle setting as a percentage. Typically 1.")]
	public float throttleMax = 1f;
	[Tooltip("Percent of throttle that can change per second.")]
	public float throttleRate = 1f;
	[Tooltip("If true, will attempt to remain within the Height Range, even if the Nav Target is outside this range.")]
	public bool maintainHeightRange;
	[Tooltip("x = low limit, y = hight limit.")]
	public Vector2 heightRange;
	[Space(8f)]
	[Tooltip("If using a rigidbody, force will be applied at the engine location. Be sure to have the engines aligned properly.")]
	public bool thrustAtEngineLoc;
	[Tooltip("Each thruster for the vehicle.")]
	public ThrusterArcadeObject[] thrusters;

	float throttleGoal;
	float totalThrust;
	float terminalVel;
	bool error;
	
	[System.Serializable]
	public class ThrusterArcadeObject {
		[Tooltip("Points to an engine object.")]
		public MF_B_Engine engine;
		[Tooltip("Where to get the thrust value from.")]
		public ThrustSource thrustSource;
		[Tooltip("If Thrust Source is Custom, this is the thrust value at 100% throttle")]
		public float customThrust;
		[HideInInspector] public float thrust;
	}

	void OnValidate () {
		totalThrust = GetThrustValue(); // also sets thrust for individual thrusters based on source
	}

	public override void Awake () {
		if ( CheckErrors() == true ) { return; }
		base.Awake();

		turnSpeed = turnRate; // used by FS_Targeting to evaluate time to aim
		terminalVel = MFcompute.FindTerminalVelocity ( totalThrust, myRigidbody );

		OnValidate();
	}

	public override void OnEnable () {
		base.OnEnable();
		rollTransform.localRotation = Quaternion.identity; // reset roll
		throttle = 0f;
		throttleGoal = 0f;
	}

	public override void FixedUpdate () {
		if ( error == true ) { return; }
		base.FixedUpdate();

		float t = throttle; // store current throttle

		if ( _navTarget != null ) {
			Steer( navTargetAim );
			float _angle = Vector3.Angle( navTargetAim - transform.position, transform.forward );
			if ( slowDownForTurns == true ) { // reduce thrust based on angle to goal
				if ( _angle > 90 ) {
					throttleGoal = .01f;
				} else if ( _angle > 10 ) { // between 10 - 90 deg
					throttleGoal = 1f - (_angle / 90f);
				} else { // within 10 deg
					throttleGoal = 1f;
				}
			} else { // always keep speed maxed during turn
				throttleGoal = 1f;
			}
		} else {
			Steer( transform.position + transform.forward );
			throttleGoal = 0f;
		}
		float mult = throttle < throttleGoal ? 1f : -1f;
		throttle = Mathf.Clamp( throttle + ( throttleRate * mult * Time.fixedDeltaTime ),		Mathf.Min( throttle, throttleGoal ), Mathf.Max( throttle, throttleGoal ) );
		throttle = Mathf.Clamp( throttle,		throttleMin, throttleMax );
		Move( throttle );

		if ( throttle != t ) { // value changed
			if ( fxScript.Count > 0 ) {
				monitor = throttle;
				for ( int i=0; i < fxScript.Count; i++ ) {
					if ( fxScript[i] != null ) { fxScript[i].CheckUnit(); }
				}
			}
		}
	}
	
	public void Steer ( Vector3 goal ) {
		Vector3 _navPlaneLoc = ModifiedPosition( goal );
		// bank
		Vector3 _bankVector = Vector3.zero;
		Quaternion _rot = Quaternion.identity;
		if ( bankAngleMax > 0 ) {
			float _curRot = MFmath.AngleSigned( _navPlaneLoc - transform.position, transform.forward, Vector3.up );
			float _factor = _curRot > 2 ? 1 : (_curRot < -2 ? -1 : 0);
			float _velFactor = .5f + ( .5F * Mathf.Clamp( myRigidbody.velocity.magnitude / terminalVel, 	0f, 1f ));
			float _bankAngle = bankAngleMax * _factor * _velFactor; // bank angle goal is at max at max speed
			_bankVector =  Quaternion.AngleAxis( _bankAngle, transform.forward ) * transform.up;
		} else {
			_bankVector = transform.up;
		}
		_rot = Quaternion.LookRotation( transform.forward, _bankVector );
		rollTransform.rotation = Quaternion.RotateTowards( rollTransform.rotation, _rot, bankRate * Time.fixedDeltaTime );

		//turn
		if (myRigidbody) {
			if ( _navPlaneLoc != myRigidbody.position ) { // avoid LookRotation of 0
				_rot = Quaternion.LookRotation( _navPlaneLoc - myRigidbody.position, Vector3.up );
				myRigidbody.MoveRotation( Quaternion.RotateTowards( myRigidbody.rotation, _rot, turnRate * Time.fixedDeltaTime ) );
			}
		} else {
			_rot = Quaternion.LookRotation( _navPlaneLoc - transform.position, transform.up );
			transform.rotation = Quaternion.RotateTowards( transform.rotation, _rot, turnRate * Time.fixedDeltaTime );
		}
	}

	public void Move ( float percent ) {
		if ( _navTarget && goalProx > 0 ) {
			if ( ( transform.position - _navTarget.position ).sqrMagnitude <= goalProx*goalProx ) {
				percent = 0f;
			}
		}
		float thrust = totalThrust * percent ;
		if ( myRigidbody ) {
			if ( thrustAtEngineLoc == false ) { 
				myRigidbody.AddForce( transform.forward * thrust * Time.fixedDeltaTime);
			} else {
				for ( int i=0; i < thrusters.Length; i++ ) {
					if ( thrusters[i].engine ) {
						myRigidbody.AddForceAtPosition( -thrusters[i].engine.transform.forward * thrusters[i].thrust * percent * Time.fixedDeltaTime, thrusters[i].engine.transform.position );
					} else {
						myRigidbody.AddForce( transform.forward * thrusters[i].thrust * percent * Time.fixedDeltaTime);
					}
				}
			}
		} else {
			transform.position = transform.position + (transform.forward * thrust * Time.fixedDeltaTime);
		}
		// send percent to thruster objects for visuals
		for ( int i=0; i < thrusters.Length; i++ ) {
			if ( thrusters[i].engine ) {
				thrusters[i].engine.throttle = percent;
			}
		}
	}

	float GetThrustValue () {
		float thrust = 0f;
		for ( int i=0; i < thrusters.Length; i++ ) {
			if ( thrusters[i].thrustSource == ThrustSource.Custom ) {
				thrust += thrusters[i].customThrust;
				thrusters[i].thrust = thrusters[i].customThrust;
			} else {
				if ( thrusters[i].engine ) {
					thrust += thrusters[i].engine.strength;
					thrusters[i].thrust = thrusters[i].engine.strength;
				} else {
					thrusters[i].thrust = 0f;
				}
			}
		}
		return thrust;
	}
	
	public override Vector3 ModifiedPosition ( Vector3 pos ) {
		if ( maintainHeightRange == true ) {
			return new Vector3( pos.x, Mathf.Clamp( pos.y, heightRange.x, heightRange.y ), pos.z );
		}
		return pos;
	}
	
	bool CheckErrors () {
		error = false;

		if ( !rollTransform ) {
			if ( transform.childCount < 1  ) {
				Debug.Log( this+": No child object found. Need first child transform as parent to the rest of the unit to control roll."); error = true;
			} else {
				rollTransform = transform.GetChild(0);
			}
		}

		return error;
	}
}



























