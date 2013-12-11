//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

#if UNITY_IPHONE || UNITY_ANDROID || UNITY_WP8 || UNITY_BLACKBERRY
#define MOBILE
#endif

using UnityEngine;
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(UIInput))]
public class UIInputEditor : UIWidgetContainerEditor
{
	public override void OnInspectorGUI ()
	{
		UIInput input = target as UIInput;
		serializedObject.Update();
		GUILayout.Space(3f);
		NGUIEditorTools.SetLabelWidth(110f);
		//NGUIEditorTools.DrawProperty(serializedObject, "m_Script");

		EditorGUI.BeginDisabledGroup(serializedObject.isEditingMultipleObjects);
		SerializedProperty label = NGUIEditorTools.DrawProperty(serializedObject, "label");
		EditorGUI.EndDisabledGroup();

		EditorGUI.BeginDisabledGroup(label == null || label.objectReferenceValue == null);
		{
			if (Application.isPlaying) NGUIEditorTools.DrawPaddedProperty("Value", serializedObject, "mValue");
			else NGUIEditorTools.DrawPaddedProperty("Starting Value", serializedObject, "mValue");
			NGUIEditorTools.DrawPaddedProperty(serializedObject, "savedAs");
			NGUIEditorTools.DrawProperty("Active Text", serializedObject, "activeTextColor");

			EditorGUI.BeginDisabledGroup(serializedObject.isEditingMultipleObjects);
			{
				if (label != null && label.objectReferenceValue != null)
				{
					SerializedObject ob = new SerializedObject(label.objectReferenceValue);
					ob.Update();
					NGUIEditorTools.DrawProperty("Inactive", ob, "mColor");
					ob.ApplyModifiedProperties();
				}
				else EditorGUILayout.ColorField("Inactive", Color.white);
			}
			EditorGUI.EndDisabledGroup();
#if !MOBILE
			NGUIEditorTools.DrawProperty(serializedObject, "selectOnTab");
#endif
			NGUIEditorTools.DrawPaddedProperty(serializedObject, "inputType");
#if MOBILE
			NGUIEditorTools.DrawPaddedProperty(serializedObject, "keyboardType");
#endif
			NGUIEditorTools.DrawPaddedProperty(serializedObject, "validation");

			SerializedProperty sp = serializedObject.FindProperty("characterLimit");

			GUILayout.BeginHorizontal();

			if (sp.hasMultipleDifferentValues || input.characterLimit > 0)
			{
				EditorGUILayout.PropertyField(sp);
				GUILayout.Space(18f);
			}
			else
			{
				EditorGUILayout.PropertyField(sp);
				GUILayout.Label("unlimited");
			}
			GUILayout.EndHorizontal();

			NGUIEditorTools.SetLabelWidth(80f);
			EditorGUI.BeginDisabledGroup(serializedObject.isEditingMultipleObjects);
			NGUIEditorTools.DrawEvents("On Submit", input, input.onSubmit);
			EditorGUI.EndDisabledGroup();
		}
		EditorGUI.EndDisabledGroup();
		serializedObject.ApplyModifiedProperties();
	}
}
