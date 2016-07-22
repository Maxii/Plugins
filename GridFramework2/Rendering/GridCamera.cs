using UnityEngine;
using GridRenderer = GridFramework.Renderers.GridRenderer;

namespace GridFramework.Rendering {
	/// <summary>
	///   Component for camera objects to render grids at runtime.
	/// </summary>
	/// <remarks>
	///   <para>
	///     At runtime every grid renderer registers itself to a static manager
	///     class. We then loop over those renderers and force them to do the
	///     rendering on this camera.
	///   </para>
	/// </remarks>
	[AddComponentMenu("Grid Framework/Rendering/GridCamera")]
	public sealed class GridCamera : MonoBehaviour {
#region  Private variables
		private Camera _camera;

		[SerializeField]
		private bool _renderWhenNotMain;  // false
#endregion  // Private variables

#region  Properties
		/// <summary>
		///   Whether to render even when this is not the main camera.
		/// </summary>
		public bool RenderWhenNotMain {
			get {
				return _renderWhenNotMain;
			} set {
				_renderWhenNotMain = value;
			}
		}
#endregion  // Properties
		
#region  Callback methods
		void Start(){
			_camera = GetComponent<Camera>();
		}
		
		void OnPostRender(){
			var doRender = false;
			doRender |= _camera == Camera.main;
			doRender |= _renderWhenNotMain;
			if(!doRender) {
				return;
			}

			foreach (var gridRenderer in RendererManager.Renderers) {
				if (gridRenderer.enabled) {
					RenderGrid(gridRenderer);
				}
			}
		}
#endregion  // Callback methods

#region  Instance methods
		private void RenderGrid(GridRenderer gridRenderer) {
			gridRenderer.Refresh();

			var material = gridRenderer.Material;
			var width    = gridRenderer.LineWidth;
			var lines    = gridRenderer.LineSets;
			var axisColors = new [] {
				gridRenderer.ColorX,
				gridRenderer.ColorY,
				gridRenderer.ColorZ
			};

			material.SetPass(0);

			if (Mathf.Abs(width) < Mathf.Epsilon) {
				RenderThinLines(lines, axisColors);
			} else {  // quads for "lines" with width
				RenderWideLines(lines, axisColors, width);
			}
		}

		private void RenderThinLines(Vector3[][][] lineSets, Color[] colors) {
			GL.Begin(GL.LINES);
			for (var i = 0; i < 3; i++) {
				if (colors[i].a < Mathf.Epsilon) {
					continue;
				}

				GL.Color(colors[i]);

				foreach (var line in lineSets[i]) {
					if (line == null) {
						continue;
					}
					GL.Vertex(line[0]);
					GL.Vertex(line[1]);
				}
			}
			GL.End();
		}

		private void RenderWideLines(Vector3[][][] lineSets, Color[] colors, float width) {
			GL.Begin(GL.QUADS);
			
			for (var i = 0; i < 3; i++) {
				if (colors[i].a < Mathf.Epsilon) {
					continue;
				}

				GL.Color(colors[i]);

				foreach (Vector3[] line in lineSets[i]) {
					if (line == null) {
						continue;
					}

					var mult = 0.5f * width; //the multiplier, half the desired width
					var dir = Vector3.Cross(line[0] - line[1], transform.forward).normalized;
					float pixelSize;
					if (_camera.orthographic) {
						pixelSize = 2f * _camera.orthographicSize / _camera.pixelHeight;
					} else {
						var p1 = _camera.ScreenToWorldPoint(50f * Vector3.forward);
						var p2 = _camera.ScreenToWorldPoint(new Vector3(20f, 0, 50f));
						pixelSize = (p1 - p2).magnitude / 20f;
					}
					dir *= pixelSize;

					GL.Vertex(line[0] - mult * dir);
					GL.Vertex(line[0] + mult * dir);
					GL.Vertex(line[1] + mult * dir);
					GL.Vertex(line[1] - mult * dir);
				}
			}
			GL.End();
		}
#endregion  // Instance methods
	}
}
