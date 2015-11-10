using UnityEngine;
using UnityEditor;
using Pathfinding;

namespace Pathfinding {
	[CustomGraphEditor (typeof(NavMeshGraph),"NavMeshGraph")]
	public class NavMeshGraphEditor : GraphEditor {

		public override void OnInspectorGUI (NavGraph target) {
			var graph = target as NavMeshGraph;

			graph.sourceMesh = ObjectField ("Source Mesh", graph.sourceMesh, typeof(Mesh), false) as Mesh;

			graph.offset = EditorGUILayout.Vector3Field ("Offset",graph.offset);

			graph.rotation = EditorGUILayout.Vector3Field ("Rotation",graph.rotation);

			graph.scale = EditorGUILayout.FloatField (new GUIContent ("Scale","Scale of the mesh"),graph.scale);
			graph.scale = (graph.scale < 0.01F && graph.scale > -0.01F) ? (graph.scale >= 0 ? 0.01F : -0.01F) : graph.scale;

			graph.accurateNearestNode = EditorGUILayout.Toggle (new GUIContent ("Accurate Nearest Node Queries","More accurate nearest node queries. See docs for more info"),graph.accurateNearestNode);
		}
	}
}
