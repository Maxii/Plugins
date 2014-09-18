using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(UIImageCheckbox))]
public class UIImageCheckboxInspector : Editor
{
	UIImageCheckbox checkbox;

	public override void OnInspectorGUI()
	{
		serializedObject.Update();
		
		NGUIEditorTools.SetLabelWidth(80f);
		checkbox = target as UIImageCheckbox;
		
		NGUIEditorTools.DrawProperty("Sprite", serializedObject, "target");
		
		if (checkbox.target != null)
		{
			SerializedObject obj = new SerializedObject(checkbox.target);
			obj.Update();
			SerializedProperty atlas = obj.FindProperty("mAtlas");
			NGUIEditorTools.DrawSpriteField("Normal", obj, atlas, obj.FindProperty("mSpriteName"));
			obj.ApplyModifiedProperties();
			
			NGUIEditorTools.DrawSpriteField("Hovered", serializedObject, atlas, serializedObject.FindProperty("hoverSprite"), true);
		}
		
		NGUIEditorTools.SetLabelWidth(120f);
		NGUIEditorTools.DrawProperty("Make Pixel Perfect", serializedObject, "makePixelPerfect");
		
		serializedObject.ApplyModifiedProperties();
	}
}