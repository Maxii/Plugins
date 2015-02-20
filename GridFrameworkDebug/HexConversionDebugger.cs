using UnityEngine;
public class HexConversionDebugger : MonoBehaviour {

	public enum CoordinateSystem {world, herringUp, herringDown, rhombic, rhombicDown};

	public GFHexGrid grid;
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
			Vector3 herring = grid.WorldToHerringU(_transform.position);
			Debug.Log ("Herring: " + herring + "; Cubic: " + cubic + "; disrepancy: " + (cubic - grid.HerringUToCubic(herring)) + " with cubic " + grid.HerringUToCubic(herring));
		} else if (coordinateSystem == CoordinateSystem.herringDown) {
			Vector3 herring = grid.WorldToHerringD(_transform.position);
			Debug.Log ("Herring: " + herring + "; Cubic: " + cubic + "; disrepancy: " + (cubic - grid.HerringDToCubic(herring)) + " with cubic " + grid.HerringDToCubic(herring));
		} else if (coordinateSystem == CoordinateSystem.rhombic) {
			Vector3 rhombic = grid.WorldToRhombic(_transform.position);
			Debug.Log ("Rhombic: " + rhombic + "; Cubic: " + cubic + "; disrepancy: " + (cubic - grid.RhombicToCubic (rhombic)) + " with cubic " + grid.RhombicToCubic (rhombic));
		} else if (coordinateSystem == CoordinateSystem.rhombicDown) {
			Vector3 rhombic = grid.WorldToRhombicD(_transform.position);
			Debug.Log ("Rhombic: " + rhombic + "; Cubic: " + cubic + "; disrepancy: " + (cubic - grid.RhombicDToCubic (rhombic)) + " with cubic " + grid.RhombicToCubic (rhombic));
		} else {
			Debug.Log ("Cubic: " + cubic + "; world: " + _transform.position + "; discrepancy = " + (_transform.position - grid.CubicToWorld (cubic)));
		}
	}
}
