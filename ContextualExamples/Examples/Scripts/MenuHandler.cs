//=========================================================
// Contextual: Context Menus for Unity & NGUI
// Copyright © 2013 Troy Heere
//=========================================================
using UnityEngine;
using System.Collections;

public class MenuHandler : MonoBehaviour
{
	enum Command
	{
		Small,
		Medium,
		Large,
		Spin,
		Reset,
		Cancel
	}
	
	CtxObject contextObject;
	Vector3 baseRotation;
	Vector3 baseScale;
	Vector3 currentRotation;
	Vector3 currentScale;
	
	void Start()
	{
		contextObject = GetComponent<CtxObject>();
		currentRotation = baseRotation = transform.rotation.eulerAngles;
		currentScale = baseScale = transform.localScale;
	}
	
	void OnMenuSelection(int cmd)
	{
		Debug.Log ("MenuHandler.OnMenuSelection("+cmd+")");
		
		switch ((Command)cmd)
		{
		case Command.Small:
			currentScale = baseScale * 0.5f;
			transform.localScale = currentScale;
			break;
		case Command.Medium:
			currentScale = baseScale;
			transform.localScale = currentScale;
			break;
		case Command.Large:
			currentScale = baseScale * 1.5f;
			transform.localScale = currentScale;
			break;
		case Command.Spin:
			currentRotation.x += 30f;
			if (currentRotation.x > 360f)
				currentRotation.x -= 360f;
			else if (currentRotation.x < 0f)
				currentRotation.x += 360f;
			transform.eulerAngles = currentRotation;
			break;
		case Command.Reset:
			transform.localScale = baseScale;
			transform.eulerAngles = baseRotation;
			contextObject.contextMenu.SetChecked((int)Command.Medium, true);
			break;
		case Command.Cancel:
			break;
		}
	}
}
