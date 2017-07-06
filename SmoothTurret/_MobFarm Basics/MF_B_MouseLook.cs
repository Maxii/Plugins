using UnityEngine;
using System.Collections;

// place on a camera or on an object which has a camera as a child.
[HelpURL("http://mobfarmgames.weebly.com/mf_b_mouselook.html")]
public class MF_B_MouseLook : MonoBehaviour {

	public float sensitivity = 5f;
	public float elevationLimit = 90f;
	[Header("Use 'z' key to toggle zoom level.")]
	public float zoom = 3f;
	[Header("Use '~' key to activate/deactivate mouse control.")]
	[Tooltip("If true, the curson will be hidden.")]
	public bool mouseControl;
	[Tooltip("Toggle whether to show Aim Object.")]
	public bool aimObjectActive;
	[Tooltip("Object that shows where the camera is pointed.")]
	public GameObject aimObject;
	
	float rotY = 0f; 
	float rotX = 0f;
	float curSens;
	bool zoomToggle;
	Camera myCamera;
	bool error;

	void Awake () {
		if ( CheckErrors() == true ) { return; }

		curSens = sensitivity;
		rotY = transform.localRotation.eulerAngles.y;
		rotX = transform.localRotation.eulerAngles.x;

	}

	void OnEnable () {
		SetMouseControl( mouseControl );
	}

	void Update () {
		if ( error == true ) { return; }

		// show and hide aimObject
		if ( aimObject ) {
			if ( mouseControl == true && aimObjectActive == true ) {
				aimObject.SetActive( true );
			} else {
				aimObject.SetActive( false );
			}
		}

		// activate/deactivate mouselook
		if ( Input.GetKeyDown( "`" ) ) { // using ` or ~ because escape has a reserved function while in play mode
			SetMouseControl( !mouseControl );
		}

		if ( mouseControl == true ) {
			rotY += Input.GetAxis("Mouse X") * curSens;
			rotX += -Input.GetAxis("Mouse Y") * curSens;
			rotX = Mathf.Clamp( 	rotX, 	-elevationLimit, elevationLimit );
			transform.localRotation = Quaternion.Euler( rotX, rotY, 0f );

			// toggle zoom level
			if ( Input.GetKeyDown( "z" ) ) {
				zoomToggle = !zoomToggle;
				if ( zoomToggle == true ) {
					if ( myCamera) { myCamera.fieldOfView = 60f / zoom; }
					curSens = sensitivity / zoom;
				} else {
					if ( myCamera) { myCamera.fieldOfView = 60f; }
					curSens = sensitivity;
				}
			}
		}
	}

	void SetMouseControl ( bool locked ) {
		if ( locked == true ) {
			Cursor.lockState = CursorLockMode.Locked;
			mouseControl = true;
		} else {
			Cursor.lockState = CursorLockMode.None;
			mouseControl = false;
		}
	}

	bool CheckErrors () {
		if ( GetComponent<Camera>() ) {
			myCamera = GetComponent<Camera>();
		} else {
			int _childCount = transform.childCount;
			if ( _childCount > 0 ) { // found at least 1 child
				for ( int i=0; i < _childCount; i++ ) {
					if ( transform.GetChild(i).GetComponent<Camera>() ) {
						myCamera = transform.GetChild(i).GetComponent<Camera>();
						break; // found camera
					}
				}
			}
		}
		if ( myCamera == null ) { Debug.Log( this+": No camera found on object or immediate children."); error = true; }
		return error;
	}
}
