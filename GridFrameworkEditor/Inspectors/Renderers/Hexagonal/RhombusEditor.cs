using UnityEngine;
using UnityEditor;
using GridFramework.Renderers.Hexagonal;

namespace GridFramework.Editor.Renderers.Hexagonal {
	/// <summary>
	///   Inspector for hexagonal rhombus renderers.
	/// </summary>
	[CustomEditor (typeof(Rhombus))]
	public class RhombusEditor : RendererEditor<Rhombus> {
		protected override void SpecificFields() {
#region  Implementation
			Direction();
			Horizontal();
			Vertical();
			Layer();
		}
#endregion  // Implementation

#region  Fields
		private void Direction() {
			_renderer.Direction = (Rhombus.RhombDirection)
				EditorGUILayout.EnumPopup("Direction", _renderer.Direction);
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
