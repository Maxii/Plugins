using UnityEditor;
using GridFramework.Renderers.Hexagonal;

namespace GridFramework.Editor.Renderers.Hexagonal {
	/// <summary>
	///   Inspector for hexagonal herringbone renderers.
	/// </summary>
	[CustomEditor (typeof(Herringbone))]
	public class HerringboneEditor : RendererEditor<Herringbone> {
#region  Implementation
		protected override void SpecificFields() {
			Shift();
			From();
			To();
		}
#endregion  // Implementation

#region  Fields
		private void Shift() {
			_renderer.Shift = (Herringbone.OddColumnShift)
				EditorGUILayout.EnumPopup("Shift", _renderer.Shift);
		}

		private void From() {
			_renderer.From = EditorGUILayout.Vector3Field("From", _renderer.From);
		}

		private void To() {
			_renderer.To   = EditorGUILayout.Vector3Field("To"  , _renderer.To  );
		}
#endregion  // Fields
	}
}
