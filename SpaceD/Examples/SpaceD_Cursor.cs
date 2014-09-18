using UnityEngine;
using System.Collections;

public class SpaceD_Cursor : MonoBehaviour {
	
	// Variable to hold the cursor textures
	private static Texture2D CursorNormal;
	private static Texture2D CursorActive;
	
	void Start()
	{
		// Try loading the cursors
		CursorNormal = Resources.Load("Cursor/normal") as Texture2D;
		CursorActive = Resources.Load("Cursor/active") as Texture2D;
	}
	
	void Update()
	{
		// Set the custom cursor
		if (Input.GetMouseButton(0))
		{
			if (CursorActive)
				Cursor.SetCursor(CursorActive, new Vector2(0f, 1.0f), CursorMode.Auto);
		}
		else
		{
			if (CursorNormal)
				Cursor.SetCursor(CursorNormal, Vector2.zero, CursorMode.Auto);
		}
	}
}
