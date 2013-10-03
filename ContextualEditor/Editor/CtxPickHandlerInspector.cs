//=========================================================
// Contextual: Context Menus for Unity & NGUI
// Copyright © 2013 Troy Heere
//=========================================================
using UnityEngine;
using UnityEditor;
using System.Collections;

/// <summary>
/// Inspector editor class for CtxPickHandler.
/// </summary>
[CustomEditor(typeof(CtxPickHandler))]
public class CtxPickHandlerInspector : Editor
{
	CtxPickHandler pickHandler;
	
	void RegisterUndo()
	{
		NGUIEditorTools.RegisterUndo("Context Menu Pick Handler Change", pickHandler);
	}

	public override void OnInspectorGUI()
	{
		pickHandler = target as CtxPickHandler;
		
		EditorGUIUtility.LookLikeControls(100f);
		
		int pickLayers = UICameraTool.LayerMaskField("Pick Layers", pickHandler.pickLayers);
		if (pickHandler.pickLayers != pickLayers)
		{
			RegisterUndo();
			pickHandler.pickLayers = pickLayers;
		}
		
		int menuButton = EditorGUILayout.IntField("Menu Button", pickHandler.menuButton);
		if (pickHandler.menuButton != menuButton)
		{
			RegisterUndo();
			pickHandler.menuButton = menuButton;
		}
		
		if (GUI.changed)
			EditorUtility.SetDirty(target);
	}
}
