using UnityEngine;
using System.Collections;

public class TransportHandler : SaucerBaseHandler
{
	CtxMenu.Item[] menuItems;
	
	enum Commands
	{
		LandAtWhiteHouse = 40,
		DeployGrays,
		RecoverGrays,
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
		Debug.Log("Transport menu hidden (popup)");
	}
	
	void OnHideMenu(CtxObject obj)
	{
		Debug.Log("Transport menu hidden (object)");
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
	
		menuItems[baseItem].text = "Transport";
		menuItems[baseItem].isSubmenu = true;
		menuItems[baseItem].submenu = submenu;
		menuItems[baseItem].submenuItems = new CtxMenu.Item[3];
		
		for (int i=0; i<3; i++)
		{
			menuItems[baseItem].submenuItems[i] = new CtxMenu.Item();
			menuItems[baseItem].submenuItems[i].id = (int)Commands.LandAtWhiteHouse+i;
		}
		
		menuItems[baseItem].submenuItems[0].text = "Land At White House";
		menuItems[baseItem].submenuItems[1].text = "Deploy Grays";
		menuItems[baseItem].submenuItems[2].text = "Recover Grays";
	}
	
	new void OnMenuSelection(int selection)
	{
		Commands cmd = (Commands)selection;
		switch (cmd)
		{
		case Commands.LandAtWhiteHouse:
		case Commands.DeployGrays:
		case Commands.RecoverGrays:
			Debug.Log("Transport: "+cmd.ToString());
			break;
		default:
			base.OnMenuSelection(selection);
			break;
		}
	}
}
