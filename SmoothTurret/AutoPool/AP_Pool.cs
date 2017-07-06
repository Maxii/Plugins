using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[HelpURL("http://mobfarmgames.weebly.com/mf_b_objectpool.html")]
public class AP_Pool : MonoBehaviour { 

	public PoolBlock poolBlock;

	[HideInInspector] public Stack<PoolItem> pool;
	[HideInInspector] public List<PoolItem> masterPool; // only used when using EmptyBehavior.ReuseOldest

	int addedObjects;
	int failedSpawns;
	int reusedObjects;
	int peakObjects;
	int origSize;
	int initSize;
	int dynamicSize;
	bool loaded;

	[System.Serializable]
	public class PoolBlock {
		[Tooltip("Initial number of object in the pool.")]
		public int size = 32;
		[Tooltip("Behavior when an object is requested and the pool is empty.\n\n" +
			"Grow - Will add a new object to the pool, \nlimited by Max Size.\n\n" +
			"Fail - No object will be spawned.\n\n" + 
			"Reuse Oldest - Will reuse the oldest active object." )] // becomes slower than grow for large pools, but faster with small pools
		public AP_enum.EmptyBehavior emptyBehavior;
		[Tooltip("When using Grow behaviour, this is the absolute max size the pool can grow to.")]
		public int maxSize = 64; // absolut limit on pool size, used with EmptyBehavior Grow mode
		[Tooltip("Behavior when ann object is requested and the pool is empty, and the max size of the pool has been reached.\n\n" +
			"Fail - No object will be spawned.\n\n" +
			"Reuse Oldest - Will reuse the oldest active object.")]
		public AP_enum.MaxEmptyBehavior maxEmptyBehavior; // mode when pool is at the max size
		[Tooltip("Object that this pool contains.")]
		public GameObject prefab;
		[Tooltip("When the scene is stopped, creates a report showing pool usage:\n\n" +
			"Start Size - Size of the pool when the scene started.\n\n" +
			"End Size - Size of the pool when the scene ended.\n\n" +
			"Added Objects - Number of objects added to the pool beyond the Start Size.\n\n" +
			"Failed Spawns - Number of spawns failed due to no objects available in the pool.\n\n" +
			"Reused Objects - Number of objects reused before they were added back to the pool.\n\n" +
			"Most Objects Active - The most pool objects ever active at the same time.")]
		public bool printLogOnQuit;

		public PoolBlock ( int size, AP_enum.EmptyBehavior emptyBehavior, int maxSize, AP_enum.MaxEmptyBehavior maxEmptyBehavior, GameObject prefab, bool printLogOnQuit ) {
			this.size = size;
			this.emptyBehavior = emptyBehavior;
			this.maxSize = maxSize;
			this.maxEmptyBehavior = maxEmptyBehavior;
			this.prefab = prefab;
			this.printLogOnQuit = printLogOnQuit;
		}
	}

	[System.Serializable]
	public class PoolItem {
		public GameObject obj;
		public AP_Reference refScript;

		public PoolItem ( GameObject obj, AP_Reference refScript ) {
			this.obj = obj;
			this.refScript = refScript;
		}
	}

	void OnValidate () {
		if ( loaded == false ) { // only run during editor
			if ( poolBlock.maxSize <= poolBlock.size ) { poolBlock.maxSize = poolBlock.size * 2; } 
		}
	}

	void Awake () {
		loaded = true;

		// required to allow creation or modification of pools at runtime. (Timing of script creation and initialization can get wonkey)
		if ( poolBlock == null ) {
			poolBlock = new PoolBlock( 0, AP_enum.EmptyBehavior.Grow, 0, AP_enum.MaxEmptyBehavior.Fail, null, false );
		} else {
			poolBlock = new PoolBlock( poolBlock.size, poolBlock.emptyBehavior, poolBlock.maxSize, poolBlock.maxEmptyBehavior, poolBlock.prefab, poolBlock.printLogOnQuit );
		}
		pool = new Stack<PoolItem>();
		masterPool = new List<PoolItem>();

		origSize = Mathf.Max( 0, poolBlock.size); 
		poolBlock.size = 0;

		for ( int i=0; i < origSize; i++ ) {
			CreateObject( true );
		}
	}

	void Start () {
		Invoke( "StatInit", 0 ); // for logging after dynamic creation of pool objects from other scripts
	}

	void StatInit () { // for logging after dynamic creation of pool objects from other scripts
		initSize = poolBlock.size - origSize;
	}
		 
