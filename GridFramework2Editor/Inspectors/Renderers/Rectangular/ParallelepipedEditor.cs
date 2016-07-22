using UnityEditor;
using GridFramework.Renderers.Rectangular;

namespace GridFramework.Editor.Renderers.Rectangular {
	/// <summary>
	///   Inspector for rectangular parallelepiped renderers.
	/// </summary>
	[CustomEditor (typeof(Parallelepiped))]
	public class ParallelepipedEditor : RendererEditor<Parallelepiped> {
#region  Implementation
		protected override void SpecificFields() {
			_renderer.From = EditorGUILayout.Vector3Field("From", _renderer.From);
			_renderer.To   = EditorGUILayout.Vector3Field("To"  , _renderer.To  );
		}
#endregion  // Implementation
	}
}
