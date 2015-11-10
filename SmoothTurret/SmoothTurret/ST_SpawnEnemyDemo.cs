using UnityEngine;
using System.Collections;

public class ST_SpawnEnemyDemo : MonoBehaviour {

	public float spawnInterval;
	public int enemiesPerSpawn;
	public float spawnRandomRadius;
	public GameObject[] spawnEnemies;
	
	public Transform[] spawns;
	public GameObject[] waypointGroups;
	
	float lastSpawnTime;
	
	void Start () {
		lastSpawnTime = -spawnInterval;
	}
	
	void Update () {
		if ( Time.time >= lastSpawnTime + spawnInterval ) {
			lastSpawnTime = Time.time;
			for (int e=0; e < enemiesPerSpawn; e++) {
				Vector3 _locRand = Random.insideUnitSphere * spawnRandomRadius;
				int _spawnLocRoll = Random.Range(0, spawns.Length);
				Vector3 _spawnLoc = spawns[ _spawnLocRoll ].position + _locRand;	
				GameObject _spawnObj = spawnEnemies[ Random.Range(0, spawnEnemies.Length) ];
				
				GameObject _target = (GameObject)Instantiate( _spawnObj, _spawnLoc, Quaternion.identity );
				MF_BasicNavigation _targetScript = _target.GetComponent<MF_BasicNavigation>();
				_targetScript.waypointGroup = waypointGroups[ Random.Range(0, waypointGroups.Length) ];
			}	
		}
	}
}
