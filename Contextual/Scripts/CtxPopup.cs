//=========================================================
// Contextual: Context Menus for Unity & NGUI
// Copyright © 2013 Troy Heere
//=========================================================
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Popup Menu Class. A simple way to attach a CtxMenu to any NGUI widget. The
/// game object will need to have a collider in order to track mouse events.
/// </summary>
[AddComponentMenu("Contextual/Popup")]
public class CtxPopup : MonoBehaviour
{
	#region Public Variables

	/// <summary>
	/// The context menu that will be displayed when this widget is pressed. The menu
	/// will be placed adjacent to the widget and that placement will be deterimined
	/// by the menu pivot. For example, if the pivot is 'Left' the menu will appear to
	/// the right of the button; if 'Top' the menu will appear below the button.
	/// </summary>
	public CtxMenu contextMenu;

	/// <summary>
	/// The menu items. If no items are specified here then the
	/// menu items from the context menu will be shown.
	/// </summary>
	public CtxMenu.Item[] menuItems;
	
	/// <summary>
	/// The current CtxObject instance. Valid only during event callbacks.
	/// </summary>
	public static CtxPopup current;
	
	/// <summary>
	/// The last selected menu item.
	/// </summary>
	public int selectedItem;
	
	/// <summary>
	/// The mouse button (0-2) that triggers this popup menu.
	/// </summary>
	public int mouseButton;
	
	/// <summary>
	/// The onSelection event.
	/// </summary>
	public List<EventDelegate> onSelection = new List<EventDelegate>();
	
	/// <summary>
	/// The onShow event.
	/// </summary>
	public List<EventDelegate> onShow = new List<EventDelegate>();
	
	/// <summary>
	/// The onHide event.
	/// </summary>
	public List<EventDelegate> onHide = new List<EventDelegate>();

	#endregion
		
	[HideInInspector]
	public bool isEditingItems = false;
	
	#region Event Handling

	void OnPress(bool isPressed)
	{
		// Filter for touches or selected mouse button.
		
		if (UICamera.currentTouchID >= 0 || UICamera.currentTouchID == (-1 - mouseButton))
		{
			// We show the menu on the up-press not the down-press. Otherwise NGUI would steal
			// the selection state from the context menu on the up-press, causing the menu to
			// close immediately.
			
			if (! isPressed)
			{
				current = this;
				EventDelegate.Execute(onShow);
	
				Vector3 menuPosition = CtxHelper.ComputeMenuPosition(contextMenu, gameObject);
				
				CtxMenu.Item[] items = MenuItems;
				
				if (items != null || contextMenu.onShow.Count > 0)
				{
					EventDelegate.Add(contextMenu.onSelection, OnMenuSelection);
					EventDelegate.Add(contextMenu.onHide, OnHide, true);
				
					if (menuItems != null && menuItems.Length > 0)
						contextMenu.Show(menuPosition, menuItems);
					else
						contextMenu.Show(menuPosition);
				}
			}
		}
	}
	
	void OnHide()
	{
		current = this;
		EventDelegate.Execute(onHide);
		EventDelegate.Remove(contextMenu.onSelection, OnMenuSelection);	// <-- In case the menu was hidden with no selection made
	}
	
	void OnMenuSelection()
	{
		// Dispatch selection events.
		current = this;
		selectedItem = CtxMenu.current.selectedItem;
		EventDelegate.Execute(onSelection);
	}
	
	void OnSelect(bool isSelected)
	{
		// When we are selected we force the contextMenu to be
		// selected instead so that subsequent keyboard/controller
		// navigation works correctly in the menu.
		
		if (isSelected && contextMenu != null)
			UICamera.selectedObject = contextMenu.gameObject;
	}
	
	#endregion
	
	#region Menu Item Manipulation

	/// <summary>
	/// Determines whether the item with the specified id number is checked.
	/// </summary>
	/// <returns>
	/// <c>true</c> if the item with the specified id number is checked; otherwise, <c>false</c>.
	/// </returns>
	/// <param name='id'>
	/// The menu item id number.
	/// </param>
	public bool IsChecked(int id)
	{
		return CtxHelper.IsChecked(menuItems, id);
	}
	
