//=========================================================
// Contextual: Context Menus for Unity & NGUI
// Copyright © 2013 Troy Heere
//=========================================================
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// A component class that can be used to attach a context menu to any object.
/// </summary>
[AddComponentMenu("Contextual/Context Object")]
public class CtxObject : MonoBehaviour
{
	#region Public Variables
	
	/// <summary>
	/// The context menu that will be opened when this item is clicked.
	/// </summary>
	public CtxMenu contextMenu;
	
	/// <summary>
	/// The mouse button number that we're waiting for. For touch-screen devices
	/// you should leave this as 0. For Windows-style right-click set this to 1.
	/// </summary>
	public int buttonNumber = 0;
	
	/// <summary>
	/// If this is true the menu will be offset so as not to obscure the object
	/// it is attached to. The object must have a collider for this to work. The
	/// offset direction is determined by the menu pivot. For example, a pivot of
	/// Left will cause the menu to be offset to the right of the object, while a
	/// pivot of Bottom will cause the menu to be offset above the object.
	/// </summary>
	public bool offsetMenu = false;

	/// <summary>
	/// Optional list of menu items. These items will replace the menu items previously
	/// assigned to the context menu object when the menu is shown for this object.
	/// This enables a use case where a single CtxMenu instance can show any
	/// number of variant menus depending on which object was picked. This may be simpler
	/// and/or more efficient in some situations.
	/// </summary>
	public CtxMenu.Item[] menuItems;
	
	/// <summary>
	/// The current CtxObject instance. Valid only during event callbacks.
	/// </summary>
	public static CtxObject current;
	
	/// <summary>
	/// The last selected menu item.
	/// </summary>
	public int selectedItem;
	
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
	
	/// <summary>
	/// The game object which sent the most recent selection event. If forwarding events to
	/// an event receiver, you can use this to recover the object which originated the
	/// selection event. This variable has valid contents only for the duration of the
	/// event and will revert to null after the event function exits.
	/// </summary>
	[HideInInspector]
	public static GameObject sender;
	
	[HideInInspector]
	public bool isEditingItems = false;

	#region Public Member Functions
	
	/// <summary>
	/// Shows the context menu associated with this object. If you are handling
	/// your own picking, then you should call this function when this object
	/// is picked.
	/// </summary>
	public void ShowMenu()
	{
		if (contextMenu != null)
		{
			EventDelegate.Add(contextMenu.onSelection, OnMenuSelect);
			EventDelegate.Add(contextMenu.onHide, OnHide, true);
			
			current = this;
			EventDelegate.Execute(onShow);
		
			gameObject.SendMessage("OnShowMenu", this, SendMessageOptions.DontRequireReceiver);
			
			if (menuItems != null && menuItems.Length > 0)
				contextMenu.Show(MenuPosition, menuItems);
			else
				contextMenu.Show(MenuPosition);
		}
	}

	/// <summary>
	/// Hides the context menu associated with this object if it is visible.
	/// </summary>
	public void HideMenu()
	{
		if (contextMenu != null)
			contextMenu.Hide();
	}
	
	void OnHide()
	{
		current = this;
		EventDelegate.Execute(onHide);
		EventDelegate.Remove(contextMenu.onSelection, OnMenuSelect);	// <-- In case the menu was hidden with no selection made

		gameObject.SendMessage("OnHideMenu", this, SendMessageOptions.DontRequireReceiver);
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

	private void OnMenuSelect()
	{
		selectedItem = CtxMenu.current.selectedItem;
		
		current = this;
		EventDelegate.Execute(onSelection);
		
		gameObject.SendMessage("OnMenuSelection", selectedItem, SendMessageOptions.DontRequireReceiver);
	}
	
	private Vector3 MenuPosition
	{
		get 
		{
			if (! offsetMenu || collider == null || (contextMenu != null && contextMenu.style == CtxMenu.Style.Pie))
			{
				// In the simplest case the menu pivot is just positioned on the
				// object's origin position. This may be fine in some cases.
				return Camera.main.WorldToScreenPoint(transform.position);
			}
			else
			{
				// For the offset case we need to determine the screen-space bounds
				// of this object and then offset the menu pivot to one side of the
				// object bounds. Note that CtxMenu itself may adjust the
				// position in order to keep the menu contents on screen.
				return CtxHelper.ComputeMenuPosition(contextMenu, 
					CtxHelper.ComputeScreenSpaceBounds(collider.bounds, Camera.main), false);
			}
		}
	}

	
	#endregion
}
