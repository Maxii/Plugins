using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(UIImageThumb))]
public class UIImageThumbInspector : Editor
{
	UIImageThumb thumb;

	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		EditorGUIUtility.labelWidth = 100f;
		thumb = target as UIImageThumb;

		NGUIEditorTools.DrawProperty("Sprite", serializedObject, "target");

		if (thumb.target != null)
		{
			SerializedObject obj = new SerializedObject(thumb.target);
			obj.Update();
			SerializedProperty atlas = obj.FindProperty("mAtlas");
			NGUIEditorTools.DrawSpriteField("Normal", obj, atlas, obj.FindProperty("mSpriteName"));
			obj.ApplyModifiedProperties();
			
			NGUIEditorTools.DrawSpriteField("Hovered", serializedObject, atlas, serializedObject.FindProperty("hoverSprite"), true);
			NGUIEditorTools.DrawSpriteField("Pressed", serializedObject, atlas, serializedObject.FindProperty("pressedSprite"), true);
		}

		serializedObject.ApplyModifiedProperties();
	}
}