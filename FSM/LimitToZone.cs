using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

public class LimitToZone : MonoBehaviour {

	public Collider allowedZone;
	
	Bounds _bounds;
	
	void Start()
	{
		_bounds = allowedZone.bounds;
	}
	
	// Update is called once per frame
	void LateUpdate () {
	
		var position = transform.position;
		position.x = Mathf.Clamp(position.x,_bounds.min.x, _bounds.max.x);
		position.z = Mathf.Clamp(position.z,_bounds.min.z, _bounds.max.z);
		transform.position = position;
	}
}
