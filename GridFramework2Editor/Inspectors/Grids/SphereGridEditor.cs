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
		private static readonly string _docsURL =
			"file://" + Application.dataPath
			+ "/Plugins/GridFramework/Documentation/html/"
			+ "class_grid_framework_1_1_grids_1_1_sphere_grid.html";

		private SphereGrid _grid;
#endregion  // Private variables

#region  Callback methods
		void OnEnable() {
			_grid = target as SphereGrid;
		}

		public override void OnInspectorGUI() {
			_grid.Radius = EditorGUILayout.FloatField("Radius"   , _grid.Radius   );

			_grid.Parallels = EditorGUILayout.IntField("Parallels", _grid.Parallels);
			_grid.Meridians = EditorGUILayout.IntField("Meridians", _grid.Meridians);

			EditorGUILayout.LabelField("Polar angle"  , _grid.Polar   / Mathf.PI + "\u03c0 = " + _grid.PolarDeg   + "\u00b0");
			EditorGUILayout.LabelField("Azimuth angle", _grid.Azimuth / Mathf.PI + "\u03c0 = " + _grid.AzimuthDeg + "\u00b0");

			if (GUI.changed) {
				EditorUtility.SetDirty(target);
			}
		}
#endregion  // Callback methods

#region  Menu items
		[MenuItem ("CONTEXT/PolarGrid/Help")]
		private static void BrowseDocs(MenuCommand command) {
			Help.ShowHelpPage(_docsURL);
		}
#endregion  // Menu items
	}
}
