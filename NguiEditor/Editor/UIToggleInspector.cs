//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(UIToggle))]
public class UIToggleInspector : UIWidgetContainerEditor
{
	enum Transition
	{
		Smooth,
		Instant,
	}

	public override void OnInspectorGUI ()
	{
		NGUIEditorTools.SetLabelWidth(80f);
		UIToggle toggle = target as UIToggle;

		GUILayout.Space(6f);
		GUI.changed = false;

		GUILayout.BeginHorizontal();
		int group = EditorGUILayout.IntField("Group", toggle.group, GUILayout.Width(120f));
		GUILayout.Label(" - zero means 'none'");
		GUILayout.EndHorizontal();

		bool starts = EditorGUILayout.Toggle("Start State", toggle.startsActive);
		bool none = toggle.optionCanBeNone;
		UIWidget w = toggle.activeSprite;
		Animation anim = toggle.activeAnimation;
		bool instant = toggle.instantTween;

		// This is a questionable feature at best... commenting it out for now
		//if (group != 0) none = EditorGUILayout.Toggle("Can Be None", toggle.optionCanBeNone);

		bool changed = GUI.changed;

		if (NGUIEditorTools.DrawHeader("State Transition"))
		{
			NGUIEditorTools.BeginContents();
			anim = EditorGUILayout.ObjectField("Animation", anim, typeof(Animation), true) as Animation;
			w = EditorGUILayout.ObjectField("Sprite", w, typeof(UIWidget), true) as UIWidget;

			Transition tr = instant ? Transition.Instant : Transition.Smooth;
			GUILayout.BeginHorizontal();
			tr = (Transition)EditorGUILayout.EnumPopup("Transition", tr);
			GUILayout.Space(18f);
			GUILayout.EndHorizontal();
			instant = (tr == Transition.Instant);
			NGUIEditorTools.EndContents();
		}

		if (changed || GUI.changed)
		{
			NGUIEditorTools.RegisterUndo("Toggle Change", toggle);
			toggle.group = group;
			toggle.activeSprite = w;
			toggle.activeAnimation = anim;
			toggle.startsActive = starts;
			toggle.instantTween = instant;
			toggle.optionCanBeNone = none;
			UnityEditor.EditorUtility.SetDirty(toggle);
		}

		NGUIEditorTools.DrawEvents("On Value Change", toggle, toggle.onChange);
	}
}
