using UnityEngine;
using System.Collections;

[HelpURL("http://mobfarmgames.weebly.com/mf_abstractplatformcontrol.html")]
public abstract class MF_AbstractPlatformControl : MonoBehaviour {

	[Header("Location of targeting script:")]
	[Tooltip("If blank: Recursively searches self and parents until a targeting script is found.")]
	public MF_AbstractTargeting targetingScript;
	[Header("Weapon list:")]
	public WeaponData[] weapons;
	[Space(8f)]
	[Split1("How long the platform must be aimed at the target before firing. Aim Time also appears on the weapon script, and the greater of the two values is used.")]
	public float aimTime;

	[HideInInspector] public int curWeapon;

	protected MF_AbstractPlatform platformScript;
	protected float curAimTime;
	protected bool error;

	[System.Serializable]
	public class WeaponData {
		public GameObject weapon;
		[HideInInspector] public MF_AbstractWeapon script;
		[HideInInspector] public bool burst;
	}

	// Use this for initialization
	public virtual void Awake () {
		if (CheckErrors() == true) { return; }

		platformScript = GetComponent<MF_AbstractPlatform>();

		// cache scripts for all weapons
		if ( weapons.Length > 0 ) {
			for (int wd=0; wd < weapons.Length; wd++) {
				if (weapons[wd].weapon) {
					weapons[wd].script = weapons[wd].weapon.GetComponent<MF_AbstractWeapon>();
				}
			}
		}
	}

	public virtual void OnEnable () {
		curAimTime = 0f;
	}

	// check target data vs weapon requirements, and sets platform checkData
	public bool CheckData () {
		if ( targetingScript ) {
			if ( weapons.Length > 0 ) {
				bool p = targetingScript.hasPrecision != MFnum.ScanSource.None ? true : false;
				bool a = targetingScript.hasAngle != MFnum.ScanSource.None ? true : false;
				bool r = targetingScript.hasRange != MFnum.ScanSource.None ? true : false;
				bool v = targetingScript.hasVelocity != MFnum.ScanSource.None ? true : false;

				// set platformScript checkData for use of intercept
				if ( a == false || r == false || v == false ) {
					platformScript.checkData = false;
				} else {
					platformScript.checkData = true;
				}

				// check if weapon requires certian data
				if ( weapons[curWeapon].script.requirePrecision == true ) {
					if ( p == false ) { return false; }
				}
				if ( weapons[curWeapon].script.requireAngle == true ) {
					if ( a == false ) { return false; }
				}
				if ( weapons[curWeapon].script.requireRange == true ) {
					if ( r == false ) { return false; }
				}
				if ( weapons[curWeapon].script.requireVelocity == true ) {
					if ( v == false ) { return false; }
				}
			}
		} else {
			platformScript.checkData = true; // default to true if no targeting script ( targets are being supplied some other way )
		}
		return true;
	}

	public virtual bool CheckErrors () {
		error = false;

		if ( weapons.Length > 0 ) {
			for (int cw=0; cw < weapons.Length; cw++) {
				if (weapons[cw].weapon == false) { Debug.Log( this+": TurretControl weapon index "+cw+" hasn't been defined." ); error = true; }
			}
		}
			
		if ( !targetingScript ) {
			targetingScript = UtilityMF.GetComponentInParent<MF_AbstractTargeting>( transform );
		}

		if ( !GetComponent<MF_AbstractPlatform>() ) { Debug.Log( this+": No platform script found." ); error = true; }
		
		return error;
	}
}
