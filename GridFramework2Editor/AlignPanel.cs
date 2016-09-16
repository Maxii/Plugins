using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using GridFramework.Grids;
using GridFramework.Extensions.Align;
using GridFramework.Extensions.Scale;

// THINGS NEEDED
//   - being able to ignore certain kinds of objects (like cameras)
//   - implement offsets

namespace GridFramework.Editor {

	/// <summary>
	///   Editor extension for aligning and scaling objects along a grid.
	/// </summary>
	public class AlignPanel : EditorWindow {
#region  Private variables
		///<summary>
		///  The rectangular grid to align objects to.
		///</summary>
		[SerializeField]
		private RectGrid _rectGrid;

		///<summary>
		///  The hexagonal grid to align objects to.
		///</summary>
		[SerializeField]
		private HexGrid _hexGrid;

		/// <summary>
		///   Whether to ignore objects with no parent.
		/// </summary>
		[SerializeField]
		private bool _ignoreRootObjects;

		/// <summary>
		///   Whether to also include the children of objects.
		/// </summary>
		[SerializeField]
		private bool _inculdeChildren;

		[SerializeField]
		private bool _rotateTransform = true;

		/// <summary>
		///   Whether to auto snap objects while dragging.
		/// </summary>
		[SerializeField]
		private bool _autoSnapping; 

		/// <summary>
		///   Which layers of the scene is are affected.
		/// </summary>
		[SerializeField]
		private LayerMask _affectedLayers;

		/// <summary>
		///   Whether to ignore the X-axis.
		/// </summary>
		private bool _ignoreX;

		/// <summary>
		///   Whether to ignore the Y-axis.
		/// </summary>
		private bool _ignoreY;

		/// <summary>
		///   Whether to ignore the Z-axis.
		/// </summary>
		private bool _ignoreZ;
#endregion  // Private variables

#region  Properties
		/// <summary>
		///   Whether any grid is present at all.
		/// </summary>
		/// <remarks>
		///   <para>
		///     The exact grid returned depends on which grid is the first one
		///     that is not <c>null</c>, but since only one of them can be
		///     non-<c>null</c> at a time it doesn't matter.
		///   </para>
		/// </remarks>
		private Grid AnyGrid {
			get {
				return (Grid)_rectGrid ?? (Grid)_hexGrid;
			}
		}

		/// <summary>
		///   The <c>Transform</c> of the active grid.
		/// </summary>
		private Transform GridTransfrom {
			get {
				var grid = AnyGrid;
				return grid ? grid.transform : null;
			}
		}

		/// <summary>
		///   The ignored axes as an array.
		/// </summary>
		private bool[] IgnoredAxes {
			get {
				return new []{_ignoreX, _ignoreY, _ignoreZ};
			}
		}
#endregion  // Properties

#region  Callback methods
		void Update(){
			if(!AnyGrid || Selection.transforms.Length == 0 || !_autoSnapping) {
				return;
			}
			
			var ts = Selection.transforms;
			foreach (var t in ts) {
				var notThis    = t != GridTransfrom;
				var notNull    = t != null;
				var notAligned = !AlreadyAligned(t);

				if (notThis && notNull && notAligned) {
					AlignTransform(t, _rotateTransform);
				}
			}
		}

		void OnGUI(){
			GridField();
			LayerField();
			RotateOptions ();
			InclusionOptions();
			AlignButtons();
			if (_rectGrid) {
				ScaleButtons();
			}
			EditorGUILayout.BeginHorizontal();
			AutoSnapFlag();	
			EditorGUILayout.EndHorizontal();
			AxisOptions();
		}
#endregion  // Callback methods

#region  GUI items
		private void GridField() {
			const string label = "Grid:";
			var currentGrid = (Grid)_rectGrid ?? (Grid)_hexGrid;
			var grid = EditorGUILayout.ObjectField(label, currentGrid, typeof(Grid), true);
			_rectGrid = grid as RectGrid;
			_hexGrid  = grid as HexGrid;
		}
		
		private void LayerField() {
			const string label = "Affected Layers";
			_affectedLayers = LayerMaskField(label, _affectedLayers);
		}
	
		private void RotateOptions() {
			const string label = "Rotate to Grid";
			_rotateTransform = EditorGUILayout.Toggle(label, _rotateTransform);
		}
		
		private void InclusionOptions() {
			const string label1 = "Ignore Root Objects";
			const string label2 = "Include Children";
			_ignoreRootObjects = EditorGUILayout.Toggle(label1, _ignoreRootObjects);
			_inculdeChildren   = EditorGUILayout.Toggle(label2, _inculdeChildren);		
		}
		
		private void AlignButtons() {
			const string label1 = "Align Scene";
			const string label2 = "Align Selected";
			EditorGUILayout.BeginHorizontal();
			if(GUILayout.Button(label1)){
				AlignScene();
			}
			if(GUILayout.Button(label2)){
				AlignSelected();
			}
			EditorGUILayout.EndHorizontal();
			
		}
		
