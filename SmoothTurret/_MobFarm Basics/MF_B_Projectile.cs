using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[HelpURL("http://mobfarmgames.weebly.com/mf_b_projectile.html")]
public class MF_B_Projectile : MF_AbstractMunition {

	[Tooltip("If true, the projectile will face the direction of velocity.")]
	public bool faceVelocity = true;
	[Tooltip("Damage of the projectile when it hits.")]
	public float damage;
	[Tooltip("Radius to apply damage to when it hits.")]
	public float damageRadius;

	Rigidbody myRigidbody;

	bool error;
	
	protected override void Awake () {
		if ( CheckErrors() ) { return; }
		base.Awake();
		myRigidbody = GetComponent<Rigidbody>();
	}

	protected override void OnEnable () {
		if ( error == true ) { return; }
		base.OnEnable();
//		if ( projectileBody ) { projectileBody.SetActive( true ); }
	}

	void OnDisable () {
		if ( error == true ) { return; }
		myRigidbody.velocity = Vector3.zero;
		myRigidbody.angularVelocity = Vector3.zero;
	}

	void Update () {
		monitor = ( Time.time - startTime ) / duration; // for fx controller
		UpdateStats();
	}

	void FixedUpdate () {
		if ( error == true ) { return; }

		if ( Time.time >= startTime + duration ) {
			DoDeath(); // projectile death
		}
			
		// angle towards velocity
		if ( faceVelocity == true && myRigidbody.velocity != Vector3.zero ) {
			myRigidbody.rotation = Quaternion.LookRotation(myRigidbody.velocity);
		}
		
		// check for a hit - cast a ray to check hits along path - compensating for fast animation
		RaycastHit hit = default(RaycastHit);
		if ( Physics.Raycast( myRigidbody.position, myRigidbody.velocity, out hit, myRigidbody.velocity.magnitude * Time.fixedDeltaTime ) ) {
			DoHit( hit ); // actual shot contact	
		}
	}
	
	void DoHit ( RaycastHit hit ) { // hit a target
		if ( damageRadius > 0 ) {
			DoExplode( hit );
		} else { 
			transform.position = hit.point; // move to hit location
			DoDamage( hit.transform );
		}
		if ( fxScript ) { fxScript.TriggerFx( 0 ); } // send trigger to FxController to activate impact fx
		DoDeath(); // projectile death for fx
	}

	private void DoExplode ( RaycastHit hit ) {
		Collider[] colliders = Physics.OverlapSphere( hit.point, damageRadius );
		for ( int i=0; i < colliders.Length; i++ ) {
			RaycastHit losHit = default(RaycastHit);
			if ( Physics.Linecast( hit.point, colliders[i].transform.position, out losHit ) ) { 
				if ( losHit.collider == colliders[i] ) { // collider not blocked
					DoDamage ( losHit.collider.transform );
				}
			}
		}
		// if shot hit a collider, that collider won't register during los check. So damage it directly:
		if ( hit.collider != null ) {
			DoDamage( hit.transform );
		}
	}

	public void DoDamage ( Transform trans ) {
		// do stuff to the target object when it gets hit
		MF_AbstractStats sScript = null;
		sScript = trans.GetComponentInParent<MF_AbstractStats>(); // look for script in parents

		if ( sScript && !Mathf.Approximately( sScript.damageID, damageID + Time.time ) ) { // don't apply damage if alread damaged by this source, this frame - so explosions don't damage more than once
			// apply damage to target
			sScript.damageID = damageID + Time.time; // mark as damaged by this source, this frame
			sScript.DoDamage( damage );

//			Debug.Log( trans+" > "+sScript+" : "+damage );
		}
	}

	bool CheckErrors() {
		error = false;

		if ( GetComponent<Rigidbody>() == null ) { Debug.Log( this + ": Projectile must have a Rigidbody on the root level." ); error = true; }

		return error;
	}

}







