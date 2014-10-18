using UnityEngine;
using UnityEditor;
//using System.Collections;
using GridFramework.Vectors;

[CustomEditor (typeof(GFRectGrid))]
public class GFRectGridEditor : GFGridEditor {
	protected GFRectGrid RGrid {get{return (GFRectGrid)grid;}}
	Vector6 shearing = Vector6.zero;

	protected override void SpacingFields () {
		shearing.Set(RGrid.shearing);
		RGrid.spacing = EditorGUILayout.Vector3Field("Spacing", RGrid.spacing);
		GUILayout.Label("Shearing");
		EditorGUIUtility.labelWidth = 35f;
		++EditorGUI.indentLevel;
		EditorGUILayout.BeginHorizontal(); {
			shearing.xy = EditorGUILayout.FloatField("XY", shearing.xy);
			shearing.xz = EditorGUILayout.FloatField("XZ", shearing.xz);
			shearing.yx = EditorGUILayout.FloatField("YX", shearing.yx);
			--EditorGUI.indentLevel;
		}
		EditorGUILayout.EndHorizontal();
		++EditorGUI.indentLevel;
		EditorGUILayout.BeginHorizontal(); {
			shearing.yz = EditorGUILayout.FloatField("YZ", shearing.yz);
			shearing.zx = EditorGUILayout.FloatField("ZX", shearing.zx);
			shearing.zy = EditorGUILayout.FloatField("ZY", shearing.zy);
			--EditorGUI.indentLevel;
		}
		EditorGUILayout.EndHorizontal();
		EditorGUIUtility.labelWidth = 0;
		RGrid.shearing = new Vector6(shearing);
	}

	[MenuItem ("CONTEXT/GFRectGrid/Help")]
	private static void BrowseDocs (MenuCommand command) {
		string url = docsDir + "class_g_f_rect_grid.html";
		Help.ShowHelpPage (url);
	}
}
