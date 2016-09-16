using UnityEngine;
using System.Collections;

public class MF_B_SelfDestruct : MonoBehaviour {

	[Tooltip("GameObject will be destroyed this long after creation.")]
	public float destructTime;
	
	float startTime;
	
	void Start () {
		startTime = Time.time;
	}
	
	void Update () {
		if ( Time.time >= startTime + destructTime ) {
			Destroy(gameObject);
		}
	}
}
