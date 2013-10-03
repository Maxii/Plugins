using UnityEngine;
using System.Collections;

public class EnforcerHandler : SaucerBaseHandler
{
	CtxMenu.Item[] menuItems;
	
	enum Commands
	{
		AttackMilitary = 30,
		AttackCivilians,
		DeployUltimateWeapon
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
		Debug.Log("Enforcer menu hidden (popup)");
	}
	
	void OnHideMenu(CtxObject obj)
	{
		Debug.Log("Enforcer menu hidden (object)");
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
	
		menuItems[baseItem].text = "Enforcer";
		menuItems[baseItem].isSubmenu = true;
		menuItems[baseItem].submenu = submenu;
		menuItems[baseItem].submenuItems = new CtxMenu.Item[3];
		
		for (int i=0; i<3; i++)
		{
			menuItems[baseItem].submenuItems[i] = new CtxMenu.Item();
			menuItems[baseItem].submenuItems[i].id = (int)Commands.AttackMilitary+i;
		}
		
		menuItems[baseItem].submenuItems[0].text = "Attack Military";
		menuItems[baseItem].submenuItems[1].text = "Attack Civilians";
		menuItems[baseItem].submenuItems[2].text = "Deploy Ultimate Weapon";
	}
	
	new void OnMenuSelection(int selection)
	{
		Commands cmd = (Commands)selection;
		switch (cmd)
		{
		case Commands.AttackMilitary:
		case Commands.AttackCivilians:
		case Commands.DeployUltimateWeapon:
			Debug.Log("Enforcer: "+cmd.ToString());
			break;
		default:
			base.OnMenuSelection(selection);
			break;
		}
	}
}
