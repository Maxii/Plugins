using UnityEditor;
using UnityEngine;
using GridFramework.Grids;

namespace GridFramework.Editor {
	/// <summary>
	///   Inspector for hexagonal grids.
	/// </summary>
	[CustomEditor(typeof(HexGrid))]
	public class HexGridEditor : UnityEditor.Editor {
#region  Private variables
		// Whether to display the computed properties
		[SerializeField]
		private static bool _showProps = true;
		private static string _docsURL;
		private HexGrid _grid;
#endregion  // Private variables

#region  Callback methods
		void OnEnable() {
			_grid = target as HexGrid;
			_docsURL = "file://" + Application.dataPath
				+ "/Plugins/GridFramework/Documentation/html/"
				+ "class_grid_framework_1_1_grids_1_1_hex_grid.html";
		}

		public override void OnInspectorGUI() {
			RadiusFields();
			DepthFields();
			OrientationFields();

			_showProps =
				EditorGUILayout.Foldout(_showProps, "Computed Properties");

			if (_showProps) {
				++EditorGUI.indentLevel;
				HeightFields();
				WidthFields();
				SideFields();
				--EditorGUI.indentLevel;
			}

			if (GUI.changed) {
				EditorUtility.SetDirty(target);
			}
		}
#endregion  // Callback methods

#region  Fields
		private void RadiusFields() {
			_grid.Radius = EditorGUILayout.FloatField("Radius", _grid.Radius);
		}

		private void DepthFields() {
			_grid.Depth  = EditorGUILayout.FloatField("Depth", _grid.Depth);
		}

		private void OrientationFields() {
			_grid.Sides = (HexGrid.Orientation)EditorGUILayout.EnumPopup("Hex Side Mode", _grid.Sides);
		}

		private void HeightFields() {
			_grid.Height = EditorGUILayout.FloatField("Height", _grid.Height);
		}

		private void WidthFields() {
			_grid.Width = EditorGUILayout.FloatField("Width", _grid.Width);
		}

		private void SideFields() {
			_grid.Side = EditorGUILayout.FloatField("Side", _grid.Side);
		}
#endregion  // Fields

#region  Menu items
		[MenuItem ("CONTEXT/HexGrid/Help")]
		private static void BrowseDocs(MenuCommand command) {
			Help.ShowHelpPage(_docsURL);
		}
#endregion  // Menu items
	}
}
