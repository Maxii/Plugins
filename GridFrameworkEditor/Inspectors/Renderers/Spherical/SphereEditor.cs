using UnityEngine;
using UnityEditor;
using GridFramework.Renderers.Spherical;

namespace GridFramework.Editor.Renderers.Spherical {
	/// <summary>
	///   Inspector for spherical sphere renderers.
	/// </summary>
	[CustomEditor (typeof(Sphere))]
	public class SphereEditor : RendererEditor<Sphere> {
#region  Implementation
		protected override void SpecificFields() {
			EditorGUIUtility.labelWidth = 75;
			Altitude();
			Longitude();
			Latitude();
			Smoothness();
			EditorGUIUtility.labelWidth = 0;
		}
#endregion  // Implementation

#region  Fields
		private void Altitude() {
			GUILayout.Label("Altitude");
			EditorGUILayout.BeginHorizontal();
			++EditorGUI.indentLevel; {
				_renderer.AltFrom = EditorGUILayout.FloatField("From", _renderer.AltFrom);
				_renderer.AltTo   = EditorGUILayout.FloatField("To"  , _renderer.AltTo  );
			}
			--EditorGUI.indentLevel;
			EditorGUILayout.EndHorizontal();
		}

		private void Longitude() {
			GUILayout.Label("Longitude");
			EditorGUILayout.BeginHorizontal();
			++EditorGUI.indentLevel; {
				_renderer.LonFrom = EditorGUILayout.FloatField("From", _renderer.LonFrom);
				_renderer.LonTo   = EditorGUILayout.FloatField("To"  , _renderer.LonTo  );
			}
			--EditorGUI.indentLevel;
			EditorGUILayout.EndHorizontal();
		}

		private void Latitude() {
			GUILayout.Label("Latitude");
			EditorGUILayout.BeginHorizontal();
			++EditorGUI.indentLevel; {
				_renderer.LatFrom = EditorGUILayout.FloatField("From", _renderer.LatFrom);
				_renderer.LatTo   = EditorGUILayout.FloatField("To"  , _renderer.LatTo  );
			}
			--EditorGUI.indentLevel;
			EditorGUILayout.EndHorizontal();
		}

		private void Smoothness() {
			GUILayout.Label("Smoothness");
			EditorGUILayout.BeginHorizontal();
			++EditorGUI.indentLevel; {
				_renderer.SmoothP = EditorGUILayout.IntField("Parallels", _renderer.SmoothP);
				_renderer.SmoothM = EditorGUILayout.IntField("Meridians" , _renderer.SmoothM);
			}
			--EditorGUI.indentLevel;
			EditorGUILayout.EndHorizontal();
		}
#endregion  // Fields
	}
}
