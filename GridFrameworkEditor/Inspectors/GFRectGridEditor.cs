#if UNITY_3_0 || UNITY_3_0_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5
#define U3D_VERSION_BETWEEN_3_AND_4
#endif

using UnityEngine;
using UnityEditor;
using GridFramework.Vectors;

[CustomEditor (typeof(GFRectGrid))]
public class GFRectGridEditor : GFGridEditor {
	protected GFRectGrid RGrid {get{return (GFRectGrid)_grid;}}
	Vector6 shearing = Vector6.zero;

	protected override void SpacingFields () {
		shearing.Set(RGrid.shearing);
		RGrid.spacing = EditorGUILayout.Vector3Field("Spacing", RGrid.spacing);
		GUILayout.Label("Shearing");
		#if !U3D_VERSION_BETWEEN_3_AND_4
		EditorGUIUtility.labelWidth = 35f;
		#endif
		++EditorGUI.indentLevel;
		#if !U3D_VERSION_BETWEEN_3_AND_4
		EditorGUILayout.BeginHorizontal(); {
		#endif
			shearing.xy = EditorGUILayout.FloatField("XY", shearing.xy);
			shearing.xz = EditorGUILayout.FloatField("XZ", shearing.xz);
			shearing.yx = EditorGUILayout.FloatField("YX", shearing.yx);
		#if !U3D_VERSION_BETWEEN_3_AND_4
			--EditorGUI.indentLevel;
		}
		EditorGUILayout.EndHorizontal();
		++EditorGUI.indentLevel;
		EditorGUILayout.BeginHorizontal(); {
		#endif
			shearing.yz = EditorGUILayout.FloatField("YZ", shearing.yz);
			shearing.zx = EditorGUILayout.FloatField("ZX", shearing.zx);
			shearing.zy = EditorGUILayout.FloatField("ZY", shearing.zy);
			--EditorGUI.indentLevel;
		#if !U3D_VERSION_BETWEEN_3_AND_4
		}
		EditorGUILayout.EndHorizontal();
		EditorGUIUtility.labelWidth = 0;
		#endif
		RGrid.shearing = new Vector6(shearing);
	}

	[MenuItem ("CONTEXT/GFRectGrid/Help")]
	private static void BrowseDocs (MenuCommand command) {
		string url = _docsDir + "class_g_f_rect_grid.html";
		Help.ShowHelpPage (url);
	}
}
