//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(UIButton))]
public class UIButtonEditor : UIWidgetContainerEditor
{
	public override void OnInspectorGUI ()
	{
		NGUIEditorTools.SetLabelWidth(80f);
		UIButton button = target as UIButton;

		GUILayout.Space(6f);

		GUI.changed = false;
		GameObject tt = (GameObject)EditorGUILayout.ObjectField("Target", button.tweenTarget, typeof(GameObject), true);

		if (GUI.changed)
		{
			NGUIEditorTools.RegisterUndo("Button Change", button);
			button.tweenTarget = tt;
			UnityEditor.EditorUtility.SetDirty(button);
		}

		if (tt != null)
		{
			UIWidget w = tt.GetComponent<UIWidget>();

			if (w != null)
			{
				GUI.changed = false;
				Color c = EditorGUILayout.ColorField("Normal", w.color);

				if (GUI.changed)
				{
					NGUIEditorTools.RegisterUndo("Button Change", w);
					w.color = c;
					UnityEditor.EditorUtility.SetDirty(w);
				}
			}
		}

		GUI.changed = false;
		Color hover = EditorGUILayout.ColorField("Hover", button.hover);
		Color pressed = EditorGUILayout.ColorField("Pressed", button.pressed);
		Color disabled = EditorGUILayout.ColorField("Disabled", button.disabledColor);

		GUILayout.BeginHorizontal();
		float duration = EditorGUILayout.FloatField("Duration", button.duration, GUILayout.Width(120f));
		GUILayout.Label("seconds");
		GUILayout.EndHorizontal();

		GUILayout.Space(3f);

		if (GUI.changed)
		{
			NGUIEditorTools.RegisterUndo("Button Change", button);
			button.hover = hover;
			button.pressed = pressed;
			button.disabledColor = disabled;
			button.duration = duration;
			UnityEditor.EditorUtility.SetDirty(button);
		}
		NGUIEditorTools.DrawEvents("On Click", button, button.onClick);
	}
}
