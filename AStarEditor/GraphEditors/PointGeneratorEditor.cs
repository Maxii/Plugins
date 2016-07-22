using UnityEngine;
using UnityEditor;

namespace Pathfinding {
	[CustomGraphEditor(typeof(PointGraph), "PointGraph")]
	public class PointGraphEditor : GraphEditor {
		public override void OnInspectorGUI (NavGraph target) {
			var graph = target as PointGraph;

			graph.root = ObjectField(new GUIContent("Root", "All childs of this object will be used as nodes, if it is not set, a tag search will be used instead (see below)"), graph.root, typeof(Transform), true) as Transform;

			graph.recursive = EditorGUILayout.Toggle(new GUIContent("Recursive", "Should childs of the childs in the root GameObject be searched"), graph.recursive);
			graph.searchTag = EditorGUILayout.TagField(new GUIContent("Tag", "If root is not set, all objects with this tag will be used as nodes"), graph.searchTag);

			if (graph.root != null) {
				EditorGUILayout.HelpBox("All childs "+(graph.recursive ? "and sub-childs " : "") +"of 'root' will be used as nodes\nSet root to null to use a tag search instead", MessageType.None);
			} else {
				EditorGUILayout.HelpBox("All object with the tag '"+graph.searchTag+"' will be used as nodes"+(graph.searchTag == "Untagged" ? "\nNote: the tag 'Untagged' cannot be used" : ""), MessageType.None);
			}

			graph.maxDistance = EditorGUILayout.FloatField(new GUIContent("Max Distance", "The max distance in world space for a connection to be valid. A zero counts as infinity"), graph.maxDistance);

			graph.limits = EditorGUILayout.Vector3Field("Max Distance (axis aligned)", graph.limits);

			graph.raycast = EditorGUILayout.Toggle(new GUIContent("Raycast", "Use raycasting to check if connections are valid between each pair of nodes"), graph.raycast);

			if (graph.raycast) {
				EditorGUI.indentLevel++;

				graph.use2DPhysics = EditorGUILayout.Toggle(new GUIContent("Use 2D Physics", "If enabled, all raycasts will use the Unity 2D Physics API instead of the 3D one."), graph.use2DPhysics);
				graph.thickRaycast = EditorGUILayout.Toggle(new GUIContent("Thick Raycast", "A thick raycast checks along a thick line with radius instead of just along a line"), graph.thickRaycast);

				if (graph.thickRaycast) {
					EditorGUI.indentLevel++;
					graph.thickRaycastRadius = EditorGUILayout.FloatField(new GUIContent("Raycast Radius", "The radius in world units for the thick raycast"), graph.thickRaycastRadius);
					EditorGUI.indentLevel--;
				}

				graph.mask = EditorGUILayoutx.LayerMaskField("Mask", graph.mask);
				EditorGUI.indentLevel--;
			}

			graph.optimizeForSparseGraph = EditorGUILayout.Toggle(new GUIContent("Optimize For Sparse Graph", "Check online documentation for more information."), graph.optimizeForSparseGraph);

			if (graph.optimizeForSparseGraph) {
				EditorGUI.indentLevel++;

				graph.optimizeFor2D = EditorGUILayout.Toggle(new GUIContent("Optimize For XZ Plane", "Check online documentation for more information."), graph.optimizeFor2D);

				EditorGUI.indentLevel--;
			}
		}

		public override void OnDrawGizmos () {
			var graph = target as PointGraph;

			if (graph == null || graph.active == null || !graph.active.showNavGraphs) {
				return;
			}

			Gizmos.color = new Color(0.161F, 0.341F, 1F, 0.5F);

			if (graph.root != null) {
				DrawChildren(graph, graph.root);
			} else if (!string.IsNullOrEmpty(graph.searchTag)) {
				GameObject[] gos = GameObject.FindGameObjectsWithTag(graph.searchTag);
				for (int i = 0; i < gos.Length; i++) {
					Gizmos.DrawCube(gos[i].transform.position, Vector3.one*HandleUtility.GetHandleSize(gos[i].transform.position)*0.1F);
				}
			}
		}

		public void DrawChildren (PointGraph graph, Transform tr) {
			foreach (Transform child in tr) {
				Gizmos.DrawCube(child.position, Vector3.one*HandleUtility.GetHandleSize(child.position)*0.1F);
				if (graph.recursive) DrawChildren(graph, child);
			}
		}
	}
}