	/// <summary>
	/// Sets the checkmark state for the specified menu item. Note that this flag
	/// will be ignored if the item didn't originally have its 'checkable' flag set.
	/// If this item is part of a mutex group, then the other items in the group 
	/// will be unchecked when this item is checked.
	/// </summary>
	/// <param name='id'>
	/// The menu item id number.
	/// </param>
	/// <param name='isChecked'>
	/// The desired checkmark state.
	/// </param>
	public void SetChecked(int id, bool isChecked)
	{
		CtxHelper.SetChecked(menuItems, id, isChecked);
		if (contextMenu != null)
			contextMenu.UpdateVisibleState();
	}

	/// <summary>
	/// Determines whether the specified menu item is disabled.
	/// </summary>
	/// <returns>
	/// <c>true</c> if the specified menu item is disabled; otherwise, <c>false</c>.
	/// </returns>
	/// <param name='id'>
	/// The menu item id number.
	/// </param>
	public bool IsDisabled(int id)
	{
		return CtxHelper.IsDisabled(menuItems, id);
	}
	
	/// <summary>
	/// Sets the disabled state for the specified menu item.
	/// </summary>
	/// <param name='id'>
	/// The menu item id number.
	/// </param>
	/// <param name='isDisabled'>
	/// The desired disable state.
	/// </param>
	public void SetDisabled(int id, bool isDisabled)
	{
		CtxHelper.SetDisabled(menuItems, id, isDisabled);
		if (contextMenu != null)
			contextMenu.UpdateVisibleState();
	}

	/// <summary>
	/// Assigns a new text string to the specified menu item. If this is a localized
	/// menu, you should only assign key strings and allow the localization
	/// logic to update the visible text.
	/// </summary>
	/// <param name='id'>
	/// The menu item id number.
	/// </param>
	/// <param name='text'>
	/// The text that will be displayed for this menu item.
	/// </param>
	public void SetText(int id, string text)
	{
		CtxHelper.SetText(menuItems, id, text);
	}
	
	/// <summary>
	/// Retrieves the text string displayed by this menu item.
	/// </summary>
	/// <returns>
	/// The text.
	/// </returns>
	/// <param name='id'>
	/// The menu item id number.
	/// </param>
	public string GetText(int id)
	{
		return CtxHelper.GetText(menuItems, id);
	}

	/// <summary>
	/// Assign a new icon sprite to this menu item.
	/// </summary>
	/// <param name='id'>
	/// The menu item id number.
	/// </param>
	/// <param name='icon'>
	/// The name of the sprite to assign. Note that the sprite must be in the atlas
	/// used by this context menu. Refer to the NGUI documentation for more information.
	/// </param>
	public void SetIcon(int id, string icon)
	{
		CtxHelper.SetIcon(menuItems, id, icon);
	}
	
	/// <summary>
	/// Retrieve the name of the icon sprite displayed by this menu item.
	/// </summary>
	/// <returns>
	/// The icon sprite name.
	/// </returns>
	/// <param name='id'>
	/// The menu item id number.
	/// </param>
	public string GetIcon(int id)
	{
		return CtxHelper.GetIcon(menuItems, id);
	}
	
	/// <summary>
	/// Retrieve the menu item descriptor with the specified id. 
	/// </summary>
	/// <returns>
	/// The menu item descriptor instance.
	/// </returns>
	/// <param name='id'>
	/// The menu item id number.
	/// </param>
	public CtxMenu.Item FindItem(int id)
	{
		return CtxHelper.FindItem(menuItems, id);
	}

	/// <summary>
	/// Retrieve the menu item descriptor with the specified id. If this menu has
	/// submenus, the search will recurse into the child menus after searching all
	/// of the items in the current menu.
	/// </summary>
	/// <returns>
	/// The menu item descriptor instance.
	/// </returns>
	/// <param name='id'>
	/// The menu item id number.
	/// </param>
	public CtxMenu.Item FindItemRecursively(int id)
	{
		return CtxHelper.FindItemRecursively(menuItems, id);
	}
	
	#endregion
	
	#region Private Member Functions

	CtxMenu.Item[] MenuItems
	{
		get
		{
			CtxMenu.Item[] items = menuItems;
			if (items == null || items.Length == 0)
				items = contextMenu.items;
			
			if (items != null && items.Length == 0)
				return null;
			
			return items;
		}
	}
	
	#endregion
}
