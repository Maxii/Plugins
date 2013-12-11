//=========================================================
// Contextual: Context Menus for Unity & NGUI
// Copyright © 2013 Troy Heere
//=========================================================
using UnityEngine;
using UnityEditor;
using System.Collections;

public abstract class CtxMenuItemInspector : Editor
{
	public abstract void RegisterUndo();
	
	protected CtxMenu.Item currentItem;
	protected GUILayoutOption[] itemSpriteOpt = { GUILayout.Height(16f), GUILayout.Width(140f) };
	protected GUILayoutOption[] itemSpriteDelOpt = { GUILayout.Height(16f), GUILayout.Width(60f) };
	
	protected void OnItemIcon(string spriteName)
	{
		RegisterUndo();

		if (currentItem != null)
			currentItem.icon = spriteName;
		
		Repaint();
	}
	
	protected int NextHighestItemID(ref CtxMenu.Item[] items)
	{
		int nextID = -1;
		if (items != null)
		{
			foreach (CtxMenu.Item item in items)
			{
				if (item.id > nextID)
					nextID = item.id;
			}
		}
		
		return nextID+1;
	}
	
	protected void AddInsertMenuItems(ref CtxMenu.Item[] items)
	{
		int inserts = 0;
		
		if (items != null)
		{
			for (int idx=0; idx < items.Length; idx++)
			{
				if (items[idx].isSelected)
				{
					items[idx].isSelected = false;
					CtxMenu.Item newItem = new CtxMenu.Item();
					newItem.id = NextHighestItemID(ref items);
					ArrayUtility.Insert<CtxMenu.Item>(ref items, idx, newItem);
					++inserts;
				}
			}
		}

		if (inserts == 0)
		{
			CtxMenu.Item newItem = new CtxMenu.Item();
			newItem.id = NextHighestItemID(ref items);
			if (items == null)
			{
				items = new CtxMenu.Item[1];
				items[0] = newItem;
			}
			else
				ArrayUtility.Add<CtxMenu.Item>(ref items, newItem);
		}
	}
	
	protected void DeleteMenuItems(ref CtxMenu.Item[] items)
	{
		for (int i=0, cnt=items.Length; i<cnt; )
		{
			if (items[i].isSelected)
			{
				ArrayUtility.RemoveAt(ref items, i);
				cnt--;
			}
			else
				i++;
		}
	}
	
	protected void EditMenuItemList(ref CtxMenu.Item[] menuItems, UIAtlas atlas, bool showHelp, ref bool editItems)
	{
		EditorGUILayout.BeginHorizontal();
		editItems = EditorGUILayout.Foldout(editItems, "Menu Items:");
		
		if (editItems)
		{
			if (GUILayout.Button("+", GUILayout.Width(50f)))
			{
				RegisterUndo();
				AddInsertMenuItems(ref menuItems);
				EditorUtility.SetDirty(target);
			}
			
			if (GUILayout.Button("-", GUILayout.Width(50f)))
			{
				if (SelectedItemCount(ref menuItems) > 0)
				{
					RegisterUndo();
					DeleteMenuItems(ref menuItems);
					EditorUtility.SetDirty(target);
				}
			}
		}
		
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.Space();
		
		if (editItems)
		{
			if (menuItems == null || menuItems.Length == 0)
			{
				if (showHelp)
				{
					string help = "Use the + button above to add items to this list.";
					if (! (target is CtxMenu))
						help += "If you don't add items here, then the items in the context menu itself will be used.";
						
					EditorGUILayout.HelpBox(help, MessageType.Info);
				}
			}
			else
			{
				for (int i=0, cnt=menuItems.Length; i<cnt; i++)
					EditMenuItem(menuItems[i], atlas);

				if (showHelp)
				{
					EditorGUILayout.HelpBox("To delete menu items you must first select the item you wish "+
						"to delete using the checkbox next to the Style option, then use the '-' button. You "+
						"may also insert a new item before an existing item by selecting that item's checkbox "+
						"and then using the '+' button.", MessageType.Info);
				}
			}
		}
	}
	
