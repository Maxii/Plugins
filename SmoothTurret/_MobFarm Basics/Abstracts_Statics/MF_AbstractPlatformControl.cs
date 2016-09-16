using UnityEngine;
using System.Collections;

public abstract class MF_AbstractPlatformControl : MonoBehaviour {

	[Header("Location of targeting script:")]
	[Tooltip("If blank: Recursively searches self and parents until a targeting script is found.")]
	public MF_AbstractTargeting targetingScript;
	[Header("Weapon list:")]
	public WeaponData[] weapons;

	[HideInInspector] public int curWeapon;

	protected bool error;

	[System.Serializable]
	public class WeaponData {
		public GameObject weapon;
		[HideInInspector] public MF_AbstractWeapon script;
		[HideInInspector] public bool burst;
	}

	// Use this for initialization
	public virtual void Start () {
		if (CheckErrors() == true) { return; }

		// cache scripts for all weapons
		if ( weapons.Length > 0 ) {
			for (int wd=0; wd < weapons.Length; wd++) {
				if (weapons[wd].weapon) {
					weapons[wd].script = weapons[wd].weapon.GetComponent<MF_AbstractWeapon>();
				}
			}
		}
	}

	public virtual bool CheckErrors () {
		error = false;
		Transform rps;

		if ( weapons.Length > 0 ) {
			for (int cw=0; cw < weapons.Length; cw++) {
				if (weapons[cw].weapon == false) { Debug.Log( this+": TurretControl weapon index "+cw+" hasn't been defined." ); error = true; }
			}
		}

		// look for defined targeting script
		if ( !targetingScript ) {
			rps = UtilityMF.RecursiveParentComponentSearch( "MF_AbstractTargeting", transform );
			if ( rps != null ) {
				targetingScript = rps.GetComponent<MF_AbstractTargeting>();
			} 
		}
		
		return error;
	}
}
