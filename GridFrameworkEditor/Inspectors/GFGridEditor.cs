using UnityEngine;
using UnityEditor;
using System.Collections;

public abstract class GFGridEditor : Editor {
	protected GFGrid grid;
	protected bool showDrawSettings;
	protected bool showOffsets;
	protected static string docsDir = "file://" + Application.dataPath + "/WebPlayerTemplates/GridFrameworkHTMLDocs/html/"; //directory of doc files

	#region enable <-> disable
	void OnEnable () {
		grid = target as GFGrid;
		showDrawSettings = EditorPrefs.HasKey("GFGridShowDraw") ? EditorPrefs.GetBool("GFGridShowDraw") : true;
		showOffsets = EditorPrefs.HasKey ("GFGridShowOffset") ? EditorPrefs.GetBool ("GFGridShowOffset") : false;
	}
	
	void OnDisable(){
		EditorPrefs.SetBool("GFGridShowDraw", showDrawSettings);
		EditorPrefs.SetBool ("GFGridShowOffset", showOffsets);
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
		grid.relativeSize = EditorGUILayout.Toggle("Relative Size", grid.relativeSize);
		grid.size = EditorGUILayout.Vector3Field("Size", grid.size);
	}
	
	protected void ColourFields () {		
		GUILayout.Label("Axis Colors");
		
		EditorGUILayout.BeginHorizontal();
		++EditorGUI.indentLevel;
		grid.axisColors.x = EditorGUILayout.ColorField(grid.axisColors.x);
		grid.axisColors.y = EditorGUILayout.ColorField(grid.axisColors.y);
		grid.axisColors.z = EditorGUILayout.ColorField(grid.axisColors.z);
		--EditorGUI.indentLevel;
		EditorGUILayout.EndHorizontal();
		
		grid.useSeparateRenderColor = EditorGUILayout.Foldout(grid.useSeparateRenderColor, "Use Separate Render Color");
		if(grid.useSeparateRenderColor){
			GUILayout.Label("Render Axis Colors");
			EditorGUILayout.BeginHorizontal();
			++EditorGUI.indentLevel;
			grid.renderAxisColors.x = EditorGUILayout.ColorField(grid.renderAxisColors.x);
			grid.renderAxisColors.y = EditorGUILayout.ColorField(grid.renderAxisColors.y);
			grid.renderAxisColors.z = EditorGUILayout.ColorField(grid.renderAxisColors.z);
			--EditorGUI.indentLevel;
		EditorGUILayout.EndHorizontal();
		}		
	}
	
	protected void DrawRenderFields () {
		showDrawSettings = EditorGUILayout.Foldout(showDrawSettings, "Draw & Render Settings");
		++EditorGUI.indentLevel;
		if(showDrawSettings){
			grid.renderGrid = EditorGUILayout.Toggle("Render Grid", grid.renderGrid);
			
			grid.useCustomRenderRange = EditorGUILayout.Foldout(grid.useCustomRenderRange, "Use Custom Render Range");
			if(grid.useCustomRenderRange){
				grid.renderFrom = EditorGUILayout.Vector3Field("Render From", grid.renderFrom);
				grid.renderTo = EditorGUILayout.Vector3Field("Render To", grid.renderTo);
			}
			
			grid.renderMaterial = (Material) EditorGUILayout.ObjectField("Render Material", grid.renderMaterial, typeof(Material), false);
			grid.renderLineWidth = EditorGUILayout.IntField("Render Line Width", grid.renderLineWidth);
			
			grid.hideGrid = EditorGUILayout.Toggle("Hide Grid", grid.hideGrid);
			grid.hideOnPlay = EditorGUILayout.Toggle("Hide On Play", grid.hideOnPlay);
			++EditorGUI.indentLevel;
			GUILayout.Label("Hide Axis (Render & Draw)");
			grid.hideAxis.x = EditorGUILayout.Toggle("X", grid.hideAxis.x);
			grid.hideAxis.y = EditorGUILayout.Toggle("Y", grid.hideAxis.y);
			grid.hideAxis.z = EditorGUILayout.Toggle("Z", grid.hideAxis.z);
			--EditorGUI.indentLevel;
			
			grid.drawOrigin = EditorGUILayout.Toggle("Draw Origin", grid.drawOrigin);
		}
		--EditorGUI.indentLevel;
	}

	protected void OffsetFields () {
		showOffsets = EditorGUILayout.Foldout (showOffsets, "Origin Point Offset");
		if (showOffsets) {
			grid.originOffset = EditorGUILayout.Vector3Field ("Origin Offset", grid.originOffset);
		}
	}
	#endregion
	
	#region groups of specific fields (abstract)
	protected abstract void SpacingFields ();
	#endregion
}
