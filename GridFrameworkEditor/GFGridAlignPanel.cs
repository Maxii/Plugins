using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;


/* THINGS NEEDED

- being able to ignore certain kinds of objects (like cameras)
- implement offsets
*/ 

public class GFGridAlignPanel : EditorWindow {
	#region class members
	GFGrid grid;
	private Transform gridTransform;
	private Transform gTrnsfrm {
		get {
			if(grid && !gridTransform)
				gridTransform = grid.transform;
			return gridTransform;
		}
	}
	bool ignoreRootObjects;
	LayerMask affectedLayers;
	bool inculdeChildren;
	bool rotateTransform = true;
	bool autoSnapping = false;
	bool autoRotating = false;
	GFBoolVector3 lockAxes = new GFBoolVector3(false);

	private bool showOffsets = false;
	public Vector3 alignOffset = Vector3.zero;
	public Vector3 scaleOffset = Vector3.zero;
	#endregion
	
	[MenuItem("Window/Grid Align Panel")]
	public static void Init(){
		GetWindow(typeof(GFGridAlignPanel), false, "Grid Align Panel");	
	}
	
	void OnGUI(){
		GridField();
		LayerField();
		RotateOptions ();
		InclusionOptions();
		AlignButtons();
		if(grid)
			AlignRotateButtons();
		ScaleButtons();
		//OffsetFoldout ();
		EditorGUILayout.BeginHorizontal();
		AutoSnapFlag();	
		if(grid)
			AutoRotateFlag();
		EditorGUILayout.EndHorizontal();
		AxisOptions();
	}
	
	#region panel fields
	void GridField () {
		grid = (GFGrid) EditorGUILayout.ObjectField("Grid:", grid, typeof(GFGrid), true);
	}
	
	void LayerField (){
		affectedLayers = LayerMaskField("Affected Layers", affectedLayers);
	}

	void RotateOptions () {
		rotateTransform = EditorGUILayout.Toggle ("Rotate to Grid", rotateTransform);
	}
	
	void InclusionOptions () {
		ignoreRootObjects = EditorGUILayout.Toggle("Ignore Root Objects", ignoreRootObjects);
		inculdeChildren = EditorGUILayout.Toggle("Include Children", inculdeChildren);		
	}
	
	void AlignButtons () {
		EditorGUILayout.BeginHorizontal();
		if(GUILayout.Button("Align Scene")){
			AlignScene();
		}
		if(GUILayout.Button("Align Selected")){
			AlignSelected();
		}
		EditorGUILayout.EndHorizontal();
		
	}
	
	void ScaleButtons () {
		EditorGUILayout.BeginHorizontal();
		if(GUILayout.Button("Scale Scene")){
			ScaleScene();
		}
		if(GUILayout.Button("Scale Selected")){
			ScaleSelected();
		}
		EditorGUILayout.EndHorizontal();
	}

	void OffsetFoldout () {
		showOffsets = EditorGUILayout.Foldout (showOffsets, "Offests");
		if (showOffsets) {
			alignOffset = EditorGUILayout.Vector3Field ("Align Offset", alignOffset);
			scaleOffset = EditorGUILayout.Vector3Field ("Scale Offset", scaleOffset);
		}
	}
	
	void AutoSnapFlag () {
		autoSnapping = EditorGUILayout.Toggle("Auto-Snapping", autoSnapping);
	}
	
	void AxisOptions () {	
		GUILayout.Label("Lock axes for Aligning");
		++EditorGUI.indentLevel;
		lockAxes[0] = EditorGUILayout.Toggle("X", lockAxes[0]);
		lockAxes[1] = EditorGUILayout.Toggle("Y", lockAxes[1]);
		lockAxes[2] = EditorGUILayout.Toggle("Z", lockAxes[2]);
		--EditorGUI.indentLevel;
	}
	#endregion
	
	#region polar panel fields & methods
	#region panels
	void AutoRotateFlag () {
		if(grid.GetType() != typeof(GFPolarGrid))
			return;
		autoRotating = EditorGUILayout.Toggle("Auto-Rotating", autoRotating);
	}

	void AlignRotateButtons () {
		if(grid.GetType() != typeof(GFPolarGrid))
			return;
		EditorGUILayout.BeginHorizontal();
		if(GUILayout.Button("Align & Rotate Scene"))
			AlignRotateScene((GFPolarGrid)grid);
		if(GUILayout.Button("Align & Rotate Selected"))
			AlignRotateSelected((GFPolarGrid)grid);
		EditorGUILayout.EndHorizontal();
	}
	#endregion

	#region methods
	void RotateTransform (GFPolarGrid  pGrid, Transform theTransform){
		theTransform.rotation = pGrid.World2Rotation (theTransform.position);
	}
	
