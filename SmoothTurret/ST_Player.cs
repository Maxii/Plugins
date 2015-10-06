using UnityEngine;
using System.Collections;

public class ST_Player : MonoBehaviour {

	public bool turretControl = true;
	public GameObject aimObject;
	
	bool turretControlToggle;
	
	void Start () {
		if ( !aimObject ) {
			aimObject = new GameObject("AimPoint");
		}
	}
	
	void Update () {
		
		if ( turretControl != turretControlToggle ) {
			turretControlToggle = turretControl;
			if (aimObject) {
				if ( turretControl == true ) {
					aimObject.SetActive(true);
				} else {
					aimObject.SetActive(false);
				}
			}
		}
		
		if ( turretControl == true && aimObject ) {
			// find mouse position in world
			Ray _ray = Camera.main.ScreenPointToRay( Input.mousePosition );
			RaycastHit _hit;
			Vector3 _objPos = Vector3.zero;
			if ( Physics.Raycast( _ray, out _hit, Mathf.Infinity, ~(1 << 9) ) ) {
				_objPos = _hit.point;
			}
			// move aimObject to mouse location
			aimObject.transform.position = _objPos;
		}
	}
}
