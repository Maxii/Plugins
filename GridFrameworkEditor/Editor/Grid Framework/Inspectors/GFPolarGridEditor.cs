using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor (typeof(GFPolarGrid))]
public class GFPolarGridEditor : GFGridEditor {
	protected GFPolarGrid pGrid {get{return (GFPolarGrid)grid;}}
	
	protected override void SizeFields (){
		grid.relativeSize = EditorGUILayout.Toggle("Relative Size", grid.relativeSize);
		grid.size = EditorGUILayout.Vector3Field("Size", grid.size);
	}
	
	protected override void SpacingFields () {
		pGrid.radius = EditorGUILayout.FloatField("Radius", pGrid.radius);
		pGrid.sectors = EditorGUILayout.IntField("Sectors", pGrid.sectors);
		EditorGUILayout.LabelField("Angle / Deg Angle", ""+ pGrid.angle / Mathf.PI + "\u03c0 = " + pGrid.angleDeg + "\u00b0");
		pGrid.depth = EditorGUILayout.FloatField("Depth", pGrid.depth);
		pGrid.gridPlane = (GFGrid.GridPlane) EditorGUILayout.EnumPopup("Grid Plane", pGrid.gridPlane);
		pGrid.smoothness = EditorGUILayout.IntField("Smoothness", pGrid.smoothness);
	}
	
	[MenuItem ("CONTEXT/GFPolarGrid/Help")]
		private static void BrowseDocs (MenuCommand command) {
		string url = docsDir + "class_g_f_polar_grid.html";
		Help.ShowHelpPage (url);
	}
}
