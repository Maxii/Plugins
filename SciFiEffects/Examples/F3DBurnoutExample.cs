using UnityEngine;
using System.Collections;

public class F3DBurnoutExample : MonoBehaviour {

    MeshRenderer[] turretParts;
    int BurnoutID;


	// Use this for initialization
	void Start () {

        BurnoutID = Shader.PropertyToID("_BurnOut");
        turretParts = GetComponentsInChildren<MeshRenderer>();

	}
	
	// Update is called once per frame
	void Update () {
	
        for(int i = 0; i < turretParts.Length; i++)
        {
            turretParts[i].material.SetFloat(BurnoutID, Mathf.Lerp(0, 2f, (Mathf.Sin(Time.time)) / 2));
        }

	}
}
