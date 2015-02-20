using UnityEngine;
using UnityEditor;
using GridFramework;

[CustomEditor (typeof(GFPolarGrid))]
public class GFPolarGridEditor : GFGridEditor {
	protected GFPolarGrid _pGrid {get{return (GFPolarGrid)_grid;}}
	
	protected override void SizeFields (){
		_grid.relativeSize = EditorGUILayout.Toggle("Relative Size", _grid.relativeSize);
		_grid.size = EditorGUILayout.Vector3Field("Size", _grid.size);
	}
	
	protected override void SpacingFields () {
		_pGrid.radius  = EditorGUILayout.FloatField("Radius", _pGrid.radius);
		_pGrid.sectors = EditorGUILayout.IntField("Sectors", _pGrid.sectors);

		EditorGUILayout.LabelField("Angle / Deg Angle", ""+ _pGrid.angle / Mathf.PI + "\u03c0 = " + _pGrid.angleDeg + "\u00b0");

		_pGrid.depth      = EditorGUILayout.FloatField("Depth", _pGrid.depth);
		_pGrid.gridPlane  = (GridPlane) EditorGUILayout.EnumPopup("Grid Plane", _pGrid.gridPlane);
		_pGrid.smoothness = EditorGUILayout.IntField("Smoothness", _pGrid.smoothness);
	}
	
	[MenuItem ("CONTEXT/GFPolarGrid/Help")]
		private static void BrowseDocs (MenuCommand command) {
		string url = _docsDir + "class_g_f_polar_grid.html";
		Help.ShowHelpPage (url);
	}
}
