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
		
		GameObject obj = EditorGUILayout.ObjectField("Event Receiver", popup.eventReceiver, typeof(GameObject), true) as GameObject;
		string func = EditorGUILayout.TextField("Function Name", popup.functionName);

		if (popup.eventReceiver != obj || popup.functionName != func)
		{
			RegisterUndo();
			
			popup.eventReceiver = obj;
			popup.functionName = func;
		}
				
		func = EditorGUILayout.TextField("Show Function", popup.showFunction);
		if (popup.showFunction != func)
		{
			RegisterUndo();
			popup.showFunction = func;
		}
		
		func = EditorGUILayout.TextField("Hide Function", popup.hideFunction);
		if (popup.hideFunction != func)
		{
			RegisterUndo();
			popup.hideFunction = func;
		}
		
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
