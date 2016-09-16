using UnityEngine;
using GridFramework.Matrices;

//  HEXAGON DIMENSIONS:
//
//    [------s------]      s = 3/2 * r
//         _________
//        / |       \
//       /  |        \
//      /   h         \
//     /    |          \
//    (     |  [--r-----]  h = 2r * sin(60°)
//     \    |          /     =  r * sqrt(3)
//      \   |         /
//       \  |        /
//        \_|_______/
//    [--------w--------]  w = 2r
//
//  r: radius  s: side  w: width  h: height

namespace GridFramework.Grids {
	/// <summary>
	///   A grid consisting of flat hexagonal grids stacked on top of each other.
	/// </summary>
	/// <remarks>
	///   <para>
	///     A regular hexagonal grid that forms a honeycomb pattern. It is
	///     characterized by the <see cref="Radius"><c>Radius</c></see>
	///     (distance from the centre of a hexagon to one of its vertices) and
	///     the <see cref="Depth"><c>Depth</c></see> (distance between two
	///     honeycomb layers). Hex grids use a herringbone pattern for their
	///     coordinate system, please refer to the user manual for information
	///     about how that coordinate system works.
	///   </para>
	/// </remarks>
	[AddComponentMenu("Grid Framework/Grids/HexGrid")]
	public sealed class HexGrid : LayeredGrid {
#region  Enumerations
		/// <summary>
		///   Orientation of hexes, pointed or flat.
		/// </summary>
		/// <remarks>
		///   <para>
		///     There are two ways a hexagon can be rotated: <c>Pointed</c> and
		///     <c>Flat</c>. This orientation applies to either the sides of
		///     the hexagon or the top.
		///   </para>
		/// </remarks>
		public enum Orientation {
			Pointed,
			Flat
		};

		/// <summary>
		///   The various coordinate systems supported by hexagonal grids.
		/// </summary>
		/// <remarks>
		///   <para>
		///     Cubic and barycentric coordinates are four-dimensional, the
		///     rest are three-dimensional.
		///   </para>
		/// </remarks>
		public enum CoordinateSystem {
			/// <summary>
			///   World-space coordinates.
			/// </summary>
			World,

			/// <summary>
			///   A rectangular grid that uses <see cref="Side">Side</see> and
			///   <see cref="Height">Height</see> as its width and height.
			/// </summary>
			/// <remarks>
			///   <para>
			///     This coordinate system does not look hexagonal and is only
			///     used as an intermediate step.
			///   </para>
			/// </remarks>
			Grid,

			/// <summary>
			///   Herringbone pattern where every odd column is shifted
			///   upwards.
			/// </summary>
			HerringboneUp,

			/// <summary>
			///   Herringbone pattern where every odd column is shifted
			///   downwards.
			/// </summary>
			HerringboneDown,

			/// <summary>
			///   Rhombic pattern where both axes go along the flat sides at a
			///   60° angle towards each other.
			/// </summary>
			RhombicUp,

			/// <summary>
			///   Rhombic pattern where both axes go along the flat sides at a
			///   300° angle towards each other.
			/// </summary>
			RhombicDown,

			/// <summary>
			///   Cubic pattern with three axes in a plane, each one going
			///   along the flat sides, at 60° to each other. The sum of all
			///   three coordinates
			///   is always 0.
			/// </summary>
			Cubic,
		};

		/// <summary>
		///   Cardinal direction of a vertex.
		/// </summary>
		/// <remarks>
		///   <para>
		///     The cardinal position of a vertex relative to the centre of a given
		///     hex. Note that using N and S for pointy sides, as well as E and W
		///     for flat sides does not make sense, but it is still possible.
		///   </para>
		/// </remarks>
		public enum HexDirection {
			/// <summary> North </summary>
			N  ,
			/// <summary> North-East </summary>
			NE ,
			/// <summary> East </summary>
			E  ,
			/// <summary> South-East </summary>
			SE ,
			/// <summary> South </summary>
			S  ,
			/// <summary> South-West </summary>
			SW ,
			/// <summary> West </summary>
			W  ,
			/// <summary> North-West </summary>
			NW ,
		};
#endregion  // Enumerations

#region  Types
		/// <summary>
		///   Arguments object of a radius event.
		/// </summary>
		public class RadiusEventArgs : System.EventArgs {
			private readonly float _difference;

			/// <summary>
			///   Instantiate a new object form previous and current value.
			/// </summary>
			/// <para name="previous">
			///   Previous value of the radius.
			/// </para>
			/// <para name="current">
			///   Current value of the radius.
			/// </para>
			public RadiusEventArgs(float previous, float current) {
				_difference = current - previous;
			}

			/// <summary>
			///   Change in radius, new value minus old value.
			/// </summary>
			public float Difference {
				get {
					return _difference;
				}
			}
		}

		/// <summary>
		///   Arguments object of an orientation event.
		/// </summary>
		public class SidesEventArgs : System.EventArgs {
			private readonly Orientation _previous;

			/// <summary>
			///   Instantiate a new object form previous value.
			/// </summary>
			/// <para name="previous">
			///   Previous value of the orientations.
			/// </para>
			public SidesEventArgs(Orientation previous) {
				_previous = previous;
			}

			/// <summary>
			///   Previous orientations.
			/// </summary>
			public Orientation Previous {
				get {
					return _previous;
				}
			}
		}

		/// <summary>
		///   Event raised when the depth of the grid changes.
		/// </summary>
		public event System.EventHandler<RadiusEventArgs> RadiusChanged;

		/// <summary>
		///   Event raised when the orientation of the grid changes.
		/// </summary>
		public event System.EventHandler<SidesEventArgs> SidesChanged;
#endregion  // Types

#region  Private varialbles
		[SerializeField]
		private float _radius = 1.0f;

