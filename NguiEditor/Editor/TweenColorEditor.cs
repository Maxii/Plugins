//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TweenColor))]
public class TweenColorEditor : UITweenerEditor
{
	public override void OnInspectorGUI ()
	{
		GUILayout.Space(6f);
		NGUIEditorTools.SetLabelWidth(120f);

		TweenColor tw = target as TweenColor;
		GUI.changed = false;

		Color from = EditorGUILayout.ColorField("From", tw.from);
		Color to = EditorGUILayout.ColorField("To", tw.to);

		if (GUI.changed)
		{
			NGUIEditorTools.RegisterUndo("Tween Change", tw);
			tw.from = from;
			tw.to = to;
			UnityEditor.EditorUtility.SetDirty(tw);
		}

		DrawCommonProperties();
	}
}
