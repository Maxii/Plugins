//=========================================================
// Contextual: Context Menus for Unity & NGUI
// Copyright © 2013 Troy Heere
//=========================================================
using UnityEngine;
using System.Collections;

public class DelegateSphereHandler : MonoBehaviour
{
	private enum MenuItemID
	{
		Small,
		Medium,
		Large,
		Attack,
		Defend,
		Retreat,
		Stealth,
		Shield,
		AggressiveAttack,
		CautiousAttack
	}
	
	void Start()
	{
		CtxObject ctxObj = GetComponent<CtxObject>();
		if (ctxObj != null)
			ctxObj.onSelection = OnMenuSelection;
	}
	
	void OnMenuSelection(int itemID)
	{
		switch ((MenuItemID)itemID)
		{
		case MenuItemID.Small:
			Debug.Log("SphereHandler: Small!");
			transform.localScale = new Vector3(1f, 1f, 1f);
			break;
		case MenuItemID.Medium:
			Debug.Log("SphereHandler: Medium!");
			transform.localScale = new Vector3(2f, 2f, 2f);
			break;
		case MenuItemID.Large:
			Debug.Log("SphereHandler: Large!");
			transform.localScale = new Vector3(3f, 3f, 3f);
			break;
		case MenuItemID.Attack:
			Debug.Log("SphereHandler: Attack!");
			break;
		case MenuItemID.Defend:
			Debug.Log("SphereHandler: Defend!");
			break;
		case MenuItemID.Retreat:
			Debug.Log("SphereHandler: Retreat!");
			break;
		case MenuItemID.Stealth:
			Debug.Log("SphereHandler: Stealth "+Checked(itemID));
			break;
		case MenuItemID.Shield:
			Debug.Log("SphereHandler: Shield "+Checked(itemID));
			break;
		case MenuItemID.AggressiveAttack:
			Debug.Log("SphereHandler: Aggressive Attack!");
			break;
		case MenuItemID.CautiousAttack:
			Debug.Log("SphereHandler: Cautious Attack!");
			break;
		}
	}
	
	string Checked(int itemID)
	{
		if (CtxMenu.current.IsChecked(itemID))
			return "On!";
		else
			return "Off!";
	}
}
