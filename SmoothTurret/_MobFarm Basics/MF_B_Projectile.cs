using UnityEngine;
using System.Collections;

public class MF_B_Projectile : MonoBehaviour {

	[Tooltip("Damage of the projectile when hit.")]
	public float damage;
	[Tooltip("Radius to apply damage to when hit.")]
	public float damageRadius;
	[Tooltip("The prefab to display when hit.")]
	public GameObject hitObject;
	[Tooltip("The scale radius of the Hit Object. 0 = Don't change prefab scale.")]
	public float fxRadius;
	[Tooltip("If true, the projectile will face the direction of velocity.")]
	public bool faceVelocity = true;

	[HideInInspector] public float duration;

	float damageID;
	float startTime;
	Rigidbody myRigidbody;
	
	void Start () {
		startTime = Time.time;
		myRigidbody = GetComponent<Rigidbody>();
		damageID = GetInstanceID() * .5f;
	}

	void FixedUpdate () {
		// destroy self at end of duration
		if ( Time.time >= startTime + duration ) {
			Destroy( gameObject );
		}
		
		// angle towards velocity
		if ( faceVelocity == true ) {
			myRigidbody.rotation = Quaternion.LookRotation(myRigidbody.velocity);
		}
		
		// check for a hit - cast a ray to check hits along path - compensating for fast animation
		RaycastHit hit = default(RaycastHit);
		if ( Physics.Raycast( myRigidbody.position, myRigidbody.velocity, out hit, myRigidbody.velocity.magnitude * Time.fixedDeltaTime ) ) {
			DoHit( hit ); // actual shot contact	
		}
	}
	
	private void DoHit ( RaycastHit hit ) {
		Destroy( gameObject );
		GameObject blastObj = (GameObject)Instantiate( hitObject, hit.point, Quaternion.identity );
		if ( fxRadius != 0 ) {
			blastObj.transform.localScale = new Vector3( fxRadius*2, fxRadius*2, fxRadius*2 );
		}
		if ( damageRadius > 0 ) {
			DoExplode( hit );
		} else { 
			DoDamage( hit.transform );
		}
	}

	private void DoExplode ( RaycastHit hit ) {
		Collider[] colliders = Physics.OverlapSphere( hit.point, damageRadius );
		foreach (Collider _thisCollider in colliders) {
			// check that collider in sphere isn't blocked by another collider
			RaycastHit _losHit = default(RaycastHit);
			if ( Physics.Linecast( hit.point, _thisCollider.transform.position, out _losHit ) ) { 
				if ( _losHit.collider == _thisCollider ) { // collider not blocked
					DoDamage ( _losHit.collider.transform );
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
		MF_AbstractStatus _script = null;
		_script = FindStatusScript( trans ); // look for script in parents
		if ( _script && !Mathf.Approximately(_script.damageID, damageID + Time.time) ) { // don't apply damage if alread damaged by this source, this frame - so explosions don't damage more than once
			// apply damage to target
			_script.damageID = damageID + Time.time; // mark as damaged by this source, this frame
			_script.health -= damage;

//			Debug.Log( trans+" > "+_script+" : "+damage );
		}
	}

	private MF_AbstractStatus FindStatusScript ( Transform thisTransform ) {
		// move up tree looking for status script
		while ( thisTransform != null ) {
			MF_AbstractStatus _script = thisTransform.GetComponent<MF_AbstractStatus>();
			if ( _script ) {
				return _script;
			} else {
				thisTransform = thisTransform.parent;
			}
		}
		return null;
	}
}







