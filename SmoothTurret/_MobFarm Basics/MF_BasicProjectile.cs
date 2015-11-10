using UnityEngine;
using System.Collections;

public class MF_BasicProjectile : MonoBehaviour {

	public float damage;
	public float damageRadius;
	public GameObject blastObject;
	public float blastRadius;

	[HideInInspector] public float duration;
	
	float startTime;
	Rigidbody myRigidbody;
	
	void Start () {
		startTime = Time.time;
		myRigidbody = GetComponent<Rigidbody>();
	}
	
	void FixedUpdate () {
		// destroy self at end of duration
		if (Time.time >= startTime + duration) {
			Destroy(gameObject);
		}
		// angle towards velocity
		transform.rotation = Quaternion.LookRotation(myRigidbody.velocity);
		// cast a ray to check hits along path - compensating for fast animation
		RaycastHit hit = default(RaycastHit);
		if ( Physics.Raycast(transform.position, myRigidbody.velocity, out hit, myRigidbody.velocity.magnitude * Time.fixedDeltaTime ) ) {
			Destroy(gameObject);
			GameObject blastObj = (GameObject)Instantiate( blastObject, hit.point, Quaternion.identity );
			if (blastRadius != 0) {
				blastObj.transform.localScale = new Vector3( blastRadius*2, blastRadius*2, blastRadius*2 );
			}
			if ( damageRadius > 0 ) {
				DoExplode( hit.point );
			} else {
				DoHit( hit.collider.gameObject );
			}
		}
	}

	private void DoExplode ( Vector3 location ) {
		Collider[] colliders = Physics.OverlapSphere( location, damageRadius );
		foreach (Collider _hit in colliders) {
			MF_AbstractStatus _script = FindStatusScript( _hit.transform );
			if ( _script ) { // hit object has a status script
				_script.health -= damage;
			}
		}	
	}
	
	private void DoHit ( GameObject thisObject ) {
		// do stuff to the target object when it gets hit
		MF_AbstractStatus _script = FindStatusScript( thisObject.transform );
		if ( _script ) {
			_script.health -= damage;
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







