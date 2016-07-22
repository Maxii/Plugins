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
		private static readonly string _docsURL =
			"file://" + Application.dataPath
			+ "/Plugins/GridFramework/Documentation/html/"
			+ "class_grid_framework_1_1_grids_1_1_hex_grid.html";

		private HexGrid _grid;
#endregion  // Private variables

#region  Callback methods
		void OnEnable() {
			_grid = target as HexGrid;
		}

		public override void OnInspectorGUI() {
			RadiusFields();
			DepthFields();
			OrientationFields();

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
#endregion  // Fields

#region  Menu items
		[MenuItem ("CONTEXT/HexGrid/Help")]
		private static void BrowseDocs(MenuCommand command) {
			Help.ShowHelpPage(_docsURL);
		}
#endregion  // Menu items
	}
}
