using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Pathfinding;

/** Editor for GraphUpdateScene */
[CustomEditor(typeof(GraphUpdateScene))]
[CanEditMultipleObjects]
public class GraphUpdateSceneEditor : Editor {
	int selectedPoint = -1;

	const float pointGizmosRadius = 0.09F;
	static Color PointColor = new Color(1, 0.36F, 0, 0.6F);
	static Color PointSelectedColor = new Color(1, 0.24F, 0, 1.0F);

	SerializedProperty points, updatePhysics, updateErosion, convex;
	SerializedProperty minBoundsHeight, applyOnStart, applyOnScan;
	SerializedProperty modifyWalkable, walkableValue, penaltyDelta, modifyTag, tagValue;
	SerializedProperty useWorldSpace, lockToY, lockToYValue, resetPenaltyOnPhysics;

	GraphUpdateScene[] scripts;

	void OnEnable () {
		// Find all properties
		points = serializedObject.FindProperty("points");
		updatePhysics = serializedObject.FindProperty("updatePhysics");
		resetPenaltyOnPhysics = serializedObject.FindProperty("resetPenaltyOnPhysics");
		updateErosion = serializedObject.FindProperty("updateErosion");
		convex = serializedObject.FindProperty("convex");
		minBoundsHeight = serializedObject.FindProperty("minBoundsHeight");
		applyOnStart = serializedObject.FindProperty("applyOnStart");
		applyOnScan = serializedObject.FindProperty("applyOnScan");
		modifyWalkable = serializedObject.FindProperty("modifyWalkability");
		walkableValue = serializedObject.FindProperty("setWalkability");
		penaltyDelta = serializedObject.FindProperty("penaltyDelta");
		modifyTag = serializedObject.FindProperty("modifyTag");
		tagValue = serializedObject.FindProperty("setTag");
		useWorldSpace = serializedObject.FindProperty("useWorldSpace");
		lockToY = serializedObject.FindProperty("lockToY");
		lockToYValue = serializedObject.FindProperty("lockToYValue");
	}

	public override void OnInspectorGUI () {
		serializedObject.Update();

		// Get a list of inspected components
		scripts = new GraphUpdateScene[targets.Length];
		targets.CopyTo(scripts, 0);

		EditorGUI.BeginChangeCheck();

		// Make sure no point arrays are null
		for (int i = 0; i < scripts.Length; i++) {
			scripts[i].points = scripts[i].points ?? new Vector3[0];
		}

		if (!points.hasMultipleDifferentValues && points.arraySize == 0) {
			if (scripts[0].GetComponent<Collider>() != null) {
				EditorGUILayout.HelpBox("No points, using collider.bounds", MessageType.Info);
			} else if (scripts[0].GetComponent<Renderer>() != null) {
				EditorGUILayout.HelpBox("No points, using renderer.bounds", MessageType.Info);
			} else {
				EditorGUILayout.HelpBox("No points and no collider or renderer attached, will not affect anything\nPoints can be added using the transform tool and holding shift", MessageType.Warning);
			}
		}

		DrawPointsField();

		EditorGUI.indentLevel = 0;

		DrawPhysicsField();

		EditorGUILayout.PropertyField(updateErosion, new GUIContent("Update Erosion", "Recalculate erosion for grid graphs.\nSee online documentation for more info"));

		DrawConvexField();

		EditorGUILayout.PropertyField(minBoundsHeight, new GUIContent("Min Bounds Height", "Defines a minimum height to be used for the bounds of the GUO.\nUseful if you define points in 2D (which would give height 0)"));
		EditorGUILayout.PropertyField(applyOnStart, new GUIContent("Apply On Start"));
		EditorGUILayout.PropertyField(applyOnScan, new GUIContent("Apply On Scan"));

		DrawWalkableField();

		DrawPenaltyField();

		DrawTagField();

		EditorGUILayout.Separator();

		DrawWorldSpaceField();

		DrawLockToYField();

		if (GUILayout.Button("Clear all points")) {
			for (int i = 0; i < scripts.Length; i++) {
				scripts[i].points = new Vector3[0];
				EditorUtility.SetDirty(scripts[i]);
				scripts[i].RecalcConvex();
			}
		}

		serializedObject.ApplyModifiedProperties();

		if (EditorGUI.EndChangeCheck()) {
			for (int i = 0; i < scripts.Length; i++) {
				EditorUtility.SetDirty(scripts[i]);
			}

			// Repaint the scene view if necessary
			if (!Application.isPlaying || EditorApplication.isPaused) SceneView.RepaintAll();
		}
	}

	void DrawPointsField () {
		EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField(points, true);
		if (EditorGUI.EndChangeCheck()) {
			for (int i = 0; i < scripts.Length; i++) {
				scripts[i].RecalcConvex();
			}
			HandleUtility.Repaint();
		}
	}

