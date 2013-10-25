//=========================================================
// Contextual: Context Menus for Unity & NGUI
// Copyright © 2013 Troy Heere
//=========================================================
using UnityEngine;
using UnityEditor;
using System.Collections;

/// <summary>
/// Inspector editor class for UIContextObject.
/// </summary>
[CustomEditor(typeof(CtxObject))]
public class CtxObjectInspector : CtxMenuItemInspector
{
	CtxObject contextObject;
		
	public override void RegisterUndo()
	{
		NGUIEditorTools.RegisterUndo("Context Object Change", contextObject);
	}

	public override void OnInspectorGUI()
	{
		contextObject = target as CtxObject;
		
		EditorGUIUtility.LookLikeControls(100f);
		
		CtxMenu contextMenu = (CtxMenu)EditorGUILayout.ObjectField("Context Menu", contextObject.contextMenu, 
			typeof(CtxMenu), true);
		
		if (contextObject.contextMenu != contextMenu)
		{
			RegisterUndo();
			contextObject.contextMenu = contextMenu;
		}
		
		bool offsetMenu = EditorGUILayout.Toggle("Offset Menu", contextObject.offsetMenu);
		if (contextObject.offsetMenu != offsetMenu)
		{
			RegisterUndo();
			contextObject.offsetMenu = offsetMenu;
		}

		NGUIEditorTools.DrawEvents("On Selection", contextObject, contextObject.onSelection);
		NGUIEditorTools.DrawEvents("On Show", contextObject, contextObject.onShow);
		NGUIEditorTools.DrawEvents("On Hide", contextObject, contextObject.onHide);

		if (contextObject.contextMenu != null)
		{
			EditMenuItemList(ref contextObject.menuItems, contextObject.contextMenu.atlas, true, ref contextObject.isEditingItems);
		}
		else
		{
			EditorGUILayout.HelpBox("You need to reference a context menu for this component to work properly.",MessageType.Warning);
		}
		
		if (GUI.changed)
			EditorUtility.SetDirty(target);
	}
}
