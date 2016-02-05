using UnityEngine;
using UnityEditor;

[CustomEditor (typeof(GFSphereGrid))]
public class GFSphereGridEditor : GFGridEditor {
	protected GFSphereGrid _sGrid {get {return (GFSphereGrid) _grid;} }


	protected override void SpacingFields () {
		_sGrid.radius  = EditorGUILayout.FloatField("Radius"   , _sGrid.radius   );
		_sGrid.parallels = EditorGUILayout.IntField("Parallels", _sGrid.parallels);
		_sGrid.meridians = EditorGUILayout.IntField("Meridians", _sGrid.meridians);

		EditorGUILayout.LabelField("Polar angle"  , _sGrid.polar   / Mathf.PI + "\u03c0 = " + _sGrid.polarDeg   + "\u00b0");
		EditorGUILayout.LabelField("Azimuth angle", _sGrid.azimuth / Mathf.PI + "\u03c0 = " + _sGrid.azimuthDeg + "\u00b0");

		_sGrid.smoothP = EditorGUILayout.IntField("Parallel Smoothness", _sGrid.smoothP);
		_sGrid.smoothM = EditorGUILayout.IntField("Meridian Smoothness", _sGrid.smoothM);
	}

	[MenuItem ("CONTEXT/GFPolarGrid/Help")]
	private static void BrowseDocs (MenuCommand command) {
		string url = _docsDir + "class_g_f_polar_grid.html";
		Help.ShowHelpPage(url);
	}
}
