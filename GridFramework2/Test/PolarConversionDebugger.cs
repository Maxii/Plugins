using UnityEngine;
using GridFramework.Grids;
using GridFramework.Extensions.Conversion;

public class PolarConversionDebugger : MonoBehaviour {
	public PolarGrid _grid;
	public bool _rotateAroundGrid;
	public float _angle;

	public bool _toggleDebug;

	private Transform cachedTransform;
	private Transform _transform {
		get {
			if (!cachedTransform)
				cachedTransform = transform;
			return cachedTransform;
		}
	}

	void OnGUI () {
		if (!_grid){
			Debug.LogWarning ("No grid assigned, cannot debug");
			return;
		}
		GUI.TextArea(
			new Rect (10, 10, 600, 150),
			"world position:\t" + transform.position.x +" / "+ transform.position.y +" / "+ transform.position.z + "\n"
			+"grid position:\t" +
			_grid.WorldToGrid(transform.position).x +" / "+ _grid.WorldToGrid(transform.position).y +" / "+ _grid.WorldToGrid(transform.position).z +"\n"
			+"polar position:\t" + _grid.WorldToPolar(transform.position).x +" / "+ _grid.WorldToPolar(transform.position).y +" / "+ _grid.WorldToPolar(transform.position).z +"\n\n"
			+"angle :\t" + _grid.World2Rad(transform.position) +" = "+ (_grid.World2Rad(transform.position) / Mathf.PI) +"\u03c0 = " + _grid.World2Deg(transform.position) + "\u00b0\n"
			+"sector: \t" + _grid.World2Sector(transform.position) +"\n\n"
			+"sector converted from angle:\t" + _grid.Rad2Sector(_grid.World2Rad(transform.position))+"\n"
			+"angle converted from Sector:\t" + _grid.Sector2Rad(_grid.World2Sector(transform.position)) +" = "+ _grid.Sector2Deg(_grid.World2Sector(transform.position)) +"\n"
		);
		if(_rotateAroundGrid)
			transform.rotation = _grid.World2Rotation(transform.position);
	}

	void OnDrawGizmos () {
		if (!_toggleDebug)
			return;
		_transform.rotation = _grid.Rad2Rotation(_angle);
	}
}
