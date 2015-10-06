using UnityEngine;
using System.Collections;

public class MF_BasicProjectile : MonoBehaviour {

	public float damage;
	public float blastRadius;
	public GameObject blastObject;

	[HideInInspector] public float duration;
	
	float startTime;
	
	void Start () {
		startTime = Time.time;
	}
	
	void FixedUpdate () {
		if (Time.time >= startTime + duration) {
			Destroy(gameObject);
		}
		// cast a ray to check hits along path - compensating for fast animation
		RaycastHit hit = default(RaycastHit);
		if ( Physics.Raycast(transform.position, rigidbody.velocity, out hit, rigidbody.velocity.magnitude * Time.fixedDeltaTime, ~(1<<9) ) ) {
			Destroy(gameObject);
			GameObject blastObj = (GameObject)Instantiate( blastObject, hit.point, Quaternion.identity );
			if ( blastRadius > 0 ) {
				blastObj.transform.localScale = new Vector3( blastRadius*2, blastRadius*2, blastRadius*2 );
				DoExplode( hit.point );
			} else {
				DoHit( hit.collider.gameObject );
			}
		}
	}

	private void DoExplode ( Vector3 location ) {
		Collider[] colliders = Physics.OverlapSphere( location, blastRadius );
		foreach (Collider _hit in colliders) {
			if ( _hit.transform.root.GetComponent<MF_BasicStatus>() ) { // hit object has a status script
				_hit.transform.root.GetComponent<MF_BasicStatus>().health -= damage;
			}
		}	
	}
	
	private void DoHit ( GameObject thisObject ) {
		// do stuff to the target object when it gets hit
		if ( thisObject.GetComponent<MF_BasicStatus>() ) {
			thisObject.GetComponent<MF_BasicStatus>().health -= damage;
		}
	}
}
