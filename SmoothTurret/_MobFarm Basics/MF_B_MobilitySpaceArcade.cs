using UnityEngine;
using System.Collections;

public class MF_B_MobilitySpaceArcade : MF_AbstractMobility {

	[Tooltip("Allows the vehile to bank.\nIf blank, will assign this object's first child.")]
	public Transform rollTransform;
	[Tooltip("(deg. / sec.)\nHow fast the vehicle can turn")]
	public float turnRate;
	[Tooltip("If true, will reduce thrust from engines the more the goal is angled away from within 10° of the front. At 90° away, thrust will be 0.")]
	public bool slowDownForTurns;
	[Tooltip("(deg. / sec.)\nHow fast the vehicle will bank.")]
	public float bankRate;
	[Tooltip("The maximum angle the vehicle will attempt to bank.")]
	public float bankAngleMax;
	[Tooltip("If true, will attempt to remain within the Height Range, even if the Nav Target is outside this range.")]
	public bool maintainHeightRange;
	[Tooltip("x = low limit, y = hight limit.")]
	public Vector2 heightRange;
	[Tooltip("Each thruster for the vehicle.")]
	public ThrusterArcadeObject[] thrusters;
	
	float totalThrust;
	float terminalVel;
	bool error;
	
	[System.Serializable]
	public class ThrusterArcadeObject {
		[Tooltip("Value of thrust at 100%")]
		public float maxThrust;
		[Tooltip("ParticleSystem to use for thruster fx.")]
		public ParticleSystem particle;
		[Tooltip("Speed of particles at 0% thrust. 0% thrust will also trun off particle emission.")]
		public float particleLow;
		[Tooltip("Speed of particles at 100% thrust.")]
		public float particleHigh;
	}

	void OnValidate () {
		totalThrust = GetThrustValue();
	}
	
	public override void Start () {
		if ( CheckErrors() == true ) { return; }
		base.Start();

		terminalVel = MFcompute.FindTerminalVelocity ( totalThrust, myRigidbody );

		OnValidate();
	}

	public override void FixedUpdate () {
		if ( error == true ) { return; }
		base.FixedUpdate();
		
		if (_navTarget) {
			Steer( navTargetAim );
			float _angle = Vector3.Angle( navTargetAim - transform.position, transform.forward );
			if ( slowDownForTurns == true ) { // reduce thrust based on angle to goal
				if ( _angle > 90 ) {
					Move( .01f );
				} else if ( _angle > 10 ) { // between 10 - 90 deg
					Move( 1f - (_angle / 90f) );
				} else { // within 10 deg
					Move( 1f );
				}
			} else { // always keep speed maxed during turn
				Move( 1f );
			}
		} else {
			Steer( transform.position + transform.forward );
			Move( 0f );
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
		float _thrust = totalThrust * percent ;
		if (myRigidbody) {
			myRigidbody.AddForce( transform.forward * _thrust * Time.fixedDeltaTime);
		} else {
			transform.position = transform.position + (transform.forward * _thrust * Time.fixedDeltaTime);
		}
		// send percent to thruster objects for visuals
		for ( int t=0; t < thrusters.Length; t++ ) {
			if ( thrusters[t].particle ) {
				if ( percent == 0 ) {
					thrusters[t].particle.emissionRate = 0f;
				} else {
					thrusters[t].particle.emissionRate = 30f;
					thrusters[t].particle.startSpeed = thrusters[t].particleLow + ( (thrusters[t].particleHigh - thrusters[t].particleLow) * percent );
				}
			}
		}
	}

	float GetThrustValue () {
		float _thrust = 0f;
		for ( int t=0; t < thrusters.Length; t++ ) {
			_thrust += thrusters[t].maxThrust;
		}
		return _thrust;
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



