		[SerializeField]
		private Orientation _sides = Orientation.Pointed;
#endregion  // Private varialbles

#region  Accessors
		/// <summary>
		///   Distance from the centre of a hex to a vertex.
		/// </summary>
		/// <remarks>
		///   <para>
		///	    This refers to the distance between the centre of a hexagon and
		///	    one of its vertices. Since the hexagon is regular all vertices
		///	    have the same distance from the centre. In other words, imagine
		///	    a circumscribed circle around the hexagon, its radius is the
		///	    radius of the hexagon. The value may not be less than
		///	    <c>Mathf.Epsilon</c>.
		///   </para>
		/// </remarks>
		public float Radius {
			get {
				return _radius;
			} set {
				var previous = _radius;
				_radius = Mathf.Max(value, Mathf.Epsilon);
				OnRadiusChanged(previous, _radius);
			}
		}

		/// <summary>
		///   Pointy sides or flat sides.
		/// </summary>
		/// <remarks>
		///   <para>
		///     Whether the grid has pointed sides or flat sides. This affects
		///     both the drawing and the calculations.
		///   </para>
		/// </remarks>
		public Orientation Sides {
			get {
				return _sides;
			} set {
				var previous = _sides;
				_sides = value;
				OnSidesChanged(previous);
			}
		}
#endregion  // Accessors

#region  Computed properties
		/// <summary>
		///   1.5 times the radius.
		/// </summary>
		/// <value>
		///   <para>
		///     Shorthand writing for <c>1.5f * Radius</c>. When setting the
		///     value of this property you are implicitly setting the
		///     <c>Radius</c> of the grid. Because this is a computed property
		///     floating point rounding errors might apply.
		///   </para>
		/// </value>
		public float Side {
			get {
				return Radius * 1.5f ;
			} set {
				Radius = value / 1.5f;
			}
		}

		/// <summary>
		///   Full width of the hex.
		/// </summary>
		/// <value>
		///   This is the full vertical height of a hex, the distance from one
		///   edge to its opposite (<c>sqrt(3) * Radius</c>). When setting the
		///   value of this property you are implicitly setting the
		///   <c>Radius</c> of the grid. Because this is a computed property
		///   floating point rounding errors might apply.
		/// </value>
		public float Height {
			get {
				return Radius * Mathf.Sqrt(3f);
			} set {
				Radius = value / Mathf.Sqrt(3f);
			}
		}

		/// <summary>
		///   Distance between vertices on opposite sides.
		/// </summary>
		/// <value>
		///   This is the full horizontal width of a hex, the distance from one
		///   vertex to its opposite (<c>2 * Radius</c>). When setting the
		///   value of this property you are implicitly setting the
		///   <c>Radius</c> of the grid. Because this is a computed property
		///   floating point rounding errors might apply.
		/// </value>
		public float Width {
			get {
				return Radius * 2f;
			} set {
				Radius = value / 2f;
			}
		}

		private Vector3 Right {
			get {
				var axis = transform.right;
				return Side * axis;
			}
		}

		private Vector3 Up {
			get {
				var axis = transform.up;
				return Height * axis;
			}
		}
#endregion  // Computed properties

#region  Cached members
		#region Variables and Accessors
		/// <summary>
		///   Matrix that transforms from world to local.
		/// </summary>
		private Matrix4x4 _wlMatrix = Matrix4x4.identity;
		/// <summary>
		///   Matrix that transforms from local to world.
		/// </summary>
		private Matrix4x4 _lwMatrix = Matrix4x4.identity;

		/// <summary> Matrix that transforms from upwards rhombic to world. </summary>
		private Matrix4x4 _ruwMatrix = Matrix4x4.identity;

		/// <summary> Matrix that transforms from world to upwards rhombic. </summary>
		private Matrix4x4 _wruMatrix = Matrix4x4.identity;

		/// <summary> Matrix that transforms from upwards rhombic to world. </summary>
		private Matrix4x4 _rdwMatrix = Matrix4x4.identity;

		/// <summary> Matrix that transforms from world to upwards rhombic. </summary>
		private Matrix4x4 _wrdMatrix = Matrix4x4.identity;

		private Matrix4x4 _gwMatrix;
		private Matrix4x4 _wgMatrix;

		// The basis vectors of the cubic coordinate system are
		//    x =  2/3 s
		//    y = -2/3 s + 1/2 h
		//    y = -2/3 s - 1/2 h
		// Every one can be see as a 2-vector in the vector space {s, h},
		// then we can use them as the columns of the matrix.
		private Matrix3x4 _cgMatrix = new Matrix3x4(2f/3f, -1f/3f, -1f/3f, 0f,
		                                               0f,  1f/2f, -1f/2f, 0f,
		                                               0f,     0f,     0f, 1f);
		private Matrix4x3 _gcMatrix;  // TODO This can be hard-coded

		/// <summary>
		///   Matrix that transforms from world to local.
		/// </summary>
		private Matrix4x4 wlMatrix {get {UpdateCachedMembers();return _wlMatrix;}}
		/// <summary>
		///   Matrix that transforms from local to world.
		/// </summary>
		private Matrix4x4 lwMatrix {get {UpdateCachedMembers();return _lwMatrix;}}

		/// <summary> Matrix that transforms from upwards rhombic to world. </summary>
		private Matrix4x4 ruwMatrix {get {UpdateCachedMembers(); return _ruwMatrix;}}
		/// <summary> Matrix that transforms from world to upwards rhombic. </summary>
		private Matrix4x4 wruMatrix {get {UpdateCachedMembers(); return _wruMatrix;}}
		/// <summary> Matrix that transforms from upwards rhombic to world. </summary>
		private Matrix4x4 rdwMatrix {get {UpdateCachedMembers(); return _rdwMatrix;}}
		/// <summary> Matrix that transforms from world to upwards rhombic. </summary>
		private Matrix4x4 wrdMatrix {get {UpdateCachedMembers(); return _wrdMatrix;}}

