using UnityEngine;
using UnityEditor;
using GridFramework.Renderers.Hexagonal;

namespace GridFramework.Editor.Renderers.Hexagonal {
	/// <summary>
	///   Inspector for hexagonal rectangular renderers.
	/// </summary>
	[CustomEditor (typeof(Rectangle))]
	public class RectangleEditor : RendererEditor<Rectangle> {
#region  Implementation
		protected override void SpecificFields() {
			Shift();
			Horizontal();
			Vertical();
			Layer();
		}
#endregion  // Implementation

#region  Fields
		private void Shift() {
			_renderer.Shift = (Rectangle.OddColumnShift)
				EditorGUILayout.EnumPopup("Shift", _renderer.Shift);
		}

		private void Horizontal() {
			GUILayout.Label("Horizontal");
			EditorGUILayout.BeginHorizontal();
			++EditorGUI.indentLevel; {
				_renderer.Left  = EditorGUILayout.IntField("Left" , _renderer.Left );
				_renderer.Right = EditorGUILayout.IntField("Right", _renderer.Right);
			}
			--EditorGUI.indentLevel;
			EditorGUILayout.EndHorizontal();
		}

		private void Vertical() {
			GUILayout.Label("Vertical");
			EditorGUILayout.BeginHorizontal();
			++EditorGUI.indentLevel; {
				_renderer.Bottom = EditorGUILayout.IntField("Bottom", _renderer.Bottom);
				_renderer.Top    = EditorGUILayout.IntField("Top"   , _renderer.Top   );
			}
			--EditorGUI.indentLevel;
			EditorGUILayout.EndHorizontal();
		}

		private void Layer() {
			GUILayout.Label("Layer");
			EditorGUILayout.BeginHorizontal();
			++EditorGUI.indentLevel; {
				_renderer.LayerFrom = EditorGUILayout.FloatField("LayerFrom", _renderer.LayerFrom);
				_renderer.LayerTo   = EditorGUILayout.FloatField("LayerTo"  , _renderer.LayerTo  );
			}
			--EditorGUI.indentLevel;
			EditorGUILayout.EndHorizontal();
		}
#endregion  // Fields
	}
}
