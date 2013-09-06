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
			UISlicedSprite bg = NGUITools.AddWidget<UISlicedSprite>(ctxButtonObj);
			bg.name = "Background";
			bg.depth = depth;
			bg.atlas = NGUISettings.atlas;
			bg.transform.localScale = new Vector3(150f, 40f, 1f);
			bg.MakePixelPerfect();

			UILabel lbl = NGUITools.AddWidget<UILabel>(ctxButtonObj);
			lbl.font = NGUISettings.font;
			lbl.text = ctxButtonObj.name;
			lbl.MakePixelPerfect();

			// Attach button and menu components.
			NGUITools.AddWidgetCollider(ctxButtonObj);

			CtxMenuButton menuButton = ctxButtonObj.AddComponent<CtxMenuButton>();
			menuButton.contextMenu = menu;
			menuButton.currentItemLabel = lbl;
			
			ctxButtonObj.AddComponent<UIButton>().tweenTarget = bg.gameObject;
			ctxButtonObj.AddComponent<UIButtonScale>();
			ctxButtonObj.AddComponent<UIButtonOffset>();
			ctxButtonObj.AddComponent<UIButtonSound>();

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
}
