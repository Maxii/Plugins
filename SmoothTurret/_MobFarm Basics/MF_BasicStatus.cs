using UnityEngine;
using System.Collections;

public class MF_BasicStatus : MF_AbstractStatus {

	[Tooltip("Object to create upon destruction.")]
	public GameObject blastObject;
	[Tooltip("Radius of the blast object. 0 = Use object's default size.")]
	public float blastRadius; // 0 = don't change size

	public override float health {
		get { return _health; }
		set { _health = value;
			if ( _health <= 0 ) {
				Destroy(gameObject);
				if (blastObject) {
					GameObject blastObj = (GameObject)Instantiate( blastObject, transform.position, Quaternion.identity );
					if ( blastRadius != 0 ) {
						blastObj.transform.localScale = new Vector3( blastRadius*2, blastRadius*2, blastRadius*2 );
					}
				}
			}
		}
	}

}
