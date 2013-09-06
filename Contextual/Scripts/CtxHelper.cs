//=========================================================
// Contextual: Context Menus for Unity & NGUI
// Copyright © 2013 Troy Heere
//=========================================================
using UnityEngine;
using System.Collections;

public class CtxHelper
{
	/// <summary>
	/// Computes a canonical position for the context menu given its pivot parameter
	/// and a rectangle defining the screen-space position of the context object. This
	/// attempts to position the menu such that it does not obscure the object within
	/// that region (except in the 'Center' case.)
	/// </summary>
	/// <returns>
	/// The menu position in screen space.
	/// </returns>
	/// <param name='menu'>
	/// The context menu.
	/// </param>
	/// <param name='rect'>
	/// The screen-space rectangle that we wish not to obscure.
	/// </param>
	/// <param name='parentIsHorizontal'>
	/// Changes the placement semantics depending on whether or not the parent object
	/// has a horizontal or vertical topology. Horizontal semantics favor positioning
	/// the menu above or below the parent object, while vertical semantics favor
	/// positioning the menu to the left or right of the parent object. This parameter
	/// particularly affects the corner pivots (i.e. TopLeft, BottomRight, etc.)
	/// </param>
	public static Vector3 ComputeMenuPosition(CtxMenu menu, Rect rect, bool parentIsHorizontal)
	{
		Vector3 resultPt = Vector3.zero;
		
		if (parentIsHorizontal)
		{
			// Horizontal semantics favor positioning the menu above or
			// below the parent object. This particularly affects the corner
			// pivots (i.e. TopLeft, BottomRight, etc.)
			switch (menu.pivot)
			{
			case UIWidget.Pivot.TopLeft:
				resultPt = new Vector3(rect.xMin, rect.yMin, 0f);
				break;
			case UIWidget.Pivot.Top:
				resultPt = new Vector3(rect.center.x, rect.yMin, 0f);
				break;
			case UIWidget.Pivot.TopRight:
				resultPt = new Vector3(rect.xMax, rect.yMin, 0f);
				break;
			case UIWidget.Pivot.Left:
				resultPt = new Vector3(rect.xMax, rect.center.y, 0f);
				break;
			case UIWidget.Pivot.Center:
				resultPt = new Vector3(rect.center.x, rect.center.y, 0f);
				break;
			case UIWidget.Pivot.Right:
				resultPt = new Vector3(rect.xMin, rect.center.y, 0f);
				break;
			case UIWidget.Pivot.BottomLeft:
				resultPt = new Vector3(rect.xMin, rect.yMax, 0f);
				break;
			case UIWidget.Pivot.Bottom:
				resultPt = new Vector3(rect.center.x, rect.yMax, 0f);
				break;
			case UIWidget.Pivot.BottomRight:
				resultPt = new Vector3(rect.xMax, rect.yMax, 0f);
				break;
			}
		}
		else
		{
			// Vertical semantics favor positioning the menu to the left or
			// right of the parent object. This particularly affects the corner
			// pivots (i.e. TopLeft, BottomRight, etc.)
			switch (menu.pivot)
			{
			case UIWidget.Pivot.TopLeft:
				resultPt = new Vector3(rect.xMax, rect.yMax, 0f);
				break;
			case UIWidget.Pivot.Top:
				resultPt = new Vector3(rect.center.x, rect.yMin, 0f);
				break;
			case UIWidget.Pivot.TopRight:
				resultPt = new Vector3(rect.xMin, rect.yMax, 0f);
				break;
			case UIWidget.Pivot.Left:
				resultPt = new Vector3(rect.xMax, rect.center.y, 0f);
				break;
			case UIWidget.Pivot.Center:
				resultPt = new Vector3(rect.center.x, rect.center.y, 0f);
				break;
			case UIWidget.Pivot.Right:
				resultPt = new Vector3(rect.xMin, rect.center.y, 0f);
				break;
			case UIWidget.Pivot.BottomLeft:
				resultPt = new Vector3(rect.xMax, rect.yMin, 0f);
				break;
			case UIWidget.Pivot.Bottom:
				resultPt = new Vector3(rect.center.x, rect.yMax, 0f);
				break;
			case UIWidget.Pivot.BottomRight:
				resultPt = new Vector3(rect.xMin, rect.yMin, 0f);
				break;
			}
		}		
		
		return resultPt;
	}
	
