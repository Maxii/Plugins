using UnityEngine;
using GridFramework.Rendering;

namespace GridFramework.Renderers {
	/// <summary>
	///   Abstract base class for all grid renderers.
	/// </summary>
	/// <remarks>
	///   <para>
	///     In order to be visible in the scene a grid has to be rendered by a
	///     renderer.  A grid renderer is similar to Unity's own
	///     <c>MeshRenderer</c> class in that it does only the displaying job.
	///   </para>
	///   <para>
	///     The shape of the rendered grid depends on the renderer used, to
	///     change the shape you have to assign a different renderer to the
	///     <c>GameObject</c>. This class is the base class for all renderers,
	///     similar to how <c>RectGrid</c> is the base class of all grids.
	///   </para>
	/// </remarks>
	[ExecuteInEditMode]
	public abstract class GridRenderer : MonoBehaviour {

#region  Types
		/// <summary>
		///   Arguments object of a priority event.
		/// </summary>
		public class PriorityEventArgs : System.EventArgs {
			private readonly int _difference;

			/// <summary>
			///   Instantiate a new object form previous and current value.
			/// </summary>
			/// <para name="previous">
			///   Previous value of the priority.
			/// </para>
			/// <para name="current">
			///   Current value of the priority.
			/// </para>
			public PriorityEventArgs(int previous, int current) {
				_difference = current - previous;
			}

			/// <summary>
			///   Change in priority, new value minus old value.
			/// </summary>
			public int Difference {
				get {
					return _difference;
				}
			}
		}

		/// <summary>
		///   Event raised when the priority of the renderer changes.
		/// </summary>
		public event System.EventHandler<PriorityEventArgs> PriorityChanged;
#endregion

#region  Private variables
		[SerializeField] private float    _lineWidth;
		[SerializeField] private int      _priority;
		[SerializeField] private Material _material;
		[SerializeField] private Color    _colorX = new Color(1f, 0f, 0f, .5f);
		[SerializeField] private Color    _colorY = new Color(0f, 1f, 0f, .5f);
		[SerializeField] private Color    _colorZ = new Color(0f, 0f, 1f, .5f);

		private Transform _transform;

		// These two are used to check when the Transform has changed
		private Vector3    _oldPosition;
		private Quaternion _oldRotation;
#endregion

#region  Protected variables
		/// <summary>
		///   Amount of draw points.
		/// </summary>
		/// <remarks>
		/// <para>
		///   Each of the three entries stands for the amount of *lines* to
		///   draw per corresponding axis. You need to mutate this member in
		///   <c>CountLines</c>.
		/// </para>
		/// </remarks>
		protected int[] _lineCount = {0, 0, 0};

		/// <summary>
		///   We store the draw points here for re-use.
		/// </summary>
		/// <remarks>
		///   <para>
		///     The outer dimension is always 3 and stands for the three axes.
		///     The middle dimension is the amount of lines per axis and it's
		///     always different. The inner dimension is always 2 and contains
		///     the two end points of each line. You need to mutate this member
		///     in <c>ComputeLines</c>.
		///   </para>
		/// </remarks>
		protected Vector3[][][] _lineSets = new Vector3[3][][];
#endregion  // Protected variables

#region  Accessors
		/// <summary>
		///   Colour of <c>X</c>-axis lines.
		/// </summary>
		public Color ColorX {
			get {
				return _colorX;
			} set {
				_colorX = value;
			}
		}

		/// <summary>
		///   Colour of <c>Y</c>-axis lines.
		/// </summary>
		public Color ColorY {
			get {
				return _colorY;
			} set {
				_colorY = value;
			}
		}

		/// <summary>
		///   Colour of <c>Z</c>-axis lines.
		/// </summary>
		public Color ColorZ {
			get {
				return _colorZ;
			} set {
				_colorZ = value;
			}
		}

		/// <summary>
		///   Sets of lines to draw separated by axis. Each line is a pair of
		///   two points in world-space.
		/// </summary>
		/// <remarks>
		///   <para>
		///     The first dimension is three objects large, one for each axis
		///     in the order <c>x</c>, <c>y</c> and <c>z</c>. The second
		///     dimension depends on the number of lines per set. The third
		///     dimension is two objects large, it represents the two end
		///     points of each line.
		///   </para>
		/// </remarks>
		public Vector3[][][] LineSets {
			get {
				return _lineSets;
			}
		}

