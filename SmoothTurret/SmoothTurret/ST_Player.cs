using UnityEngine;
using System.Collections;

public class ST_Player : MonoBehaviour {

	public bool turretControl = true;
	public GameObject aimObject;
	public GameObject fireObject;
	[Tooltip("Targeting range to use if clicking on empty space.")]
	public float emptyClickRange = 100;
	
	bool? turretControlToggle = null;
	bool error;
	
	void Start () {
		if ( CheckErrors() == true ) { return; }

		if ( !aimObject ) {
			aimObject = new GameObject("AimPoint");
		}
		if ( !fireObject ) {
			fireObject = new GameObject("FirePoint");
		}
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
				_objPos = _hit.point;
			} else { // hit nothing
				_objPos = _ray.origin + (_ray.direction * emptyClickRange);
			}
			// move aimObject to mouse location
			aimObject.transform.position = _objPos;
		}
	}

	private bool CheckErrors () {
		error = false;
//		string _object = gameObject.name;

//		if ( clickLayer != "" ) {
//			if ( LayerMask.NameToLayer( clickLayer ) != -1 ) {
//				mask = LayerMask.GetMask( clickLayer );
//			} else {
//				Debug.Log(_object+": Couldn't find layer: "+clickLayer); error = true;
//			}
//		}

		return error;
	}
}
