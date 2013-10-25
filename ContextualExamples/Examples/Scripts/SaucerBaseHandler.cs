using UnityEngine;
using System.Collections;

public class SaucerBaseHandler : MonoBehaviour
{
	protected enum MenuCommands
	{
		Small,
		Medium,
		Large,
		
		ColorScheme1 = 5,
		ColorScheme2,
		ColorScheme3,
		ColorScheme4,
		ColorScheme5,
		
		Last
	}
	
	public CtxMenu submenu;
	
	protected int MenuItemCount
	{
		get { return 5; }
	}
	
	protected void FillMenuItems(CtxMenu.Item[] items)
	{
		items[0].text = "Small";
		items[0].isCheckable = true;
		items[0].mutexGroup = 0;
		items[0].id = (int)MenuCommands.Small;
		
		items[1].text = "Medium";
		items[1].isCheckable = true;
		items[1].mutexGroup = 0;
		items[1].id = (int)MenuCommands.Medium;
		
		items[2].text = "Large";
		items[2].isCheckable = true;
		items[2].mutexGroup = 0;
		items[2].id = (int)MenuCommands.Large;
		
		float s = transform.localScale.x;
		if (s < 1f)
			items[0].isChecked = true;
		else if (s > 1f)
			items[2].isChecked = true;
		else
			items[1].isChecked = true;
		
		items[3].isSeparator = true;
		
		items[4].text = "Color Scheme";
		items[4].isSubmenu = true;
		items[4].submenu = submenu;
		items[4].submenuItems = new CtxMenu.Item[5];
		
		int matID = FindMaterialIndex();
		
		for (int i=0; i<5; i++)
		{
			items[4].submenuItems[i] = new CtxMenu.Item();
			items[4].submenuItems[i].isCheckable = true;
			items[4].submenuItems[i].mutexGroup = 0;
			items[4].submenuItems[i].id = (int)MenuCommands.ColorScheme1 + i;
			
			if (i == matID)
				items[4].submenuItems[i].isChecked = true;
		}
		
		items[4].submenuItems[0].text = "Mirage";
		items[4].submenuItems[1].text = "Vinyard";
		items[4].submenuItems[2].text = "Sunset";
		items[4].submenuItems[3].text = "Holly";
		items[4].submenuItems[4].text = "Embers";
	}
	
	protected void OnMenuSelection()
	{
		int item = CtxMenu.current.selectedItem;
		
		MenuCommands cmd = (MenuCommands)item;
		
		switch (cmd)
		{
		case MenuCommands.Small:
			transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
			break;
		case MenuCommands.Medium:
			transform.localScale = Vector3.one;
			break;
		case MenuCommands.Large:
			transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);
			break;
		
		case MenuCommands.ColorScheme1:
		case MenuCommands.ColorScheme2:
		case MenuCommands.ColorScheme3:
		case MenuCommands.ColorScheme4:
		case MenuCommands.ColorScheme5:
		{
			int cs = item - (int)MenuCommands.ColorScheme1;
			SaucerScene saucerScene = SaucerScene.Instance;
			SetColorScheme(saucerScene.hullMaterials[cs], saucerScene.glowMaterials[cs]);
			break;
		}
		}
	}

	void SetColorScheme(Material hullMat, Material glowMat)
	{
		MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();
		foreach (MeshRenderer mr in meshRenderers)
		{
			if (mr.material.name.Contains("Glow"))
				mr.material = glowMat;
			else
				mr.material = hullMat;
		}
	}
	
	int FindMaterialIndex()
	{
		int result = 0;
		
		Transform hull = transform.FindChild("Hull");
		if (hull != null && hull.renderer != null)
		{
			Material mat = hull.renderer.sharedMaterial;
			Material[] hullMaterials = SaucerScene.Instance.hullMaterials;
			
			for (int i=0, cnt = hullMaterials.Length; i<cnt; i++)
			{
				if (hullMaterials[i].name == mat.name)
				{
					result = i;
					break;
				}
			}
		}
		
		return result;
	}
}
