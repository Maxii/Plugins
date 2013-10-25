//=========================================================
// Contextual: Context Menus for Unity & NGUI
// Copyright © 2013 Troy Heere
//=========================================================
using UnityEngine;
using UnityEditor;
using System.Collections;

public class CtxMenus
{
	[MenuItem("GameObject/Contextual/Add Context Menu")]
	public static void AddContextMenu()
	{
		GameObject gameObj = NGUIMenu.SelectedRoot();

		if (gameObj != null)
		{
			Undo.RegisterSceneUndo("Add a Context Menu");
			EditorUtility.SetDirty(gameObj);
						
			GameObject ctxMenuObj = new GameObject("Context Menu");
			ctxMenuObj.layer = gameObj.layer;

			Transform ct = ctxMenuObj.transform;
			ct.parent = gameObj.transform;
			ct.localPosition = Vector3.zero;
			ct.localRotation = Quaternion.identity;
			ct.localScale = Vector3.one;

			ctxMenuObj.AddComponent<CtxMenu>();
			Selection.activeGameObject = ctxMenuObj;
		}
	}

	[MenuItem("GameObject/Contextual/Add Menu Button")]
	public static void AddMenuButton()
	{
		GameObject rootObj = NGUIMenu.SelectedRoot();

		if (rootObj != null)
		{
			Undo.RegisterSceneUndo("Add a Menu Button");
			EditorUtility.SetDirty(rootObj);
			
			// Create a menu object.
			GameObject ctxMenuObj = new GameObject("Button Menu");
			ctxMenuObj.layer = rootObj.layer;

			Transform ct = ctxMenuObj.transform;
			ct.parent = rootObj.transform;
			ct.localPosition = Vector3.zero;
			ct.localRotation = Quaternion.identity;
			ct.localScale = Vector3.one;

			CtxMenu menu = ctxMenuObj.AddComponent<CtxMenu>();
			menu.atlas = PickAtlas();
			menu.font = PickFont();
			
			// Create button object.
			GameObject ctxButtonObj = new GameObject("Menu Button");
			ctxButtonObj.layer = rootObj.layer;

			ct = ctxButtonObj.transform;
			ct.parent = rootObj.transform;
			ct.localPosition = Vector3.zero;
			ct.localRotation = Quaternion.identity;
			ct.localScale = Vector3.one;
			
			int depth = NGUITools.CalculateNextDepth(ctxButtonObj);
			
			// Create child objects.
			
			UISprite bg = NGUITools.AddSprite(ctxButtonObj, PickAtlas(), "Button");
			bg.name = "Background";
			bg.depth = depth;
			bg.width = 150;
			bg.height = 40;
			bg.MakePixelPerfect();

			UILabel lbl = NGUITools.AddWidget<UILabel>(ctxButtonObj);
			lbl.font = PickFont();
			lbl.text = ctxButtonObj.name;
			lbl.color = Color.black;
			lbl.MakePixelPerfect();
			Vector2 size = lbl.printedSize;		// Force NGUI to process metrics before adding collider. Otherwise you get incorrect-sized collider.
			size.x -= 1f;						// Supress compiler warning for unused 'size' variable. Sheesh...

			// Attach button and menu components.
			NGUITools.AddWidgetCollider(ctxButtonObj, true);

			CtxMenuButton menuButton = ctxButtonObj.AddComponent<CtxMenuButton>();
			menuButton.contextMenu = menu;
			menuButton.currentItemLabel = lbl;
			
			ctxButtonObj.AddComponent<UIButton>().tweenTarget = bg.gameObject;
			ctxButtonObj.AddComponent<UIButtonScale>();
			ctxButtonObj.AddComponent<UIButtonOffset>();
			ctxButtonObj.AddComponent<UIPlaySound>();

			Selection.activeGameObject = ctxButtonObj;
		}
	}
	
	[MenuItem("GameObject/Contextual/Add Menu Bar")]
	public static void AddMenuBar()
	{
		GameObject gameObj = NGUIMenu.SelectedRoot();

		if (gameObj != null)
		{
			Undo.RegisterSceneUndo("Add a Menu Bar");
			EditorUtility.SetDirty(gameObj);
						
			GameObject ctxMenuObj = new GameObject("MenuBar");
			ctxMenuObj.layer = gameObj.layer;

			Transform ct = ctxMenuObj.transform;
			ct.parent = gameObj.transform;
			ct.localPosition = Vector3.zero;
			ct.localRotation = Quaternion.identity;
			ct.localScale = Vector3.one;

			CtxMenu ctxMenu = ctxMenuObj.AddComponent<CtxMenu>();
			ctxMenu.menuBar = true;
			ctxMenu.style = CtxMenu.Style.Horizontal;
			
			Selection.activeGameObject = ctxMenuObj;
		}
	}
	
	private static UIAtlas PickAtlas()
	{
		UIAtlas atlas = NGUISettings.atlas;
		
		if (atlas == null)
		{
			UIAtlas[] atlases = Resources.FindObjectsOfTypeAll(typeof(UIAtlas)) as UIAtlas[];
			if (atlases != null)
			{
				foreach (UIAtlas a in atlases)
				{
					if (a.name == "ExampleAtlas")
					{
						atlas = a;
						break;
					}
				}
				
				if (atlas == null && atlases.Length > 0)
					atlas = atlases[0];
			}
		}
		
		return atlas;
	}
	
	private static UIFont PickFont()
	{
		UIFont font = NGUISettings.font;
		
		if (font == null)
		{
			UIFont[] fonts = Resources.FindObjectsOfTypeAll(typeof(UIFont)) as UIFont[];
			if (fonts != null)
			{
				foreach (UIFont f in fonts)
				{
					if (f.name == "MedSans")
					{
						font = f;
						break;
					}
				}
				
				if (fonts == null && fonts.Length > 0)
					font = fonts[0];
			}
		}
		
		return font;
	}
}
