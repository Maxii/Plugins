using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(UIInputFocus))]
public class UIInputFocusInspector : Editor
{
	UIInputFocus input;

	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		EditorGUIUtility.labelWidth = 80f;
		input = target as UIInputFocus;

		NGUIEditorTools.DrawProperty("Sprite", serializedObject, "target");

		if (input.target != null)
		{
			SerializedObject obj = new SerializedObject(input.target);
			obj.Update();
			SerializedProperty atlas = obj.FindProperty("mAtlas");

			NGUIEditorTools.DrawSpriteField("Normal", obj, atlas, obj.FindProperty("mSpriteName"));
			NGUIEditorTools.DrawSpriteField("Focused", serializedObject, atlas, serializedObject.FindProperty("selectedSprite"), true);

			NGUIEditorTools.DrawProperty("Normal", obj, "mColor");
			NGUIEditorTools.DrawProperty("Focused", serializedObject, "selectedColor");

			obj.ApplyModifiedProperties();
		}

		serializedObject.ApplyModifiedProperties();
	}
}