using UnityEngine;
using System.Collections;

public class ST_Player : MonoBehaviour {

	public bool turretControl = true;
	public GameObject aimObject;
	public GameObject fireObject;
	[Tooltip("Targeting range to use if clicking on empty space.")]
	public float emptyAimRange = 100;
	[Tooltip("aimObject always appears at Empty Aim Range.")]
	public bool fixedAimObjectRange;
	
	bool? turretControlToggle = null;
	bool error;

	void Start () {
		if ( CheckErrors() == true ) { return; }

		if ( aimObject ) { // aimObject is defined
			if ( !aimObject.activeInHierarchy ) { // not active in hierarchy - object is a prefab
				// create from prefab
				aimObject = (GameObject)Instantiate( aimObject, Vector3.zero, Quaternion.identity );
			}
		} else {
			aimObject = new GameObject("AimPoint");
		}
		aimObject.SetActive(false);

		if ( fireObject ) { // aimObject is defined
			if ( !fireObject.activeInHierarchy ) { // not active in hierarchy - object is a prefab
				// create from prefab
				fireObject = (GameObject)Instantiate( fireObject, Vector3.zero, Quaternion.identity );
			}
		} else {
			fireObject = new GameObject("FirePoint");
		}
		fireObject.SetActive(false);

	}
	
	void Update () {
		if ( error == true ) { return; }

		if ( turretControl != turretControlToggle ) {
			turretControlToggle = turretControl;
			if ( turretControl == true ) {
				aimObject.SetActive(true);
				fireObject.SetActive(false);
			} else {
				aimObject.SetActive(false);
				fireObject.SetActive(false);
			}
		}
		
		if ( turretControl == true && aimObject ) {
			// find mouse position in world
			Ray _ray = Camera.main.ScreenPointToRay( Input.mousePosition );
			RaycastHit _hit;
			Vector3 _objPos;

			if ( Physics.Raycast( _ray, out _hit, Mathf.Infinity ) ) { // hit collider
				if ( fixedAimObjectRange == true ) {
					_objPos = _ray.origin + (_ray.direction * emptyAimRange);
				} else {
					_objPos = _hit.point;
				}
			} else { // hit nothing
				_objPos = _ray.origin + (_ray.direction * emptyAimRange);
			}
			// move aimObject to mouse location
			aimObject.transform.position = _objPos;
		}
	}

	private bool CheckErrors () {
		error = false;

		return error;
	}
}
