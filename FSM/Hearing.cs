using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

public class Hearing : MonoBehaviour {
	
	
	GunBehaviour _gun;
	Transform _player;
	
	// Use this for initialization
	void Start () {
		_player = Player.current.transform;
		_gun = GunBehaviour.Gun;
	}
	
	// Update is called once per frame
	void Update () {
		var vectorToPlayer = (_player.position - Vector3.up * 0.5f - transform.position);
		if(vectorToPlayer.sqrMagnitude < 150*150 && (Time.time - _gun.lastFiredTime < 1))
		{
			SendMessage("DetectedPlayer");
		}
	}
}
