//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Inspector class used to edit UIPopupLists.
/// </summary>

[CustomEditor(typeof(UIPopupList))]
public class UIPopupListInspector : UIWidgetContainerEditor
{
	UIPopupList mList;

	void RegisterUndo ()
	{
		NGUIEditorTools.RegisterUndo("Popup List Change", mList);
	}

	void OnSelectAtlas (MonoBehaviour obj)
	{
		RegisterUndo();
		mList.atlas = obj as UIAtlas;
	}
	
	void OnSelectFont (MonoBehaviour obj)
	{
		RegisterUndo();
		mList.font = obj as UIFont;
	}

	void OnBackground (string spriteName)
	{
		RegisterUndo();
		mList.backgroundSprite = spriteName;
		Repaint();
	}

	void OnHighlight (string spriteName)
	{
		RegisterUndo();
		mList.highlightSprite = spriteName;
		Repaint();
	}

	public override void OnInspectorGUI ()
	{
		NGUIEditorTools.SetLabelWidth(80f);
		mList = target as UIPopupList;

		ComponentSelector.Draw<UIAtlas>(mList.atlas, OnSelectAtlas);
		ComponentSelector.Draw<UIFont>(mList.font, OnSelectFont);

		GUILayout.BeginHorizontal();
		UILabel lbl = EditorGUILayout.ObjectField("Text Label", mList.textLabel, typeof(UILabel), true) as UILabel;

		if (mList.textLabel != lbl)
		{
			RegisterUndo();
			mList.textLabel = lbl;
			if (lbl != null) lbl.text = mList.value;
		}
		GUILayout.Space(44f);
		GUILayout.EndHorizontal();

		if (mList.textLabel == null)
		{
			EditorGUILayout.HelpBox("This popup list has no label to update, so it will behave like a menu.", MessageType.Info);
		}

		if (mList.atlas != null)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Space(6f);
			GUILayout.Label("Options");
			GUILayout.EndHorizontal();

			string text = "";
			foreach (string s in mList.items) text += s + "\n";

			GUILayout.Space(-14f);
			GUILayout.BeginHorizontal();
			GUILayout.Space(84f);
			string modified = EditorGUILayout.TextArea(text, GUILayout.Height(100f));
			GUILayout.EndHorizontal();

			if (modified != text)
			{
				RegisterUndo();
				string[] split = modified.Split(new char[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
				mList.items.Clear();
				foreach (string s in split) mList.items.Add(s);

				if (string.IsNullOrEmpty(mList.value) || !mList.items.Contains(mList.value))
				{
					mList.value = mList.items.Count > 0 ? mList.items[0] : "";
				}
			}

			string sel = NGUIEditorTools.DrawList("Default", mList.items.ToArray(), mList.value);

			if (mList.value != sel)
			{
				RegisterUndo();
				mList.value = sel;
			}

			UIPopupList.Position pos = (UIPopupList.Position)EditorGUILayout.EnumPopup("Position", mList.position);

			if (mList.position != pos)
			{
				RegisterUndo();
				mList.position = pos;
			}

			bool isLocalized = EditorGUILayout.Toggle("Localized", mList.isLocalized);

			if (mList.isLocalized != isLocalized)
			{
				RegisterUndo();
				mList.isLocalized = isLocalized;
			}

			if (NGUIEditorTools.DrawHeader("Appearance"))
			{
				NGUIEditorTools.BeginContents();

				NGUIEditorTools.SpriteField("Background", mList.atlas, mList.backgroundSprite, OnBackground);
				NGUIEditorTools.SpriteField("Highlight", mList.atlas, mList.highlightSprite, OnHighlight);

				EditorGUILayout.Space();

				Color tc = EditorGUILayout.ColorField("Text Color", mList.textColor);
				Color bc = EditorGUILayout.ColorField("Background", mList.backgroundColor);
				Color hc = EditorGUILayout.ColorField("Highlight", mList.highlightColor);

				if (mList.textColor != tc ||
					mList.highlightColor != hc ||
					mList.backgroundColor != bc)
				{
					RegisterUndo();
					mList.textColor = tc;
					mList.backgroundColor = bc;
					mList.highlightColor = hc;;
				}

				EditorGUILayout.Space();

				GUILayout.BeginHorizontal();
				GUILayout.Label("Padding", GUILayout.Width(76f));
				Vector2 padding = mList.padding;
				padding.x = EditorGUILayout.FloatField(padding.x);
				padding.y = EditorGUILayout.FloatField(padding.y);
				GUILayout.Space(18f);
				GUILayout.EndHorizontal();

				if (mList.padding != padding)
				{
					RegisterUndo();
					mList.padding = padding;
				}

				float ts = EditorGUILayout.FloatField("Text Scale", mList.textScale, GUILayout.Width(120f));

				if (mList.textScale != ts)
				{
					RegisterUndo();
					mList.textScale = ts;
				}

				bool isAnimated = EditorGUILayout.Toggle("Animated", mList.isAnimated);

				if (mList.isAnimated != isAnimated)
				{
					RegisterUndo();
					mList.isAnimated = isAnimated;
				}
				NGUIEditorTools.EndContents();
			}

			NGUIEditorTools.DrawEvents("On Value Change", mList, mList.onChange);
		}
	}
}