	protected void EditMenuItem(CtxMenu.Item item, UIAtlas atlas)
	{
		if (item == null)
			return;
		
		GUILayoutOption[] itemSpriteOpt = { GUILayout.Height(16f), GUILayout.Width(140f) };
		GUILayoutOption[] itemSpriteDelOpt = { GUILayout.Height(16f), GUILayout.Width(60f) };
		
		Color normalColor, disabledColor;
		
		Rect box = EditorGUILayout.BeginVertical();
		GUILayout.Space(4f);
		GUI.Box(box, "");
		
		EditorGUILayout.BeginHorizontal();
		item.isSelected = EditorGUILayout.Toggle(item.isSelected, GUILayout.Width(12f));
		
		EditorGUIUtility.labelWidth = 64f;
		CtxMenu.ItemStyle itemStyle = (CtxMenu.ItemStyle)EditorGUILayout.EnumMaskField("Style", item.style,
			GUILayout.Width(188f));
		
		if (item.style != itemStyle)
		{
			RegisterUndo();
			
			bool wasSubmenu = item.isSubmenu;
			
			item.style = itemStyle;
			
			if (item.isSubmenu && ! wasSubmenu)
				item.id = -1;
		}
		
		if (item.isCheckable)
		{
			EditorGUIUtility.labelWidth = 44f;
			int mutexGroup = EditorGUILayout.IntField("Mutex", item.mutexGroup, GUILayout.Width(88f));
			if (mutexGroup != item.mutexGroup)
			{
				RegisterUndo();
				item.mutexGroup = mutexGroup;
			}
		}
		 
		EditorGUIUtility.labelWidth = 80f;
		EditorGUILayout.EndHorizontal();

		if ((item.style & CtxMenu.ItemStyle.Separator) != (CtxMenu.ItemStyle)0)
			item.id = -1;
		else
		{
			EditorGUILayout.BeginHorizontal();
			string text = EditorGUILayout.TextField("    Text", item.text, GUILayout.Width(204f));
			if (item.text != text)
			{
				RegisterUndo();
				item.text = text;
			}
		
			EditorGUIUtility.labelWidth = 32f;
			GUILayout.Space(12f);
			int itemId = EditorGUILayout.IntField("ID", item.id, GUILayout.Width(76f));
			if (item.id != itemId)
			{
				RegisterUndo();
				item.id = itemId;
			}
			EditorGUIUtility.labelWidth = 80f;
			
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("    Icon", GUILayout.Width(76f));
	
			string iconTitle = item.icon;
			if (string.IsNullOrEmpty(iconTitle))
				iconTitle = "...";
			
			if (GUILayout.Button(iconTitle, itemSpriteOpt))
			{
				currentItem = item;
				NGUISettings.atlas = atlas;
				SpriteSelector.Show(OnItemIcon);
			}

			GUILayout.Space(12f);
			if (GUILayout.Button("None", itemSpriteDelOpt))
			{
				if (! string.IsNullOrEmpty(item.icon))
					RegisterUndo();
				
				item.icon = "";
			}
			
			EditorGUILayout.EndHorizontal();
			
			if (! string.IsNullOrEmpty(item.icon))
			{
				EditorGUILayout.BeginHorizontal();
				normalColor = EditorGUILayout.ColorField("    Normal", item.spriteColor, GUILayout.Width(140f));
				if (normalColor != item.spriteColor)
				{
					RegisterUndo();
					item.spriteColor = normalColor;
				}
				GUILayout.Space(32f);
				disabledColor = EditorGUILayout.ColorField("Disabled", item.spriteColorDisabled, GUILayout.Width(140f));
				if (item.spriteColorDisabled != disabledColor)
				{
					RegisterUndo();
					item.spriteColorDisabled = disabledColor;
				}
				EditorGUILayout.EndHorizontal();
			}
			
			if (item.isSubmenu)
			{
				CtxMenu submenu = (CtxMenu)EditorGUILayout.ObjectField("    Submenu", item.submenu, typeof(CtxMenu), true, GUILayout.Width(317f));
				if (item.submenu != submenu)
				{
					RegisterUndo();
					item.submenu = submenu;
				}
				
				if (submenu != null)
					EditMenuItemList(ref item.submenuItems, submenu.atlas, false, ref item.isEditingItems);
			}
		}
		
		GUILayout.Space(4f);
		EditorGUILayout.EndVertical();
		GUILayout.Space(4f);
	}
	
	protected int SelectedItemCount(ref CtxMenu.Item[] items)
	{
		int result = 0;
		
		foreach (CtxMenu.Item item in items)
		{
			if (item.isSelected)
				++result;
		}
		
		return result;
	}
	
	protected string EditSprite(UIAtlas atlas, string label, string sprite, SpriteSelector.Callback callback)
	{
		EditorGUILayout.BeginHorizontal();
		GUILayout.Label(label, GUILayout.Width(76f));

		string buttonLabel = sprite;
		if (string.IsNullOrEmpty(buttonLabel))
			buttonLabel = "...";
		
		if (GUILayout.Button(buttonLabel, itemSpriteOpt))
		{
			NGUISettings.atlas = atlas;
			SpriteSelector.Show(callback);
		}

		GUILayout.Space(12f);
		if (GUILayout.Button("None", itemSpriteDelOpt))
		{
			if (! string.IsNullOrEmpty(sprite))
				RegisterUndo();
			
			sprite = "";
		}
		EditorGUILayout.EndHorizontal();
			
		GUILayout.Space(2f);
	
		return sprite;
	}
}
