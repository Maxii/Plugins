using UnityEngine;
using System.Collections;

public class AbductorHandler : SaucerBaseHandler
{
	CtxMenu.Item[] menuItems;
	
	enum Commands
	{
		AbductSpecimens = 20,
		EraseMemory,
		UseProbeDevice,
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
		Debug.Log("Abductor menu hidden (popup)");
	}
	
	void OnHideMenu(CtxObject obj)
	{
		Debug.Log("Abductor menu hidden (object)");
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
	
		menuItems[baseItem].text = "Abductor";
		menuItems[baseItem].isSubmenu = true;
		menuItems[baseItem].submenu = submenu;
		menuItems[baseItem].submenuItems = new CtxMenu.Item[3];
		
		for (int i=0; i<3; i++)
		{
			menuItems[baseItem].submenuItems[i] = new CtxMenu.Item();
			menuItems[baseItem].submenuItems[i].id = (int)Commands.AbductSpecimens+i;
		}
		
		menuItems[baseItem].submenuItems[0].text = "Abduct Specimens";
		menuItems[baseItem].submenuItems[1].text = "Erase Memory";
		menuItems[baseItem].submenuItems[2].text = "Use 'Probe' Device";
	}
	
	new void OnMenuSelection(int selection)
	{
		Commands cmd = (Commands)selection;
		switch (cmd)
		{
		case Commands.AbductSpecimens:
		case Commands.EraseMemory:
		case Commands.UseProbeDevice:
			Debug.Log("Abductor: "+cmd.ToString());
			break;
		default:
			base.OnMenuSelection(selection);
			break;
		}
	}
}
