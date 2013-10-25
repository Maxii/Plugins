using UnityEngine;
using System.Collections;

public class ScriptedMenuHandler : MonoBehaviour 
{
	public CtxMenu menu;
	
	void Update()
	{
		if (Input.GetMouseButtonUp(0))
		{
			Camera mainCam = Camera.main;
			RaycastHit hit = new RaycastHit();
			Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
			if (collider.Raycast(ray, out hit, mainCam.farClipPlane))
			{
				Vector3 screenPos = mainCam.WorldToScreenPoint(transform.position);
				EventDelegate.Add(menu.onSelection, OnMenuSelection);
				menu.Show(screenPos);
			}
		}
	}
	
	void OnMenuSelection()
	{
		Debug.Log("ScriptedMenuHandler.OnMenuSelection() "+this+" "+CtxMenu.current.selectedItem);
	}
}
