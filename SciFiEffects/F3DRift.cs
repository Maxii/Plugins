using UnityEngine;
using System.Collections;

public class F3DRift : MonoBehaviour {

    public float RotationSpeed;
    public float MorphSpeed, MorphFactor;

    Vector3 dScale;


	// Use this for initialization
	void Start () {

        dScale = transform.localScale;

	}
	
	// Update is called once per frame
	void Update () {

        transform.rotation = transform.rotation * Quaternion.Euler(0, 0, RotationSpeed * Time.deltaTime);
        transform.localScale = new Vector3(dScale.x, dScale.y, dScale.z + Mathf.Sin(Time.time * MorphSpeed) * MorphFactor);

	}
}
