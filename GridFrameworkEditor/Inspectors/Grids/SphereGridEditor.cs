using UnityEngine;
using UnityEditor;
using SphereGrid = GridFramework.Grids.SphereGrid;

namespace GridFramework.Editor {
	/// <summary>
	///   Inspector for spherical grids.
	/// </summary>
	[CustomEditor (typeof(SphereGrid))]
	public class SphereGridEditor : UnityEditor.Editor {
#region  Private variables
		// Whether to display the computed properties
		[SerializeField]
		private static bool _showProps = true;
		private static string _docsURL;
		private SphereGrid _grid;
#endregion  // Private variables

#region  Callback methods
		void OnEnable() {
			_grid = target as SphereGrid;
			_docsURL = "file://" + Application.dataPath
				+ "/Plugins/GridFramework/Documentation/html/"
				+ "class_grid_framework_1_1_grids_1_1_sphere_grid.html";
		}

		public override void OnInspectorGUI() {
			MainFields();

			_showProps =
				EditorGUILayout.Foldout(_showProps, "Computed Properties");

			if (_showProps) {
				++EditorGUI.indentLevel;
				PolarFields();
				AzimuthFields();
				--EditorGUI.indentLevel;
			}

			if (GUI.changed) {
				EditorUtility.SetDirty(target);
			}
		}
#endregion  // Callback methods

#region  Field methods
		private void MainFields() {
			_grid.Radius    = EditorGUILayout.FloatField("Radius"   , _grid.Radius   );
			_grid.Parallels = EditorGUILayout.IntField(  "Parallels", _grid.Parallels);
			_grid.Meridians = EditorGUILayout.IntField(  "Meridians", _grid.Meridians);
		}

		private void PolarFields() {
			_grid.Polar = EditorGUILayout.FloatField("Polar", _grid.Polar);
			_grid.Polar /= Mathf.PI;
			_grid.Polar = EditorGUILayout.FloatField("Polar (in \u03c0)", _grid.Polar);
			_grid.Polar *= Mathf.PI;
			_grid.PolarDeg = EditorGUILayout.FloatField("Polar (degrees)", _grid.PolarDeg);
		}

		private void AzimuthFields() {
			_grid.Azimuth = EditorGUILayout.FloatField("Azimuth", _grid.Azimuth);
			_grid.Azimuth /= Mathf.PI;
			_grid.Azimuth = EditorGUILayout.FloatField("Azimuth (in \u03c0)", _grid.Azimuth);
			_grid.Azimuth *= Mathf.PI;
			_grid.AzimuthDeg = EditorGUILayout.FloatField("Azimuth (degrees)", _grid.AzimuthDeg);
		}
#endregion  // Field methods

#region  Menu items
		[MenuItem ("CONTEXT/PolarGrid/Help")]
		private static void BrowseDocs(MenuCommand command) {
			Help.ShowHelpPage(_docsURL);
		}
#endregion  // Menu items
	}
}
