using UnityEngine;
using UnityEditor;
using GridFramework.Grids;
using Vector6 = GridFramework.Vectors.Vector6;

namespace GridFramework.Editor {
	/// <summary>
	///   Inspector for rectangular grids.
	/// </summary>
	[CustomEditor (typeof(RectGrid))]
	public class RectGridEditor : UnityEditor.Editor {
#region  Private variables
		private static string _docsURL;
		private RectGrid _grid;
		private Vector6 _shearing = Vector6.Zero;
#endregion  // Private variables

#region  Callback methods
		void OnEnable() {
			_grid = target as RectGrid;
			_docsURL = "file://" + Application.dataPath
				+ "/Plugins/GridFramework/Documentation/html/"
				+ "class_grid_framework_1_1_grids_1_1_rect_grid.html";
		}

		public override void OnInspectorGUI() {
			_shearing.Set(_grid.Shearing);

			_grid.Spacing = EditorGUILayout.Vector3Field("Spacing", _grid.Spacing);
			GUILayout.Label("Shearing");

			EditorGUIUtility.labelWidth = 35f;
			++EditorGUI.indentLevel;

			EditorGUILayout.BeginHorizontal(); {
				_shearing.XY = EditorGUILayout.FloatField("XY", _shearing.XY);
				_shearing.XZ = EditorGUILayout.FloatField("XZ", _shearing.XZ);
				_shearing.YX = EditorGUILayout.FloatField("YX", _shearing.YX);
				--EditorGUI.indentLevel;
			}

			EditorGUILayout.EndHorizontal();

			++EditorGUI.indentLevel;

			EditorGUILayout.BeginHorizontal(); {
				_shearing.YZ = EditorGUILayout.FloatField("YZ", _shearing.YZ);
				_shearing.ZX = EditorGUILayout.FloatField("ZX", _shearing.ZX);
				_shearing.ZY = EditorGUILayout.FloatField("ZY", _shearing.ZY);
				--EditorGUI.indentLevel;
			}

			EditorGUILayout.EndHorizontal();

			EditorGUIUtility.labelWidth = 0;
			_grid.Shearing = _shearing;

			serializedObject.ApplyModifiedProperties();

			if (GUI.changed) {
				EditorUtility.SetDirty(target);
			}
		}
#endregion  // Callback methods

#region  Menu items
		[MenuItem ("CONTEXT/RectGrid/Help")]
		private static void BrowseDocs(MenuCommand command) {
			Help.ShowHelpPage(_docsURL);
		}
#endregion  // Menu items
	}
}
