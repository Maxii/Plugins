using UnityEngine;
using UnityEditor;
using GridFramework.Renderers.Polar;

namespace GridFramework.Editor.Renderers.Polar {
	/// <summary>
	///   Inspector for polar cylinder renderers.
	/// </summary>
	[CustomEditor(typeof(Cylinder))]
	public class CylinderEditor : RendererEditor<Cylinder> {
#region  Implementation
		protected override void SpecificFields() {
			Smoothness();
			EditorGUIUtility.labelWidth = 50;
			Radial();
			Sector();
			Layer();
			EditorGUIUtility.labelWidth = 0;
		}
#endregion  // Implementation

#region  Fields
		private void Smoothness() {
			_renderer.Smoothness = EditorGUILayout.IntField("Smoothness", _renderer.Smoothness);
		}

		private void Radial() {
			GUILayout.Label("Radial");
			EditorGUILayout.BeginHorizontal();
			++EditorGUI.indentLevel; {
				_renderer.RadialFrom = EditorGUILayout.FloatField("From", _renderer.RadialFrom);
				_renderer.RadialTo   = EditorGUILayout.FloatField("To"  , _renderer.RadialTo  );
			}
			--EditorGUI.indentLevel;
			EditorGUILayout.EndHorizontal();
		}

		private void Sector() {
			GUILayout.Label("Sector");
			EditorGUILayout.BeginHorizontal();
			++EditorGUI.indentLevel; {
				_renderer.SectorFrom = EditorGUILayout.FloatField("From", _renderer.SectorFrom);
				_renderer.SectorTo   = EditorGUILayout.FloatField("To"  , _renderer.SectorTo  );
			}
			--EditorGUI.indentLevel;
			EditorGUILayout.EndHorizontal();
		}

		private void Layer() {
			GUILayout.Label("Layer");
			EditorGUILayout.BeginHorizontal();
			++EditorGUI.indentLevel; {
				_renderer.LayerFrom = EditorGUILayout.FloatField("From", _renderer.LayerFrom);
				_renderer.LayerTo   = EditorGUILayout.FloatField("To"  , _renderer.LayerTo  );
			}
			--EditorGUI.indentLevel;
			EditorGUILayout.EndHorizontal();
		}
#endregion  // Fields
	}
}
