using UnityEngine;
using System.Collections;

// place on a camera or on an object which has a camera as a child.
public class MF_B_MouseLook : MonoBehaviour {

	public float sensitivity = 5f;
	public float elevationLimit = 90f;
	[Header("Use 'z' key to toggle zoom level.")]
	public float zoom = 3f;
	[Header("Use '~' key to activate/deactivate mouse control.")]
	[Tooltip("If true, the curson will be hidden.")]
	public bool hideCursor;
	[Tooltip("If true, the cursor will locked to the center of the screen.")]
	public bool centerCursor;
	
	float rotY = 0f; 
	float rotX = 0f;
	float curSens;
	bool cursorLock = true;
	bool zoomToggle;
	Camera myCamera;
	bool error;
	
	void Start () {
		if ( CheckErrors() == true ) { return; }

		curSens = sensitivity;
		rotY = transform.localRotation.eulerAngles.y;
		rotX = transform.localRotation.eulerAngles.x;
	}

	void OnEnable () {
		SetLockCursor( cursorLock );
	}
	
	void Update () {
		if ( error == true ) { return; }

		// activate/deactivate mouselook
		if ( Input.GetKeyDown( "`" ) ) { // using `/~ because escape has a reserved function in the editor
			SetLockCursor( !cursorLock );
		}

		if ( cursorLock == true ) {
			rotY += Input.GetAxis("Mouse X") * curSens;
			rotX += -Input.GetAxis("Mouse Y") * curSens;
			rotX = Mathf.Clamp( 	rotX, 	-elevationLimit, elevationLimit );
			transform.localRotation = Quaternion.Euler( rotX, rotY, 0f );

			// toggle zoom level
			if ( Input.GetKeyDown( "z" ) ) {
				zoomToggle = !zoomToggle;
				if ( zoomToggle == true ) {
					myCamera.fieldOfView = 60f / zoom;
					curSens = sensitivity / zoom;
				} else {
					myCamera.fieldOfView = 60f;
					curSens = sensitivity;
				}
			}
		}
	}

	void SetLockCursor ( bool locked ) {
		if ( locked == true ) {
			Cursor.visible = !hideCursor;
			if ( centerCursor == true ) {
				Cursor.lockState = CursorLockMode.Locked;
			} else {
				Cursor.lockState = CursorLockMode.None;
			}
			cursorLock = true;
		} else {
			Cursor.visible = true;
			Cursor.lockState = CursorLockMode.None;
			cursorLock = false;
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