		protected Transform Transform_ {
			get {
				if (!_transform) {
					_transform = GetComponent<Transform>();
				}
				return _transform;
			}
		}

		/// <summary>
		///   The width of the lines used when rendering the grid.
		/// </summary>
		/// <value>
		///   The width of the render line.
		/// </value>
		/// <remarks>
		///   <para>
		///     The width of the rendered lines, if it is set to 0 all lines will
		///     be one pixel wide, otherwise they will have the specified width in
		///     world units.
		///   </para>
		/// </remarks>
		public float LineWidth {
			get {
				return _lineWidth;
			} set {
				_lineWidth = value;
			}
		}

		/// <summary>
		///   Priority of the renderer, higher renderers first.
		/// </summary>
		/// <remarks>
		///   <para>
		///     Renderers with higher priority are rendered first. This means
		///     that lesser renderers will draw on top of higher, coverting
		///     them up.
		///   </para>
		///   <para>
		///     If two renderers have the same priority their order is
		///     undefined, but guaranteed to be fixed as long as there are no
		///     renderers added.
		///   </para>
		/// </remarks>
		public int Priority {
			get {
				return _priority;
			} set {
				var oldPriority = _priority;
				_priority = value;
				OnPriorityChanged(oldPriority, _priority);
			}
		}

		/// <summary>
		///   The material for rendering, if none is given it uses a default
		///   material.
		/// </summary>
		/// <remarks>
		///   <para>
		///     You can use you own material if you want control over the shader
		///     used, otherwise a default material with the following shader will
		///     be used:
		///   </para>
		///   <code>
		///     Shader "GridFramework/DefaultShader" {
		///         SubShader {
		///             Pass {
		///                 Blend SrcAlpha OneMinusSrcAlpha
		///                 ZWrite Off Cull Off Fog {
		///                     Mode Off
		///                 }
		///                 BindChannels {
		///                     Bind "vertex", vertex Bind "color", color
		///                 }
		///             }
		///         }
		///     }
		///   </code>
		///   <para>
		///     The shader itself can be find among the shaders as
		///     <c>GridFramework/DefaultShader</c>.
		///   </para>
		/// </remarks>
		public Material Material {
			get {
				if (!_material) {
					_material = new Material(Shader.Find("GridFramework/DefaultShader"));
				}
				return _material;
			} set {
				_material = value;
			}
		}
#endregion

#region  Caching methods
		/// <summary>
		///   Updates the draw points array.
		/// </summary>
		/// <summary>
		///   <para>
		///     This method needs to be called by the renderer when the points
		///     to draw need to be updated. This is usually the case when the
		///     properties of the grid have changed. The method counts the
		///     lines, allocates memory if needed and finally computes the
		///     end points of all lines.
		///   </para>
		///   <para>
		///     This three-step process can be expensive, depending on the
		///     complexity of the grid and the renderer. Therefore it is not
		///     recommended to call it every frame, instead one should call it
		///     only as needed. In order to implement such lazy behaviour the
		///     grid provided by Grid Framework fire events when the properties
		///     of the grid change. A renderer can then subscribe to these
		///     properties and call this method as needed.
		///   </para>
		/// </summary>
		protected void UpdatePoints() {
			CountLines();
			AllocatePoints();
			ComputeLines();
		}

		/// <summary>
		///   Allocates a memory array for new draw points when needed.
		/// </summary>
		/// <remarks>
		///   <para>
		///     This method first checks is the size of the individual line sets
		///     has changed or if they even exist. If so, then it simply returns
		///     false.  Otherwise the size array is updated and then the line
		///     arrays are created and all vectors set to <c>(0, 0, 0)</c>.
		///   </para>
		/// </remarks>
		private void AllocatePoints() {
			for (var i = 0; i < 3; ++i) {
				// If the array already has the right size skip
				if (_lineSets[i] != null && _lineSets[i].Length == _lineCount[i]) {
					continue;
				}

				_lineSets[i] = new Vector3[_lineCount[i]][];

				for (var j = 0; j < _lineCount[i]; ++j) {
					_lineSets[i][j] = new Vector3[2];
					for (var k = 0; k < 2; ++k) {
						_lineSets[i][j][k] = Vector3.zero;
					}
				}
			}
		}