	void DrawPhysicsField () {
		EditorGUILayout.PropertyField(updatePhysics, new GUIContent("Update Physics", "Perform similar calculations on the nodes as during scan.\n" +
				"Grid Graphs will update the position of the nodes and also check walkability using collision.\nSee online documentation for more info."));

		if (!updatePhysics.hasMultipleDifferentValues && updatePhysics.boolValue) {
			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField(resetPenaltyOnPhysics, new GUIContent("Reset Penalty On Physics", "Will reset the penalty to the default value during the update."));
			EditorGUI.indentLevel--;
		}
	}

	void DrawConvexField () {
		EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField(convex, new GUIContent("Convex", "Sets if only the convex hull of the points should be used or the whole polygon"));
		if (EditorGUI.EndChangeCheck()) {
			for (int i = 0; i < scripts.Length; i++) {
				scripts[i].RecalcConvex();
			}
			HandleUtility.Repaint();
		}
	}

	void DrawWalkableField () {
		EditorGUILayout.PropertyField(modifyWalkable, new GUIContent("Modify walkability", "If true, walkability of all nodes will be modified"));
		if (!modifyWalkable.hasMultipleDifferentValues && modifyWalkable.boolValue) {
			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField(walkableValue, new GUIContent("Walkability Value", "Nodes' walkability will be set to this value"));
			EditorGUI.indentLevel--;
		}
	}

	void DrawPenaltyField () {
		EditorGUILayout.PropertyField(penaltyDelta, new GUIContent("Penalty Delta", "A penalty will be added to the nodes, usually you need very large values, at least 1000-10000.\n" +
				"A higher penalty will mean that agents will try to avoid those nodes."));

		if (!penaltyDelta.hasMultipleDifferentValues && penaltyDelta.intValue < 0) {
			EditorGUILayout.HelpBox("Be careful when lowering the penalty. Negative penalties are not supported and will instead underflow and get really high.\n" +
				"You can set an initial penalty on graphs (see their settings) and then lower them like this to get regions which are easier to traverse.", MessageType.Warning);
		}
	}

	void DrawTagField () {
		EditorGUILayout.PropertyField(modifyTag, new GUIContent("Modify Tag", "Should the tags of the nodes be modified"));
		if (!modifyTag.hasMultipleDifferentValues && modifyTag.boolValue) {
			EditorGUI.indentLevel++;
			EditorGUI.showMixedValue = tagValue.hasMultipleDifferentValues;
			EditorGUI.BeginChangeCheck();
			var newTag = EditorGUILayoutx.TagField("Tag Value", tagValue.intValue);
			if (EditorGUI.EndChangeCheck()) {
				tagValue.intValue = newTag;
			}
			EditorGUI.indentLevel--;
		}

		if (GUILayout.Button("Tags can be used to restrict which units can walk on what ground. Click here for more info", "HelpBox")) {
			Application.OpenURL(AstarUpdateChecker.GetURL("tags"));
		}
	}

	static void SphereCap (int controlID, Vector3 position, Quaternion rotation, float size) {
#if UNITY_5_5_OR_NEWER
		Handles.SphereHandleCap(controlID, position, rotation, size, Event.current.type);
#else
		Handles.SphereCap(controlID, position, rotation, size);
#endif
	}

	void DrawWorldSpaceField () {
		EditorGUI.showMixedValue = useWorldSpace.hasMultipleDifferentValues;

		EditorGUI.BeginChangeCheck();
		var newWorldSpace = EditorGUILayout.Toggle(new GUIContent("Use World Space", "Specify coordinates in world space or local space. When using local space you can move the GameObject " +
				"around and the points will follow.\n" +
				"Some operations, like calculating the convex hull, and snapping to Y will change axis depending on how the object is rotated if world space is not used."
				), useWorldSpace.boolValue);
		if (EditorGUI.EndChangeCheck()) {
			for (int i = 0; i < scripts.Length; i++) {
				if (scripts[i].useWorldSpace != newWorldSpace) {
					Undo.RecordObject(scripts[i], "switch use-world-space");
					scripts[i].ToggleUseWorldSpace();
				}
			}
		}
		EditorGUI.showMixedValue = false;
	}

	void DrawLockToYField () {
		EditorGUILayout.PropertyField(lockToY, new GUIContent("Lock to Y"));

		if (!lockToY.hasMultipleDifferentValues && lockToY.boolValue) {
			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField(lockToYValue, new GUIContent("Lock to Y value"));
			EditorGUI.indentLevel--;
		}

		for (int i = 0; i < scripts.Length; i++) {
			if (scripts[i].lockToY) {
				scripts[i].LockToY();
			}
		}
	}

