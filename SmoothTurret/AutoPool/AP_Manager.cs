using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[HelpURL("http://mobfarmgames.weebly.com/mf_b_objectpoolmanager.html")]
public class AP_Manager : MonoBehaviour {

	public bool allowCreate = true;
	public bool allowModify = true;

	[Tooltip("When the scene is stopped, creates a report showing pool usage:\n\n" +
		"Start Size - Size of pool when beginning the scene.\n\n" +
		"Init Added - Number of objects added by InitializeSpawn() at runtime.\n\n" +
		"Grow Objects - Number of objects added with EMptyBehavior.Grow.\n\n" +
		"End Size - Total objects of this pool, active and inactive, at the time of the log report.\n\n" +
		"Failed Spawns - Number of Spawn() requests that didn't return a spawn.\n\n" +
		"Reused Objects - Number of times an object was reused before despawning normally.\n\n" +
		"Most Objects Active - The most items for this pool active at once.")]
	public bool printAllLogsOnQuit;

	[HideInInspector] public Dictionary<GameObject, AP_Pool> poolRef;

	void Awake () {
		CheckDict();
	}

	void CheckDict() {
		if ( poolRef == null ) { // dictionary hasn't been created yet
			poolRef = new Dictionary<GameObject, AP_Pool>();
		}
	}

	public bool InitializeSpawn ( GameObject obj, float addPool, int minPool, AP_enum.EmptyBehavior emptyBehavior, AP_enum.MaxEmptyBehavior maxEmptyBehavior, bool modBehavior ) { 
		if ( obj == null ) { return false; }
		CheckDict();
		bool result = false;
		bool tempModify = false;

		if ( poolRef.ContainsKey( obj ) == true && poolRef[obj] == null ) { // check for broken reference
			poolRef.Remove( obj ); // remove it
		}
		if ( poolRef.ContainsKey( obj ) == true ) {
				result = true; // already have refrence
		} else {
			if ( MakePoolRef( obj ) == null ) { // ref not found
				if ( allowCreate == true ) {
					CreatePool( obj, 0, 0, emptyBehavior, maxEmptyBehavior );
					tempModify = true; // may modify a newly created pool
					result = true;
				} else {
					result = false;
				}
			} else {
				result = true; // ref was created
			}
		}

		if ( result == true ) { // hava a valid pool ref
			if ( allowModify == true || tempModify == true ) { // may modify a newly created pool
				if ( addPool > 0 || minPool > 0 ) {
					int size = poolRef[obj].poolBlock.size;
					int l1 = 0; int l2 = 0;
					if ( addPool >= 0 ) { // not negative
						if ( addPool < 1 ) { // is a percentage
							l2 = Mathf.RoundToInt( size * addPool );
						} else { // not a percentage
							l1 = Mathf.RoundToInt( addPool );
						}
					}
					int loop = 0;
					int a = size == 0 ? 0 : Mathf.Max( l1, l2 );
					if ( size < minPool ) { loop = minPool - size; }
					loop += a;
					for ( int i=0; i < loop; i++ ) {
						poolRef[obj].CreateObject( true );
					}
					poolRef[obj].poolBlock.maxSize = poolRef[obj].poolBlock.size * 2;
					if ( modBehavior == true ) {
						poolRef[obj].poolBlock.emptyBehavior = emptyBehavior;
						poolRef[obj].poolBlock.maxEmptyBehavior = maxEmptyBehavior;
					}
				}
			}
		}

		return result;
	}

	public GameObject Spawn ( GameObject obj, int? child, Vector3 pos, Quaternion rot, bool usePosRot ) {
		if ( obj == null ) { return null; } // object wasn't defined
		CheckDict();

		if ( poolRef.ContainsKey( obj ) == true ) { // reference already created
			if ( poolRef[obj] != null ) { // make sure pool still exsists
				return poolRef[obj].Spawn( child, pos, rot, usePosRot ); // create spawn
			} else { // pool no longer exsists
				poolRef.Remove( obj ); // remove reference
				return null;
			}
		} else { // ref not yet created
			AP_Pool childScript = MakePoolRef ( obj ); // create ref
			if ( childScript == null ) { // ref not found
				return null;
			} else {
				return childScript.Spawn( child, pos, rot, usePosRot ); // create spawn
			}
		}
	}

