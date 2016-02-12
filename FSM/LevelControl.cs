using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

public class LevelControl : MonoBehaviour {
	
	public Transform monster;
	public Transform health;
	public Transform ammo;
	
	
	// Use this for initialization
	IEnumerator Start () {
		
		for(var i = 0; i < 30; i++)
		{
			Spawn(monster,true);
		}
		for(var i = 0; i < 15; i++)
		{
			Spawn(health,false);
		}
		for(var i = 0; i < 30; i++)
		{
			Spawn(ammo, false);
		}
		while(true)
		{
			yield return new WaitForSeconds(10 + UnityEngine.Random.value * 40);
			Spawn(monster, true);
			if(UnityEngine.Random.value < 0.1f)
			{
				Spawn(health, false);
			}
			if(UnityEngine.Random.value < 0.1f)
			{
				Spawn(ammo, false);
			}
		}
		
			
		
	}
	
	void Spawn(Transform prefab, bool grounded)
	{
		Vector3 position = Vector3.zero;
		float height = 0f;
		do
		{
			position = new Vector3(UnityEngine.Random.value * 500, 0, UnityEngine.Random.value * 500);
		} while((height = Terrain.activeTerrain.SampleHeight(position)) > 13);
		
		if(grounded)
		{
			position.y = height;
		}
		else
		{
			position.y = 200;
		}
		var newObj = Instantiate(prefab, position, Quaternion.Euler(0, UnityEngine.Random.Range(0,359),0)) as Transform;
		newObj.SendMessage("Spawned", SendMessageOptions.DontRequireReceiver);
		
	}
	
}
