using UnityEngine;
using UnityEditor;

public abstract class GFGridEditor : Editor {
	protected GFGrid _grid;
	protected bool _showDrawSettings;
	protected bool _showOffsets;

	///<summary> Directory of Doc files. </summary>
	protected static string _docsDir =
		"file://" + Application.dataPath + "/WebPlayerTemplates/GridFrameworkHTMLDocs/html/";

	#region enable <-> disable
	void OnEnable () {
		_grid = target as GFGrid;
		//_showDrawSettings = EditorPrefs.HasKey("GFGridShowDraw") ? EditorPrefs.GetBool("GFGridShowDraw") : true;
		_showDrawSettings = !EditorPrefs.HasKey("GFGridShowDraw") || EditorPrefs.GetBool("GFGridShowDraw");
		//_showOffsets = EditorPrefs.HasKey ("GFGridShowOffset") ? EditorPrefs.GetBool ("GFGridShowOffset") : false;
		_showOffsets = EditorPrefs.HasKey ("GFGridShowOffset") && EditorPrefs.GetBool ("GFGridShowOffset");
	}
	
	void OnDisable(){
		EditorPrefs.SetBool("GFGridShowDraw", _showDrawSettings);
		EditorPrefs.SetBool ("GFGridShowOffset", _showOffsets);
	}
	#endregion
	
	#region OnInspectorGUI()
	public override void OnInspectorGUI () {
		StandardFields ();
		
		if (GUI.changed)
			EditorUtility.SetDirty (target);
	}
	#endregion

	#region Stadard Fields
	public void StandardFields () {
		SizeFields();
		SpacingFields();

		EditorGUILayout.Space();
		ColourFields();

		EditorGUILayout.Space();
		DrawRenderFields();

		EditorGUILayout.Space();
		OffsetFields ();
	}
	#endregion
	
	#region groups of common fields
	protected virtual void SizeFields () {
		_grid.useCustomRenderRange = EditorGUILayout.Toggle("Custom Render Range", _grid.useCustomRenderRange);
		if (_grid.useCustomRenderRange) {
			_grid.renderFrom = EditorGUILayout.Vector3Field("Render From", _grid.renderFrom);
			_grid.renderTo   = EditorGUILayout.Vector3Field("Render To"  , _grid.renderTo  );
		} else {
			_grid.size = EditorGUILayout.Vector3Field("Size", _grid.size);
		}
		_grid.relativeSize = EditorGUILayout.Toggle("Relative Size", _grid.relativeSize);
	}
	
	protected void ColourFields () {		
		GUILayout.Label("Axis Colors");
		
		EditorGUILayout.BeginHorizontal();
		++EditorGUI.indentLevel; {
			_grid.axisColors.x = EditorGUILayout.ColorField(_grid.axisColors.x);
			_grid.axisColors.y = EditorGUILayout.ColorField(_grid.axisColors.y);
			_grid.axisColors.z = EditorGUILayout.ColorField(_grid.axisColors.z);
		}
		--EditorGUI.indentLevel;
		EditorGUILayout.EndHorizontal();
		
		_grid.useSeparateRenderColor = EditorGUILayout.Foldout(_grid.useSeparateRenderColor, "Use Separate Render Color");
		if(_grid.useSeparateRenderColor){
			GUILayout.Label("Render Axis Colors");
			EditorGUILayout.BeginHorizontal(); {
			++EditorGUI.indentLevel;
				_grid.renderAxisColors.x = EditorGUILayout.ColorField(_grid.renderAxisColors.x);
				_grid.renderAxisColors.y = EditorGUILayout.ColorField(_grid.renderAxisColors.y);
				_grid.renderAxisColors.z = EditorGUILayout.ColorField(_grid.renderAxisColors.z);
			}
			--EditorGUI.indentLevel;
		EditorGUILayout.EndHorizontal();
		}		
	}
	
	protected void DrawRenderFields () {
		_showDrawSettings = EditorGUILayout.Foldout(_showDrawSettings, "Draw & Render Settings");
		++EditorGUI.indentLevel;
		if(_showDrawSettings){
			_grid.renderGrid = EditorGUILayout.Toggle("Render Grid", _grid.renderGrid);
			
			_grid.renderMaterial = (Material) EditorGUILayout.ObjectField("Render Material", _grid.renderMaterial, typeof(Material), false);
			_grid.renderLineWidth = EditorGUILayout.IntField("Render Line Width", _grid.renderLineWidth);
			
			_grid.hideGrid = EditorGUILayout.Toggle("Hide Grid", _grid.hideGrid);
			_grid.hideOnPlay = EditorGUILayout.Toggle("Hide On Play", _grid.hideOnPlay);
			++EditorGUI.indentLevel;
			GUILayout.Label("Hide Axis (Render & Draw)");
			_grid.hideAxis.x = EditorGUILayout.Toggle("X", _grid.hideAxis.x);
			_grid.hideAxis.y = EditorGUILayout.Toggle("Y", _grid.hideAxis.y);
			_grid.hideAxis.z = EditorGUILayout.Toggle("Z", _grid.hideAxis.z);
			--EditorGUI.indentLevel;
			
			_grid.drawOrigin = EditorGUILayout.Toggle("Draw Origin", _grid.drawOrigin);
		}
		--EditorGUI.indentLevel;
	}

	protected void OffsetFields () {
		_showOffsets = EditorGUILayout.Foldout (_showOffsets, "Origin Point Offset");
		if (_showOffsets) {
			_grid.originOffset = EditorGUILayout.Vector3Field ("Origin Offset", _grid.originOffset);
		}
	}
	#endregion
	
	#region groups of specific fields (abstract)
	protected abstract void SpacingFields ();
	#endregion
}
