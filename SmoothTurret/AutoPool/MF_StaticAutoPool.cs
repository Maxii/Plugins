using UnityEngine;
using System.Collections;

namespace AP_enum {
	public enum EmptyBehavior { Grow, Fail, ReuseOldest }
	public enum MaxEmptyBehavior { Fail, ReuseOldest }
}

[HelpURL("http://mobfarmgames.weebly.com/mf_staticobjectpool.html")]
public class MF_AutoPool {

	static AP_Manager opmScript;

	// may be be called early and won't create a spawn, but will create a pool reference and return true if the reference was created or already exsists.
	// use if you'd like to link pool references before the first spawn of a particular pool. (probably not necessary except for the most demanding of scenes.)
	// Additionaly, can be used to dynamically create pools at runtime.
	public static bool InitializeSpawn ( GameObject prefab ) { 
		return InitializeSpawn ( prefab, 0f, 0 );
	}
	// parameters assigned can be used to create pools at runtime
	// if addPool is < 1, it will be used to increase the exsisting pool by a percentage. Otherwise it will round to the nearest integer and increase by that ammount
	// minPool is the min object that must be in that pool. If the current pool + addPool < minPool, minPool will be used
	public static bool InitializeSpawn ( GameObject prefab, float addPool, int minPool ) { 
		return InitializeSpawn( prefab, addPool, minPool, AP_enum.EmptyBehavior.Grow, AP_enum.MaxEmptyBehavior.Fail, false ); 
	}
	public static bool InitializeSpawn ( GameObject prefab, float addPool, int minPool, AP_enum.EmptyBehavior emptyBehavior, AP_enum.MaxEmptyBehavior maxEmptyBehavior ) { 
		return InitializeSpawn( prefab, addPool, minPool, emptyBehavior, maxEmptyBehavior, true ); 
	}
	static bool InitializeSpawn ( GameObject prefab, float addPool, int minPool, AP_enum.EmptyBehavior emptyBehavior, AP_enum.MaxEmptyBehavior maxEmptyBehavior, bool modBehavior ) { 
		if ( prefab == null ) { return false; } // object wasn't defined

		if ( opmScript == null ) { // object pool manager script not located yet
			opmScript = Object.FindObjectOfType<AP_Manager>(); // find it in the scene
			if ( opmScript == null ) { Debug.Log( "No Object Pool Manager found in scene." ); return false; } // didn't find an object pool manager
		}
		// found an object pool manager
		return opmScript.InitializeSpawn( prefab, addPool, minPool, emptyBehavior, maxEmptyBehavior, modBehavior ); 
	}

	// use to create a spawn of the obj prefab. returns the spawned object
	public static GameObject Spawn ( GameObject prefab ) { // spawns at the position and rotation of the pool
		return Spawn ( prefab, null, Vector3.zero, Quaternion.identity, false );
	}
	public static GameObject Spawn ( GameObject prefab, int? child ) { // child allows a single object to hold multiple versions of objects, and only activate a specific child. null = don't use children
		return Spawn ( prefab, child, Vector3.zero, Quaternion.identity, false );
	}
	public static GameObject Spawn ( GameObject prefab, Vector3 pos, Quaternion rot ) { // specify a specific position and rotation
		return Spawn ( prefab, null, pos, rot, true );
	}
	public static GameObject Spawn ( GameObject prefab, int? child, Vector3 pos, Quaternion rot ) {
		return Spawn ( prefab, child, pos, rot, true );
	}
	static GameObject Spawn ( GameObject prefab, int? child, Vector3 pos, Quaternion rot, bool usePosRot ) {
		FindOPM();
		if ( opmScript == null ) { // didn't find an object pool manager
			return null;
		} else { // found an object pool manager
			return opmScript.Spawn( prefab, child, pos, rot, usePosRot );
		} 
	}

	public static bool Despawn ( GameObject obj ) {
		if ( obj == null ) { return false; }
		return Despawn( obj.GetComponent<AP_Reference>(), -1f );
	}
	public static bool Despawn ( GameObject obj, float time ) {
		if ( obj == null ) { return false; }
		return Despawn( obj.GetComponent<AP_Reference>(), time );
	}
	public static bool Despawn ( AP_Reference script ) {
		return Despawn( script, -1f );
	} 
	public static bool Despawn ( AP_Reference script, float time ) {
		if ( script == null ) { return false; }
		return script.Despawn( time );
	}

	public static int GetActiveCount ( GameObject obj ) {
		FindOPM();
		if ( opmScript == null ) { // didn't find an object pool manager
			return 0;
		} else { 
			return opmScript.GetActiveCount( obj );
		}
	}

	public static int GetAvailableCount ( GameObject obj ) {
		FindOPM();
		if ( opmScript == null ) { // didn't find an object pool manager
			return 0;
		} else { 
			return opmScript.GetAvailableCount( obj );
		}
	}

	public static bool DespawnPool ( GameObject obj ) {
		FindOPM();
		if ( opmScript == null ) { // didn't find an object pool manager
			return false;
		} else { 
			return opmScript.DespawnPool( obj );
		}
	}

	public static bool DespawnAll () {
		bool result = false;
		FindOPM();
		if ( opmScript == null ) { // didn't find an object pool manager
			return false;
		} else { 
			result = opmScript.RemoveAll();
			if ( result == true ) { opmScript.poolRef.Clear(); }
			return result;
		}
	}

	public static bool RemovePool ( GameObject obj ) {
		bool result = false;
		FindOPM();
		if ( opmScript == null ) { // didn't find an object pool manager
			return false;
		} else { 
			result = opmScript.RemovePool( obj );
			if ( result == true ) { opmScript.poolRef.Remove( obj ); }
			return result;
		}
	}

	public static bool RemoveAll () {
		FindOPM();
		if ( opmScript == null ) { // didn't find an object pool manager
			return false;
		} else { 
			return opmScript.RemoveAll();
		}
	}

	static void FindOPM () {
		if ( opmScript == null ) { // object pool manager script not located yet
			opmScript = Object.FindObjectOfType<AP_Manager>(); // find it in the scene
		}
	}

}
