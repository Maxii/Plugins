using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor (typeof(GFRectGrid))]
public class GFRectGridEditor : GFGridEditor {
	protected GFRectGrid rGrid {get{return (GFRectGrid)grid;}}
	
	protected override void SpacingFields () {
		rGrid.spacing = EditorGUILayout.Vector3Field("Spacing", rGrid.spacing);
	}

	[MenuItem ("CONTEXT/GFRectGrid/Help")]
	private static void BrowseDocs (MenuCommand command) {
		string url = docsDir + "class_g_f_rect_grid.html";
		Help.ShowHelpPage (url);
	}
}