	void AlignRotateTransforms (GFPolarGrid pGrid, List<Transform> allTransforms) {
		foreach(Transform curTransform in allTransforms){
			if(!(ignoreRootObjects && curTransform.parent == null && curTransform.childCount > 0) && (affectedLayers.value & 1<<curTransform.gameObject.layer) != 0){
				pGrid.AlignRotateTransform(curTransform, lockAxes);
				if(inculdeChildren){
					foreach(Transform child in curTransform){
						pGrid.AlignRotateTransform(child, lockAxes);
					}
				}
			}
		}
	}

	void AlignRotateSomething (GFPolarGrid pGrid, List<Transform> allTransforms, string name) {
		RemoveAlignedRotated(ref allTransforms, pGrid);
		if(allTransforms.Count == 0)
			return;
		//Undo.RegisterSceneUndo(name);
		Undo.RecordObjects (allTransforms.ToArray (), name);
		AlignRotateTransforms (pGrid, allTransforms);
		foreach (Transform t in allTransforms)
			EditorUtility.SetDirty (t);
	}

	void AlignRotateSelected (GFPolarGrid pGrid) {
		AlignRotateSomething (pGrid, new List<Transform>((Transform[])Selection.transforms), "Align & Rotate Selected");
	}
	
	void AlignRotateScene (GFPolarGrid pGrid) {
		AlignRotateSomething (pGrid, new List<Transform>((Transform[])Transform.FindObjectsOfType(typeof(Transform))), "Align & Rotate Scene");
	}
	
	bool AlreadyRotated (Transform trans, GFPolarGrid pGrid) {
		return Quaternion.Angle (trans.rotation, pGrid.World2Rotation (trans.position)) < 0.1;
	}

	private bool AlreadyAlignedRotated (Transform trans, GFPolarGrid pGrid) {
		return AlreadyAligned(trans) && AlreadyRotated(trans, pGrid);
	}
	
	private void RemoveAlignedRotated (ref List<Transform> transformList, GFPolarGrid pGrid) {
		//we'll keep a counter for the amount of objects in the list to avoid calling transformList.Count each iteration
		int counter = transformList.Count;
		for(int i = 0; i <= counter - 1; i++){
			if(AlreadyAlignedRotated(transformList[i], pGrid)){
				transformList.RemoveAt(i);
				i --; //reduce the indexer because we removed an entry from list
				counter --; //reduce the counter since the list has become smaller
			}
		}
		
		transformList.Remove(grid.transform);
	}
	#endregion
	#endregion
	
	#region update
	void Update(){
		if(!grid || Selection.transforms.Length == 0)
			return;
		
		if(autoSnapping){
			foreach(Transform selectedTransform in Selection.transforms){
				if(selectedTransform != gTrnsfrm){
					if (selectedTransform)
						if (!AlreadyAligned (selectedTransform))
							AlignTransform (selectedTransform, true);
				}
			}
		}
		if(autoRotating && grid.GetType() == typeof(GFPolarGrid)){
			foreach (Transform selectedTransform in Selection.transforms){
				if(selectedTransform != gTrnsfrm){
					if (selectedTransform)
						if (!AlreadyRotated (selectedTransform, (GFPolarGrid)grid))
							RotateTransform ((GFPolarGrid)grid, selectedTransform);
				}
			}
		}
	}
	#endregion
	
	#region align commands
	void AlignScene(){
		AlignSomething (new List<Transform>((Transform[])Transform.FindObjectsOfType(typeof(Transform))), "Align Scene");
	}
	
	void AlignSelected(){
		AlignSomething(new List<Transform>((Transform[])Selection.transforms), "Align Selected");
	}

	void AlignSomething (List<Transform> allTransforms, string name) {
		if(!grid)
			return;
		RemoveAligned(ref allTransforms);
		if(allTransforms.Count == 0)
			return;
		//Undo.RegisterSceneUndo(name);
		Undo.RecordObjects (allTransforms.ToArray (), name);
		AlignTransforms (allTransforms);
		foreach (Transform t in allTransforms)
			EditorUtility.SetDirty (t);
	}

	void AlignTransforms (List<Transform> allTransforms) {
		foreach(Transform curTransform in allTransforms){
			if(!(ignoreRootObjects && curTransform.parent == null && curTransform.childCount > 0) && (affectedLayers.value & 1<<curTransform.gameObject.layer) != 0){
				AlignTransform (curTransform, true);
				if(inculdeChildren){
					foreach(Transform child in curTransform){
						AlignTransform (child, rotateTransform);
					}
				}
			}
		}
	}

	private bool AlreadyAligned(Transform trans){
		return (trans.position - grid.AlignVector3(trans.position, trans.lossyScale) + alignOffset).sqrMagnitude < 0.0001;
	}
	
