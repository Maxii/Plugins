//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using UnityEditor;
using System;

/// <summary>
/// Inspector class used to edit UILabels.
/// </summary>

[CustomEditor(typeof(UILabel))]
public class UILabelInspector : UIWidgetInspector
{
	UILabel mLabel;

	/// <summary>
	/// Register an Undo command with the Unity editor.
	/// </summary>

	void RegisterUndo () { NGUIEditorTools.RegisterUndo("Label Change", mLabel); }

	/// <summary>
	/// Font selection callback.
	/// </summary>

	void OnSelectFont (MonoBehaviour obj)
	{
		if (mLabel != null)
		{
			NGUIEditorTools.RegisterUndo("Font Selection", mLabel);
			bool resize = (mLabel.font == null);
			mLabel.font = obj as UIFont;
			if (resize) mLabel.MakePixelPerfect();
		}
	}

	protected override bool DrawProperties ()
	{
		mLabel = mWidget as UILabel;
		ComponentSelector.Draw<UIFont>(mLabel.font, OnSelectFont);

		if (mLabel.font != null)
		{
			GUI.skin.textArea.wordWrap = true;
			string text = string.IsNullOrEmpty(mLabel.text) ? "" : mLabel.text;
			text = EditorGUILayout.TextArea(mLabel.text, GUI.skin.textArea, GUILayout.Height(100f));
			if (!text.Equals(mLabel.text)) { RegisterUndo(); mLabel.text = text; }

			UILabel.Overflow ov = (UILabel.Overflow)EditorGUILayout.EnumPopup("Overflow", mLabel.overflowMethod);
			if (ov != mLabel.overflowMethod) { RegisterUndo(); mLabel.overflowMethod = ov; }

			// Only input fields need this setting exposed, and they have their own "is password" setting, so hiding it here.
			//GUILayout.BeginHorizontal();
			//bool password = EditorGUILayout.Toggle("Password", mLabel.password, GUILayout.Width(100f));
			//GUILayout.Label("- hide characters");
			//GUILayout.EndHorizontal();
			//if (password != mLabel.password) { RegisterUndo(); mLabel.password = password; }

			GUILayout.BeginHorizontal();
			bool encoding = EditorGUILayout.Toggle("Encoding", mLabel.supportEncoding, GUILayout.Width(100f));
			GUILayout.Label("use emoticons and colors");
			GUILayout.EndHorizontal();
			if (encoding != mLabel.supportEncoding) { RegisterUndo(); mLabel.supportEncoding = encoding; }

			//GUILayout.EndHorizontal();

			if (encoding && mLabel.font.hasSymbols)
			{
				UIFont.SymbolStyle sym = (UIFont.SymbolStyle)EditorGUILayout.EnumPopup("Symbols", mLabel.symbolStyle, GUILayout.Width(170f));
				if (sym != mLabel.symbolStyle) { RegisterUndo(); mLabel.symbolStyle = sym; }
			}

			GUILayout.BeginHorizontal();
			{
				UILabel.Effect effect = (UILabel.Effect)EditorGUILayout.EnumPopup("Effect", mLabel.effectStyle, GUILayout.Width(170f));
				if (effect != mLabel.effectStyle) { RegisterUndo(); mLabel.effectStyle = effect; }

				if (effect != UILabel.Effect.None)
				{
					Color c = EditorGUILayout.ColorField(mLabel.effectColor);
					if (mLabel.effectColor != c) { RegisterUndo(); mLabel.effectColor = c; }
				}
			}
			GUILayout.EndHorizontal();

			if (mLabel.effectStyle != UILabel.Effect.None)
			{
#if UNITY_3_5 || UNITY_4_0 || UNITY_4_1 || UNITY_4_2
				GUILayout.Label("Distance", GUILayout.Width(70f));
				GUILayout.Space(-34f);
				GUILayout.BeginHorizontal();
				GUILayout.Space(70f);
				Vector2 offset = EditorGUILayout.Vector2Field("", mLabel.effectDistance);
				GUILayout.Space(20f);
				GUILayout.EndHorizontal();
#else
				Vector2 offset = mLabel.effectDistance;

				GUILayout.BeginHorizontal();
				GUILayout.Label("Distance", GUILayout.Width(76f));
				offset.x = EditorGUILayout.FloatField(offset.x);
				offset.y = EditorGUILayout.FloatField(offset.y);
				GUILayout.Space(18f);
				GUILayout.EndHorizontal();
#endif
				if (offset != mLabel.effectDistance)
				{
					RegisterUndo();
					mLabel.effectDistance = offset;
				}
			}

			int count = EditorGUILayout.IntField("Max Lines", mLabel.maxLineCount, GUILayout.Width(100f));
			if (count != mLabel.maxLineCount) { RegisterUndo(); mLabel.maxLineCount = count; }
			return true;
		}
		EditorGUILayout.Space();
		return false;
	}
}
