using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

public class Eyesight : MonoBehaviour {
	
	Transform _player;
	// Use this for initialization
	void Start () {
		_player = Player.current.transform;
	
	}
	
	// Update is called once per frame
	void Update () {
		var vectorToPlayer = (_player.position - Vector3.up * 0.5f - transform.position);
		
		if(vectorToPlayer.sqrMagnitude < 10000)
		{
			if(Vector3.Angle(transform.forward, vectorToPlayer) < 40)
			{
				RaycastHit hit;
				Debug.DrawRay(transform.position + Vector3.up * 0.75f, vectorToPlayer.normalized * 100);
				if(Physics.Raycast(transform.position+ Vector3.up *0.75f, vectorToPlayer, out hit, 100))
				{
					if(hit.transform == _player)
					{
						SendMessage("DetectedPlayer");
					}
				}
				
			}
			
		}
		if(vectorToPlayer.sqrMagnitude < 100)
		{
			SendMessage("DetectedPlayer");
			SendMessage("NearPlayer");
		}
	}
}