		private Matrix4x4 wgMatrix {get {UpdateCachedMembers(); return _wgMatrix;}}
		private Matrix4x4 gwMatrix {get {UpdateCachedMembers(); return _gwMatrix;}}
		private Matrix3x4 cgMatrix {get {return _cgMatrix;}}
		private Matrix4x3 gcMatrix {get {UpdateCachedMembers(); return _gcMatrix;}}
		#endregion

		protected override void UpdateCachedMembers() {
			if (!_cacheIsDirty && !TransfromHasChanged()) {
				return;
			}
			// Order of operations: Sides -> Coordinate -> Grid -> Plane -> World

			const float rhombShear = 0.5f;  // Grid-coordinates, half a height for every side
			var spacing = new Vector3(Side, Height, Depth);

			var gridMatrix  = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, spacing);
			var worldMatrix = Matrix4x4.TRS(Transform_.position, Transform_.rotation, Vector3.one);

			// Shearing for rhombic coordinates
			var rhombicUpMatrix = Matrix4x4.identity;
			rhombicUpMatrix[1, 0] = rhombShear;

			var rhombicDownMatrix = Matrix4x4.identity;
			rhombicDownMatrix[1, 0] = -rhombShear;

			_ruwMatrix = worldMatrix
			             * Matrix4x4.TRS(Vector3.zero,
			                             // identity or 30° clockwise
			                             Sides == Orientation.Pointed
			                               ? Quaternion.identity
			                               : Quaternion.AngleAxis(-30f, Vector3.forward),
			                             Vector3.one)
				         * gridMatrix
				         * rhombicUpMatrix;

			_rdwMatrix = worldMatrix
			             * Matrix4x4.TRS(Vector3.zero,
			                             // identity or 30° clockwise
			                             Sides == Orientation.Pointed
			                               ? Quaternion.identity
			                               : Quaternion.AngleAxis(30f, Vector3.forward),
			                             Vector3.one)
				         * gridMatrix
				         * rhombicDownMatrix;

			_lwMatrix  = worldMatrix;

			_wruMatrix = _ruwMatrix.inverse;
			_wrdMatrix = _rdwMatrix.inverse;
			_wlMatrix  = _lwMatrix.inverse;

			_gwMatrix = worldMatrix
			            * Matrix4x4.TRS(Vector3.zero,
			                            // identity or 30° clockwise
			                            Sides == Orientation.Pointed
			                              ? Quaternion.identity
			                              : Quaternion.AngleAxis(-30f, Vector3.forward),
			                            Vector3.one)
			            * gridMatrix;
			_wgMatrix = _gwMatrix.inverse;

			/* _cgMatrix = cubicMatrix; */
			_gcMatrix = _cgMatrix.PseudoInverse;

			_cacheIsDirty = false;
		}
#endregion  // Cached members

#region  World -> Conversion
		private Vector3 WorldToGrid(Vector3 point) {
			return wgMatrix.MultiplyPoint3x4(point);
		}

		/// <summary>
		///   Returns the upwards herringbone coordinates of a point in world space.
		/// </summary>
		/// <param name="world">
		///   Point in world coordinates.
		/// </param>
		/// <returns>
		///   Point in upwards herringbone coordinates.
		/// </returns>
		/// <summary>
		///   <para>
		///     This method takes a point in world space and returns the
		///     corresponding upwards herringbone coordinates. Every odd
		///     numbered column is offset upwards, giving this coordinate
		///     system the herringbone pattern. This means that the Y
		///     coordinate directly depends on the X coordinate. The Z
		///     coordinate is simply which layer of the grid is on, relative to
		///     the grid's central layer.
		///   </para>
		/// </summary>
		public Vector3 WorldToHerringUp(Vector3 world) {
			return CubicToHerringU(WorldToCubic(world));
		}

		/// <summary>
		///   Returns the downwards herringbone coordinates of a point in world space.
		/// </summary>
		/// <param name="world">
		///   Point in world coordinates.
		/// </param>
		/// <returns>
		///   Point in downwards herringbone coordinates.
		/// </returns>
		/// <summary>
		///   <para>
		///     This method takes a point in world space and returns the
		///     corresponding downwards herringbone coordinates.  Every odd
		///     numbered column is offset downwards, giving this coordinate
		///     system the herringbone pattern. This means that the Y
		///     coordinate directly depends on the X coordinate. The Z
		///     coordinate is simply which layer of the grid is on, relative to
		///     the grid's central layer.
		///   </para>
		/// </summary>
		public Vector3 WorldToHerringDown(Vector3 world) {
			return CubicToHerringD(WorldToCubic(world));
		}

		/// <summary>
		///   Returns the rhombic coordinates of a point in world space.
		/// </summary>
		/// <param name="world">
		///   Point in world coordinates.
		/// </param>
		/// <returns>
		///   Point in rhombic coordinates.
		/// </returns>
		/// <summary>
		///   <para>
		///     This method takes a point in world space and returns the
		///     corresponding rhombic coordinates. The rhombic coordinate
		///     system uses three axes; the X-axis rotated 30°
		///     counter-clockwise, the regular Y-axis, and the Z coordinate is
		///     which layer of the grid the point is on, relative to the grid's
		///     central layer.
		///   </para>
		/// </summary>
		public Vector3 WorldToRhombicUp(Vector3 world) {
			Vector3 rhombic = wruMatrix.MultiplyPoint3x4(world);
			return  rhombic;
		}

		/// <summary>
		///   Returns the downwards rhombic coordinates of a point in world space.
		/// </summary>
		/// <param name="world">
		///   Point in world coordinates.
		/// </param>
		/// <returns>
		///   Point in downwards rhombic coordinates.
		/// </returns>
		/// <summary>
		///   <para>
		///     This method takes a point in world space and returns the
		///     corresponding downwards rhombic coordinates. The downwards
		///     rhombic coordinate system uses three axes; the X-axis rotated
		///     300° counter-clockwise, the regular Y-axis, and the Z
		///     coordinate is which layer of the grid the point is on, relative
		///     to the grid's central layer.
		///   </para>
		/// </summary>
		public Vector3 WorldToRhombicDown(Vector3 world) {
			Vector3 rhombic = wrdMatrix.MultiplyPoint3x4(world);
			return  rhombic;
		}