	/// <summary>
	/// Computes a menu position that will avoid obscuring a hierarchy
	/// of NGUI UI widgets. 
	/// </summary>
	/// <returns>
	/// The menu position.
	/// </returns>
	/// <param name='menu'>
	/// The Context Menu.
	/// </param>
	/// <param name='uiObject'>
	/// The game object that is the parent for one or more NGUI UI widgets.
	/// </param>
	public static Vector3 ComputeMenuPosition(CtxMenu menu, GameObject uiObject)
	{
		Bounds bounds = NGUIMath.CalculateAbsoluteWidgetBounds(uiObject.transform);
		UICamera uiCam = UICamera.FindCameraForLayer(uiObject.layer);
		Rect rect = ComputeScreenSpaceBounds(bounds, uiCam.cachedCamera);
		return ComputeMenuPosition(menu, rect, true);
	}
	
	/// <summary>
	// Admittedly brute-force approach to determining the screen-space bounds.
	// This uses the collider world-space bounding box, computes the screen
	// space position of its corners and then builds a screen space bounding
	// box using the minima and maxima. For some object topologies and/or
	// orientations this may not be a good fit.
	/// </summary>
	/// <returns>
	/// The screen-space bounding rectangle.
	/// </returns>
	/// <param name='bounds'>
	/// The world-space bounding volume we're interested in.
	/// </param>
	public static Rect ComputeScreenSpaceBounds(Bounds bounds, Camera cam)
	{
		Vector3[] corners = new Vector3[8];
		Vector3 center = bounds.center;
		Vector3 extents = bounds.extents;
		
		corners[0] = center + new Vector3(-extents.x, -extents.y, -extents.z);
		corners[1] = center + new Vector3( extents.x, -extents.y, -extents.z);
		corners[2] = center + new Vector3( extents.x, -extents.y,  extents.z);
		corners[3] = center + new Vector3(-extents.x, -extents.y,  extents.z);
		corners[4] = center + new Vector3(-extents.x,  extents.y, -extents.z);
		corners[5] = center + new Vector3( extents.x,  extents.y, -extents.z);
		corners[6] = center + new Vector3( extents.x,  extents.y,  extents.z);
		corners[7] = center + new Vector3(-extents.x,  extents.y,  extents.z);
		
		Vector3 vmin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
		Vector3 vmax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
		
		for (int i=0; i<8; i++)
		{
			Vector3 corner = cam.WorldToScreenPoint(corners[i]);
			vmin = Vector3.Min(corner, vmin);
			vmax = Vector3.Max(corner, vmax);
		}				
		
		return new Rect(vmin.x, vmin.y, vmax.x - vmin.x, vmax.y - vmin.y);
	}
	
	/// <summary>
	/// Transforms the specified bounds into world space using the specified transform.
	/// </summary>
	/// <returns>
	/// The world space bounds.
	/// </returns>
	/// <param name='bounds'>
	/// The local space bounds.
	/// </param>
	/// <param name='transform'>
	/// The transform used to compute the world space bounds.
	/// </param>
	public static Bounds LocalToWorldBounds(Bounds bounds, Transform transform)
	{
		Vector3 vmax = transform.TransformPoint(bounds.max);
		Vector3 vmin = transform.TransformPoint(bounds.min);
		
		Bounds b = new Bounds(vmin, Vector3.zero);
		b.Encapsulate(vmax);
		return b;
	}
	
	/// <summary>
	/// Insets the rect boundaries by the specified amount in x and y.
	/// </summary>
	/// <returns>
	/// The adjusted rect.
	/// </returns>
	/// <param name='rect'>
	/// The input rect.
	/// </param>
	/// <param name='dx'>
	/// The amount to compress the rect boundaries left and right. If you
	/// want to grow the rect instead, use a negative value.
	/// </param>
	/// The amount to compress the rect boundaries up and down. If you
	/// want to grow the rect instead, use a negative value.
	/// </param>
	public static Rect InsetRect(Rect rect, float dx, float dy)
	{
		rect.x += dx;
		rect.y += dy;
		rect.width -= dx*2f;
		rect.height -= dy*2f;
		
		return rect;
	}

	/// <summary>
	/// Destroys all children of the specified transform.
	/// </summary>
	/// <param name='trx'>
	/// The transform whose children we want to destroy.
	/// </param>
	public static void DestroyAllChildren(Transform trx)
	{
		if (trx.childCount > 0)
		{
			Transform[] children = new Transform[trx.childCount];
			int i=0;
			
			foreach (Transform child in trx)
				children[i++] = child;
			
			trx.DetachChildren();
			
			if (Application.isEditor && ! Application.isPlaying)
			{
				foreach (Transform child in children)
					Object.DestroyImmediate(child.gameObject);
			}
			else
			{
				foreach (Transform child in children)
					Object.DestroyObject(child.gameObject);
			}
		}
	}
	
