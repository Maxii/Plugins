using UnityEngine;
using UnityEditor;
using GridFramework.Grids;

namespace GridFramework.Editor {
	/// <summary>
	///   Inspector for polar grids.
	/// </summary>
	[CustomEditor (typeof(PolarGrid))]
	public class PolarGridEditor : UnityEditor.Editor {
#region  Private variables
		// Whether to display the computed properties
		[SerializeField]
		private static bool _showProps = true;
		private static string _docsURL;
		private PolarGrid _grid;
#endregion  // Private variables

#region  Callback methods
		void OnEnable() {
			_grid = target as PolarGrid;
			_docsURL = "file://" + Application.dataPath
				+ "/Plugins/GridFramework/Documentation/html/"
				+ "class_grid_framework_1_1_grids_1_1_polar_grid.html";
		}
		
		public override void OnInspectorGUI() {
			RadiusFields();
			SectorFields();
			DepthFields();

			_showProps =
				EditorGUILayout.Foldout(_showProps, "Computed Properties");

			if (_showProps) {
				++EditorGUI.indentLevel;
				RadiansFields();
				PiFields();
				DegreesField();
				--EditorGUI.indentLevel;
			}

			if (GUI.changed) {
				EditorUtility.SetDirty(target);
			}
		}
#endregion  // Callback methods

#region  Fields
		private void RadiusFields() {
			_grid.Radius  = EditorGUILayout.FloatField("Radius", _grid.Radius);
		}

		private void SectorFields() {
			_grid.Sectors = EditorGUILayout.IntField("Sectors", _grid.Sectors);
		}

		private void PiFields() {
			const string label = "Radians (in \u03c0)";
			_grid.Radians =
				EditorGUILayout.FloatField(label, _grid.Radians / Mathf.PI);
			_grid.Radians *= Mathf.PI;
		}

		private void DepthFields() {
			_grid.Depth = EditorGUILayout.FloatField("Depth", _grid.Depth);
		}

		private void RadiansFields() {
			const string label = "Radians";
			_grid.Radians = EditorGUILayout.FloatField(label, _grid.Radians);
		}

		private void DegreesField() {
			const string label = "Degrees";
			_grid.Degrees = EditorGUILayout.FloatField(label, _grid.Degrees);
		}
#endregion  // Fields

#region  Menu items
		[MenuItem ("CONTEXT/PolarGrid/Help")]
		private static void BrowseDocs(MenuCommand command) {
			Help.ShowHelpPage(_docsURL);
		}
#endregion  // Menu items
	}
}
