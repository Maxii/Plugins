using UnityEngine;
using UnityEditor;
using Pathfinding;

[CustomEditor(typeof(SimpleSmoothModifier))]
[CanEditMultipleObjects]
public class SmoothModifierEditor : Editor {
	SerializedProperty smoothType, uniformLength, maxSegmentLength, subdivisions, iterations, strength, offset, bezierTangentLength, factor;

	void OnEnable () {
		smoothType = serializedObject.FindProperty("smoothType");
		uniformLength = serializedObject.FindProperty("uniformLength");
		maxSegmentLength = serializedObject.FindProperty("maxSegmentLength");
		subdivisions = serializedObject.FindProperty("subdivisions");
		iterations = serializedObject.FindProperty("iterations");
		strength = serializedObject.FindProperty("strength");
		offset = serializedObject.FindProperty("offset");
		bezierTangentLength = serializedObject.FindProperty("bezierTangentLength");
		factor = serializedObject.FindProperty("factor");
	}

	public override void OnInspectorGUI () {
		EditorGUILayout.PropertyField(smoothType);

		if (!smoothType.hasMultipleDifferentValues) {
			switch ((SimpleSmoothModifier.SmoothType)smoothType.enumValueIndex) {
			case SimpleSmoothModifier.SmoothType.Simple:
				EditorGUILayout.PropertyField(uniformLength);

				if (uniformLength.boolValue) {
					EditorGUILayout.PropertyField(maxSegmentLength);
					maxSegmentLength.floatValue = Mathf.Max(maxSegmentLength.floatValue, 0.005f);
				} else {
					EditorGUILayout.IntSlider(subdivisions, 0, 6);
				}

				EditorGUILayout.PropertyField(iterations);
				iterations.intValue = Mathf.Max(iterations.intValue, 0);

				EditorGUILayout.Slider(strength, 0f, 1f);
				break;
			case SimpleSmoothModifier.SmoothType.OffsetSimple:
				EditorGUILayout.PropertyField(iterations);
				iterations.intValue = Mathf.Max(iterations.intValue, 0);

				EditorGUILayout.PropertyField(offset);
				offset.floatValue = Mathf.Max(offset.floatValue, 0);
				break;
			case SimpleSmoothModifier.SmoothType.Bezier:
				EditorGUILayout.IntSlider(subdivisions, 0, 6);
				EditorGUILayout.PropertyField(bezierTangentLength);
				break;
			case SimpleSmoothModifier.SmoothType.CurvedNonuniform:
				EditorGUILayout.PropertyField(maxSegmentLength);
				maxSegmentLength.floatValue = Mathf.Max(maxSegmentLength.floatValue, 0.005f);

				EditorGUILayout.PropertyField(factor);
				break;
			}
		}

		serializedObject.ApplyModifiedProperties();
	}
}