		/// <summary>
		///   Whether the postion or rotation of the renderer have changed.
		/// </summary>
		private bool TransformHasChanged() {
			var position = Transform_.position;
			var rotation = Transform_.rotation;

			if (_oldPosition == position && _oldRotation == rotation ) {
				return false;
			}

			_oldPosition = position;
			_oldRotation = rotation;
			return true;
		}
#endregion  // Caching methods

#region  Abstract methods
		/// <summary>
		///   Computes the coordinates of the end points of all lines.
		/// </summary>
		/// <remarks>
		///   <para>
		///     This is an abstract method, the implementations are up to the
		///     subclasses. Call the method after the amount of lines is known
		///     and the <c>_lineSets</c> array is allocated.
		///   </para>
		///   <para>
		///     When writing an implementation the method must compute for
		///     every line set every line. Each line consists of two end
		///     points, the order of which does not matter.
		///   </para>
		///   <para>
		///     It is advised not to call this method directly, rather call the
		///     <c>UpdatePoints</c> method which will ensure the proper order
		///     of calls.
		///   </para>
		/// </remarks>
		protected abstract void ComputeLines();

		/// <summary>
		///   Computes the amount of lines.
		/// </summary>
		/// <remarks>
		///   <para>
		///     This is an abstract method, the implementations are up to the
		///     subclasses. This method should be called before the array is
		///     allocated. note that both <c>from</c> and <c>to</c> are
		///     references, this allows us to convert them into a common format
		///     that can be used in the subsequent calculations. For example,
		///     absolute world dimensions could be converted to relative grid
		///     dimensions and all other calculations would only need to be
		///     implemented for relative grid dimensions.
		///   </para>
		///   <para>
		///     It is advised not to call this method directly, rather call the
		///     <c>UpdatePoints</c> method which will ensure the proper order
		///     of calls.
		///   </para>
		/// </remarks>
		protected abstract void CountLines();
#endregion  // Abstract methods

#region  Visual methods
		/// <summary>
		///   Refresh the renderer's lines (required before rendering).
		/// </summary>
		/// <remarks>
		///   <para>
		///     Unity has no way of notifying an object when its
		///     <c>Transfrom</c> component has changed, so we force a refresh
		///     manually before rendering the grid.
		///   </para>
		/// </remarks>
		public void Refresh() {
			if (TransformHasChanged()) {
				UpdatePoints();
			}
		}

		/// <summary>
		///   Draws the grid using gizmos.
		/// </summary>
		/// <remarks>
		///   <para>
		///     This method draws the grid in the editor using gizmos. There is
		///     usually no reason to call this method manually, you should
		///     instead set the drawing flags of the grid itself. However, if
		///     you must, call this method from inside <c>OnDrawGizmos</c>.
		///   </para>
		/// </remarks>
		private void DrawGrid() {
			if (!enabled) {
				return;
			}

			// On the first run the individual line sets will be uninitialized.
			var hasToUpdate = false
				|| _lineSets[0] == null
				|| _lineSets[1] == null
				|| _lineSets[2] == null
				|| TransformHasChanged();

			if (hasToUpdate) {
				UpdatePoints();
			}

			Color[] axisColors = {ColorX, ColorY, ColorZ};
			
			for (var i = 0; i < 3; ++i) {
				if (Mathf.Abs(axisColors[i].a) < Mathf.Epsilon) {
					continue;
				}
				Gizmos.color = axisColors[i];
				foreach (var line in _lineSets[i]) {
					if (line == null) {
						continue;
					}
					Gizmos.DrawLine(line[0], line[1]);
				}
			}
		}
#endregion  // Visual methods

#region  Callback Methods
		void Awake() {
			Register();
			UpdatePoints();
		}
		
		void OnDestroy() {
			Unregister();
		}

		void OnEnable() {
			UpdatePoints();
		}

		void OnDrawGizmos() {
			DrawGrid();
		}
#endregion

#region  Hook methods
		private void Register() {
			RendererManager.RegisterRenderer(this);
		}

		private void Unregister() {
			RendererManager.UnregisterRenderer(this);
		}

		/// <summary>
		///   This method is called when the <c>PriorityChanged</c> event has
		///   been fired.
		/// </summary>
		/// <param name="oldPriority">
		///   The priority this renderer had previously.
		/// </param>
		/// <param name="newPriority">
		///   The priority this renderer has now.
		/// </param>
		protected virtual void OnPriorityChanged(int oldPriority, int newPriority) {
			if (PriorityChanged == null) {
				return;
			}
			var args = new PriorityEventArgs(oldPriority, newPriority);
			PriorityChanged(this, args);
		}
	}
#endregion
}
