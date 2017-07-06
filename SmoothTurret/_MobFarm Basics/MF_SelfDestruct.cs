using UnityEngine;
using System.Collections;

[HelpURL("http://mobfarmgames.weebly.com/mf_selfdestruct.html")]
public class MF_SelfDestruct : MonoBehaviour {

	[Tooltip("GameObject will be despawned or destroyed this long after creation.")]
	public float destructTime;

	AP_Reference poolRefScript;

	void Start () {
		poolRefScript = GetComponent<AP_Reference>();
	}
	
	void OnEnable () {
		Invoke( "DoDestroy", destructTime );
	}

	void OnDisable () {
		CancelInvoke();
	}

	void DoDestroy () {
		if ( poolRefScript ) {
			MF_AutoPool.Despawn( poolRefScript );
		} else {
			Destroy( gameObject );
		}
	}
}
