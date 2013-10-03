//=========================================================
// Contextual: Context Menus for Unity & NGUI
// Copyright © 2013 Troy Heere
//=========================================================
using UnityEngine;
using UnityEditor;
using System.Collections;

/// <summary>
/// Inspector editor class for CtxMenu.
/// </summary>
[CustomEditor(typeof(CtxMenu))]
public class CtxMenuInspector : CtxMenuItemInspector
{
	CtxMenu contextMenu;
	bool refresh = false;
	
	enum Flags
	{
		EditItems =			(1<<0),
		EditBackground = 	(1<<1),
		EditHighlight =		(1<<2),
		EditText =			(1<<3),
		EditCheckmark =		(1<<4),
		EditSubmenu =		(1<<5),
		EditSeparator =		(1<<6),
		EditPie =			(1<<7),
		EditAnimation =		(1<<8),
		EditShadow =		(1<<9),
		EditAudio =			(1<<10),
	}

	public override void RegisterUndo()
	{
		NGUIEditorTools.RegisterUndo("Context Menu Change", contextMenu);
		refresh = true;
	}

	void OnSelectAtlas(MonoBehaviour obj)
	{
		RegisterUndo();
		contextMenu.atlas = obj as UIAtlas;
	}
	
	void OnSelectFont(MonoBehaviour obj)
	{
		RegisterUndo();
		contextMenu.font = obj as UIFont;
	}

	void OnBackground(string spriteName)
	{
		RegisterUndo();
		contextMenu.backgroundSprite = spriteName;
		Repaint();
	}
	
	void OnShadow(string spriteName)
	{
		RegisterUndo();
		contextMenu.shadowSprite = spriteName;
		Repaint();
	}

	void OnHighlight(string spriteName)
	{
		RegisterUndo();
		contextMenu.highlightSprite = spriteName;
		Repaint();
	}
	
	void OnCheckmark(string spriteName)
	{
		RegisterUndo();
		contextMenu.checkmarkSprite = spriteName;
		Repaint();
	}
	
	void OnSubmenuIndicator(string spriteName)
	{
		RegisterUndo();
		contextMenu.submenuIndicatorSprite = spriteName;
		Repaint();
	}

	void OnSeparator(string spriteName)
	{
		RegisterUndo();
		contextMenu.separatorSprite = spriteName;
		Repaint();
	}

