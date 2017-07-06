using UnityEditor;
using UnityEngine;
using GridFramework.Renderers;

namespace GridFramework.Editor.Renderers {
	/// <summary>
	///   Base class for all renderer inspectors.
	/// </summary>
	/// <typeparam name="T">
	///   Type of renderer the inspector is for
	/// </typeparam>
	/// <remarks>
	///   <para>
	///     You should use this class as the base of your own renderer
	///     components. It displays the common fields and offers a function to
	///     override for your own fields.
	///   </para>
	///   <para>
	///     You do not have to inherit from this class, but it helps to have
	///     uniformity in the look of the inspectors. If you decide to write an
	///     inspector from scratch make sure you have fields for the renderer's
	///     <c>Meterial</c>, <c>Priority</c>, <c>LineWidth</c> and the axis
	///     colours (<c>ColorX</c>, <c>ColorY</c> and <c>ColorZ</c>).
	///   </para>
	/// </remarks>
	public abstract class RendererEditor<T> : UnityEditor.Editor where T: GridRenderer {
#region  Protected variables
		/// <summary>
		///   Reference to the target renderer.
		/// </summary>
		protected T _renderer;
#endregion  // Protected variables

#region  Callback methods
		public override void OnInspectorGUI() {
			InspectorFields();
			
			if (GUI.changed)
				EditorUtility.SetDirty(_renderer);
		}

		void OnEnable () {
			_renderer = target as T;
		}
#endregion  // Callback methods

#region  Field methods
		private void InspectorFields() {
			MaterialFields();
			PriorityFields();
			LineWidthFields();
			ColorFields();
			SpecificFields();
		}

		private void MaterialFields() {
			_renderer.Material = (Material)EditorGUILayout.ObjectField(
				"Material",
				_renderer.Material,
				typeof(Material),
				false
			);
		}

		private void ColorFields() {
			GUILayout.Label("Axis Colors");
			
			EditorGUILayout.BeginHorizontal();
			++EditorGUI.indentLevel; {
				_renderer.ColorX = EditorGUILayout.ColorField(_renderer.ColorX);
				_renderer.ColorY = EditorGUILayout.ColorField(_renderer.ColorY);
				_renderer.ColorZ = EditorGUILayout.ColorField(_renderer.ColorZ);
			}
			--EditorGUI.indentLevel;
			EditorGUILayout.EndHorizontal();
		}

		private void LineWidthFields() {
			var width = EditorGUILayout.FloatField("Line Width", _renderer.LineWidth);
			width = Mathf.Max(width, 0f);

			_renderer.LineWidth = width;
		}

		private void PriorityFields() {
			var priority = _renderer.Priority;
			_renderer.Priority = EditorGUILayout.IntField("Priority", priority);
		}
#endregion  // Field methods

#region  Protected methods
		/// <summary>
		///   Override this for your own inspector fields.
		/// </summary>
		/// <remarks>
		///   <para>
		///     This method will be called after all common fields from within
		///     the <c>InspectorFields()</c> callback. You should implement
		///     your own inspector fields here.
		///   </para>
		/// </remarks>
		protected abstract void SpecificFields();
#endregion  // Protected methods
	}
}