		/// <summary>
		///   Returns the cubic coordinates of a point in world space.
		/// </summary>
		/// <param name="point">
		///   Point in world coordinates.
		/// </param>
		/// <returns>
		///   Point in cubic coordinates.
		/// </returns>
		/// <summary>
		///   <para>
		///     This method takes a point in world space and returns the
		///     corresponding rhombic coordinates.  The cubic coordinate system
		///     uses four axes; X, Y and Z are used to fix the point on the
		///     layer while W is which layer of the grid the point is on,
		///     relative to the grid's central layer. The central hex has
		///     coordinates (0, 0, 0, 0) and the sum of the first three
		///     coordinates is always 0.
		///   </para>
		/// </summary>
		public Vector4 WorldToCubic(Vector3 point) {
			var gridPoint = WorldToGrid(point);
			return gcMatrix * gridPoint;
		}

		/// <summary>
		///   Returns the barycentric coordinates of a point in world space.
		/// </summary>
		/// <param name="world">
		///   Point in world coordinates.
		/// </param>
		/// <returns>
		///   Point in barycentric coordinates.
		/// </returns>
		/// <summary>
		///   <para>
		///     This method takes a point in world space and returns the
		///     corresponding barycentric coordinates. (subject to change?)
		///     Barycentric coordinates are similar to cubic ones, except the
		///     sum of the first three coordinates is 1.  The central hex has
		///     coordinates (0, 0, -1, 0), its north-eastern neighbour has
		///     coordinates (1, 0, 0, 0) and its northern neighbour has
		///     coordinates (0, 1, 0, 0).  In other words, it is the cubic
		///     coordinate system with +1 added to the Z-coordinate.
		///   </para>
		/// </summary>
		private Vector4 WorldToBarycentric(Vector3 world) {
			return CubicToBarycentric(WorldToCubic(world));
		}
#endregion  // World -> Conversion

#region  Grid -> Conversion
		private Vector3 GridToWorld(Vector3 point) {
			return gwMatrix.MultiplyPoint3x4(point);
		}
#endregion  // Grid -> Conversion

#region  Herring Upwards -> Conversion
		/// <summary>
		///   Returns the world coordinates of a point in upwards herringbone coordinates.
		/// </summary>
		/// <param name="herring">
		///   Point in upwards herringbone coordinates.
		/// </param>
		/// <returns>
		///   Point in world coordinates.
		/// </returns>
		///
		/// <summary>
		///   <para>
		///     Takes a point in upwards herringbone coordinates and returns
		///     its world position.
		///   </para>
		/// </summary>
		public Vector3 HerringUpToWorld(Vector3 herring) {
			return CubicToWorld(HerringUpToCubic(herring));
		}

		/// <summary>
		///   Returns the downwards herringbone coordinates of a point in upwards- coordinates.
		/// </summary>
		/// <param name="herring">
		///   Point in upwards herringbone coordinates.
		/// </param>
		/// <returns>
		///   Point in downwards herringbone coordinates.
		/// </returns>
		///
		/// <summary>
		///   <para>
		///     Takes a point in upwards herringbone coordinates and returns
		///     its downwards herringbone position.
		///   </para>
		/// </summary>
		public Vector3 HerringUpToHerringDown(Vector3 herring) {
			// This could be done directly without going to cubic
			return CubicToHerringD(HerringUpToCubic(herring));
		}

		/// <summary>
		///   Returns the rhombic coordinates of a point in upwards herringbone coordinates.
		/// </summary>
		/// <param name="herring">
		///   Point in upwards herringbone coordinates.
		/// </param>
		/// <returns>
		///   Point in rhombic coordinates.
		/// </returns>
		///
		/// <summary>
		///   <para>
		///     Takes a point in upwards herringbone coordinates and returns
		///     its rhombic position.
		///   </para>
		/// </summary>
		public Vector3 HerringUpToRhombicUp(Vector3 herring) {
			return CubicToRhombic(HerringUpToCubic(herring));
		}

		/// <summary>
		///   Returns the downwards rhombic coordinates of a point in upwards herringbone coordinates.
		/// </summary>
		/// <param name="herring">
		///   Point in upwards herringbone coordinates.
		/// </param>
		/// <returns>
		///   Point in downwards rhombic coordinates.
		/// </returns>
		///
		/// <summary>
		///   <para>
		///     Takes a point in upwards herringbone coordinates and returns
		///     its downwards rhombic position.
		///   </para>
		/// </summary>
		public Vector3 HerringUpToRhombicDown(Vector3 herring) {
			return CubicToRhombicD(HerringUpToCubic(herring));
		}

		/// <summary>
		///   Returns the cubic coordinates of a point in upwards herringbone coordinates.
		/// </summary>
		/// <param name="herring">
		///   Point in upwards herringbone coordinates.
		/// </param>
		/// <returns>
		///   Point in cubic coordinates.
		/// </returns>
		///
		/// <summary>
		///   <para>
		///     Takes a point in upwards herringbone coordinates and returns its cubic
		///     position.
		///   </para>
		/// </summary>
		public Vector4 HerringUpToCubic(Vector3 herring) {
			return HerringToCubic(herring, true);
		}