	/// <summary>
	/// Determines whether the specified game object is active. Compatibility 
	/// function provided so that code which uses active/inactive idiom will work
	/// correctly in Unity 4.x and Unity 3.x.
	/// </summary>
	/// <returns>
	/// <c>true</c> if the game object is active; otherwise, <c>false</c>.
	/// </returns>
	/// <param name='go'>
	/// The game object to test.
	/// </param>
	public static bool IsActive(GameObject go)
	{
		return go.activeSelf;
	}
	
	/// <summary>
	/// Activate or deactivate the game object hierarchy rooted at go. Compatibility 
	/// function provided so that code which uses active/inactive idiom will work
	/// correctly in Unity 4.x and Unity 3.x.
	/// </summary>
	/// <param name='go'>
	/// The game object to activate/deactivate.
	/// </param>
	/// <param name='active'>
	/// If true, go hierarchy is activated, if false it is deactivated.
	/// </param>
	public static void SetActive(GameObject go, bool active)
	{
		go.SetActive(active);
	}
	
	/// <summary>
	/// Determines whether the item with the specified id number is checked.
	/// </summary>
	/// <returns>
	/// <c>true</c> if the item with the specified id number is checked; otherwise, <c>false</c>.
	/// </returns>
	/// <param name='items'>
	/// The array of items to search.
	/// </param>
	/// <param name='id'>
	/// The menu item id number.
	/// </param>
	public static bool IsChecked(CtxMenu.Item[] items, int id)
	{
		CtxMenu.Item item = FindItemRecursively(items, id);
		if (item != null)
			return item.isChecked;
		
		return false;
	}
	
	/// <summary>
	/// Sets the checkmark state for the specified menu item. Note that this flag
	/// will be ignored if the item didn't originally have its 'checkable' flag set.
	/// If this item is part of a mutex group, then the other items in the group 
	/// will be unchecked when this item is checked.
	/// </summary>
	/// <param name='items'>
	/// The array of items to search.
	/// </param>
	/// <param name='id'>
	/// The menu item id number.
	/// </param>
	/// <param name='isChecked'>
	/// The desired checkmark state.
	/// </param>
	public static void SetChecked(CtxMenu.Item[] items, int id, bool isChecked)
	{
		CtxMenu.Item item = FindItemRecursively(items, id);
		if (item != null && item.isCheckable)
		{
			item.isChecked = isChecked;
			if (item.mutexGroup >= 0)
				MutexItems(items, id, item.mutexGroup);
		}
	}

	/// <summary>
	/// Determines whether the specified menu item is disabled.
	/// </summary>
	/// <returns>
	/// <c>true</c> if the specified menu item is disabled; otherwise, <c>false</c>.
	/// </returns>
	/// <param name='items'>
	/// The array of items to search.
	/// </param>
	/// <param name='id'>
	/// The menu item id number.
	/// </param>
	public static bool IsDisabled(CtxMenu.Item[] items, int id)
	{
		CtxMenu.Item item = FindItemRecursively(items, id);
		if (item != null)
			return item.isDisabled;
		
		return false;
	}
	
	/// <summary>
	/// Sets the disabled state for the specified menu item.
	/// </summary>
	/// <param name='id'>
	/// The menu item id number.
	/// </param>
	/// <param name='items'>
	/// The array of items to search.
	/// </param>
	/// <param name='isDisabled'>
	/// The desired disable state.
	/// </param>
	public static void SetDisabled(CtxMenu.Item[] items, int id, bool isDisabled)
	{
		CtxMenu.Item item = FindItemRecursively(items, id);
		if (item != null)
			item.isDisabled = isDisabled;
	}

	/// <summary>
	/// Assigns a new text string to the specified menu item. If this is a localized
	/// menu, you should only assign key strings and allow the localization
	/// logic to update the visible text.
	/// </summary>
	/// <param name='items'>
	/// The array of items to search.
	/// </param>
	/// <param name='id'>
	/// The menu item id number.
	/// </param>
	/// <param name='text'>
	/// The text that will be displayed for this menu item.
	/// </param>
	public static void SetText(CtxMenu.Item[] items, int id, string text)
	{
		CtxMenu.Item item = FindItemRecursively(items, id);
		if (item != null)
			item.text = text;
	}
	
	/// <summary>
	/// Retrieves the text string displayed by this menu item.
	/// </summary>
	/// <returns>
	/// The text.
	/// </returns>
	/// <param name='items'>
	/// The array of items to search.
	/// </param>
	/// <param name='id'>
	/// The menu item id number.
	/// </param>
	public static string GetText(CtxMenu.Item[] items, int id)
	{
		CtxMenu.Item item = FindItemRecursively(items, id);
		if (item != null)
			return item.text;
		
		return null;
	}

