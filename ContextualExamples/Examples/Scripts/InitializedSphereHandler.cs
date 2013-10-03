//=========================================================
// Contextual: Context Menus for Unity & NGUI
// Copyright © 2013 Troy Heere
//=========================================================
using UnityEngine;
using System.Collections;

public class InitializedSphereHandler : MonoBehaviour
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
	
	bool stealthOn;
	bool shieldOn;
	
	void OnMenuShow(CtxObject obj)
	{
		if (transform.localScale.x == 1f)
			obj.contextMenu.SetChecked((int)MenuItemID.Small, true);
		else if (transform.localScale.x == 2f)
			obj.contextMenu.SetChecked((int)MenuItemID.Medium, true);
		else
			obj.contextMenu.SetChecked((int)MenuItemID.Large, true);
		
		obj.contextMenu.SetChecked((int)MenuItemID.Stealth, stealthOn);
		obj.contextMenu.SetChecked((int)MenuItemID.Shield, shieldOn);
		
		StartCoroutine(CheckTest(obj));
	}
	
	// Silly trivial test to demonstrate the ability to update the menu's
	// visible state. This coroutine is fired in OnMenuShow() and toggles
	// the disabled state of the 'Stealth' menu item once per second. Note
	// that SetDisabled() implicitly calls CtxMenu.UpdateVisibleState(),
	// which is why this works.
	//
	// The coroutine exits when the menu is hidden.
	IEnumerator CheckTest(CtxObject obj)
	{
		yield return new WaitForSeconds(1f);
		
		bool toggle = false;
		
		while (obj.contextMenu.IsVisible)
		{
			toggle = ! toggle;
			obj.contextMenu.SetDisabled((int)MenuItemID.Stealth, toggle);
			
			yield return new WaitForSeconds(1f);
		}
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
			Debug.Log("SphereHandler: Stealth "+Checked(itemID)+" "+this);
			break;
		case MenuItemID.Shield:
			shieldOn = CtxMenu.current.IsChecked(itemID);
			Debug.Log("SphereHandler: Shield "+Checked(itemID)+" "+this);
			break;
		case MenuItemID.AggressiveAttack:
			Debug.Log("SphereHandler: Aggressive Attack "+this);
			break;
		case MenuItemID.CautiousAttack:
			Debug.Log("SphereHandler: Cautious Attack "+this);
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
