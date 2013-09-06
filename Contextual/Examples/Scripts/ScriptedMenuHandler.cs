using UnityEngine;
using System.Collections;

public class ScriptedMenuHandler : MonoBehaviour 
{
	public CtxMenu menu;
	
	void Update()
	{
		if (Input.GetMouseButtonUp(0))
		{
			Camera mainCam = Camera.mainCamera;
			RaycastHit hit = new RaycastHit();
			Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
			if (collider.Raycast(ray, out hit, mainCam.farClipPlane))
			{
				Vector3 screenPos = mainCam.WorldToScreenPoint(transform.position);
				menu.onSelection = OnMenuSelection;
				menu.Show(screenPos);
			}
		}
	}
	
	void OnMenuSelection(int selection)
	{
		Debug.Log("ScriptedMenuHandler.OnMenuSelection() "+this+" "+selection);
	}
}
