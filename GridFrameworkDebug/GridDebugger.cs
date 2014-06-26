using UnityEngine;
using System.Collections;

public class GridDebugger : MonoBehaviour {
	public bool toggleDebugging = false;
	public bool printLogs = true;
	public GFGrid theGrid;
	public GFGrid.GridPlane debuggedPlane = GFGrid.GridPlane.XY;
	public enum GridFunction {NearestVertexW, NearestFaceW, NearestBoxW, WorldToGrid, GridToWorld};
	public GridFunction debuggedFunction = GridFunction.NearestBoxW;
	public Color debugColor = Color.red;
	//public int[] index = new int[3];
	
	private Transform cachedTransform;
	Transform _transform{get{if(!cachedTransform) cachedTransform = transform; return cachedTransform;}}
	
	// Update is called once per frame
	protected void OnDrawGizmos() {
		if(!theGrid || ! toggleDebugging)
			return;
				
		Gizmos.color = debugColor;
		if ((int)debuggedFunction == 0) {
			DebugNearestVertex ();
		} else if ((int)debuggedFunction == 1) {
			DebugNearestFace ();
		} else if ((int)debuggedFunction == 2) {
			DebugNearestBox ();
		} else if ((int)debuggedFunction == 3) {
			DebugWorldToGrid ();
		} else if ((int)debuggedFunction == 4) {
			DebugGridToWorld ();
		}
	}
	
	protected void DebugNearestVertex(){
		theGrid.NearestVertexW(_transform.position, true);
		if(printLogs)
			Debug.Log(theGrid.NearestVertexG(_transform.position));
	}
	
	protected void DebugNearestFace(){
		theGrid.NearestFaceW(_transform.position, debuggedPlane, true);
		if(printLogs)
			Debug.Log(theGrid.NearestFaceG(_transform.position, debuggedPlane));
	}
	
	protected void DebugNearestBox(){
		theGrid.NearestBoxW(_transform.position, true);
		if(printLogs)
			Debug.Log(theGrid.NearestBoxG(_transform.position));
	}
	
	protected void DebugWorldToGrid(){
		theGrid.WorldToGrid(_transform.position);
		if(printLogs)
			Debug.Log(theGrid.WorldToGrid(_transform.position));
	}
	
	protected void DebugGridToWorld(){
		Vector3 converted = theGrid.GridToWorld(theGrid.WorldToGrid(_transform.position));
		if(printLogs)
			Debug.Log(Mathf.Abs(_transform.position.x - converted.x) <= Mathf.Epsilon && Mathf.Abs(_transform.position.y - converted.y) <= Mathf.Epsilon && Mathf.Abs(_transform.position.z - converted.z) <= Mathf.Epsilon ? "No descrepancy." : "Descrepancy between true world and calculated world: " + (_transform.position - converted) +" = " + 
				_transform.position + " - " + converted);
	}
	
	protected void DrawSphere (Vector3 pos){
		Gizmos.DrawSphere(pos, 0.3f);
	}
}
