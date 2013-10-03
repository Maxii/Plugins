using UnityEngine;
using System.Collections;

public class HexConversionDebugger : MonoBehaviour {

	public enum CoordinateSystem {world, herringOdd, rhombic};

	public GFHexGrid grid;
	public CoordinateSystem coordinateSystem = CoordinateSystem.herringOdd;
	public float angle = 0.0f;
	public bool toggleDebug = false;

	private Transform cachedTransform;
	private Transform _transform {
		get {
			if (!cachedTransform)
				cachedTransform = transform;
			return cachedTransform;
		}
	}

	void OnDrawGizmos () {
        if (!toggleDebug)
            return;

		if (!grid) {
			Debug.LogWarning ("No grid assigned, cannot debug");
			return;
		}
		Vector4 cubic = grid.WorldToCubic (_transform.position);

		if (coordinateSystem == CoordinateSystem.herringOdd) {
			Vector3 herring = grid.WorldToHerringOdd (_transform.position);
			Debug.Log ("Herring: " + herring + "; Cubic: " + cubic + "; disrepancy: " + (cubic - grid.HerringOddToCubic(herring)) + " with cubic " + grid.HerringOddToCubic(herring));
		} else if (coordinateSystem == CoordinateSystem.rhombic) {
			Vector3 rhombic = grid.WorldToRhombic (_transform.position);
			Debug.Log ("Rhombic: " + rhombic + "; Cubic: " + cubic + "; disrepancy: " + (cubic - grid.RhombicToCubic (rhombic)) + " with cubic " + grid.RhombicToCubic (rhombic));
		} else {
			Debug.Log ("Cubic: " + cubic + "; world: " + _transform.position + "; discrepancy = " + (_transform.position - grid.CubicToWorld (cubic)));
		}
	}
}