	AP_Pool MakePoolRef ( GameObject obj ) { // attempt to create and return script reference
		for ( int i=0; i < transform.childCount; i++ ) {
			AP_Pool childScript = transform.GetChild(i).GetComponent<AP_Pool>();
			if ( childScript && obj == childScript.poolBlock.prefab ) {
				poolRef.Add( obj, childScript );
				return childScript;
			}
		}
//		Debug.Log( obj.name + ": Tried to reference object pool, but no matching pool was found." );
		return null;
	}

	public int GetActiveCount ( GameObject prefab ) {
		if ( prefab == null ) { return 0; } // object wasn't defined
		AP_Pool childScript = null;
		if ( poolRef.ContainsKey( prefab ) == true ) { // reference already created
			childScript = poolRef[prefab];
		} else { // ref not yet created
			childScript = MakePoolRef ( prefab ); // create ref
		}
		if ( childScript == null ) { // pool not found
			return 0;
		} else {
			return childScript.poolBlock.size - childScript.pool.Count;
		}
	}

	public int GetAvailableCount ( GameObject prefab ) {
		if ( prefab == null ) { return 0; } // object wasn't defined
		AP_Pool childScript = null;
		if ( poolRef.ContainsKey( prefab ) == true ) { // reference already created
			childScript = poolRef[prefab];
		} else { // ref not yet created
			childScript = MakePoolRef ( prefab ); // create ref
		}
		if ( childScript == null ) { // pool not found
			return 0;
		} else {
			return childScript.pool.Count;
		}
	}

	public bool RemoveAll () {
		bool result = true;
		foreach ( GameObject obj in poolRef.Keys ) {
			if ( RemovePool( obj ) == false ) { result = false; }
		}
		return result;
	}

	public bool DespawnAll () {
		bool result = true;
		foreach ( GameObject obj in poolRef.Keys ) {
			if ( DespawnPool( obj ) == false ) { result = false; }
		}
		return result;
	}

	public bool RemovePool ( GameObject prefab ) {
		if ( prefab == null ) { return false; } // object wasn't defined
		bool result = false;
		AP_Pool childScript = null;
		if ( poolRef.ContainsKey( prefab ) == true ) { // reference already created
			childScript = poolRef[prefab];
		} else { // ref not yet created
			childScript = MakePoolRef ( prefab ); // create ref
		}
		if ( childScript == null ) { // pool not found
			return false;
		} else {
			result = DespawnPool( prefab );
			Destroy( childScript.gameObject );
			poolRef.Remove( prefab );
			return result;
		}
	}
		
	public bool DespawnPool ( GameObject prefab ) {
		if ( prefab == null ) { return false; } // object wasn't defined
		AP_Pool childScript = null;
		if ( poolRef.ContainsKey( prefab ) == true ) { // reference already created
			childScript = poolRef[prefab];
		} else { // ref not yet created
			childScript = MakePoolRef ( prefab ); // create ref
		}
		if ( childScript == null ) { // pool not found
			return false;
		} else {
			for ( int i=0; i < childScript.masterPool.Count; i++ ) {
				childScript.Despawn( childScript.masterPool[i].obj, childScript.masterPool[i].refScript ) ;
			}
			return true;
		}
	}

	public void CreatePool () {
		CreatePool ( null, 32, 64, AP_enum.EmptyBehavior.Grow, AP_enum.MaxEmptyBehavior.Fail );
	}
	public void CreatePool ( GameObject prefab, int size, int maxSize, AP_enum.EmptyBehavior emptyBehavior, AP_enum.MaxEmptyBehavior maxEmptyBehavior ) {
		GameObject obj = new GameObject("Object Pool");
		obj.transform.parent = transform;
		obj.transform.localPosition = Vector3.zero;
		obj.transform.localRotation = Quaternion.identity;
		AP_Pool script = obj.AddComponent<AP_Pool>();
		if ( Application.isPlaying == true ) {
			obj.name = prefab.name;
			script.poolBlock.size = size;
			script.poolBlock.maxSize = maxSize;
			script.poolBlock.emptyBehavior = emptyBehavior;
			script.poolBlock.maxEmptyBehavior = maxEmptyBehavior;
			script.poolBlock.prefab = prefab;
			if ( prefab ) { MakePoolRef( prefab ); }
		}
	}

	void OnApplicationQuit () { 
		if ( printAllLogsOnQuit == true ) {
			PrintAllLogs();
		}
	}

	public void PrintAllLogs () {
		foreach ( AP_Pool script in poolRef.Values ) {
			script.PrintLog();
		}
	}

}
