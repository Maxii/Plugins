using UnityEngine;
using System.Collections;
using UnityEditor;

namespace Pathfinding {
	[CustomEditor(typeof(NavmeshCut))]
	[CanEditMultipleObjects]
	public class NavmeshCutEditor : Editor {
		SerializedProperty type, mesh, rectangleSize, circleRadius, circleResolution, height, meshScale, center, updateDistance, isDual, cutsAddedGeom, updateRotationDistance, useRotation;

		void OnEnable () {
			type = serializedObject.FindProperty("type");
			mesh = serializedObject.FindProperty("mesh");
			rectangleSize = serializedObject.FindProperty("rectangleSize");
			circleRadius = serializedObject.FindProperty("circleRadius");
			circleResolution = serializedObject.FindProperty("circleResolution");
			height = serializedObject.FindProperty("height");
			meshScale = serializedObject.FindProperty("meshScale");
			center = serializedObject.FindProperty("center");
			updateDistance = serializedObject.FindProperty("updateDistance");
			isDual = serializedObject.FindProperty("isDual");
			cutsAddedGeom = serializedObject.FindProperty("cutsAddedGeom");
			updateRotationDistance = serializedObject.FindProperty("updateRotationDistance");
			useRotation = serializedObject.FindProperty("useRotation");
		}

		public override void OnInspectorGUI () {
			serializedObject.Update();

			EditorGUILayout.PropertyField(type);

			if (!type.hasMultipleDifferentValues) {
				switch ((NavmeshCut.MeshType)type.intValue) {
				case NavmeshCut.MeshType.Circle:
					EditorGUILayout.PropertyField(circleRadius);
					EditorGUILayout.PropertyField(circleResolution);

					if (circleResolution.intValue >= 20) {
						EditorGUILayout.HelpBox("Be careful with large values. It is often better with a relatively low resolution since it generates cleaner navmeshes with fewer nodes.", MessageType.Warning);
					}
					break;
				case NavmeshCut.MeshType.Rectangle:
					EditorGUILayout.PropertyField(rectangleSize);
					break;
				case NavmeshCut.MeshType.CustomMesh:
					EditorGUILayout.PropertyField(mesh);
					EditorGUILayout.PropertyField(meshScale);
					EditorGUILayout.HelpBox("This mesh should be a planar surface. Take a look at the documentation for an example.", MessageType.Info);
					break;
				}
			}

			EditorGUILayout.PropertyField(height);
			if (!height.hasMultipleDifferentValues) {
				height.floatValue = Mathf.Max(height.floatValue, 0);
			}

			EditorGUILayout.PropertyField(center);

			EditorGUILayout.Separator();
			EditorGUILayout.PropertyField(updateDistance);
			EditorGUILayout.PropertyField(useRotation);
			if (useRotation.boolValue) {
				EditorGUI.indentLevel++;
				EditorGUILayout.PropertyField(updateRotationDistance);
				if (!updateRotationDistance.hasMultipleDifferentValues) {
					updateRotationDistance.floatValue = Mathf.Clamp(updateRotationDistance.floatValue, 0, 180);
				}
				EditorGUI.indentLevel--;
			}

			EditorGUILayout.PropertyField(isDual);
			EditorGUILayout.PropertyField(cutsAddedGeom);

			serializedObject.ApplyModifiedProperties();
		}
	}
}
