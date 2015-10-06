using UnityEngine;
using System.Collections;

public class MFmath {

	// proper modulo, NOT ramainder operator (%)
	public static int Mod ( int a, int n) {
		return a - (n * Mathf.FloorToInt(a / n));
	}
	
	// Determine the signed angle between two vectors around an axis
	public static float AngleSigned( Vector3 v1, Vector3 v2, Vector3 axis ) {
		return Mathf.Atan2(
			Vector3.Dot(axis, Vector3.Cross(v1, v2)),
			Vector3.Dot(v1, v2)) * Mathf.Rad2Deg;
	}
}
