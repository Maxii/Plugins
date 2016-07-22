using UnityEngine;
using GridFramework.Grids;


public class GridDebugger : MonoBehaviour {
	public bool _toggleDebugging;
	public bool _printLogs = true;
	public Grid _theGrid;
	public enum GridFunction {WorldToGrid, GridToWorld};
	public GridFunction _debuggedFunction = GridFunction.WorldToGrid;
	public Color _debugColor = Color.red;
	//public int[] index = new int[3];
	
	private Transform cachedTransform;
	Transform _transform{get{if(!cachedTransform) cachedTransform = transform; return cachedTransform;}}
	
	// Update is called once per frame
	protected void OnDrawGizmos() {
		if(!_theGrid || ! _toggleDebugging)
			return;
				
		Gizmos.color = _debugColor;
		if ((int)_debuggedFunction == 0) {
			DebugWorldToGrid ();
		} else if ((int)_debuggedFunction == 1) {
			DebugGridToWorld ();
		}
	}
	
	
	protected void DebugWorldToGrid(){
		/* /1* _theGrid.WorldToGrid(_transform.position); *1/ */
		/* if(_printLogs) */
		/* 	Debug.Log(_theGrid.WorldToGrid(_transform.position)); */
	}
	
	protected void DebugGridToWorld(){
		/* Vector3 converted = _theGrid.GridToWorld(_theGrid.WorldToGrid(_transform.position)); */
		/* if(_printLogs) */
		/* 	Debug.Log(Mathf.Abs(_transform.position.x - converted.x) <= Mathf.Epsilon && Mathf.Abs(_transform.position.y - converted.y) <= Mathf.Epsilon && Mathf.Abs(_transform.position.z - converted.z) <= Mathf.Epsilon ? "No descrepancy." : "Descrepancy between true world and calculated world: " + (_transform.position - converted) +" = " + */ 
		/* 		_transform.position + " - " + converted); */
	}
	
	protected static void DrawSphere (Vector3 pos){
		Gizmos.DrawSphere(pos, 0.3f);
	}
}
