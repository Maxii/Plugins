using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

public class TestEvent : MonoBehaviour {

	public event Action Clicked = delegate {};
	
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	void OnGUI()
	{
		if(GUI.Button(new Rect(200,200,100,100), "Press Me"))
		{
			Clicked();
		}
	}
}