		/// <summary>
		///   Returns the barycentric coordinates of a point in upwards herringbone coordinates.
		/// </summary>
		/// <param name="herring">
		///   Point in upwards herringbone coordinates.
		/// </param>
		/// <returns>
		///   Point in barycentric coordinates.
		/// </returns>
		///
		/// <summary>
		///   <para>
		///     Takes a point in upwards herringbone coordinates and returns
		///     its barycentric position.
		///   </para>
		/// </summary>
		private Vector4 HerringUpToBarycentric(Vector3 herring) {
			return CubicToBarycentric(HerringUpToCubic(herring));
		}
#endregion  // Herring Upwards -> Conversion

#region  Herring Down -> Conversion
		/// <summary>
		///   Returns the world coordinates of a point in downwards herringbone coordinates.
		/// </summary>
		/// <param name="herring">
		///   Point in downwards herringbone coordinates.
		/// </param>
		/// <returns>
		///   Point in world coordinates.
		/// </returns>
		///
		/// <summary>
		///   <para>
		///     Takes a point in downwards herringbone coordinates and returns
		///     its world position.
		///   </para>
		/// </summary>
		public Vector3 HerringDownToWorld(Vector3 herring) {
			return CubicToWorld(HerringDownToCubic(herring));
		}

		/// <summary>
		///   Returns the upwards herringbone coordinates of a point in downwards- coordinates.
		/// </summary>
		/// <param name="herring">
		///   Point in downwards herringbone coordinates.
		/// </param>
		/// <returns>
		///   Point in upwards herringbone coordinates.
		/// </returns>
		///
		/// <summary>
		///   <para>
		///     Takes a point in downwards herringbone coordinates and returns
		///     its upwards herringbone position.
		///   </para>
		/// </summary>
		public Vector3 HerringDownToHerringUp(Vector3 herring) {
			// This could be done directly without going to cubic
			return CubicToHerringU(HerringDownToCubic(herring));
		}

		/// <summary>
		///   Returns the rhombic coordinates of a point in downwards herringbone coordinates.
		/// </summary>
		/// <param name="herring">
		///   Point in downwards herringbone coordinates.
		/// </param>
		/// <returns>
		///   Point in rhombic coordinates.
		/// </returns>
		///
		/// <summary>
		///   <para>
		///     Takes a point in downwards herringbone coordinates and returns
		///     its rhombic position.
		///   </para>
		/// </summary>
		public Vector3 HerringDownToRhombicUp(Vector3 herring) {
			return CubicToRhombic(HerringDownToCubic(herring));
		}

		/// <summary>
		///   Returns the downwards rhombic coordinates of a point in downwards herringbone coordinates.
		/// </summary>
		/// <param name="herring">
		///   Point in downwards herringbone coordinates.
		/// </param>
		/// <returns>
		///   Point in downwards rhombic coordinates.
		/// </returns>
		///
		/// <summary>
		///   <para>
		///     Takes a point in downwards herringbone coordinates and returns
		///     its downwards rhombic position.
		///   </para>
		/// </summary>
		public Vector3 HerringDownToRhombicDown(Vector3 herring) {
			return CubicToRhombicD(HerringDownToCubic(herring));
		}

		/// <summary>
		///   Returns the cubic coordinates of a point in downwards herringbone coordinates.
		/// </summary>
		/// <param name="herring">
		///   Point in downwards herringbone coordinates.
		/// </param>
		/// <returns>
		///   Point in cubic coordinates.
		/// </returns>
		///
		/// <summary>
		///   <para>
		///     Takes a point in downwards herringbone coordinates and returns
		///     its cubic position.
		///   </para>
		/// </summary>
		public Vector4 HerringDownToCubic(Vector3 herring) {
			return HerringToCubic(herring, false);
		}

		/// <summary>
		///   Returns the barycentric coordinates of a point in downwards herringbone coordinates.
		/// </summary>
		/// <param name="herring">
		///   Point in downwards herringbone coordinates.
		/// </param>
		/// <returns>
		///   Point in barycentric coordinates.
		/// </returns>
		///
		/// <summary>
		///   <para>
		///     Takes a point in downwards herringbone coordinates and returns
		///     its barycentric position.
		///   </para>
		/// </summary>
		private Vector4 HerringDownToBarycentric(Vector3 herring) {
			return CubicToBarycentric(HerringDownToCubic(herring));
		}
#endregion  // Herring Down -> Conversion

#region  Herringbone -> Conversion
		/// <summary>
		///   Converts any herring- to cubic coordinates.
		/// </summary>
		/// <param name="herring">
		///   Herring coordinates.
		/// </param>
		/// <param name="up">
		///   True for upwards herring, false for downwards.
		/// </param>
		/// <returns>
		///   Cubic coordinates.
		/// </returns>
		private Vector4 HerringToCubic(Vector3 herring, bool up) {
			var pointed = Sides == Orientation.Pointed;
			var index   = Mathf.FloorToInt(pointed ? herring.x : herring.y);
			var even    = index % 2 == 0;

			float x, y, z;  // Cubic coordinates

			if (pointed) {
				x = herring.x;
				if (up) {
					if (even) {
						y = herring.y - index / 2f;
						z = -(x + y);
					} else {
						z = -herring.y - (index + 1f) / 2;
						y = -(x + z);
					}
				} else {
					if (even) {
						z = -herring.y - index / 2f;
						y = -(x + z);
					} else {
						y = herring.y - (index + 1f) / 2f;
						z = -(x + y);
					}
				}
			} else {
				y = herring.y;
				if (up) {
					if (even) {
						x = herring.x - index / 2f;
						z = -(y + x);
					} else {
						z = -herring.x - (index + 1f) / 2f;
						x = -(z + y);
					}
				} else {
					if (even) {
						z = -herring.x - index / 2f;
						x = -(z + y);
					} else {
						x = herring.x - (index + 1f) / 2f;
						z = -(y + x);
					}
				}
			}

			return new Vector4(x, y, z, herring.z);
		}
#endregion  // Herringbone -> Conversion

#region  Rhombic Upwards -> Conversion
		/// <summary>
		///   Returns the world coordinates of a point in rhombic coordinates.
		/// </summary>
		/// <param name="rhombic">
		///   Point in rhombic coordinates.
		/// </param>
		/// <returns>
		///   Point in world coordinates.
		/// </returns>
		///
		/// <summary>
		///   <para>
		///     Takes a point in rhombic coordinates and returns its world
		///     position.
		///   </para>
		/// </summary>
		public Vector3 RhombicUpToWorld(Vector3 rhombic) {
			Vector3 world = ruwMatrix.MultiplyPoint3x4(rhombic);
			return world;
		}

