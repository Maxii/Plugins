using UnityEngine;
using System.Collections;

public class ST_TurretSwitcher : MonoBehaviour {

	public ST_TurretControl.ControlType ActivatedBehavior = ST_TurretControl.ControlType.Player_Mouse;
	public ST_TurretControl.ControlType DeactivatedBehavior = ST_TurretControl.ControlType.None;
	public bool switchCamera;
	[Header("Use number keys to switch between turrets.")]
	public TurretData[] turrets;
	public int selectedTurret;
	[Tooltip("If blank: Will assume the player script is on the same object. May also be left blank if none of the behavious use a player script.")]
	public ST_Player playerScript;

	[System.Serializable]
	public class TurretData {
		public ST_TurretControl turretScript;
		public GameObject turretCamera;
		[HideInInspector] public MF_AbstractPlatform platformScript;
	}

	void Start () {
		if ( !playerScript ) {
			if ( GetComponent<ST_Player>() ) {
				playerScript = GetComponent<ST_Player>();
			}
		}
		for ( int t=0; t < turrets.Length; t++ ) {
			if (turrets[t].turretScript) {
				turrets[t].platformScript = turrets[t].turretScript.GetComponent<MF_AbstractPlatform>();
			}
		}

		SelectTurret( selectedTurret );
	}

	void Update () {
		if ( Input.anyKeyDown == true ) {
			int? _value = NumKey2Int( Input.inputString );
			if ( _value > 0 && _value <= turrets.Length ) {
				SelectTurret( (int)_value - 1 );
			}
		}
	}

	void SelectTurret ( int index ) {
		for ( int i=0; i < turrets.Length; i++ ) {
			if ( i == index ) { // activate
				if ( switchCamera == true && turrets[i].turretCamera ) {
					turrets[i].turretCamera.SetActive(true);
				}
				if ( turrets[i].turretScript ) {
					turrets[i].turretScript.controller = ActivatedBehavior;
					if ( playerScript ) {
						playerScript.turretControl = true;
						turrets[i].turretScript.playerScript = playerScript;
						turrets[i].turretScript.target = playerScript.aimObject.transform;
						if ( ActivatedBehavior == ST_TurretControl.ControlType.Player_Mouse || ActivatedBehavior == ST_TurretControl.ControlType.Player_Click ) {
							turrets[i].platformScript.aimObjectActive = true;
						} else {
							turrets[i].platformScript.aimObjectActive = false;
						}
					}
				} else if ( playerScript ) { // no turret
					playerScript.turretControl = false;
				}
			} else { // deactivate
				if ( switchCamera == true && turrets[i].turretCamera ) {
					turrets[i].turretCamera.SetActive(false);
				}
				if ( turrets[i].turretScript ) {
					turrets[i].turretScript.controller = DeactivatedBehavior;
					if ( ActivatedBehavior == ST_TurretControl.ControlType.Player_Mouse || ActivatedBehavior == ST_TurretControl.ControlType.Player_Click ) {
						turrets[i].platformScript.aimObjectActive = false;
					} else {
						turrets[i].platformScript.aimObjectActive = true;
					}
				}
			}
		}
	}

	// returns a single digit number string as an int. returns null if any other characters
	int? NumKey2Int (string value) {
		if ( value.Length != 1 ) { return null; }
		char letter = value[0];
		return letter - 48;
	}
}
