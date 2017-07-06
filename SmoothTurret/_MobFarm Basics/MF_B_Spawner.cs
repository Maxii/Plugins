using UnityEngine;
using System.Collections;

[HelpURL("http://mobfarmgames.weebly.com/mf_b_spawner.html")]
public class MF_B_Spawner : MonoBehaviour {

	[Split1("Number of units spawned when the scene starts.")]
	public int beginAmount;
	[Split2("Number of units spawned at every spawn interval.")]
	public int unitsPerSpawn;
	[Split1("Time between spawns.")]
	public float interval;
	[Split2("Maximum number of timed spawn events. -1 = unlimited.")]
	public int maxEvents = -1; // -1 = unlimited
	[Tooltip("Group of spawn points each unit can spawn at. Randomly chosen.")]
	public Transform spawnPointsGroup;
	[Split1("Units spawns at a random location within radius.")]
	public float randomRadius;
	[Split2("Keeps the spawned unit at the same height as the spawn point, no matter the Spawn Random Radius.")]
	public bool sameHeight;

	[Tooltip("Group of waypoints given to each object's navigation script. Randomly chosen if more than one group.")]
	public Transform[] waypointGroups;
	[Tooltip("If Spawned object has a navigation script, it begins with Random Wpt set to true.")]
	public bool randomWpt;
	[Header("List of possible spawned units:")]
	[Tooltip("Chance to spawn is the listed chance out of the sum of all listed chances of the group.\n" +
		"Example: If 3 elements are listed with chances of [10, 25, 15] The first has a 20% chance to be picked. ( 10 / 50 )")]
	public SpawnType[] spawnTypes;

	Transform[] spawnsPoints;
	float lastSpawnTime;
	float spawnNumber;

	[System.Serializable]
	public class SpawnType {
		[Tooltip("Spawn chance. Based on the sum of all spawn chances.")]
		public float chance;
		[Tooltip("Prefab to be spawned.")]
		public GameObject prefab;
		public bool objectPool;
		public int addPool;
		public int minPool;
	}

	void OnValidate () {
		spawnsPoints = UtilityMF.BuildArrayFromChildren( spawnPointsGroup );
	}
	
	void Start () {
		for ( int i=0; i < spawnTypes.Length; i++ ){
			if ( spawnTypes[i].objectPool == true ) {
				MF_AutoPool.InitializeSpawn( spawnTypes[i].prefab, spawnTypes[i].addPool, spawnTypes[i].minPool );
			}
		}
		Spawn( beginAmount );
	}
	
	void Update () {
		if ( spawnNumber < maxEvents || maxEvents == -1 ) {
			if ( Time.time >= lastSpawnTime + interval ) {
				lastSpawnTime = Time.time;
				Spawn( unitsPerSpawn );
				spawnNumber++;
			}
		}
	}

	void Spawn ( int num ) {
		if ( spawnsPoints.Length > 0 ) { // at least 1 spawn point defined
			// find total chance number
			float spawnRollMax = 0;
			for ( int i=0; i < spawnTypes.Length; i++ ) {
				if ( spawnTypes[i].prefab ) {
					spawnRollMax += spawnTypes[i].chance;
				}
			}
			for (int i=0; i < num; i++) { // for every spawn requested
				float spawnRoll = Random.Range(0f, spawnRollMax);
				SpawnType spawnEntry = null;
				// find which unit spawn roll picked
				float count = 0;
				for ( int t=0; t < spawnTypes.Length; t++ ) {
					if ( spawnTypes[t].prefab ) {
						if ( spawnRoll > count && spawnRoll < count + spawnTypes[t].chance) {
							spawnEntry = spawnTypes[t];
							break;
					    }
						count += spawnTypes[t].chance;
					}
				}

				if ( spawnEntry != null && spawnEntry.prefab ) {
					int spawnIndex = Random.Range( 0, spawnsPoints.Length );
					Vector3 spawnCenter = spawnsPoints[ spawnIndex ].position;
					Vector3 spawnLoc = spawnCenter + ( Random.insideUnitSphere * randomRadius );
					if ( sameHeight == true ) {
						spawnLoc = new Vector3( spawnLoc.x, spawnCenter.y, spawnLoc.z );
					}

					GameObject unit = null;
					if ( spawnEntry.objectPool == true ) {
						unit = MF_AutoPool.Spawn( spawnEntry.prefab, spawnLoc, spawnsPoints[ spawnIndex ].rotation );
					} else {
						unit = (GameObject) Instantiate( spawnEntry.prefab, spawnLoc, spawnsPoints[ spawnIndex ].rotation );
					}

					if ( unit ) {
						MF_AbstractNavigation unitNavScript = unit.GetComponent<MF_AbstractNavigation>();
						if ( unitNavScript ) {
							unitNavScript.waypointGroup = waypointGroups[ Random.Range(0, waypointGroups.Length) ];
							unitNavScript.randomWpt = randomWpt;
						}
					}
				}
			}	
		}
	}
}