	public override void OnInspectorGUI()
	{
		contextMenu = target as CtxMenu;
		
		EditorGUIUtility.LookLikeControls(80f);
		
		ComponentSelector.Draw<UIAtlas>(contextMenu.atlas, OnSelectAtlas);
		
		EditorGUILayout.BeginHorizontal();
		
		CtxMenu.Style style = (CtxMenu.Style)EditorGUILayout.EnumPopup("Style", contextMenu.style,GUILayout.Width(180f));
		if (contextMenu.style != style)
		{
			RegisterUndo();
			contextMenu.style = style;
			
			if (style == CtxMenu.Style.Pie)
			{
				if (contextMenu.menuBar)
				{
					contextMenu.menuBar = false;
					CtxHelper.DestroyAllChildren(contextMenu.transform);
					
					UIPanel panel = NGUITools.FindInParents<UIPanel>(contextMenu.gameObject);
					if (panel != null)
						panel.Refresh();
					
					refresh = false;
				}
			}
		}
		
		if (contextMenu.style != CtxMenu.Style.Pie)
		{
			GUILayout.Space(32f);
			
			bool menuBar = EditorGUILayout.Toggle("Menu Bar", contextMenu.menuBar);
			if (contextMenu.menuBar != menuBar)
			{
				RegisterUndo();
				contextMenu.menuBar = menuBar;
			}
		}
		
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal();
		
		if (contextMenu.style != CtxMenu.Style.Pie)
		{
			UIWidget.Pivot pivot = (UIWidget.Pivot)EditorGUILayout.EnumPopup("Pivot", contextMenu.pivot,GUILayout.Width(180f));
			if (contextMenu.pivot != pivot)
			{
				RegisterUndo();
				contextMenu.pivot = pivot;
			}

			GUILayout.Space(32f);
		}
		
		bool isLocalized = EditorGUILayout.Toggle("Localized", contextMenu.isLocalized);
		if (contextMenu.isLocalized != isLocalized)
		{
			RegisterUndo();
			contextMenu.isLocalized = isLocalized;
		}

		EditorGUILayout.EndHorizontal();

		Vector2 padding = CompactVector2Field("Padding", contextMenu.padding);	//EditorGUILayout.Vector2Field("Padding", contextMenu.padding);
		if (contextMenu.padding != padding)
		{
			RegisterUndo();
			contextMenu.padding = padding;
		}
		
		EditorGUIUtility.LookLikeControls(100f);
		
		GameObject obj = EditorGUILayout.ObjectField("Event Receiver", contextMenu.eventReceiver, typeof(GameObject), true) as GameObject;
		string func = EditorGUILayout.TextField("Event Function", contextMenu.functionName);

		if (contextMenu.eventReceiver != obj || contextMenu.functionName != func)
		{
			RegisterUndo();
			
			contextMenu.eventReceiver = obj;
			contextMenu.functionName = func;
		}
		
		func = EditorGUILayout.TextField("Show Function", contextMenu.showFunction);
		if (contextMenu.showFunction != func)
		{
			RegisterUndo();
			contextMenu.showFunction = func;
		}
		
		func = EditorGUILayout.TextField("Hide Function", contextMenu.hideFunction);
		if (contextMenu.hideFunction != func)
		{
			RegisterUndo();
			contextMenu.hideFunction = func;
		}
		
		EditorGUILayout.Space();

		EditorGUIUtility.LookLikeControls(80f);
		
		Rect box = EditorGUILayout.BeginVertical();
		GUI.Box(box, "");

		if (EditorFoldout(Flags.EditBackground, "Background Options:"))
		{
			contextMenu.backgroundSprite = EditSprite(contextMenu.atlas, "Sprite", contextMenu.backgroundSprite, OnBackground);
			Color backgroundColor = EditorGUILayout.ColorField("Normal", contextMenu.backgroundColor);
			if (contextMenu.backgroundColor != backgroundColor)
			{
				RegisterUndo();
				contextMenu.backgroundColor = backgroundColor;
			}
			
			Color highlightedColor, disabledColor;
			
			if (contextMenu.style == CtxMenu.Style.Pie)
			{
				highlightedColor = EditorGUILayout.ColorField("Highlighted", contextMenu.backgroundColorSelected);
				if (contextMenu.backgroundColorSelected != highlightedColor)
				{
					RegisterUndo();
					contextMenu.backgroundColorSelected = highlightedColor;
				}
				
				disabledColor = EditorGUILayout.ColorField("Disabled", contextMenu.backgroundColorDisabled);
				if (contextMenu.backgroundColorDisabled != disabledColor)
				{
					RegisterUndo();
					contextMenu.backgroundColorDisabled = disabledColor;
				}
			}
			
			GUILayout.Space(4f);
		}
		
		EditorGUILayout.EndVertical();
		EditorGUILayout.Space();
		
		if (contextMenu.style == CtxMenu.Style.Pie)
		{
			EditorGUIUtility.LookLikeControls(100f);
			
			box = EditorGUILayout.BeginVertical();
			GUI.Box(box, "");
			if (EditorFoldout(Flags.EditPie, "Pie Menu Options:"))
			{
				GUILayout.Space(4f);
				
				float pieRadius = EditorGUILayout.FloatField("Radius", contextMenu.pieRadius);
				if (contextMenu.pieRadius != pieRadius)
				{
					RegisterUndo();
					contextMenu.pieRadius = pieRadius;
				}
				
				float pieStartingAngle = EditorGUILayout.FloatField("Starting Angle", contextMenu.pieStartingAngle);
				if (contextMenu.pieStartingAngle != pieStartingAngle)
				{
					RegisterUndo();
					contextMenu.pieStartingAngle = pieStartingAngle;
				}
				
				float pieArc = EditorGUILayout.FloatField("Placement Arc", contextMenu.pieArc);
				if (contextMenu.pieArc != pieArc)
				{
					RegisterUndo();
					contextMenu.pieArc = pieArc;
				}
				
				bool pieCenterItem = EditorGUILayout.Toggle("Center Items", contextMenu.pieCenterItem);
				if (contextMenu.pieCenterItem != pieCenterItem)
				{
					RegisterUndo();
					contextMenu.pieCenterItem = pieCenterItem;
				}
			
				GUILayout.Space(4f);
			}
			EditorGUILayout.EndVertical();
			EditorGUILayout.Space();
			
			EditorGUIUtility.LookLikeControls(80f);
		}
		else
		{
			box = EditorGUILayout.BeginVertical();
			GUI.Box(box, "");
			if (EditorFoldout(Flags.EditHighlight, "Highlight Options:"))
			{
				GUILayout.Space(4f);
				contextMenu.highlightSprite = EditSprite(contextMenu.atlas, "Sprite", contextMenu.highlightSprite, OnHighlight);
				Color highlightColor = EditorGUILayout.ColorField("Color", contextMenu.highlightColor);
				if (contextMenu.highlightColor != highlightColor)
				{
					RegisterUndo();
					contextMenu.highlightColor = highlightColor;
				}
			
				GUILayout.Space(4f);
			}
			EditorGUILayout.EndVertical();
			EditorGUILayout.Space();
		}
		
		box = EditorGUILayout.BeginVertical();
		GUI.Box(box, "");
		if (EditorFoldout(Flags.EditText, "Text Options:"))
		{
			GUILayout.Space(4f);
			ComponentSelector.Draw<UIFont>(contextMenu.font, OnSelectFont);
			if (contextMenu.font == null)
				EditorGUILayout.HelpBox("Warning: please select a valid font if you want this menu to behave correctly.", MessageType.Warning);
			
			float labelScale = EditorGUILayout.FloatField("Scale", contextMenu.labelScale);
			if (contextMenu.labelScale != labelScale)
			{
				RegisterUndo();
				contextMenu.labelScale = labelScale;
			}
			
			Color normalColor = EditorGUILayout.ColorField("Normal", contextMenu.labelColorNormal);
			if (contextMenu.labelColorNormal != normalColor)
			{
				RegisterUndo();
				contextMenu.labelColorNormal = normalColor;
			}
			
			Color highlightedColor = EditorGUILayout.ColorField("Highlighted", contextMenu.labelColorSelected);
			if (contextMenu.labelColorSelected != highlightedColor)
			{
				RegisterUndo();
				contextMenu.labelColorSelected = highlightedColor;
			}
			
			Color disabledColor = EditorGUILayout.ColorField("Disabled", contextMenu.labelColorDisabled);
			if (contextMenu.labelColorDisabled != disabledColor)
			{
				RegisterUndo();
				contextMenu.labelColorDisabled = disabledColor;
			}
			
			GUILayout.Space(4f);
		}
		EditorGUILayout.EndVertical();
		EditorGUILayout.Space();
		
		box = EditorGUILayout.BeginVertical();
		GUI.Box(box, "");
		if (EditorFoldout(Flags.EditCheckmark, "Checkmark Options:"))
		{
			GUILayout.Space(4f);
			contextMenu.checkmarkSprite = EditSprite(contextMenu.atlas, "Sprite", contextMenu.checkmarkSprite, OnCheckmark);
			Color checkmarkColor = EditorGUILayout.ColorField("Color", contextMenu.checkmarkColor);
			if (contextMenu.checkmarkColor != checkmarkColor)
			{
				RegisterUndo();
				contextMenu.checkmarkColor = checkmarkColor;
			}
			
			GUILayout.Space(4f);
		}
		EditorGUILayout.EndVertical();
		EditorGUILayout.Space();
				
		box = EditorGUILayout.BeginVertical();
		GUI.Box(box, "");
		if (EditorFoldout(Flags.EditSubmenu, "Submenu Options:"))
		{
			GUILayout.Space(4f);
	
			contextMenu.submenuIndicatorSprite =
				EditSprite(contextMenu.atlas, "Indicator", contextMenu.submenuIndicatorSprite, OnSubmenuIndicator);
			
			Color submenuIndColor = EditorGUILayout.ColorField("Color", contextMenu.submenuIndicatorColor);
			if (contextMenu.submenuIndicatorColor != submenuIndColor)
			{
				RegisterUndo();
				contextMenu.submenuIndicatorColor = submenuIndColor;
			}
			
			float submenuTimeDelay = EditorGUILayout.FloatField("Show Delay", contextMenu.submenuTimeDelay);
			if (contextMenu.submenuTimeDelay != submenuTimeDelay)
			{
				RegisterUndo();
				contextMenu.submenuTimeDelay = submenuTimeDelay;
			}
			
			GUILayout.Space(4f);
		}
		EditorGUILayout.EndVertical();
		EditorGUILayout.Space();

		if (contextMenu.style != CtxMenu.Style.Pie)
		{
			box = EditorGUILayout.BeginVertical();
			GUI.Box(box, "");
			if (EditorFoldout(Flags.EditSeparator, "Separator Options:"))
			{
				GUILayout.Space(4f);
				contextMenu.separatorSprite = EditSprite(contextMenu.atlas, "Sprite", contextMenu.separatorSprite, OnSeparator);
				Color separatorColor = EditorGUILayout.ColorField("Color", contextMenu.separatorColor);
				if (contextMenu.separatorColor != separatorColor)
				{
					RegisterUndo();
					contextMenu.separatorColor = separatorColor;
				}
			
				GUILayout.Space(4f);
			}
			EditorGUILayout.EndVertical();
			EditorGUILayout.Space();
		}
		
		if (! contextMenu.menuBar)
		{
			box = EditorGUILayout.BeginVertical();
			GUI.Box(box, "");
			
			if (EditorFoldout(Flags.EditAnimation, "Animation Options:"))
			{
				bool isAnimated = EditorGUILayout.Toggle("Animated", contextMenu.isAnimated);
				if (contextMenu.isAnimated != isAnimated)
				{
					RegisterUndo();
					contextMenu.isAnimated = isAnimated;
				}
				
				float animationDuration = EditorGUILayout.FloatField("Duration", contextMenu.animationDuration);
				if (contextMenu.animationDuration != animationDuration)
				{
					RegisterUndo();
					contextMenu.animationDuration = animationDuration;
				}
				
				EditorGUIUtility.LookLikeControls(100f);
	
				CtxMenu.GrowDirection growDirection = (CtxMenu.GrowDirection)EditorGUILayout.EnumPopup("Grow Direction", 
					contextMenu.growDirection, GUILayout.Width(192f));
					
				if (contextMenu.growDirection != growDirection)
				{
					RegisterUndo();
					contextMenu.growDirection = growDirection;
				}
			
				GUILayout.Space(4f);
			}
			
			EditorGUILayout.EndVertical();
			EditorGUILayout.Space();
		}
		
		box = EditorGUILayout.BeginVertical();
		GUI.Box(box, "");

		if (EditorFoldout(Flags.EditShadow, "Shadow Options:"))
		{
			contextMenu.shadowSprite = EditSprite(contextMenu.atlas, "Sprite", contextMenu.shadowSprite, OnShadow);
			Color shadowColor = EditorGUILayout.ColorField("Color", contextMenu.shadowColor);
			if (contextMenu.shadowColor != shadowColor)
			{
				RegisterUndo();
				contextMenu.shadowColor = shadowColor;
			}
			
			Vector2 shadowOffset = CompactVector2Field("Offset", contextMenu.shadowOffset);
			if (shadowOffset != contextMenu.shadowOffset)
			{
				RegisterUndo();
				contextMenu.shadowOffset = shadowOffset;
			}
			
			Vector2 shadowSizeDelta = CompactVector2Field("Size +/-", contextMenu.shadowSizeDelta);
			if (shadowSizeDelta != contextMenu.shadowSizeDelta)
			{
				RegisterUndo();
				contextMenu.shadowSizeDelta = shadowSizeDelta;
			}
			
			GUILayout.Space(4f);
		}
		
		EditorGUILayout.EndVertical();
		EditorGUILayout.Space();

		box = EditorGUILayout.BeginVertical();
		GUI.Box(box, "");
		if (EditorFoldout(Flags.EditAudio, "Audio Options:"))
		{
			GUILayout.Space(4f);
			
			EditorGUIUtility.LookLikeControls(70f);
			EditorGUILayout.BeginHorizontal();
			
			AudioClip showSound = EditorGUILayout.ObjectField("Show", contextMenu.showSound, typeof(AudioClip), false) as AudioClip;
			if (contextMenu.showSound != showSound)
			{
				RegisterUndo();
				contextMenu.showSound = showSound;
			}
			GUILayout.Space(20f);
			AudioClip hideSound = EditorGUILayout.ObjectField("Hide", contextMenu.hideSound, typeof(AudioClip), false) as AudioClip;
			if (contextMenu.hideSound != hideSound)
			{
				RegisterUndo();
				contextMenu.hideSound = hideSound;
			}
			
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			
			AudioClip highlightSound = EditorGUILayout.ObjectField("Highlight", contextMenu.highlightSound, typeof(AudioClip), false) as AudioClip;
			if (contextMenu.highlightSound != highlightSound)
			{
				RegisterUndo();
				contextMenu.highlightSound = highlightSound;
			}
			GUILayout.Space(20f);
			AudioClip selectSound = EditorGUILayout.ObjectField("Select", contextMenu.selectSound, typeof(AudioClip), false) as AudioClip;
			if (contextMenu.selectSound != selectSound)
			{
				RegisterUndo();
				contextMenu.selectSound = selectSound;
			}
			
			EditorGUILayout.EndHorizontal();
			EditorGUIUtility.LookLikeControls(100f);
			
			GUILayout.Space(4f);
		}
			
		EditorGUILayout.EndVertical();
		EditorGUILayout.Space();
		
		bool isEditingItems = IsEditing(Flags.EditItems);
		EditMenuItemList(ref contextMenu.items, contextMenu.atlas, true, ref isEditingItems);
		SetEditing(Flags.EditItems, isEditingItems);
		
		if (refresh)
		{
			if (contextMenu.menuBar)
				contextMenu.Refresh();
			
			refresh = false;
		}
		
		// How to tell if undo or redo have been hit:
		else if (contextMenu.menuBar && Event.current.type == EventType.ValidateCommand && (Event.current.commandName == "UndoRedoPerformed"))
			contextMenu.Refresh();
	}
	
