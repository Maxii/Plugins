using UnityEngine;
using System.Collections;

public class MF_BasicSelfDestruct : MonoBehaviour {

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
