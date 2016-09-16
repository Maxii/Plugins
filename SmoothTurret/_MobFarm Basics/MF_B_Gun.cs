using UnityEngine;
using System.Collections;

public class MF_B_Gun : MF_AbstractWeapon {

	public enum GravityUsage { Default, Gravity, No_Gravity }

	[Header("Projectile specific settings:")]
	public GameObject shot;
	[Tooltip("True: Change all shots to use gravity.\nFalse: Will use gravity only if shot rigidbody uses gravity.")]
	public GravityUsage gravity;

	bool usingGravity;
	bool sceneLoaded;

	public void OnValidate() {
		if ( sceneLoaded == true ) {
			usingGravity = false; // remains false if forceing no gravity or if projectil doesn't use it
			if ( shot.GetComponent<Rigidbody>() ) { // verify rigidbody
				if ( gravity == GravityUsage.Gravity ) { // force gravity
					usingGravity = true;
				} else if ( shot.GetComponent<Rigidbody>().useGravity == true && gravity == GravityUsage.Default ) { // use gravity if projectile does
					usingGravity = true;
				}
			}

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

	public override void Start () {
		if (error == true) { return; }
		base.Start();
		sceneLoaded = true;
		OnValidate();
	}

	// use this only if already checked if weapon is ready to fire
	public override void DoFire () {
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
			MF_B_Projectile shotScript = myShot.GetComponent<MF_B_Projectile>();
			shotScript.duration = shotDuration;
		}

		base.DoFire();
	}

	public override bool RangeCheck ( Transform target ) {
		return RangeCheck ( target, 1f );
	}
	public override bool RangeCheck ( Transform target, float mult ) {
		if (active == false) { return false; }
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

	public override bool CheckErrors () {
		base.CheckErrors();

		if ( shot == null ) { Debug.Log( this+": Weapon shot prefab hasn't been defined."); error = true; }
		if ( shot && shot.GetComponent<MF_B_Projectile>() == null ) { Debug.Log( this+": Shot prefab does not have a compatible projectile script. (Requires MF_B_Projectile)"); error = true; }

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
















