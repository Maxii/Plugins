using UnityEngine;
using UnityEditor;
using GridFramework.Renderers.Hexagonal;

namespace GridFramework.Editor.Renderers.Hexagonal {
	/// <summary>
	///   Inspector for hexagonal cone renderers.
	/// </summary>
	[CustomEditor (typeof(Cone))]
	public class ConeEditor : RendererEditor<Cone> {
#region  Implementation
		protected override void SpecificFields() {
			EditorGUIUtility.labelWidth = 80;
			Origin();
			Radius();
			Hex();
			Layer();
			EditorGUIUtility.labelWidth = 0;
		}
#endregion  // Implementation

#region  Fields
		private void Origin() {
			GUILayout.Label("Origin");
			EditorGUILayout.BeginHorizontal();
			++EditorGUI.indentLevel; {
				_renderer.OriginX = EditorGUILayout.IntField("X", _renderer.OriginX);
				_renderer.OriginY = EditorGUILayout.IntField("Y", _renderer.OriginY);
			}
			--EditorGUI.indentLevel;
			EditorGUILayout.EndHorizontal();
		}

		private void Radius() {
			GUILayout.Label("Radius");
			EditorGUILayout.BeginHorizontal();
			++EditorGUI.indentLevel; {
				_renderer.RadiusFrom = EditorGUILayout.IntField("From", _renderer.RadiusFrom);
				_renderer.RadiusTo   = EditorGUILayout.IntField("To"  , _renderer.RadiusTo  );
			}
			--EditorGUI.indentLevel;
			EditorGUILayout.EndHorizontal();
		}

		private void Hex() {
			GUILayout.Label("Hex");
			EditorGUILayout.BeginHorizontal();
			++EditorGUI.indentLevel; {
				_renderer.HexFrom = EditorGUILayout.IntField("From", _renderer.HexFrom);
				_renderer.HexTo   = EditorGUILayout.IntField("To"  , _renderer.HexTo  );
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