		private void ScaleButtons() {
			const string label1 = "Scale Scene";
			const string label2 = "Scale Selected";
			if (!_rectGrid) {
				return;
			}

			EditorGUILayout.BeginHorizontal();
			if(GUILayout.Button(label1)){
				ScaleScene();
			}
			if(GUILayout.Button(label2)){
				ScaleSelected();
			}
			EditorGUILayout.EndHorizontal();
		}
		
		private void AutoSnapFlag() {
			const string label = "Auto-Snapping";
			_autoSnapping = EditorGUILayout.Toggle(label, _autoSnapping);
		}
		
		private void AxisOptions() {	
			const string label = "Ignore these axes";
			GUILayout.Label(label);
			++EditorGUI.indentLevel;
			_ignoreX = EditorGUILayout.Toggle("X", _ignoreX);
			_ignoreY = EditorGUILayout.Toggle("Y", _ignoreY);
			_ignoreZ = EditorGUILayout.Toggle("Z", _ignoreZ);
			--EditorGUI.indentLevel;
		}
#endregion  // GUI items

#region  Menu methods
		[MenuItem("Window/Grid Align Panel")]
		public static void Init(){
			GetWindow(typeof(AlignPanel), false, "Grid Align Panel");	
		}
#endregion  // Menu methods

#region  Align commands
		private void AlignScene() {
			const string label = "Align Scene";
			var ts = Object.FindObjectsOfType(typeof(Transform)) as Transform[];
			var l = new List<Transform>(ts);
			AlignTransfroms(l, label);
		}
		
		private void AlignSelected() {
			const string label = "Align Selection";
			var ts = Selection.transforms;
			var l = new List<Transform>((Transform[])ts);
			AlignTransfroms(l, label);
		}
	
		private void AlignTransfroms(List<Transform> ts, string name) {
			if(!AnyGrid) {
				return;
			}

			RemoveAligned(ref ts);

			if(ts.Count == 0) {
				return;
			}

			Undo.RecordObjects(ts.ToArray(), name);
			foreach (var t in ts) {
				var layer = t.gameObject.layer;
				var noParent = t.parent == null;
				var hasChildren = t.childCount > 0;
				var ignoredRoot = !(_ignoreRootObjects && noParent && hasChildren);
				var affecedLayer = (_affectedLayers.value & 1 << layer) != 0;

				if(ignoredRoot && affecedLayer){
					AlignTransform(t, _rotateTransform);
					if (_inculdeChildren) {
						foreach (Transform child in t) {
							AlignTransform(child, _rotateTransform);
						}
					}
				}
				EditorUtility.SetDirty(t);
			}
		}
#endregion  // Align commands

#region  Scale commands
		private void ScaleScene() {
			const string label = "Scale Scene";
			var ts = Object.FindObjectsOfType(typeof(Transform));
			var l  = new List<Transform>((Transform[])ts);
			ScaleTransforms(l, label);
		}
		
		private void ScaleSelected() {
			const string label = "Scale Selected";
			var ts = Selection.transforms;
			var l  = new List<Transform>((Transform[])ts);
			ScaleTransforms(l, label);
		}	

		private void ScaleTransforms(List<Transform> ts, string name) {
			if(!_rectGrid) {
				return;
			}

			ts.Remove(_rectGrid.transform);
			RemoveScaled(ref ts);
			Undo.RecordObjects(ts.ToArray(), name);
			foreach(var t in ts){
				var layer = t.gameObject.layer;
				var noParent = t.parent == null;
				var hasChildren = t.childCount > 0;
				var ignoredRoot = !(_ignoreRootObjects && noParent && hasChildren);
				var affecedLayer = (_affectedLayers.value & 1 << layer) != 0;

				if(ignoredRoot && affecedLayer){
					ScaleTransform(t);
					if(_inculdeChildren){
						foreach(Transform child in t){
							ScaleTransform(child);
						}
					}
				}
				EditorUtility.SetDirty (t);
			}
		}
#endregion  // Scale commands

#region  Verification methods
		/// <summary>
		///   Whether a <c>Transform</c> is already aligned.
		/// </summary>
		private bool AlreadyAligned(Transform t){
			var p = t.position;
			Vector3 aligned;
			if (_rectGrid) {
				var s = t.lossyScale;
				aligned = _rectGrid.AlignVector3(p, s);
			} else if (_hexGrid) {
				aligned = _hexGrid.AlignVector3(p);
			} else {
				const string message = "Only rectangular or hexagonal grids supported.";
				throw new System.NotSupportedException(message);
			}
			var inPlace = (p - aligned).sqrMagnitude <= Mathf.Epsilon;
			var deltaAngle = Quaternion.Angle(GridTransfrom.rotation, t.rotation);
			var rotated = !_rotateTransform || deltaAngle <= Mathf.Epsilon;
			return inPlace && rotated;
		}

