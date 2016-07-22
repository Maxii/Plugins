using UnityEngine;
using UnityEditor;

using Vector6 = GridFramework.Vectors.Vector6;

namespace GridFramework.PropertyDrawers {

	[CustomPropertyDrawer(typeof(Vector6))]
	public class Vector6Drawer : PropertyDrawer {

		private const float lineHeight  = 16f;
		private const float linePadding =  2f;

		const float singleLine = 2f * lineHeight + linePadding;
		const float doubleLine = singleLine + lineHeight + linePadding;

		private const int breakWidth = 333;

		public override void OnGUI (Rect position, SerializedProperty property, GUIContent label) {
			var previousIndentLevel = EditorGUI.indentLevel;

			var yz = property.FindPropertyRelative("yz");
			var zx = property.FindPropertyRelative("zx");
			var xy = property.FindPropertyRelative("xy");
			var zy = property.FindPropertyRelative("zy");
			var xz = property.FindPropertyRelative("xz");
			var yx = property.FindPropertyRelative("yx");

			var broken = position.height > singleLine;

			label = EditorGUI.BeginProperty(position, label, property);
			EditorGUI.indentLevel = 0;

			var contentPosition = EditorGUI.PrefixLabel(position, label);

			if (broken) {
				position.height = lineHeight;
				EditorGUI.indentLevel += 1;
				contentPosition = EditorGUI.IndentedRect(position);
				contentPosition.y += lineHeight + linePadding;
			}

			EditorGUIUtility.labelWidth = broken ? 35f : 20f;
			contentPosition.width /= 3f;

			DrawFloat(ref contentPosition, yz, "YZ");
			DrawFloat(ref contentPosition, zx, "ZX");
			DrawFloat(ref contentPosition, xy, "XY");

			contentPosition.x -= 3f * contentPosition.width;
			contentPosition.y += lineHeight + linePadding;
			if (!broken) {
				contentPosition.height -= lineHeight + linePadding;
			}

			DrawFloat(ref contentPosition, zy, "ZY");
			DrawFloat(ref contentPosition, xz, "XZ");
			DrawFloat(ref contentPosition, yx, "YX");

			EditorGUI.EndProperty();
			EditorGUI.indentLevel = previousIndentLevel;
		}

		private void DrawFloat(ref Rect position, SerializedProperty property, string label) {
			EditorGUI.PropertyField(position, property, new GUIContent(label));
			position.x += position.width;
		}

		public override float GetPropertyHeight (SerializedProperty property, GUIContent label) {
			return Screen.width < breakWidth ? doubleLine : singleLine;
		}
	}
}