		/// <summary>
		///   Returns the upwards herringbone coordinates of a point in rhombic coordinates.
		/// </summary>
		/// <param name="rhombic">
		///   Point in rhombic coordinates.
		/// </param>
		/// <returns>
		///   Point in upwards herring coordinates.
		/// </returns>
		///
		/// <summary>
		///   <para>
		///     Takes a point in rhombic coordinates and returns its upwards
		///     herring position.
		///   </para>
		/// </summary>
		public Vector3 RhombicUpToHerringUp(Vector3 rhombic) {
			return CubicToHerringU(RhombicUpToCubic(rhombic));
		}

		/// <summary>
		///   Returns the downwards rhombic coordinates of a point in rhombic coordinates.
		/// </summary>
		/// <param name="rhombic">
		///   Point in rhombic coordinates.
		/// </param>
		/// <returns>
		///   Point in downwards rhombic coordinates.
		/// </returns>
		///
		/// <summary>
		///   <para>
		///     Takes a point in rhombic coordinates and returns its downwards
		///     rhombic position.
		///   </para>
		/// </summary>
		public Vector3 RhombicUpToRhombicDown(Vector3 rhombic) {
			return CubicToRhombicD(RhombicUpToCubic(rhombic));
		}

		/// <summary>
		///   Returns the downwards herringbone coordinates of a point in
		///   rhombic coordinates.
		/// </summary>
		/// <param name="rhombic">
		///   Point in rhombic coordinates.
		/// </param>
		/// <returns>
		///   Point in downwards herring coordinates.
		/// </returns>
		///
		/// <summary>
		///   <para>
		///     Takes a point in rhombic coordinates and returns its downwards
		///     herring position.
		///   </para>
		/// </summary>
		public Vector3 RhombicUpToHerringDown(Vector3 rhombic) {
			return CubicToHerringD(RhombicUpToCubic(rhombic));
		}

		/// <summary>
		///   Returns the cubic coordinates of a point in rhombic coordinates.
		/// </summary>
		/// <param name="rhombic">
		///   Point in rhombic coordinates.
		/// </param>
		/// <returns>
		///   Point in cubic coordinates.
		/// </returns>
		///
		/// <summary>
		///   <para>
		///     Takes a point in rhombic coordinates and returns its cubic
		///     position.
		///   </para>
		/// </summary>
		public Vector4 RhombicUpToCubic(Vector3 rhombic) {
			var pointy = Sides == Orientation.Pointed;
			var r_x = rhombic.x;
			var r_y = rhombic.y;

			float x = pointy ?   r_x        :  r_x + r_y;
			float y = pointy ?   r_y        : -r_x      ;
			float z = pointy ? -(r_x + r_y) : -r_y      ;
			return new Vector4(x, y, z, rhombic.z);
		}

		/// <summary>
		///   Returns the barycentric coordinates of a point in rhombic coordinates.
		/// </summary>
		/// <param name="rhombic">
		///   Point in rhombic coordinates.
		/// </param>
		/// <returns>
		///   Point in barycentric coordinates.
		/// </returns>
		///
		/// <summary>
		///   <para>
		///     Takes a point in rhombic coordinates and returns its
		///     barycentric position.
		///   </para>
		/// </summary>
		private Vector4 RhombicUpToBarycentric(Vector3 rhombic) {
			return CubicToBarycentric(RhombicUpToCubic(rhombic));
		}
#endregion  // Rhombic Upwards -> Conversion

#region  Rhombic Downwards -> Conversion
		/// <summary>
		///   Returns the world coordinates of a point in downwards rhombic coordinates.
		/// </summary>
		/// <param name="rhombic">
		///   Point in downwards rhombic coordinates.
		/// </param>
		/// <returns>
		///   Point in world coordinates.
		/// </returns>
		///
		/// <summary>
		///   <para>
		///     Takes a point in downwards rhombic coordinates and returns its
		///     world position.
		///   </para>
		/// </summary>
		public Vector3 RhombicDownToWorld(Vector3 rhombic) {
			Vector3 world = rdwMatrix.MultiplyPoint3x4(rhombic);
			return  world;
		}

		/// <summary>
		///   Returns the upwards herringbone coordinates of a point in downwards rhombic coordinates.
		/// </summary>
		/// <param name="rhombic">
		///   Point in downwards rhombic coordinates.
		/// </param>
		/// <returns>
		///   Point in upwards herring coordinates.
		/// </returns>
		///
		/// <summary>
		///   <para>
		///     Takes a point in downwards rhombic coordinates and returns its
		///     upwards herring position.
		///   </para>
		/// </summary>
		public Vector3 RhombicDownToHerringUp(Vector3 rhombic) {
			return CubicToHerringU(RhombicDownToCubic(rhombic));
		}

		/// <summary>
		///   Returns the downwards herringbone coordinates of a point in downwards rhombic coordinates.
		/// </summary>
		/// <param name="rhombic">
		///   Point in downwards rhombic coordinates.
		/// </param>
		/// <returns>
		///   Point in downwards herring coordinates.
		/// </returns>
		///
		/// <summary>
		///   <para>
		///     Takes a point in downwards rhombic coordinates and returns its
		///     downwards herring position.
		///   </para>
		/// </summary>
		public Vector3 RhombicDownToHerringDown(Vector3 rhombic) {
			return CubicToHerringD(RhombicDownToCubic(rhombic));
		}

		/// <summary>
		///   Returns the downwards herringbone coordinates of a point in
		///   downwards rhombic coordinates.
		/// </summary>
		/// <param name="rhombic">
		///   Point in downwards rhombic coordinates.
		/// </param>
		/// <returns>
		///   Point in downwards herring coordinates.
		/// </returns>
		///
		/// <summary>
		///   <para>
		///     Takes a point in downwards rhombic coordinates and returns its
		///     downwards herring position.
		///   </para>
		/// </summary>
		public Vector3 RhombicDownToRhombicUp(Vector3 rhombic) {
			return CubicToRhombic(RhombicDownToCubic(rhombic));
		}

