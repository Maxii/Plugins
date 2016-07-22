using UnityEditor;
using UnityEngine;

namespace Pathfinding {
	public class GraphEditor : GraphEditorBase {
		public AstarPathEditor editor;

		/** Called by editor scripts to rescan the graphs e.g when the user moved a graph.
		 * Will only scan graphs if not playing and time to scan last graph was less than some constant (to avoid lag with large graphs) */
		public bool AutoScan () {
			if (!Application.isPlaying && AstarPath.active != null && AstarPath.active.lastScanTime < 0.11F) {
				AstarPath.active.Scan();
				return true;
			}
			return false;
		}

		public virtual void OnEnable () {
		}

		public virtual void UnloadGizmoMeshes () {
		}

		public static Object ObjectField (string label, Object obj, System.Type objType, bool allowSceneObjects) {
			return ObjectField(new GUIContent(label), obj, objType, allowSceneObjects);
		}

		public static Object ObjectField (GUIContent label, Object obj, System.Type objType, bool allowSceneObjects) {
			obj = EditorGUILayout.ObjectField(label, obj, objType, allowSceneObjects);

			if (obj != null) {
				if (allowSceneObjects && !EditorUtility.IsPersistent(obj)) {
					//Object is in the scene
					var com = obj as Component;
					var go = obj as GameObject;
					if (com != null) {
						go = com.gameObject;
					}
					if (go != null) {
						var urh = go.GetComponent<UnityReferenceHelper>();
						if (urh == null) {
							if (FixLabel("Object's GameObject must have a UnityReferenceHelper component attached")) {
								go.AddComponent<UnityReferenceHelper>();
							}
						}
					}
				} else if (EditorUtility.IsPersistent(obj)) {
					string path = AssetDatabase.GetAssetPath(obj);

					var rg = new System.Text.RegularExpressions.Regex(@"Resources[/|\\][^/]*$");


					if (!rg.IsMatch(path)) {
						if (FixLabel("Object must be in the 'Resources' folder, top level")) {
							if (!System.IO.Directory.Exists(Application.dataPath+"/Resources")) {
								System.IO.Directory.CreateDirectory(Application.dataPath+"/Resources");
								AssetDatabase.Refresh();
							}
							string ext = System.IO.Path.GetExtension(path);

							string error = AssetDatabase.MoveAsset(path, "Assets/Resources/"+obj.name+ext);

							if (error == "") {
								//Debug.Log ("Successful move");
								path = AssetDatabase.GetAssetPath(obj);
							} else {
								Debug.LogError("Couldn't move asset - "+error);
							}
						}
					}

					if (!AssetDatabase.IsMainAsset(obj) && obj.name != AssetDatabase.LoadMainAssetAtPath(path).name) {
						if (FixLabel("Due to technical reasons, the main asset must\nhave the same name as the referenced asset")) {
							string error = AssetDatabase.RenameAsset(path, obj.name);
							if (error == "") {
								//Debug.Log ("Successful");
							} else {
								Debug.LogError("Couldn't rename asset - "+error);
							}
						}
					}
				}
			}

			return obj;
		}

		/** Draws common graph settings */
		public void OnBaseInspectorGUI (NavGraph target) {
			int penalty = EditorGUILayout.IntField(new GUIContent("Initial Penalty", "Initial Penalty for nodes in this graph. Set during Scan."), (int)target.initialPenalty);

			if (penalty < 0) penalty = 0;
			target.initialPenalty = (uint)penalty;
		}

		/** Override to implement graph inspectors */
		public virtual void OnInspectorGUI (NavGraph target) {
		}

		/** Override to implement scene GUI drawing for the graph */
		public virtual void OnSceneGUI (NavGraph target) {
		}

		/** Override to implement scene Gizmos drawing for the graph editor */
		public virtual void OnDrawGizmos () {
		}

		/** Draws a thin separator line */
		public static void Separator () {
			GUIStyle separator = AstarPathEditor.astarSkin.FindStyle("PixelBox3Separator") ?? new GUIStyle();

			Rect r = GUILayoutUtility.GetRect(new GUIContent(), separator);

			if (Event.current.type == EventType.Repaint) {
				separator.Draw(r, false, false, false, false);
			}
		}

		/** Draws a small help box with a 'Fix' button to the right. \returns Boolean - Returns true if the button was clicked */
		public static bool FixLabel (string label, string buttonLabel = "Fix", int buttonWidth = 40) {
			GUILayout.BeginHorizontal();
			GUILayout.Space(14*EditorGUI.indentLevel);
			GUILayout.BeginHorizontal(AstarPathEditor.helpBox);
			GUILayout.Label(label, EditorGUIUtility.isProSkin ? EditorStyles.whiteMiniLabel : EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
			var returnValue = GUILayout.Button(buttonLabel, EditorStyles.miniButton, GUILayout.Width(buttonWidth));
			GUILayout.EndHorizontal();
			GUILayout.EndHorizontal();
			return returnValue;
		}

		/** Draws a small help box.
		 * Works with EditorGUI.indentLevel
		 */
		public static void HelpBox (string label) {
			GUILayout.BeginHorizontal();
			GUILayout.Space(14*EditorGUI.indentLevel);
			GUILayout.Label(label, AstarPathEditor.helpBox);
			GUILayout.EndHorizontal();
		}

		/** Draws a toggle with a bold label to the right. Does not enable or disable GUI */
		public bool ToggleGroup (string label, bool value) {
			return ToggleGroup(new GUIContent(label), value);
		}

		/** Draws a toggle with a bold label to the right. Does not enable or disable GUI */
		public static bool ToggleGroup (GUIContent label, bool value) {
			GUILayout.BeginHorizontal();
			GUILayout.Space(13*EditorGUI.indentLevel);
			value = GUILayout.Toggle(value, "", GUILayout.Width(10));
			GUIStyle boxHeader = AstarPathEditor.astarSkin.FindStyle("CollisionHeader");
			if (GUILayout.Button(label, boxHeader, GUILayout.Width(100))) {
				value = !value;
			}

			GUILayout.EndHorizontal();
			return value;
		}

		/** Draws a wire cube using handles */
		public static void DrawWireCube (Vector3 center, Vector3 size) {
			size *= 0.5F;

			var dx = new Vector3(size.x, 0, 0);
			var dy = new Vector3(0, size.y, 0);
			var dz = new Vector3(0, 0, size.z);

			Vector3 p1 = center-dy-dz-dx;
			Vector3 p2 = center-dy-dz+dx;
			Vector3 p3 = center-dy+dz+dx;
			Vector3 p4 = center-dy+dz-dx;

			Vector3 p5 = center+dy-dz-dx;
			Vector3 p6 = center+dy-dz+dx;
			Vector3 p7 = center+dy+dz+dx;
			Vector3 p8 = center+dy+dz-dx;

			Handles.DrawLine(p1, p2);
			Handles.DrawLine(p2, p3);
			Handles.DrawLine(p3, p4);
			Handles.DrawLine(p4, p1);

			Handles.DrawLine(p5, p6);
			Handles.DrawLine(p6, p7);
			Handles.DrawLine(p7, p8);
			Handles.DrawLine(p8, p5);

			Handles.DrawLine(p1, p5);
			Handles.DrawLine(p2, p6);
			Handles.DrawLine(p3, p7);
			Handles.DrawLine(p4, p8);
		}
	}
}
