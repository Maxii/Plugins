using UnityEngine;
using System.Collections;

public class F3DTurnTable : MonoBehaviour {

    public float speed;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {

        transform.rotation = transform.rotation * Quaternion.Euler(0, speed * Time.deltaTime, 0);

	}
}