	public void OnSceneGUI () {
		var script = target as GraphUpdateScene;

		// Make sure the points array is not null
		script.points = script.points ?? new Vector3[0];

		List<Vector3> points = Pathfinding.Util.ListPool<Vector3>.Claim();
		points.AddRange(script.points);

		Matrix4x4 invMatrix = script.useWorldSpace ? Matrix4x4.identity : script.transform.worldToLocalMatrix;

		if (!script.useWorldSpace) {
			Matrix4x4 matrix = script.transform.localToWorldMatrix;
			for (int i = 0; i < points.Count; i++) points[i] = matrix.MultiplyPoint3x4(points[i]);
		}


		if (Tools.current != Tool.View && Event.current.type == EventType.Layout) {
			for (int i = 0; i < script.points.Length; i++) {
				HandleUtility.AddControl(-i - 1, HandleUtility.DistanceToLine(points[i], points[i]));
			}
		}

		if (Tools.current != Tool.View)
			HandleUtility.AddDefaultControl(0);

		for (int i = 0; i < points.Count; i++) {
			if (i == selectedPoint && Tools.current == Tool.Move) {
				Handles.color = PointSelectedColor;
				Undo.RecordObject(script, "Moved Point");
				SphereCap(-i-1, points[i], Quaternion.identity, HandleUtility.GetHandleSize(points[i])*pointGizmosRadius*2);

				Vector3 pre = points[i];
				Vector3 post = Handles.PositionHandle(points[i], Quaternion.identity);
				if (pre != post) {
					script.points[i] = invMatrix.MultiplyPoint3x4(post);
				}
			} else {
				Handles.color = PointColor;
				SphereCap(-i-1, points[i], Quaternion.identity, HandleUtility.GetHandleSize(points[i])*pointGizmosRadius);
			}
		}

		if (Event.current.type == EventType.MouseDown) {
			int pre = selectedPoint;
			selectedPoint = -(HandleUtility.nearestControl+1);
			if (pre != selectedPoint) GUI.changed = true;
		}

		if (Event.current.type == EventType.MouseDown && Event.current.shift && Tools.current == Tool.Move) {
			if (((int)Event.current.modifiers & (int)EventModifiers.Alt) != 0) {
				if (selectedPoint >= 0 && selectedPoint < points.Count) {
					Undo.RecordObject(script, "Removed Point");
					var arr = new List<Vector3>(script.points);
					arr.RemoveAt(selectedPoint);
					points.RemoveAt(selectedPoint);
					script.points = arr.ToArray();
					script.RecalcConvex();
					GUI.changed = true;
				}
			} else if (((int)Event.current.modifiers & (int)EventModifiers.Control) != 0 && points.Count > 1) {
				int minSeg = 0;
				float minDist = float.PositiveInfinity;
				for (int i = 0; i < points.Count; i++) {
					float dist = HandleUtility.DistanceToLine(points[i], points[(i+1)%points.Count]);
					if (dist < minDist) {
						minSeg = i;
						minDist = dist;
					}
				}

				System.Object hit = HandleUtility.RaySnap(HandleUtility.GUIPointToWorldRay(Event.current.mousePosition));
				if (hit != null) {
					var rayhit = (RaycastHit)hit;
					Undo.RecordObject(script, "Added Point");
					var arr = Pathfinding.Util.ListPool<Vector3>.Claim();
					arr.AddRange(script.points);

					points.Insert(minSeg+1, rayhit.point);
					if (!script.useWorldSpace) rayhit.point = invMatrix.MultiplyPoint3x4(rayhit.point);

					arr.Insert(minSeg+1, rayhit.point);
					script.points = arr.ToArray();
					script.RecalcConvex();
					Pathfinding.Util.ListPool<Vector3>.Release(arr);
					GUI.changed = true;
				}
			} else {
				System.Object hit = HandleUtility.RaySnap(HandleUtility.GUIPointToWorldRay(Event.current.mousePosition));
				if (hit != null) {
					var rayhit = (RaycastHit)hit;

					Undo.RecordObject(script, "Added Point");

					var arr = new Vector3[script.points.Length+1];
					for (int i = 0; i < script.points.Length; i++) {
						arr[i] = script.points[i];
					}
					points.Add(rayhit.point);
					if (!script.useWorldSpace) rayhit.point = invMatrix.MultiplyPoint3x4(rayhit.point);

					arr[script.points.Length] = rayhit.point;
					script.points = arr;
					script.RecalcConvex();
					GUI.changed = true;
				}
			}
			Event.current.Use();
		}

		// Make sure the convex hull stays up to date
		script.RecalcConvex();

		Pathfinding.Util.ListPool<Vector3>.Release(points);

		if (GUI.changed) {
			HandleUtility.Repaint();
			EditorUtility.SetDirty(target);
		}
	}
}
