using UnityEngine;
using System.Collections;

public class SpyHandler : SaucerBaseHandler
{
	CtxMenu.Item[] menuItems;
	
	enum Commands
	{
		FakeAutopsy = 50,
		MakeCropCircles,
		BuzzRadarTowers
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
		Debug.Log("Spy menu hidden (popup)");
	}
	
	void OnHideMenu(CtxObject obj)
	{
		Debug.Log("Spy menu hidden (object)");
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
	
		menuItems[baseItem].text = "Spy";
		menuItems[baseItem].isSubmenu = true;
		menuItems[baseItem].submenu = submenu;
		menuItems[baseItem].submenuItems = new CtxMenu.Item[3];
		
		for (int i=0; i<3; i++)
		{
			menuItems[baseItem].submenuItems[i] = new CtxMenu.Item();
			menuItems[baseItem].submenuItems[i].id = (int)Commands.FakeAutopsy+i;
		}
		
		menuItems[baseItem].submenuItems[0].text = "Fake Autopsy";
		menuItems[baseItem].submenuItems[1].text = "Make Crop Circles";
		menuItems[baseItem].submenuItems[2].text = "Buzz Radar Towers";
	}
	
	new void OnMenuSelection(int selection)
	{
		Commands cmd = (Commands)selection;
		switch (cmd)
		{
		case Commands.FakeAutopsy:
		case Commands.MakeCropCircles:
		case Commands.BuzzRadarTowers:
			Debug.Log("Spy: "+cmd.ToString());
			break;
		default:
			base.OnMenuSelection(selection);
			break;
		}
	}
}