		/// <summary>
		///   Returns the cubic coordinates of a point in downwards rhombic coordinates.
		/// </summary>
		/// <param name="rhombic">
		///   Point in downwards rhombic coordinates.
		/// </param>
		/// <returns>
		///   Point in cubic coordinates.
		/// </returns>
		///
		/// <summary>
		///   <para>
		///     Takes a point in downwards rhombic coordinates and returns its
		///     cubic position.
		///   </para>
		/// </summary>
		public Vector4 RhombicDownToCubic(Vector3 rhombic) {
			var pointy = Sides == Orientation.Pointed;
			var r_x = rhombic.x;
			var r_y = rhombic.y;

			float c_x = r_x;
			float c_y = pointy ? -r_x + r_y : -r_x - r_y;
			float c_z = pointy ? -r_y       :  r_y      ;
			return new Vector4(c_x, c_y, c_z, rhombic.z);
		}

		/// <summary>
		///   Returns the barycentric coordinates of a point in downwards
		///   rhombic coordinates.
		/// </summary>
		/// <param name="rhombic">
		///   Point in downwards rhombic coordinates.
		/// </param>
		/// <returns>
		///   Point in barycentric coordinates.
		/// </returns>
		///
		/// <summary>
		///   <para>
		///     Takes a point in downwards rhombic coordinates and returns its
		///     barycentric position.
		///   </para>
		/// </summary>
		private Vector4 RhombicDownToBarycentric(Vector3 rhombic) {
			return CubicToBarycentric(RhombicDownToCubic(rhombic));
		}
#endregion  // Rhombic Downwards -> Conversion

#region  Cubic -> Conversion
		//     _____     The X-axis goes from the origin through the eastern
		//    /Y    \    vertex.
		//   /  \    \   The Y-axis goes from the origin through the north-
		//  (    O---X)  western vertex.
		//   \  /    /   The Z-axis goes from the origin through the south-
		//    \Z____/    western vertex.
		//
		//   Basis vectors:
		//      x = ( 2/3,    0)
		//      y = (-2/3,  1/2)
		//      z = (-2/3, -1/2)
		//   Where the first component is a multiple of 's' and the second
		//   of 'h'.
		//
		//
        //    /Y\     The X-axis goes from the south-western vertex through
        //   / | \    north-eastern vertex.                                        
        //  /  |  \   The Y-axis goes from the south-eastern vertex through        
        // |   |   |  the north-western vertex                                     
        // |   O   |  The Z-axis goes from the northern vertex through the         
        // |  / \  |  southern vertex                                              
        //  Z/   \X                                                                 
        //   \   /    It's the same as above, except rotated 30° clockwise
        //    \_/

		/// <summary>
		///   Returns the world coordinates of a point in cubic coordinates.
		/// </summary>
		/// <param name="point">
		///   Point in cubic coordinates.
		/// </param>
		/// <returns>
		///   Point in world coordinates.
		/// </returns>
		/// <summary>
		///   <para>
		///     Takes a point in cubic coordinates and returns its world
		///     position.
		///   </para>
		/// </summary>
		public Vector3 CubicToWorld(Vector4 point) {
			var gridPoint = CubicToGrid(point);
			return gwMatrix.MultiplyPoint3x4(gridPoint);
		}

		private Vector3 CubicToGrid(Vector4 point) {
			return cgMatrix * point;
		}

		/// <summary>
		///   Returns the upwards herring coordinates of a point in cubic coordinates.
		/// </summary>
		/// <param name="cubic">
		///   Point in cubic coordinates.
		/// </param>
		/// <returns>
		///   Point in upwards herring coordinates.
		/// </returns>
		/// <summary>
		///   <para>
		///     Takes a point in cubic coordinates and returns its upwards
		///     herring position.
		///   </para>
		/// </summary>
		public Vector3 CubicToHerringU(Vector4 cubic) {
			return CubicToHerring(cubic, true);
		}

		/// <summary>
		///   Returns the downwards herring coordinates of a point in cubic coordinates.
		/// </summary>
		/// <param name="cubic">
		///   Point in cubic coordinates.
		/// </param>
		/// <returns>
		///   Point in downwards herring coordinates.
		/// </returns>
		/// <summary>
		///   <para>
		///     Takes a point in cubic coordinates and returns its downwards
		///     herring position.
		///   </para>
		/// </summary>
		public Vector3 CubicToHerringD(Vector4 cubic) {
			return CubicToHerring(cubic, false);
		}

		/// <summary>
		///   Converts cubic- to any herring coordinates.
		/// </summary>
		/// <param name="cubic">
		///   Cubic coordinates.
		/// </param>
		/// <param name="up">
		///   True for upwards herring, false for downwards.
		/// </param>
		/// <summary>
		///   <para>
		///     Takes a point in cubic coordinates and returns its herring
		///     position, either up- or downwards. Use this method only
		///     internally and expost the up- and downwards case individually.
		///   </para>
		/// </summary>
		private Vector3 CubicToHerring(Vector4 cubic, bool up) {
			var pointed = Sides == Orientation.Pointed;
			var index   = Mathf.FloorToInt(pointed ? cubic.x : cubic.y);
			var even    = (index & 1) == 0;

			float row, column;

			if (pointed) {
				row = cubic.x;
				if (up) {
					column = even ?  cubic.y + index / 2 : -cubic.z - (index + 1) / 2;
				} else {
					column = even ? -cubic.z - index / 2 :  cubic.y + (index + 1) / 2;
				}
			} else {
				column = cubic.y;
				if (up) {  // i.e. right
					row = even ?  cubic.x + index / 2 : -cubic.z - (index + 1) / 2;
				} else {
					row = even ? -cubic.z - index / 2 : cubic.x + (index + 1) / 2;
				}
			}

			return new Vector3(row, column, cubic.w);
		}

