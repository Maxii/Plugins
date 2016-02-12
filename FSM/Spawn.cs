using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

public class Spawn : MonoBehaviour {
	
	public Transform prefab;
	
	public int numberToSpawn;
	
	// Use this for initialization
	void Start () {
		
		for(var i = 0; i < numberToSpawn; i++)
			Instantiate(prefab, transform.position, Quaternion.identity);
	
	}
	
	
}
