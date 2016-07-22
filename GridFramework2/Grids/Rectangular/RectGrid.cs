using UnityEngine;
using GridFramework.Vectors;
using Grid = GridFramework.Grids.Grid;

namespace GridFramework.Grids {
	/// <summary>
	///   A standard three-dimensional rectangular grid.
	/// </summary>
	/// <remarks>
	///   <para>
	///     Your standard rectangular grid, the characterising values is its
	///     spacing, which can be set for each axis individually.
	///   </para>
	/// </remarks>
	[AddComponentMenu("Grid Framework/Grids/RectGrid")]
	public sealed class RectGrid: Grid {

#region  Types
		/// <summary>
		///   The various coordinate systems supported by rectangular grids.
		/// </summary>
		public enum CoordinateSystem {
			/// <summary>
			///   World-space coordinates.
			/// </summary>
			World,

			/// <summary>
			///   Grid coordinates, multiples of <see cref="Spacing"/>.
			/// </summary>
			Grid
		}

		/// <summary>
		///   Arguments object of a spacing event.
		/// </summary>
		public class SpacingEventArgs : System.EventArgs {
			private readonly Vector3 _difference;

			/// <summary>
			///   Instantiate a new object form previous and current value.
			/// </summary>
			/// <para name="previous">
			///   Previous value of the spacing.
			/// </para>
			/// <para name="current">
			///   Current value of the spacing.
			/// </para>
			public SpacingEventArgs(Vector3 previous, Vector3 current) {
				_difference = current - previous;
			}

			/// <summary>
			///   Change in spacing, new value minus old value.
			/// </summary>
			public Vector3 Difference {
				get {
					return _difference;
				}
			}
		}

		/// <summary>
		///   Arguments object of a shearing event.
		/// </summary>
		public class ShearingEventArgs : System.EventArgs {
			private readonly Vector6 _difference;

			/// <summary>
			///   Instantiate a new object form previous and current value.
			/// </summary>
			/// <para name="previous">
			///   Previous value of the shearing.
			/// </para>
			/// <para name="current">
			///   Current value of the shearing.
			/// </para>
			public ShearingEventArgs(Vector6 previous, Vector6 current) {
				_difference = current - previous;
			}

			/// <summary>
			///   Change in shearing, new value minus old value.
			/// </summary>
			public Vector6 Difference {
				get {
					return _difference;
				}
			}
		}

		/// <summary>
		///   Event raised when the spacing of the grid changes.
		/// </summary>
		public event System.EventHandler<SpacingEventArgs> SpacingChanged;

		/// <summary>
		///   Event raised when the shearing of the grid changes.
		/// </summary>
		public event System.EventHandler<ShearingEventArgs> ShearingChanged;
#endregion  // Types

#region  Private member variables
		[SerializeField] private Vector3 _spacing  = Vector3.one;
		[SerializeField] private Vector6 _shearing = Vector6.Zero;
#endregion  // Private member variables

#region  Accessors
		/// <summary>
		///   How large the grid boxes are.
		/// </summary>
		/// <value>
		///   The spacing of the grid.
		/// </value>
		/// <remarks>
		///   <para>
		///     How far apart the lines of the grid are. You can set each axis
		///     separately, but none may be less than `Mathf.Epsilon`, in order
		///     to prevent values that don't make any sense.
		///   </para>
		/// </remarks>
		public Vector3 Spacing {
			get {
				return _spacing;
			}
			set {
				var oldSpacing = _spacing;
				_spacing = Vector3.Max(value, Vector3.one * Mathf.Epsilon);
				OnSpacingChanged(oldSpacing, _spacing);
			}
		}

		/// <summary>
		///   How the axes are sheared.
		/// </summary>
		/// <value>
		///   Shearing vector of the grid.
		/// </value>
		/// <remarks>
		///   <para>
		///     How much the individual axes of the grid are skewed towards
		///     each other. For instance, this means the if _XY_ is set to _2_,
		///     then for each point with grid coordinates _(x, y)_ will be
		///     mapped to _(x, y + 2x)_, while the uninvolved _Z_ coordinate
		///     remains the same. For more information refer to the manual.
		///   </para>
		/// </remarks>
		public Vector6 Shearing {
			get {
				return _shearing;
			}
			set {
				var oldShearing = _shearing;
				_shearing = value;
				OnShearingChanged(oldShearing, _shearing);
			}
		}
#endregion  // Accessors

#region  Computed properties
		/// <summary>
		///   Direction along the X-axis of the grid in world space.
		/// </summary>
		/// <value>
		///   Unit vector in grid scale along the grid's X-axis.
		/// </value>
		/// <remarks>
		///   <para>
		///     The X-axis of the grid in world space.
		///   </para>
		/// </remarks>
		public Vector3 Right {
			get {
				return Axis(Vector3.right);
			}
		}

