using UnityEngine;
using System.Collections;

[HelpURL("http://mobfarmgames.weebly.com/mf_b_gun.html")]
public class MF_B_Gun : MF_AbstractWeapon {

	public enum GravityUsage { Default, Gravity, NoGravity }

	[Header("Projectile specific settings:")]
	public MF_B_Projectile shot;
	public bool objectPool;
	public float addToPool;
	public int minPool;
	[Tooltip("Default: Will use gravity only if shot rigidbody uses gravity.\nGravity: Change all shots to use gravity.\nNo Gravity: Change all shots to not use gravity.")]
	public GravityUsage gravity;

	bool usingGravity;

	public void OnValidate() {
		if ( Application.isPlaying == true ) {
			if ( CheckErrors() == true ) { return; }

			usingGravity = false; // remains false if forceing no gravity or if projectile doesn't use it
			if ( shot.GetComponent<Rigidbody>() ) { // verify rigidbody
				if ( gravity == GravityUsage.Gravity ) { // force gravity
					usingGravity = true;
				} else {
					if ( shot.GetComponent<Rigidbody>().useGravity == true && gravity == GravityUsage.Default ) { // use gravity if projectile does
						usingGravity = true;
					}
				}
			}
			curInaccuracy = inaccuracy;
			// compute missing value: shotSpeed, maxRange, shotDuration
			if ( shotSpeed <= 0 ) {
				shotSpeed = maxRange / shotDuration;
			} else if ( maxRange <= 0 ) {
				maxRange = shotSpeed * shotDuration;
			} else { // compute shotDuration even if all 3 are set to keep math consistant
				shotDuration = maxRange / shotSpeed;
			}
		}
	}

	public override void Awake () {
		if (error == true) { return; }
		base.Awake();
		if ( objectPool == true ) {
			MF_AutoPool.InitializeSpawn( shot.gameObject, addToPool, minPool );
		}
	}

	// use this only if already checked if weapon is ready to fire
	public void DoFire () {
		DoFire( null );
	}
	public override void DoFire ( Transform target ) {
		if (error == true) { return; }
		if (active == false) { return; }
		
		// fire weapon
		// create shot
		GameObject myShot = null;
		for (int spr=0; spr < shotsPerRound; spr++) {
			if ( objectPool == true ) {
				myShot = MF_AutoPool.Spawn( shot.gameObject, exits[curExit].transform.position, exits[curExit].transform.rotation );
			} else {
				myShot = (GameObject) Instantiate( shot.gameObject, exits[curExit].transform.position, exits[curExit].transform.rotation );
			}
			if ( myShot != null ) {
				Vector2 errorV2 = Random.insideUnitCircle * curInaccuracy;
				myShot.transform.rotation *= Quaternion.Euler(errorV2.x, errorV2.y, 0);
				Rigidbody _rb = myShot.GetComponent<Rigidbody>();
				_rb.velocity = platformVelocity + (myShot.transform.forward * shotSpeed);
				_rb.useGravity = usingGravity;
				MF_B_Projectile shotScript = myShot.GetComponent<MF_B_Projectile>();
				shotScript.duration = shotDuration;
			}
		}
		if ( myShot != null ) { // at least one shot was created
			base.DoFire( target );
		}
	}

	public override bool RangeCheck ( Transform target ) {
		return RangeCheck ( target, 1f );
	}
	public override bool RangeCheck ( Transform target, float mult ) {
		if ( active == false || target == null ) { return false; }

		float _sqRange = (exits[curExit].transform.position - target.position).sqrMagnitude;
		if ( usingGravity == true ) { // ballistic range **** does not account for height ?
			float _ballRange = (shotSpeed*shotSpeed) / -Physics.gravity.y ;
			if ( _sqRange <= (_ballRange*_ballRange) * (mult*mult) ) {
				return true;
			}
		} else { // standard range
			if ( _sqRange <= (maxRange*maxRange) * (mult*mult) ) {
				return true;
			}
		}
		return false;
	}

	public override float GetTimeOfFlight ( Transform trans ) {
		if ( active == false || trans == null ) { return 0f; }

		float _range = Vector3.Distance( exits[curExit].transform.position, trans.position );
		return ( _range / shotSpeed ) * 1.1f; // give 10% extra time
	}

	public override bool CheckErrors () {
		base.CheckErrors();

		if ( shot == null ) { Debug.Log( this+": Weapon shot hasn't been defined."); error = true; }

		int _e1 = 0;
		if (shotSpeed <= 0) { _e1++; }
		if (maxRange <= 0) { _e1++; }
		if (shotDuration <= 0) { _e1++; }
		if (_e1 > 1) {
			maxRange = 1f; // prevent div 0 error if another script accesses maxRange
			Debug.Log( this+": 2 of 3 need to be > 0: shotSpeed, maxRange, shotDuration");
			error = true;
		}
		
		return error;
	}
}
















