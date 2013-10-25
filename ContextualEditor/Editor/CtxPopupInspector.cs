//=========================================================
// Contextual: Context Menus for Unity & NGUI
// Copyright © 2013 Troy Heere
//=========================================================
using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(CtxPopup))]
public class CtxPopupInspector : CtxMenuItemInspector
{
	CtxPopup popup;
	int itemIndex;
		
	public override void RegisterUndo()
	{
		NGUIEditorTools.RegisterUndo("Menu Button Change", popup);
	}

	public override void OnInspectorGUI()
	{
		popup = target as CtxPopup;
		
		EditorGUIUtility.LookLikeControls(120f);
		
		CtxMenu contextMenu = (CtxMenu)EditorGUILayout.ObjectField("Context Menu", popup.contextMenu, 
			typeof(CtxMenu), true);
		
		if (popup.contextMenu != contextMenu)
		{
			RegisterUndo();
			popup.contextMenu = contextMenu;
		}

		NGUIEditorTools.DrawEvents("On Selection", popup, popup.onSelection);
		NGUIEditorTools.DrawEvents("On Show", popup, popup.onShow);
		NGUIEditorTools.DrawEvents("On Hide", popup, popup.onHide);
		
		if (popup.contextMenu != null)
		{
			EditMenuItemList(ref popup.menuItems, popup.contextMenu.atlas, true, ref popup.isEditingItems);
		}
		else
		{
			EditorGUILayout.HelpBox("You need to reference a context menu for this component to work properly.",MessageType.Warning);
		}
		
		if (GUI.changed)
			EditorUtility.SetDirty(target);
	}
}
