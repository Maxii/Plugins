using UnityEngine;
using System.Collections;

public class MF_B_Spawner : MonoBehaviour {

	[Tooltip("Number of units spawned when the scene starts.")]
	public int initialSpawnAmount;
	[Tooltip("Number of units spawned at every spawn interval.")]
	public int unitsPerTimedSpawn;
	[Tooltip("Time between spawns.")]
	public float spawnInterval;
	[Tooltip("Maximum number of spawns. -1 = unlimited.")]
	public int maxTimedSpawns = -1; // -1 = unlimited
	[Tooltip("Units spawns at a random location within radius.")]
	public float spawnRandomRadius;
	[Tooltip("Keeps the spawned unit at the same height as the spawn point, no matter the Spawn Random Radius.")]
	public bool sameHeight;
	[Tooltip("Group of spawn points each unit can spawn at. Randomly chosen.")]
	public Transform spawnPointsGroup;
	[Tooltip("Group of waypoints given to each object's navigation script. Randomly chosen if more than one group.")]
	public Transform[] waypointGroups;
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
		[Tooltip("Prefab to be created.")]
		public GameObject unit;
		[Tooltip("Change the faction of the prefab.")]
		public bool overrideFaction;
		[Tooltip("Faction to change the prefab to. Note, that this won't change what the prefab's scanners may target.")]
		public MFnum.FactionType faction;
		[Tooltip("Optional spawn point to override spawn points.")]
		public Transform spawn;
		[Tooltip("Optional waypoint group to override waypoint groups.")]
		public Transform wpt;
		[Tooltip("Spawned object begins with a random waypoint set to true.")]
		public bool randomWpt;
	}

	void OnValidate () {
		spawnsPoints = UtilityMF.BuildArrayFromChildren( spawnPointsGroup );
	}
	
	void Start () {
		Spawn( initialSpawnAmount );
	}
	
	void Update () {
		if ( spawnNumber < maxTimedSpawns || maxTimedSpawns == -1 ) {
			if ( Time.time >= lastSpawnTime + spawnInterval ) {
				lastSpawnTime = Time.time;
				Spawn( unitsPerTimedSpawn );
				spawnNumber++;
			}
		}
	}

	void Spawn ( int num ) {
		if ( spawnsPoints.Length > 0 ) { // at least 1 spawn point defined
			// find total chance number
			float _spawnRollMax = 0;
			for ( int t=0; t < spawnTypes.Length; t++ ) {
				_spawnRollMax += spawnTypes[t].chance;
			}
			for (int s=0; s < num; s++) { // for every spawn requested
				float _spawnRoll = Random.Range(0f, _spawnRollMax);
				SpawnType _spawnEntry = null;
				// find which unit spawn roll picked
				float _count = 0;
				for ( int t=0; t < spawnTypes.Length; t++ ) {
					if ( _spawnRoll > _count && _spawnRoll < _count + spawnTypes[t].chance) {
						_spawnEntry = spawnTypes[t];
						break;
				    }
					_count += spawnTypes[t].chance;
				}

				Vector3 _locRand = Random.insideUnitSphere * spawnRandomRadius;
				// if spawn point provided, use it. Else pick random from default list
				Vector3 _spawnCenter;
				if ( _spawnEntry.spawn ) {
					_spawnCenter = _spawnEntry.spawn.position;
				} else {
					_spawnCenter = spawnsPoints[ Random.Range(0, spawnsPoints.Length) ].position;
				}
				Vector3 _spawnLoc = _spawnCenter + _locRand;
				if ( sameHeight == true ) {
					_spawnLoc = new Vector3( _spawnLoc.x, _spawnCenter.y, _spawnLoc.z );
				}
				// if waypoint group provided, use it. Else pick random from default list
				Transform _wptGroup;
				if ( _spawnEntry.wpt ) {
					_wptGroup = _spawnEntry.wpt;
				} else {
					_wptGroup = waypointGroups[ Random.Range(0, waypointGroups.Length) ];
				}
	
				GameObject _unit = (GameObject)Instantiate( _spawnEntry.unit, _spawnLoc, Quaternion.identity );
				float _angle = MFmath.AngleSigned( Vector3.forward, _spawnLoc - _spawnCenter, Vector3.up );
				_unit.transform.rotation = Quaternion.Euler( new Vector3( 0f, _angle, 0f ) );
				if ( _spawnEntry.overrideFaction == true && _spawnEntry.faction != MFnum.FactionType.None ) {
					_unit.tag = _spawnEntry.faction.ToString();
					MF_AbstractStatus _unitStatusScript = _unit.GetComponent<MF_AbstractStatus>();
					if ( _unitStatusScript && _unitStatusScript.layerColliderLocation ) {
						_unitStatusScript.layerColliderLocation.gameObject.layer = LayerMask.NameToLayer( _spawnEntry.faction.ToString() );
					}
				}
				MF_AbstractNavigation _unitNavScript = _unit.GetComponent<MF_AbstractNavigation>();
				if (_unitNavScript) {
					_unitNavScript.waypointGroup = _wptGroup;
					_unitNavScript.randomWpt = _spawnEntry.randomWpt;
				}
			}	
		}
	}
}
