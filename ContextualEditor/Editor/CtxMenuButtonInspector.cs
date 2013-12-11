//=========================================================
// Contextual: Context Menus for Unity & NGUI
// Copyright © 2013 Troy Heere
//=========================================================
using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(CtxMenuButton))]
public class CtxMenuButtonInspector : CtxMenuItemInspector
{
	CtxMenuButton menuButton;
	int itemIndex;
		
	public override void RegisterUndo()
	{
		NGUIEditorTools.RegisterUndo("Menu Button Change", menuButton);
	}

	public override void OnInspectorGUI()
	{
		menuButton = target as CtxMenuButton;
		
		EditorGUIUtility.labelWidth = 120f;
		
		CtxMenu contextMenu = (CtxMenu)EditorGUILayout.ObjectField("Context Menu", menuButton.contextMenu, 
			typeof(CtxMenu), true);
		
		if (menuButton.contextMenu != contextMenu)
		{
			RegisterUndo();
			menuButton.contextMenu = contextMenu;
		}
		
		int sel = EditorGUILayout.IntField("Selected Item", menuButton.selectedItem);
		if (menuButton.selectedItem != sel)
		{
			RegisterUndo();
			menuButton.selectedItem = sel;
		}
		
		UILabel label = (UILabel)EditorGUILayout.ObjectField("Current Item Label", menuButton.currentItemLabel, typeof(UILabel), true);
		if (menuButton.currentItemLabel != label)
		{
			RegisterUndo();
			menuButton.currentItemLabel = label;
		}
		
		UISprite icon = (UISprite)EditorGUILayout.ObjectField("Current Item Icon", menuButton.currentItemIcon, typeof(UISprite), true);
		if (menuButton.currentItemIcon != icon)
		{
			RegisterUndo();
			menuButton.currentItemIcon = icon;
		}

		NGUIEditorTools.DrawEvents("On Selection", menuButton, menuButton.onSelection);
		NGUIEditorTools.DrawEvents("On Show", menuButton, menuButton.onShow);
		NGUIEditorTools.DrawEvents("On Hide", menuButton, menuButton.onHide);

		if (menuButton.contextMenu != null)
		{
			EditMenuItemList(ref menuButton.menuItems, menuButton.contextMenu.atlas, true, ref menuButton.isEditingItems);
		}
		else
		{
			EditorGUILayout.HelpBox("You need to reference a context menu for this component to work properly.",MessageType.Warning);
		}
		
		if (GUI.changed)
			EditorUtility.SetDirty(target);
	}
}
