//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(UIInput))]
public class UIInputEditor : UIWidgetContainerEditor
{
	public enum DefaultText
	{
		Blank,
		KeepLabelsText,
	}

	public override void OnInspectorGUI ()
	{
		NGUIEditorTools.SetLabelWidth(120f);
		UIInput input = target as UIInput;

		GUILayout.Space(6f);
		GUI.changed = false;

		UILabel label = (UILabel)EditorGUILayout.ObjectField("Input Label", input.label, typeof(UILabel), true);

		if (GUI.changed)
		{
			NGUIEditorTools.RegisterUndo("Input Change", input);
			input.label = label;
			UnityEditor.EditorUtility.SetDirty(input);
		}

		if (input.label != null)
		{
			GUI.changed = false;
			Color ia = EditorGUILayout.ColorField("Inactive Color", input.label.color);

			if (GUI.changed)
			{
				NGUIEditorTools.RegisterUndo("Input Change", input.label);
				input.label.color = ia;
				UnityEditor.EditorUtility.SetDirty(input.label);
			}
		}

		GUI.changed = false;
		Color c = EditorGUILayout.ColorField("Active Color", input.activeColor);

		GUILayout.BeginHorizontal();
		DefaultText dt = input.useLabelTextAtStart ? DefaultText.KeepLabelsText : DefaultText.Blank;
		bool def = (DefaultText)EditorGUILayout.EnumPopup("Default Text", dt) == DefaultText.KeepLabelsText;
		GUILayout.Space(18f);
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		UIInput.KeyboardType type = (UIInput.KeyboardType)EditorGUILayout.EnumPopup("Keyboard Type", input.type);
		GUILayout.Space(18f);
		GUILayout.EndHorizontal();

		GameObject sel = (GameObject)EditorGUILayout.ObjectField("Select on Tab", input.selectOnTab, typeof(GameObject), true);

		if (GUI.changed)
		{
			NGUIEditorTools.RegisterUndo("Input Change", input);
			input.activeColor = c;
			input.type = type;
			input.useLabelTextAtStart = def;
			input.selectOnTab = sel;
			UnityEditor.EditorUtility.SetDirty(input);
		}

		GUI.changed = false;
		GUILayout.BeginHorizontal();
		string pp = EditorGUILayout.TextField("Auto-save Key", input.playerPrefsField);
		GUILayout.Space(18f);
		GUILayout.EndHorizontal();

		int max = EditorGUILayout.IntField("Max Characters", input.maxChars, GUILayout.Width(160f));
		string car = EditorGUILayout.TextField("Carat Character", input.caratChar, GUILayout.Width(160f));
		bool pw = EditorGUILayout.Toggle("Password", input.isPassword);
		bool ac = EditorGUILayout.Toggle("Auto-correct", input.autoCorrect);

		if (GUI.changed)
		{
			NGUIEditorTools.RegisterUndo("Input Change", input);
			input.playerPrefsField = pp;
			input.maxChars = max;
			input.caratChar = car;
			input.isPassword = pw;
			input.autoCorrect = ac;
			UnityEditor.EditorUtility.SetDirty(input);
		}

		NGUIEditorTools.SetLabelWidth(80f);
		NGUIEditorTools.DrawEvents("On Submit", input, input.onSubmit);
	}
}
