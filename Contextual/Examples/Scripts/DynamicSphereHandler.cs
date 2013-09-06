//=========================================================
// Contextual: Context Menus for Unity & NGUI
// Copyright © 2013 Troy Heere
//=========================================================
using UnityEngine;
using System.Collections;

public class DynamicSphereHandler : MonoBehaviour
{
	private enum MenuItemID
	{
		Small,
		Medium,
		Large,
		Separator1,
		Attack,
		Defend,
		Retreat,
		Separator2,
		Stealth,
		Shield,
		Separator3,
		AggressiveAttack,
		CautiousAttack,
		Count
	}
	
	private CtxMenu.Item[] menuItems;
	private bool stealthOn;
	private bool shieldOn;
	
	void OnMenuShow(CtxObject obj)
	{
		if (menuItems == null)
		{
			int cnt = (int)MenuItemID.Count;
			menuItems = new CtxMenu.Item[cnt];
			for (int i=0; i<cnt; i++)
			{
				MenuItemID itemID = (MenuItemID)i;
				
				menuItems[i] = new CtxMenu.Item();
				menuItems[i].text = ItemText(itemID);
				if (menuItems[i].text.StartsWith("Separator"))
					menuItems[i].isSeparator = true;
				else
					menuItems[i].id = i;
			}
			
			menuItems[(int)MenuItemID.Small].isCheckable = true;
			menuItems[(int)MenuItemID.Medium].isCheckable = true;
			menuItems[(int)MenuItemID.Large].isCheckable = true;
						
			menuItems[(int)MenuItemID.Small].mutexGroup = 0;
			menuItems[(int)MenuItemID.Medium].mutexGroup = 0;
			menuItems[(int)MenuItemID.Large].mutexGroup = 0;

			menuItems[(int)MenuItemID.Stealth].isCheckable = true;
			menuItems[(int)MenuItemID.Shield].isCheckable = true;
		}
			
		if (transform.localScale.x == 1f)
			CtxHelper.SetChecked(menuItems, (int)MenuItemID.Small, true);
		else if (transform.localScale.x == 2f)
			CtxHelper.SetChecked(menuItems, (int)MenuItemID.Medium, true);
		else
			CtxHelper.SetChecked(menuItems, (int)MenuItemID.Large, true);
		
		menuItems[(int)MenuItemID.Stealth].isChecked = stealthOn;
		menuItems[(int)MenuItemID.Shield].isChecked = shieldOn;
		
		obj.menuItems = menuItems;
	}
	
	void OnMenuHide(CtxObject obj)
	{
		Debug.Log("Menu hidden for "+obj.name);
	}
	
	void OnMenuSelection(int itemID)
	{
		switch ((MenuItemID)itemID)
		{
		case MenuItemID.Small:
			Debug.Log("SphereHandler: Small "+this);
			transform.localScale = new Vector3(1f, 1f, 1f);
			break;
		case MenuItemID.Medium:
			Debug.Log("SphereHandler: Medium "+this);
			transform.localScale = new Vector3(2f, 2f, 2f);
			break;
		case MenuItemID.Large:
			Debug.Log("SphereHandler: Large "+this);
			transform.localScale = new Vector3(3f, 3f, 3f);
			break;
		case MenuItemID.Attack:
			Debug.Log("SphereHandler: Attack "+this);
			break;
		case MenuItemID.Defend:
			Debug.Log("SphereHandler: Defend "+this);
			break;
		case MenuItemID.Retreat:
			Debug.Log("SphereHandler: Retreat "+this);
			break;
		case MenuItemID.Stealth:
			stealthOn = CtxMenu.current.IsChecked(itemID);
			Debug.Log("SphereHandler: Stealth "+stealthOn+" "+this);
			break;
		case MenuItemID.Shield:
			shieldOn = CtxMenu.current.IsChecked(itemID);
			Debug.Log("SphereHandler: Shield "+shieldOn+" "+this);
			break;
		case MenuItemID.AggressiveAttack:
			Debug.Log("SphereHandler: Aggressive Attack "+this);
			break;
		case MenuItemID.CautiousAttack:
			Debug.Log("SphereHandler: Cautious Attack "+this);
			break;
		}
	}

	string ItemText(MenuItemID itemID)
	{
		string upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
		string text = itemID.ToString();
		string result = "";
		int start = 0, i, cnt = text.Length;
		
		for (i=1; i<cnt; i++)
		{
			if (upperCase.IndexOf(text[i]) >= 0)
			{
				result += text.Substring(start, i-start)+" ";
				start = i;
			}
		}
		
		if (i > start)
			result += text.Substring(start, i-start);
		
		return result;
	}
}
