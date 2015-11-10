using UnityEngine;
using System.Collections;

namespace MFnum {
    public enum ArcType { Low, High }
}

public class MFmath {

    // proper modulo, NOT ramainder operator (%)
    public static int Mod(int a, int b) {
        return (((a) % b) + b) % b;
    }

    // Determine the signed angle between two vectors around an axis
    public static float AngleSigned(Vector3 v1, Vector3 v2, Vector3 axis) {
        return Mathf.Atan2(
            Vector3.Dot(axis, Vector3.Cross(v1, v2)),
            Vector3.Dot(v1, v2)) * Mathf.Rad2Deg;
    }
}

public class UtilityMF {

    // searches starting location object, then recursively up the tree for a matching component name
    public static Transform RecursiveParentSearch(string name, Transform location) {
        while (location != null) {
            if (location.GetComponent(name)) {
                return location;
            }
            else {
                location = location.parent;
            }
        }
        return null;
    }

    // searches starting location object, then recursively up the tree for a matching target
    public static Transform RecursiveParentTransformSearch(Transform target, Transform location) {
        while (location != null) {
            if (location == target) {
                return location;
            }
            else {
                location = location.parent;
            }
        }
        return null;
    }

    // find largest dimention of a GameObject's collider
    public static float FindColliderBoundsSize(GameObject thisObject) {
        return FindColliderBoundsSize(thisObject, false);
    }
    public static float FindColliderBoundsSize(GameObject thisObject, bool checkChildren) {
        float size = 0;
        Bounds bounds = default(Bounds);    // Bounds bounds;   My addition - caused by VS.Net3.5 compiler different from Mono.Net3.5 compiler
        bool foundCollider = false;

        if (thisObject.GetComponent<Collider>()) { // check object
            bounds = thisObject.GetComponent<Collider>().bounds;
            foundCollider = true;
        }
        else if (checkChildren == true) { // no colldier found on object
            // check children for the first collider found
            for (int c = 0; c < thisObject.transform.childCount; c++) {
                if (thisObject.transform.GetChild(c).GetComponent<Collider>()) { // found a collider
                    bounds = thisObject.transform.GetChild(c).GetComponent<Collider>().bounds;
                    foundCollider = true;
                    break;
                }
            }
        }
        if (foundCollider) { // found a collider
            size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        }
        return size;
    }
}