	private void RemoveAligned(ref List<Transform> transformList){
		//we'll keep a counter for the amount of objects in the list to avoid calling transformList.Count each iteration
		int counter = transformList.Count;
		for(int i = 0; i <= counter - 1; i++){
			if(AlreadyAligned(transformList[i])){
				transformList.RemoveAt(i);
				i --; //reduce the indexer because we removed an entry from list
				counter --; //reduce the counter since the list has become smaller
			}
		}
		
		transformList.Remove(grid.transform);
	}
	#endregion
	
	#region scale commands
	void ScaleSomething (List<Transform> allTransforms, string name) {
		if(!grid)
			return;
		allTransforms.Remove(grid.transform);
		//Undo.RegisterSceneUndo(name);
		Undo.RecordObjects (allTransforms.ToArray (), name);
		ScaleTransforms (allTransforms);
		foreach (Transform t in allTransforms)
			EditorUtility.SetDirty (t);
	}

	void ScaleTransforms (List<Transform> allTransforms) {
		foreach(Transform curTransform in allTransforms){
			if(!(ignoreRootObjects && curTransform.parent == null && curTransform.childCount > 0) && (affectedLayers.value & 1<<curTransform.gameObject.layer) != 0){
				ScaleTransform (curTransform);
			}
			if(inculdeChildren){
				foreach(Transform child in curTransform){
					ScaleTransform (child);
				}
			}
		}
	}

	void ScaleScene(){
		ScaleSomething(new List<Transform>((Transform[])Transform.FindObjectsOfType(typeof(Transform))), "Scale Scene");
	}
	
	void ScaleSelected(){
		ScaleSomething(new List<Transform>((Transform[])Selection.transforms), "Scale Selected");
	}	
	#endregion

	#region Actual Align & Scale
	void AlignTransform (Transform t, bool rotate = true) {
		grid.AlignTransform (t, rotate, lockAxes);
		t.position += alignOffset;
	}

	void ScaleTransform (Transform t) {
		grid.ScaleTransform (t, lockAxes);
		t.localScale += scaleOffset;
	}
	#endregion
	
	#region LayerMask
	public static LayerMask LayerMaskField (string label, LayerMask selected) {
    	return LayerMaskField (label,selected,true);
	}

	public static LayerMask LayerMaskField (string label, LayerMask selected, bool showSpecial) {

	    List<string> layers = new List<string>();
		List<int> layerNumbers = new List<int>();

		string selectedLayers = "";

		for (int i=0;i<32;i++) {
			string layerName = LayerMask.LayerToName (i);

			if (layerName != "") {
				if (selected == (selected | (1 << i))) {
					if (selectedLayers == "") {
						selectedLayers = layerName;
					} else {
						selectedLayers = "Mixed";
					}
				}
			}
		}

//		EventType lastEvent = Event.current.type; //used for debugging

		if (Event.current.type != EventType.MouseDown && Event.current.type != EventType.ExecuteCommand) {
			if (selected.value == 0) {
				layers.Add ("Nothing");
			} else if (selected.value == -1) {
				layers.Add ("Everything");
			} else {
				layers.Add (selectedLayers);
			}
			layerNumbers.Add (-1);
		}

		if (showSpecial) {
			layers.Add ((selected.value == 0 ? "\u2713 " : "     ") + "Nothing");
			layerNumbers.Add (-2);

			layers.Add ((selected.value == -1 ? "\u2713 " : "     ") + "Everything");
			layerNumbers.Add (-3);
		}

		for (int i=0;i<32;i++) {

			string layerName = LayerMask.LayerToName (i);

			if (layerName != "") {
				if (selected == (selected | (1 << i))) {
					layers.Add ("\u2713 "+layerName);
				} else {
					layers.Add ("     "+layerName);
				}
				layerNumbers.Add (i);
			}
		}

		bool preChange = GUI.changed;

		GUI.changed = false;

		int newSelected = 0;

		if (Event.current.type == EventType.MouseDown) {
			newSelected = -1;
		}

		newSelected = EditorGUILayout.Popup (label,newSelected,layers.ToArray(),EditorStyles.layerMaskField);

		if (GUI.changed && newSelected >= 0) {
			//newSelected -= 1;

//			Debug.Log (lastEvent +" "+newSelected + " "+layerNumbers[newSelected]);

			if (showSpecial && newSelected == 0) {
				selected = 0;
			} else if (showSpecial && newSelected == 1) {
				selected = -1;
			} else {

				if (selected == (selected | (1 << layerNumbers[newSelected]))) {
					selected &= ~(1 << layerNumbers[newSelected]);
					//Debug.Log ("Set Layer "+LayerMask.LayerToName (LayerNumbers[newSelected]) + " To False "+selected.value);
				} else {
					//Debug.Log ("Set Layer "+LayerMask.LayerToName (LayerNumbers[newSelected]) + " To True "+selected.value);
					selected = selected | (1 << layerNumbers[newSelected]);
				}
			}
		} else {
			GUI.changed = preChange;
		}
	
//	Debug.Log(selected.value);
	return selected;
	}
	#endregion
}