		/// <summary>
		///   Returns the rhombic coordinates of a point in cubic coordinates.
		/// </summary>
		/// <param name="cubic">
		///   Point in cubic coordinates.
		/// </param>
		/// <returns>
		///   Point in rhombic coordinates.
		/// </returns>
		///
		/// <summary>
		///   <para>
		///     Takes a point in cubic coordinates and returns its rhombic
		///     position.
		///   </para>
		/// </summary>
		public Vector3 CubicToRhombic(Vector4 cubic) {
			var pointed = Sides == Orientation.Pointed;
			var x = pointed ? cubic.x : -cubic.y;
			var y = pointed ? cubic.y : -cubic.z;
			return new Vector3(x, y, cubic.w);
		}

		/// <summary>
		///   Returns the downwards rhombic coordinates of a point in cubic coordinates.
		/// </summary>
		/// <param name="cubic">
		///   Point in cubic coordinates.
		/// </param>
		/// <returns>
		///   Point in downwards rhombic coordinates.
		/// </returns>
		///
		/// <summary>
		///   <para>
		///     Takes a point in cubic coordinates and returns its rhombic
		///     position.
		///   </para>
		/// </summary>
		public Vector3 CubicToRhombicD(Vector4 cubic) {
			var pointed = Sides == Orientation.Pointed;
			var x = cubic.x;
			var y = pointed ? -cubic.z :  cubic.z;
			return new Vector3(x, y, cubic.w);
		}

		/// <summary>
		///   Returns the barycentric coordinates of a point in cubic coordinates.
		/// </summary>
		/// <param name="cubic">
		///   Point in cubic coordinates.
		/// </param>
		/// <returns>
		///   Point in world coordinates.
		/// </returns>
		///
		/// <summary>
		///   <para>
		///     Takes a point in cubic coordinates and returns its barycentric
		///     position.
		///   </para>
		/// </summary>
		private Vector4 CubicToBarycentric(Vector4 cubic) {
			return new Vector4(cubic.x, cubic.y, cubic.z + 1.0f, cubic.w);
		}
#endregion  // Cubic -> Conversion

#region  Barycentric -> Conversion
		/// <summary>
		///   Returns the world coordinates of a point in barycentric coordinates.
		/// </summary>
		/// <param name="barycentric">
		///   Point in barycentric coordinates.
		/// </param>
		/// <returns>
		///   Point in world coordinates.
		/// </returns>
		///
		/// <summary>
		///   <para>
		///     Takes a point in barycentric coordinates and returns its world
		///     position.
		///   </para>
		/// </summary>
		private Vector3 BarycentricToWorld(Vector4 barycentric) {
			return CubicToWorld(BarycentricToCubic(barycentric));
		}

		/// <summary>
		///   Returns the upwards herring coordinates of a point in barycentric coordinates.
		/// </summary>
		/// <param name="barycentric">
		///   Point in barycentric coordinates.
		/// </param>
		/// <returns>
		///   Point in upwards herring coordinates.
		/// </returns>
		///
		/// <summary>
		///   <para>
		///     Takes a point in barycentric coordinates and returns its
		///     upwards herring position.
		///   </para>
		/// </summary>
		private Vector3 BarycentricToHerringU(Vector4 barycentric) {
			return CubicToHerringU(BarycentricToCubic(barycentric));
		}

		/// <summary>
		///   Returns the downwards herring coordinates of a point in barycentric coordinates.
		/// </summary>
		/// <param name="barycentric">
		///   Point in barycentric coordinates.
		/// </param>
		/// <returns>
		///   Point in downwards herring coordinates.
		/// </returns>
		///
		/// <summary>
		///   <para>
		///     Takes a point in barycentric coordinates and returns its
		///     downwards herring position.
		///   </para>
		/// </summary>
		private Vector3 BarycentricToHerringD(Vector4 barycentric) {
			return CubicToHerringD(BarycentricToCubic(barycentric));
		}

		/// <summary>
		///   Returns the rhombic coordinates of a point in barycentric coordinates.
		/// </summary>
		/// <param name="barycentric">
		///   Point in barycentric coordinates.
		/// </param>
		/// <returns>
		///   Point in rhombic coordinates.
		/// </returns>
		///
		/// <summary>
		///   <para>
		///     Takes a point in barycentric coordinates and returns its
		///     rhombic position.
		///   </para>
		/// </summary>
		private Vector3 BarycentricToRhombic(Vector4 barycentric) {
			return CubicToRhombic(BarycentricToCubic(barycentric));
		}

		/// <summary>
		///   Returns the cubic coordinates of a point in barycentric coordinates.
		/// </summary>
		/// <param name="barycentric">
		///   Point in barycentric coordinates.
		/// </param>
		/// <returns>
		///   Point in cubic coordinates.
		/// </returns>
		///
		/// <summary>
		///   <para>
		///     Takes a point in barycentric coordinates and returns its cubic
		///     position.
		///   </para>
		/// </summary>
		private Vector4 BarycentricToCubic(Vector4 barycentric) {
			return new Vector4(barycentric.x, barycentric.y, barycentric.z - 1.0f, barycentric.w);
		}
#endregion  // Barycentric -> Conversion

#region  Hook methods
		private void OnRadiusChanged(float previous, float current) {
			var delta = Mathf.Abs(previous - current);
			_cacheIsDirty |= delta > Mathf.Epsilon;

			if (RadiusChanged == null) {
				return;
			}
			var args = new RadiusEventArgs(previous, current);
			RadiusChanged(this, args);
		}

		private void OnSidesChanged(Orientation previous) {
			_cacheIsDirty |= previous != _sides;

			if (SidesChanged == null) {
				return;
			}
			var args = new SidesEventArgs(previous);
			SidesChanged(this, args);
		}
#endregion  // Hook methods
	}
}
