using UnityEngine;
using System.Collections;

public class ScoutHandler : SaucerBaseHandler
{
	public CtxMenu commandSubmenu;
	CtxMenu.Item[] menuItems;
	
	enum Commands
	{
		BuzzAirliners = 10,
		FlyInCircles,
		HoverAimlessly,
	}
	
	public void OnShowPopupMenu()
	{
		BuildMenu();
		CtxPopup.current.menuItems = menuItems;
	}
	
	public void OnShowMenu(CtxObject obj)
	{
		BuildMenu();
		obj.menuItems = menuItems;
	}
	
	public void OnHidePopupMenu()
	{
		Debug.Log("Scout menu hidden (popup)");
	}
	
	public void OnHideMenu(CtxObject obj)
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
		menuItems[baseItem].submenu = commandSubmenu;
		
#if CTX_NO_SERIALIZATION_FIX
		CtxMenu.Item[] submenuItems = new CtxMenu.Item[3];
		menuItems[baseItem].submenuItems = submenuItems;
#else
		CtxMenu.Item[] submenuItems = new CtxMenu.Item[3];
		commandSubmenu.items = submenuItems;
#endif

		for (int i=0; i<3; i++)
		{
			submenuItems[i] = new CtxMenu.Item();
			submenuItems[i].id = (int)Commands.BuzzAirliners+i;
		}
		
		submenuItems[0].text = "Buzz Airliners";
		submenuItems[1].text = "Fly in Circles";
		submenuItems[2].text = "Hover Aimlessly";
	}
	
	public new void OnMenuSelection()
	{
		int selection = CtxMenu.current.selectedItem;
		
		Commands cmd = (Commands)selection;
		switch (cmd)
		{
		case Commands.BuzzAirliners:
		case Commands.FlyInCircles:
		case Commands.HoverAimlessly:
			Debug.Log("Scout: "+cmd.ToString());
			break;
		default:
			base.OnMenuSelection();
			break;
		}
	}
}
