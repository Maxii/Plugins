using UnityEngine;
using System.Collections;

public class MFmath {

	// proper modulo, NOT ramainder operator (%)
	public static int Mod ( int a, int b) {
		return (((a) % b) + b) % b;
	}
	
	// Determine the signed angle between two vectors around an axis
	public static float AngleSigned( Vector3 v1, Vector3 v2, Vector3 axis ) {
		return Mathf.Atan2(
			Vector3.Dot(axis, Vector3.Cross(v1, v2)),
			Vector3.Dot(v1, v2)) * Mathf.Rad2Deg;
	}
}

public class UtilityMF {

	// searches starting location object, then recursively up the tree for a matching component name
	public static Transform RecursiveParentComponentSearch ( string name, Transform location ) {
		while ( location != null ) {
			if ( location.GetComponent(name) ) {
				return location;
			} else {
				location = location.parent;
			}
		}
		return null;
	}

	// searches starting location object, then recursively up the tree for a matching target. Mainly used to see if a location is within the hierarchy of target.
	public static bool RecursiveParentTransformSearch ( Transform target, Transform location ) {
		while ( location != null ) {
			if ( location == target ) {
				return true;
			} else {
				location = location.parent;
			}
		}
		return false;
	}

	// find largest dimention of a GameObject's collider
	public static float FindColliderBoundsSize ( Transform trans ) {
		return FindColliderBoundsSize( trans, false );
	}
	public static float FindColliderBoundsSize ( Transform trans, bool checkChildren ) {
		float size = 0;
		Bounds bounds = default(Bounds);
		bool foundCollider = false;

		if ( trans.GetComponent<Collider>() ) { // check object
			bounds = trans.GetComponent<Collider>().bounds;
			foundCollider = true;
		} else if ( checkChildren == true ) { // no colldier found on object
			// check children for the first collider found
			for  ( int c=0; c < trans.childCount; c++ ) {
				if ( trans.GetChild(c).GetComponent<Collider>() ) { // found a collider
					bounds = trans.GetChild(c).GetComponent<Collider>().bounds;
					foundCollider = true;
					break;
				}
			}

			if ( foundCollider == false ) { // still not found, check children's children
				for  ( int c=0; c < trans.childCount; c++ ) {
					for ( int l=0; l < trans.GetChild(c).childCount; l++ ) {
						if ( trans.GetChild(c).GetChild(l).GetComponent<Collider>() ) { // found a collider
							bounds = trans.GetChild(c).GetChild(l).GetComponent<Collider>().bounds;
							foundCollider = true;
							break;
						}
					}
					if ( foundCollider == true ) { break; }
				}
			}

		}
		if ( foundCollider ) { // found a collider
//			size = Mathf.Max( bounds.size.x, bounds.size.y, bounds.size.z );
			size = ( bounds.size.x + bounds.size.y + bounds.size.z ) * .33f; // average bound size
		}
		return size;
	}

	// Build an array from a given parent's children
	public static Transform[] BuildArrayFromChildren ( Transform trans ) {
		Transform[] bArray;
		if (trans) { // build array contents from children of trans
			int _childCount = trans.childCount;
			if ( _childCount > 0 ) { // found at least 1 child, use children
				bArray = new Transform[_childCount];
				for ( int i=0; i < _childCount; i++ ) {
					bArray[i] = trans.GetChild(i);
				}
			} else { // no children, use parent 
				bArray = new Transform[1];
				bArray[0] = trans;
			}
		} else {
			bArray = new Transform[0]; 
		}
		return bArray;
	}
}