		private bool AlreadyScaled(Transform t) {
			var scale = t.lossyScale;
			var spacing = _rectGrid.Spacing;
			for (var i = 0; i < 3; ++i) {
				var remainder = scale[i] % spacing[i];
				if (remainder > Mathf.Epsilon) {
					return false;
				}
			}
			return true;
		}
#endregion  // Verification methods

#region  Data transformation methods
		/// <summary>
		///   Remove already aligned <c>Transform</c> objects from the list.
		/// </summary>
		private void RemoveAligned(ref List<Transform> ts){
			// We'll keep a counter for the amount of objects in the list to
			// avoid calling transformList.Count each iteration.
			int count = ts.Count;
			for (var i = 0; i < count; ++i){
				var t = ts[i];
				if (AlreadyAligned(t)){
					ts.RemoveAt(i);
					--i; // Reduce the indexer because we removed an entry from list
					--count; // Reduce the count since the list has become smaller
				}
			}
			
			ts.Remove(AnyGrid.transform);
		}

		private void RemoveScaled(ref List<Transform> ts) {
			int count = ts.Count;
			for (var i = 0; i < count; ++i){
				var t = ts[i];
				if (AlreadyScaled(t)) {
					ts.RemoveAt(i);
					--i; // Reduce the indexer because we removed an entry from list
					--count; // Reduce the count since the list has become smaller
				}
			}
		}
#endregion  // Data transformation methods

#region  Actual Align & Scale
		private void AlignTransform(Transform t, bool rotate) {
			if (rotate) {
				t.rotation = GridTransfrom.rotation;
			}
			var oldPosition = t.position;
			if (_rectGrid) {
				_rectGrid.AlignTransform(t);
			} else if (_hexGrid) {
				_hexGrid.AlignTransform(t);
			} else {
				throw new System.NullReferenceException("No grid set");
			}

			for (var i = 0; i < 3; ++i) {
				if (IgnoredAxes[i]) {
					var newPosition = t.position;
					newPosition[i] = oldPosition[i];
					t.position = newPosition;
				}
			}
		}
	
		private void ScaleTransform(Transform t) {
			var oldScale = t.localScale;
			_rectGrid.ScaleTransform(t);

			for (var i = 0; i < 3; ++i) {
				if (IgnoredAxes[i]) {
					var newScale = t.localScale;
					newScale[i] = oldScale[i];
					t.localScale = newScale;
				}
			}
		}
#endregion  // Actual Align & Scale
		
#region  LayerMask
		public static LayerMask LayerMaskField(string label, LayerMask mask) {
	    	return LayerMaskField(label, mask, true);
		}
	
		public static LayerMask LayerMaskField (string label, LayerMask mask, bool showSpecial) {
		    var layers       = new List<string>();
			var layerNumbers = new List<int>();
	
			string selectedLayers = "";
	
			for (var i = 0; i < 32; ++i) {
				var layerName = LayerMask.LayerToName(i);
	
				if (layerName != "") {
					if (mask == (mask | (1 << i))) {
						selectedLayers = selectedLayers == "" ? layerName : "Mixed";
					}
				}
			}

			var eventIsMouseDown  = Event.current.type == EventType.MouseDown;
			var eventIsExecuteCmd = Event.current.type == EventType.ExecuteCommand;
	
			if (!eventIsMouseDown && !eventIsExecuteCmd) {
				if (mask.value == 0) {
					layers.Add("Nothing");
				} else if (mask.value == -1) {
					layers.Add("Everything");
				} else {
					layers.Add(selectedLayers);
				}
				layerNumbers.Add(-1);
			}
	
			if (showSpecial) {
				layers.Add((mask.value == 0 ? "\u2713 " : "     ") + "Nothing");
				layerNumbers.Add(-2);
	
				layers.Add((mask.value == -1 ? "\u2713 " : "     ") + "Everything");
				layerNumbers.Add(-3);
			}
	
			for (var i = 0; i < 32; ++i) {
				var layerName = LayerMask.LayerToName(i);
				if (layerName != "") {
					if (mask == (mask | (1 << i))) {
						layers.Add ("\u2713 " + layerName);
					} else {
						layers.Add ("     " + layerName);
					}
					layerNumbers.Add(i);
				}
			}
	
			var newSelected = 0;
	
			if (eventIsMouseDown) {
				newSelected = -1;
			}
	
			var style = EditorStyles.layerMaskField;
			newSelected = EditorGUILayout.Popup(label, newSelected, layers.ToArray(), style);
	
			if (GUI.changed && newSelected >= 0) {
				if (showSpecial && newSelected == 0) {
					mask = 0;
				} else if (showSpecial && newSelected == 1) {
					mask = -1;
				} else {
					if (mask == (mask | (1 << layerNumbers[newSelected]))) {
						mask &= ~(1 << layerNumbers[newSelected]);
					} else {
						mask = mask | (1 << layerNumbers[newSelected]);
					}
				}
			}

			return mask;
		}
#endregion  // LayerMask
	}
}
