using UnityEngine;
using System.Collections;

public class ScoutHandler : SaucerBaseHandler
{
	CtxMenu.Item[] menuItems;
	
	enum Commands
	{
		BuzzAirliners = 10,
		FlyInCircles,
		HoverAimlessly,
	}
	
	void OnShowMenu(CtxPopup popup)
	{
		BuildMenu();
		popup.menuItems = menuItems;
	}
	
	void OnShowMenu(CtxObject obj)
	{
		BuildMenu();
		obj.menuItems = menuItems;
	}
	
	void OnHideMenu(CtxPopup popup)
	{
		Debug.Log("Scout menu hidden (popup)");
	}
	
	void OnHideMenu(CtxObject obj)
	{
		Debug.Log("Scout menu hidden (object)");
	}
	
	void BuildMenu()
	{
		int baseItem = base.MenuItemCount;
		
		if (menuItems == null)
		{
			int itemCnt = baseItem+1;
			menuItems = new CtxMenu.Item[itemCnt];
			
			for (int i=0; i<itemCnt; i++)
				menuItems[i] = new CtxMenu.Item();
		}
		
		base.FillMenuItems(menuItems);
	
		menuItems[baseItem].text = "Scout";
		menuItems[baseItem].isSubmenu = true;
		menuItems[baseItem].submenu = submenu;
		menuItems[baseItem].submenuItems = new CtxMenu.Item[3];
		
		for (int i=0; i<3; i++)
		{
			menuItems[baseItem].submenuItems[i] = new CtxMenu.Item();
			menuItems[baseItem].submenuItems[i].id = (int)Commands.BuzzAirliners+i;
		}
		
		menuItems[baseItem].submenuItems[0].text = "Buzz Airliners";
		menuItems[baseItem].submenuItems[1].text = "Fly in Circles";
		menuItems[baseItem].submenuItems[2].text = "Hover Aimlessly";
	}
	
	new void OnMenuSelection(int selection)
	{
		Commands cmd = (Commands)selection;
		switch (cmd)
		{
		case Commands.BuzzAirliners:
		case Commands.FlyInCircles:
		case Commands.HoverAimlessly:
			Debug.Log("Scout: "+cmd.ToString());
			break;
		default:
			base.OnMenuSelection(selection);
			break;
		}
	}
}