		/// <summary>
		///   Direction along the Y-axis of the grid in world space.
		/// </summary>
		/// <value>
		///   Unit vector in grid scale along the grid's Y-axis.
		/// </value>
		/// <remarks>
		///   <para>
		///     The Y-axis of the grid in world space.
		///   </para>
		/// </remarks>
		public Vector3 Up {
			get {
				return Axis(Vector3.up);
			}
		}

		/// <summary>
		///   Direction along the Z-axis of the grid in world space.
		/// </summary>
		/// <value>
		///   Unit vector in grid scale along the grid's Z-axis.
		/// </value>
		/// <remarks>
		///   <para>
		///     The Z-axis of the grid in world space.
		///   </para>
		/// </remarks>
		public Vector3 Forward {
			get { 
				return Axis(Vector3.forward);
			}
		}

		/// <summary>
		///   Common code for <c>Right</c>, <c>Up</c> and <c>Forward</c>.
		/// </summary>
		private Vector3 Axis(Vector3 axis) {
			return GridToWorld(axis) - GridToWorld(Vector3.zero);
		}
#endregion  // Computed properties

#region  Cached members
		private Matrix4x4 _gwMatrix = Matrix4x4.identity;
		private Matrix4x4 _wgMatrix = Matrix4x4.identity;
		
		private Matrix4x4 gwMatrix {
			get {
				UpdateCachedMembers();
				return _gwMatrix;
			}
		}
		
		private Matrix4x4 wgMatrix {
			get {
				UpdateCachedMembers();
				return _wgMatrix;
			}
		}
		
		protected override void UpdateCachedMembers() {
			if (!_cacheIsDirty && !TransfromHasChanged()) {
				return;
			}

			var shearMatrix = Shearing.ShearMatrix();
			var rectMatrix  = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
			var scaleMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Spacing);

			_gwMatrix = rectMatrix * shearMatrix * scaleMatrix;
			_wgMatrix = _gwMatrix.inverse;

			_cacheIsDirty = false;
		}
#endregion  // Cached members

#region  Coordinate conversion
		/// <summary>
		///   Converts world coordinates to grid coordinates.
		/// </summary>
		/// <returns>
		///   Grid coordinates of the world point.
		/// </returns>
		/// <param name="world">
		///   Point in world space.
		/// </param>
		/// <remarks>
		///   <para>
		///     Takes in a position in wold space and calculates where in the
		///     grid that position is. The origin of the grid is the world
		///     position of its GameObject and its axes lie on the
		///     corresponding axes of the Transform.  Rotation is taken into
		///     account for this operation.
		///   </para>
		/// </remarks>
		public Vector3 WorldToGrid(Vector3 world) {
			return wgMatrix.MultiplyPoint3x4(world);
		}

		/// <summary>
		///   Converts grid coordinates to world coordinates.
		/// </summary>
		/// <returns>
		///   World coordinates of the Grid point.
		/// </returns>
		/// <param name="grid">
		///   Point in grid space.
		/// </param>
		/// <remarks>
		///   <para>
		///     The opposite of <see cref="WorldToGrid"/>, this returns the
		///     world position of a point in the grid. The origin of the grid
		///     is the world position of its GameObject and its axes lie on the
		///     corresponding axes of the Transform. Rotation is taken into
		///     account for this operation.
		///   </para>
		/// </remarks>
		public Vector3 GridToWorld(Vector3 grid) {
			return gwMatrix.MultiplyPoint3x4(grid);
		}
#endregion  // Coordinate conversion

#region  Hook methods
		/// <summary>
		///   This method is called when the <c>SpacingChanged</c> event has
		///   been triggered.
		/// </summary>
		/// <param name="oldSpacing">
		///   The spacing this grid had previously.
		/// </param>
		/// <param name="newSpacing">
		///   The spacing this grid has now.
		/// </param>
		private void OnSpacingChanged(Vector3 oldSpacing, Vector3 newSpacing) {
			var args = new SpacingEventArgs(oldSpacing, newSpacing);
			var delta = args.Difference;

			for (var i = 0; i < 3; ++i) {
				_cacheIsDirty |= Mathf.Abs(delta[i]) > Mathf.Epsilon;
			}

			if (SpacingChanged == null) {
				return;
			}

			SpacingChanged(this, args);
		}

		/// <summary>
		///   This method is called when the <c>ShearingChanged</c> event has
		///   been triggered.
		/// </summary>
		/// <param name="oldShearing">
		///   The shearing this grid had previously.
		/// </param>
		/// <param name="newShearing">
		///   The shearing this grid has now.
		/// </param>
		private void OnShearingChanged(Vector6 oldShearing, Vector6 newShearing) {
			var args = new ShearingEventArgs(oldShearing, newShearing);
			var delta = args.Difference;

			for (var i = 0; i < 6; ++i) {
				_cacheIsDirty |= Mathf.Abs(delta[i]) > Mathf.Epsilon;
			}

			if (ShearingChanged == null) {
				return;
			}

			ShearingChanged(this, args);
		}
#endregion  // Hook methods
	}
}
