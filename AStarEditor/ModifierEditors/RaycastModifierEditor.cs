using UnityEngine;
using UnityEditor;
using Pathfinding;

[CustomEditor(typeof(RaycastModifier))]
public class RaycastModifierEditor : Editor {
	public override void OnInspectorGUI () {
		DrawDefaultInspector();
		var ob = target as RaycastModifier;

		EditorGUI.indentLevel = 0;
		Undo.RecordObject(ob, "modify settings on Raycast Modifier");

		if (ob.iterations < 0) ob.iterations = 0;

		ob.useRaycasting = EditorGUILayout.Toggle(new GUIContent("Use Physics Raycasting"), ob.useRaycasting);

		if (ob.useRaycasting) {
			EditorGUI.indentLevel++;
			ob.thickRaycast = EditorGUILayout.Toggle(new GUIContent("Use Thick Raycast", "Checks around the line between two points, not just the exact line.\n" +
					"Make sure the ground is either too far below or is not inside the mask since otherwise the raycast might always hit the ground"), ob.thickRaycast);
			if (ob.thickRaycast) {
				EditorGUI.indentLevel++;
				ob.thickRaycastRadius = EditorGUILayout.FloatField(new GUIContent("Thick Raycast Radius"), ob.thickRaycastRadius);
				if (ob.thickRaycastRadius < 0) ob.thickRaycastRadius = 0;
				EditorGUI.indentLevel--;
			}

			ob.raycastOffset = EditorGUILayout.Vector3Field(new GUIContent("Raycast Offset", "Offset from the original positions to perform the raycast.\n" +
					"Can be useful to avoid the raycast intersecting the ground or similar things you do not want to it intersect."), ob.raycastOffset);

			EditorGUILayout.PropertyField(serializedObject.FindProperty("mask"));

			EditorGUI.indentLevel--;
		}

		ob.useGraphRaycasting = EditorGUILayout.Toggle(new GUIContent("Use Graph Raycasting", "Raycasts on the graph to see if it hits any unwalkable nodes"), ob.useGraphRaycasting);

		ob.subdivideEveryIter = EditorGUILayout.Toggle(new GUIContent("Subdivide Every Iteration", "Subdivides the path every iteration to be able to find shorter paths"), ob.subdivideEveryIter);
	}
}
