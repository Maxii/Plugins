using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

public class TestOnGui : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	void Update()
	{
		Something();
	}
	
	void Something()
	{
	}
	
	void OnGUI()
	{
		GUI.Label(new Rect(0,0,100,100), "Hello");
	}
}