	/// <summary>
	/// Assign a new icon sprite to this menu item.
	/// </summary>
	/// <param name='items'>
	/// The array of items to search.
	/// </param>
	/// <param name='id'>
	/// The menu item id number.
	/// </param>
	/// <param name='icon'>
	/// The name of the sprite to assign. Note that the sprite must be in the atlas
	/// used by this context menu. Refer to the NGUI documentation for more information.
	/// </param>
	public static void SetIcon(CtxMenu.Item[] items, int id, string icon)
	{
		CtxMenu.Item item = FindItemRecursively(items, id);
		if (item != null)
			item.icon = icon;
	}
	
	/// <summary>
	/// Retrieve the name of the icon sprite displayed by this menu item.
	/// </summary>
	/// <returns>
	/// The icon sprite name.
	/// </returns>
	/// <param name='items'>
	/// The array of items to search.
	/// </param>
	/// <param name='id'>
	/// The menu item id number.
	/// </param>
	public static string GetIcon(CtxMenu.Item[] items, int id)
	{
		CtxMenu.Item item = FindItemRecursively(items, id);
		if (item != null)
			return item.icon;
		
		return null;
	}
	
	/// <summary>
	/// Retrieve the menu item descriptor with the specified id. 
	/// </summary>
	/// <returns>
	/// The menu item descriptor instance.
	/// </returns>
	/// <param name='items'>
	/// The array of items to search.
	/// </param>
	/// <param name='id'>
	/// The menu item id number.
	/// </param>
	public static CtxMenu.Item FindItem(CtxMenu.Item[] items, int id)
	{
		foreach (CtxMenu.Item item in items)
		{
			if (item.id == id)
				return item;
		}
		
		return null;
	}

	// Uncheck all but the specified item in a mutex group. This is a recursive
	// process due to the fact that the item and/or mutex group may not be in the
	// root menu.
	public static bool MutexItems(CtxMenu.Item[] itemsArray, int id, int mutexGroup)
	{
		bool hasSubmenus = false;
		
		foreach (CtxMenu.Item item in itemsArray)
		{
			if (item.id == id)
			{
				UncheckMutexItems(itemsArray, id, mutexGroup);
				return true;
			}
			
			if (item.submenuItems != null || item.submenu)
				hasSubmenus = true;
		}
		
		if (hasSubmenus)
		{
			foreach (CtxMenu.Item item in itemsArray)
			{
				if (item.submenuItems != null)
				{
					if (MutexItems(item.submenuItems, id, mutexGroup))
						return true;
				}
				else if (item.submenu != null)
				{
					if (MutexItems(item.submenu.items, id, mutexGroup))
						return true;
				}
			}
		}
		
		return false;
	}
		
	/// <summary>
	/// Unchecks the items in the same mutex group.
	/// </summary>
	/// <param name='itemsArray'>
	/// Items array.
	/// </param>
	/// <param name='id'>
	/// Identifier.
	/// </param>
	/// <param name='mutexGroup'>
	/// Mutex group.
	/// </param>
	public static void UncheckMutexItems(CtxMenu.Item[] itemsArray, int id, int mutexGroup)
	{
		foreach (CtxMenu.Item item in itemsArray)
		{
			if (item.id != id && item.mutexGroup == mutexGroup)
				item.isChecked = false;
		}
	}

	/// <summary>
	/// Retrieve the menu item descriptor with the specified id. If this menu has
	/// submenus, the search will recurse into the child menus after searching all
	/// of the items in the current menu.
	/// </summary>
	/// <returns>
	/// The menu item descriptor instance.
	/// </returns>
	/// <param name='items'>
	/// The array of items to search.
	/// </param>
	/// <param name='id'>
	/// The menu item id number.
	/// </param>
	public static CtxMenu.Item FindItemRecursively(CtxMenu.Item[] itemsArray, int id)
	{
		bool hasSubmenus = false;
		
		foreach (CtxMenu.Item item in itemsArray)
		{
			if (item.id == id)
				return item;
			
			if (item.submenuItems != null || item.submenu)
				hasSubmenus = true;
		}
		
		if (hasSubmenus)
		{
			foreach (CtxMenu.Item item in itemsArray)
			{
				if (item.submenuItems != null)
				{
					CtxMenu.Item res = FindItemRecursively(item.submenuItems, id);
					if (res != null)
						return res;
				}
				else if (item.submenu != null)
				{
					CtxMenu.Item res = FindItemRecursively(item.submenu.items, id);
					if (res != null)
						return res;
				}
			}
		}
		
		return null;
	}
}