	public GameObject Spawn () { // use to call spawn directly from the pool, and also used by the "Spawn" button in the editor
		return Spawn( null, Vector3.zero, Quaternion.identity, false );
	}
	public GameObject Spawn ( int? child ) { // use to call spawn directly from the pool
		return Spawn( child, Vector3.zero, Quaternion.identity, false );
	}
	public GameObject Spawn ( Vector3 pos, Quaternion rot ) { // use to call spawn directly from the pool
		return Spawn( null, pos, rot, true );
	}
	public GameObject Spawn ( int? child, Vector3 pos, Quaternion rot ) { // use to call spawn directly from the pool
		return Spawn( child, pos, rot, true );
	}
	public GameObject Spawn ( int? child, Vector3 pos, Quaternion rot, bool usePosRot ) {
		GameObject obj = GetObject();
		if ( obj == null ) { return null; } // early out

		obj.SetActive(false); // reset item in case object is being reused, has no effect if object is already disabled
		obj.transform.parent = null;
		obj.transform.position = usePosRot ? pos : transform.position;
		obj.transform.rotation = usePosRot ? rot : transform.rotation;
	
		obj.SetActive(true);

		if ( child != null && child < obj.transform.childCount ) { // activate a specific child
			obj.transform.GetChild( (int)child ).gameObject.SetActive(true); 
		}

		if ( peakObjects < poolBlock.size - pool.Count ) { peakObjects = poolBlock.size - pool.Count; } // for logging
		return obj;
	}

	public void Despawn ( GameObject obj, AP_Reference oprScript ) { // return an object back to this pool
		if ( obj.transform.parent == transform ) { return; } // already in pool
		obj.SetActive(false);
		obj.transform.parent = transform;
		obj.transform.localPosition = Vector3.zero;
		obj.transform.localRotation = Quaternion.identity;
		oprScript.CancelInvoke();
		pool.Push( new PoolItem( obj, oprScript ) );
	}

	public GameObject GetObject () { // get object from pool, creating one if necessary and if settings allow
		GameObject result = null;
		if ( pool.Count == 0 ) {
			if ( poolBlock.emptyBehavior == AP_enum.EmptyBehavior.Fail ) { failedSpawns++; return null; }

			if ( poolBlock.emptyBehavior == AP_enum.EmptyBehavior.ReuseOldest ) {
				result = FindOldest();
				if ( result != null ) { reusedObjects++; }
			}

			if ( poolBlock.emptyBehavior == AP_enum.EmptyBehavior.Grow ) {
				if ( poolBlock.size >= poolBlock.maxSize ) {
					if ( poolBlock.maxEmptyBehavior == AP_enum.MaxEmptyBehavior.Fail ) { failedSpawns++; return null; }
					if ( poolBlock.maxEmptyBehavior == AP_enum.MaxEmptyBehavior.ReuseOldest ) {
						result = FindOldest();
						if ( result != null ) { reusedObjects++; }
					}
				} else {
					addedObjects++;
					return CreateObject();
				}
			}
		} else {
			pool.Peek().refScript.timeSpawned = Time.time;
			return pool.Pop().obj;
		}
		return result;
	}

	GameObject FindOldest () { // will also set timeSpawned for returned object
		GameObject result = null;
		int oldestIndex = 0;
		float oldestTime = Mathf.Infinity;
		if ( masterPool.Count > 0 ) {
			for ( int i = 0; i < masterPool.Count; i++ ) {
				if ( masterPool[i] == null || masterPool[i].obj == null ) { continue; } // make sure object still exsists
				if ( masterPool[i].refScript.timeSpawned < oldestTime ) { 
					oldestTime = masterPool[i].refScript.timeSpawned;
					result = masterPool[i].obj;
					oldestIndex = i;
				}
			}
			masterPool[ oldestIndex ].refScript.timeSpawned = Time.time;
		}
		return result;
	}

	public GameObject CreateObject () {
		return CreateObject ( false );
	}
	public GameObject CreateObject ( bool createInPool ) { // true when creating an item in the pool without spawing it
		GameObject obj = null;
		if ( poolBlock.prefab ) {
			obj = (GameObject) Instantiate( poolBlock.prefab, transform.position, transform.rotation );
			AP_Reference oprScript = obj.GetComponent<AP_Reference>();
			if ( oprScript == null ) { oprScript = obj.AddComponent<AP_Reference>(); }
			oprScript.poolScript = this;
			oprScript.timeSpawned = Time.time;
			masterPool.Add( new PoolItem( obj, oprScript ) );

			if ( createInPool == true ) {
				pool.Push( new PoolItem( obj, oprScript ) );
				obj.SetActive(false);
				obj.transform.parent = transform;
			}
			poolBlock.size++;
		}
		return obj;
	}

	public int GetActiveCount () {
		return poolBlock.size - pool.Count;
	}

	public int GetAvailableCount () {
		return pool.Count;
	}

	void OnApplicationQuit () { 
		if ( poolBlock.printLogOnQuit == true ) {
			PrintLog();
		}
	}

	public void PrintLog () {
		Debug.Log( transform.name + ":       Start Size: " + origSize + "    Init Added: " + initSize + "    Grow Objects: " + addedObjects + "    End Size: " + poolBlock.size + "\n" +
			"    Failed Spawns: " + failedSpawns + "    Reused Objects: " + reusedObjects + "     Most objects active at once: " + peakObjects );
	}

}
