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
		private static readonly string _docsURL =
			"file://" + Application.dataPath
			+ "/Plugins/GridFramework/Documentation/html/"
			+ "class_grid_framework_1_1_grids_1_1_polar_grid.html";

		private PolarGrid _grid;
#endregion  // Private variables

#region  Callback methods
		void OnEnable() {
			_grid = target as PolarGrid;
		}
		
		public override void OnInspectorGUI() {
			RadiusFields();
			SectorFields();
			AngleFields();
			DepthFields();

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

		private void AngleFields() {
			var radians = _grid.Radians / Mathf.PI;
			var degrees = _grid.Degrees;

			EditorGUILayout.LabelField("Radians / Deg Angle", ""+ radians + "\u03c0 = " + degrees + "\u00b0");
		}

		private void DepthFields() {
			_grid.Depth = EditorGUILayout.FloatField("Depth", _grid.Depth);
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
