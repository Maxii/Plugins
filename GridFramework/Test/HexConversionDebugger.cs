using UnityEngine;
using GridFramework.Grids;


public class HexConversionDebugger : MonoBehaviour {

	public enum CoordinateSystem {world, herringUp, herringDown, rhombic, rhombicDown};

	public HexGrid grid;
	public CoordinateSystem coordinateSystem = CoordinateSystem.herringUp;
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
		Vector4 cubic = grid.WorldToCubic(_transform.position);

		if (coordinateSystem == CoordinateSystem.herringUp) {
			Vector3 herring = grid.WorldToHerringUp(_transform.position);
			Debug.Log ("Herring: " + herring + "; Cubic: " + cubic + "; disrepancy: " + (cubic - grid.HerringUpToCubic(herring)) + " with cubic " + grid.HerringUpToCubic(herring));
		} else if (coordinateSystem == CoordinateSystem.herringDown) {
			Vector3 herring = grid.WorldToHerringDown(_transform.position);
			Debug.Log ("Herring: " + herring + "; Cubic: " + cubic + "; disrepancy: " + (cubic - grid.HerringDownToCubic(herring)) + " with cubic " + grid.HerringDownToCubic(herring));
		} else if (coordinateSystem == CoordinateSystem.rhombic) {
			Vector3 rhombic = grid.WorldToRhombicUp(_transform.position);
			Debug.Log ("Rhombic: " + rhombic + "; Cubic: " + cubic + "; disrepancy: " + (cubic - grid.RhombicUpToCubic (rhombic)) + " with cubic " + grid.RhombicUpToCubic (rhombic));
		} else if (coordinateSystem == CoordinateSystem.rhombicDown) {
			Vector3 rhombic = grid.WorldToRhombicDown(_transform.position);
			Debug.Log ("Rhombic: " + rhombic + "; Cubic: " + cubic + "; disrepancy: " + (cubic - grid.RhombicDownToCubic (rhombic)) + " with cubic " + grid.RhombicUpToCubic (rhombic));
		} else {
			Debug.Log ("Cubic: " + cubic + "; world: " + _transform.position + "; discrepancy = " + (_transform.position - grid.CubicToWorld (cubic)));
		}
	}
}
