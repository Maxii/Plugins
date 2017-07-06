using UnityEngine;
using System.Collections;

[HelpURL("http://mobfarmgames.weebly.com/mf_b_objectpoolreference.html")]
public class AP_Reference : MonoBehaviour {

	[Tooltip("When Despawn() is called, this object will be disabled, but will wait for delay time before becomeing available in the object pool.")]
	public float delay;

	[HideInInspector] public AP_Pool poolScript; // stores the location of the object pool script for this object
	[HideInInspector] public float timeSpawned;

	public bool Despawn ( float del ) { // -1 will use delay specified in this script
		if ( del >= 0 ) { // override delay
			if ( poolScript ) {
				Invoke( "DoDespawn", del );
				gameObject.SetActive(false);
				return true;
			} else {
				return false;
			}
		} else {
			return DoDespawn();
		}
	}

	bool DoDespawn() {
		if ( poolScript ) {
			poolScript.Despawn( gameObject, this );
			return true;
		} else {
			return false;
		}
	}

}
