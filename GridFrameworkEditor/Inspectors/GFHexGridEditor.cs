using UnityEditor;
using GridFramework;

[CustomEditor (typeof(GFHexGrid))]
public class GFHexGridEditor : GFGridEditor {
	protected GFHexGrid _hGrid {get{return (GFHexGrid)_grid;}}
	
	protected override void SpacingFields () {
		_hGrid.radius = EditorGUILayout.FloatField("Radius", _hGrid.radius);
		_hGrid.depth = EditorGUILayout.FloatField("Depth", _hGrid.depth);
		_hGrid.gridPlane = (GridPlane) EditorGUILayout.EnumPopup("Grid Plane", _hGrid.gridPlane);
		_hGrid.hexSideMode = (GFHexGrid.HexOrientation) EditorGUILayout.EnumPopup("Hex Side Mode", _hGrid.hexSideMode);
		_hGrid.gridStyle = (GFHexGrid.HexGridShape)EditorGUILayout.EnumPopup("Grid Style", _hGrid.gridStyle);
		if (_hGrid.gridStyle == GFHexGrid.HexGridShape.Circle) {
			++EditorGUI.indentLevel; {
				_hGrid.renderAround = EditorGUILayout.Vector3Field("Render Around", _hGrid.renderAround);
			}
			--EditorGUI.indentLevel;
		}
	}

	[MenuItem ("CONTEXT/GFHexGrid/Help")]
	private static void BrowseDocs (MenuCommand command) {
		string url = _docsDir + "class_g_f_hex_grid.html";
		Help.ShowHelpPage (url);
	}
}
