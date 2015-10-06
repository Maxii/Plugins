using UnityEngine;
using System.Collections;

public class MF_BasicStatus : MF_AbstractStatus {

	// Update is called once per frame
	void Update () {
		if ( health <= 0 ) {
			Destroy(gameObject);
		}
	}
}