	bool EditorFoldout(Flags flag, string label)
	{
		bool isEditing = EditorGUILayout.Foldout(IsEditing(flag), label);
		GUILayout.Space(4f);
		
		SetEditing(flag, isEditing);
		
		return isEditing;
	}
	
	bool IsEditing(Flags flag)
	{
		CtxMenu contextMenu = target as CtxMenu;
		if (contextMenu != null)
			return (contextMenu.editorFlags & ((uint)flag)) != 0;
		
		return false;
	}
	
	void SetEditing(Flags flag, bool editing)
	{
		CtxMenu contextMenu = target as CtxMenu;
		if (contextMenu != null)
		{
			if (editing)
				contextMenu.editorFlags |= ((uint)flag);
			else
				contextMenu.editorFlags &= ~((uint)flag);
		}
	}
	
	Vector2 CompactVector2Field(string label, Vector2 v)
	{
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField(label, GUILayout.Width(76f));
		EditorGUIUtility.LookLikeControls(20f);
		v.x = EditorGUILayout.FloatField("X", v.x, GUILayout.Width(115));
		GUILayout.Space(15f);
		v.y = EditorGUILayout.FloatField("Y", v.y, GUILayout.Width(115));
		EditorGUILayout.EndHorizontal();

		EditorGUIUtility.LookLikeControls(100f);
		
		return v;
	}
}
