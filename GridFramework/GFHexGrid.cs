using UnityEngine;
using System;
using System.Collections;
using GridFramework;
using GridFramework.Vectors;

/*
HEXAGON DIMENSIONS:

[------s------]      s = 3/2 * r
     _________
    / |       \
   /  |        \
  /   h         \
 /    |          \
(     |  [--r-----]  h = 2r * sin(60°)
 \    |          /     =  r * sqrt(3)
  \   |         /
   \  |        /
    \_|_______/
[--------w--------]  w = 2r

r: radius  s: side  w: width  h: height
*/

/// <summary>
/// A grid consisting of flat hexagonal grids stacked on top of each other.
/// </summary>
/// <remarks>
/// A regular hexagonal grid that forms a honeycomb pattern. It is
/// characterized by the <c>radius</c> (distance from the centre of a hexagon
/// to one of its vertices) and the <c>depth</c> (distance between two
/// honeycomb layers). Hex grids use a herringbone pattern for their coordinate
/// system, please refer to the user manual for information about how that
/// coordinate system works.
/// </remarks>
public class GFHexGrid : GFLayeredGrid {

	#region Enums

	/// <summary>orientation of hexes, pointy or flat sides.</summary>
	/// There are two ways a hexagon can be rotated: <c>PointySides</c> has
	/// flat tops, parallel to the grid's X-axis, and <c>FlatSides</c> has
	/// pointy tops, parallel to the grid's Y-axis.
	public enum HexOrientation {
		/// Pointy east and west, flat north and south, equal to <c>HexTopOrientation.FlatTops</c>.
		PointySides,
		/// Flat east and west, pointy north and south, equal to <c>HexTopOrientation.PointyTops</c>.
		FlatSides
	};

	/// <summary>orientation of hexes, flat or pointy tops.</summary>
	/// There are two ways a hexagon can be rotated: <c>FlatTops</c> has flat
	/// tops, parallel to the grid's X-axis, and <c>PointyTops</c> has pointy
	/// tops, parallel to the grid's Y-axis. This enum is best used to
	/// complement <c>HexOrientation</c>.
	public enum HexTopOrientation {
		/// Flat north and south, pointy east and west, equal to
		/// <c>HexOrientation.PointySides</c>.
		FlatTops,
		/// Pointy north and south, flat east and west, equal to
		/// <c>HexOrientation.FlatSides</c>.
		PointyTops
	};

	/// <summary>Different coordinate systems for hexagonal grids.</summary>
	/// This is an enumeration of all the currently supported coordinate
	/// systems for hexagonal grids.  Cubic and barycentric coordinates are
	/// four-dimensional, the rest are three-dimensional.
	private enum HexCoordinateSystem {
		/// Herringbone pattern where every odd column is shifted upwards.
		HerringUp,
		/// Herringbone pattern where every odd column is shifted downwards.
		HerringDown,
		/// Rhombic pattern where both axes go along the flat sides at a 60°
		/// angle towards each other.
		Rhombic,
		/// Rhombic pattern where both axes go along the flat sides at a 300°
		/// angle towards each other.
		RhombicDown,
		/// Cubic pattern with three axes in a plane, each one going along the
		/// flat sides, at 60° to each other. The sum of all three coordinates
		/// is always 0.
		Cubic,
		//Barycentric  ///< Don't use, not yeat ready!
	};

	/// <summary>Shape of the drawing and rendering.</summary>
	/// Different shapes of hexagonal grids: <c>Rectangle</c> looks like a
	/// rectangle with every odd-numbered column offset,
	/// <c>CompactRectangle</c> is similar with the odd-numbered columns one
	/// hex shorter.
	public enum HexGridShape {
		Rectangle        , ///< Rectangular upwards herringbone pattern.
		CompactRectangle , ///< Rectangular upwards herringbone pattern with
		                   ///< the top of every odd column clipped.
		RectangleDown    , ///< Rectangular downwards herringbone pattern.
		Rhombus          , ///< Rhombus-like upwards pattern.
		RhombusDown      , ///< Rhombus-like downwards pattern.
		HerringboneUp    , ///< Upwards herringbone pattern.
		HerringboneDown  , ///< Downwards herringbone pattern.
		Circle           , ///< Circle around a hexagon.
	}

	/// <summary>Cardinal direction of a vertex.</summary>
	/// The cardinal position of a vertex relative to the centre of a given
	/// hex.  Note that using N and S for pointy sides, as well as E and W for
	/// flat sides does not make sense, but it is still possible.
	public enum HexDirection {
		N  , ///< North
		NE , ///< South
		E  , ///< East
		SE , ///< South-East
		S  , ///< South
		SW , ///< South-West
		W  , ///< West
		NW , ///< North-West
	};
	#endregion

	#region class members
	#region public members
	[SerializeField] private float _radius = 1.0f;

	/// <summary>Distance from the centre of a hex to a vertex.</summary>
	///
	///	This refers to the distance between the centre of a hexagon and one of
	///	its vertices. Since the hexagon is regular all vertices have the same
	///	distance from the centre. In other words, imagine a circumscribed
	///	circle around the hexagon, its radius is the radius of the hexagon. The
	///	value may not be less than `Mathf.Epsilon`.
	public float radius {
		get { return _radius; }
		set { SetMember<float>(value, ref _radius, restrictor: Mathf.Max, limit: Mathf.Epsilon); }
	}

	///<summary> Hex to render the cone around (grid coordinates).</summary>
	[SerializeField] private Vector3 _renderAround = Vector3.zero;

	///<summary> Hex to render the cone around (grid coordinates).</summary>
	public Vector3 renderAround {
		get {return _renderAround;}
		set {SetMember<Vector3>(value, ref _renderAround, updateMatrix: false);}
	}

	[SerializeField] protected HexOrientation _hexSideMode;

	/// <summary>Pointy sides or flat sides.</summary>
	/// <value>
	/// Whether the grid has pointy sides or flat sides. This affects both the
	/// drawing and the calculations.
	/// </value>
	public HexOrientation hexSideMode {
		get {return _hexSideMode;}
		set {SetMember<HexOrientation>(value, ref _hexSideMode);}
	}

	/// <summary>Flat tops or pointy tops.</summary>
	/// Whether the grid has flat tops or pointy tops.  This is directly
	/// connected to <c>hexSideMode</c>, in fact this is just an accessor that
	/// gets and sets the appropriate value for it.
	public HexTopOrientation hexTopMode {
		get {
			if (hexSideMode == HexOrientation.PointySides) {
				return HexTopOrientation.FlatTops;
			} else {
				return HexTopOrientation.PointyTops;
			}
		}
		set {
			if (value == HexTopOrientation.FlatTops) {
				hexSideMode = HexOrientation.PointySides;
			} else {
				hexSideMode = HexOrientation.FlatSides;
			}
		}
	}

	[SerializeField]
	protected HexGridShape _gridStyle = HexGridShape.Rectangle;

	/// <summary>
	///   The shape of the overall grid, affects only drawing and
	///   rendering, not the calculations.
	/// </summary>
	/// <value>
	///   The shape when drawing or rendering the grid. This only affects the
	///   grid’s appearance, but not how it works.
	/// </value>
	public HexGridShape gridStyle {
		get {return _gridStyle;}
		set { SetMember<HexGridShape>(value, ref _gridStyle, updateMatrix: false); }
	}
	#endregion

	#region helper values (read only)
	#region Agnostic Helper Values
	// These helper values are orientation-agnostic, meaning they are normal
	// for pointy  sides, and flipped 90° for flat sides (keeps formulas
	// simple)
	private float h {get {return radius * Mathf.Sqrt(3.0f);}} ///< radius * sqrt(3)
	private float w {get {return radius * 2.0f            ;}} ///< radius * 2
	private float s {get {return radius * 1.5f            ;}} ///< radius * 3/2
	private float d {get {return depth                    ;}} ///< depth
	#endregion
	//use these helper values to keep the formulae simple (everything depends on radius)

	/// <summary>1.5 times the radius.</summary>
	/// <value>Shorthand writing for <c>1.5f * #radius</c> (read-only).</value>
	public float side {get {return s;}}

	/// <summary>Full width of the hex.</summary>
	/// This is the full vertical height of a hex.  For pointy side hexes this
	/// is the distance from one edge to its opposite (<c>sqrt(3) * radius</c>)
	/// and for flat side hexes it is the distance between two opposite
	/// vertices (<c>2 * radius</c>).
	public float height {get {return hexSideMode == HexOrientation.PointySides ? h : w;}}

	/// <summary>Distance between vertices on opposite sides.</summary>
	/// This is the full horizontal width of a hex.  For pointy side hexes this
	/// is the distance from one vertex to its opposite (<c>2 * radius</c>) and
	/// for flat side hexes it is the distance between to opposite edges
	/// (<c>sqrt(3) * radius</c>).
	public float width {get {return hexSideMode == HexOrientation.PointySides ? w : h;}}
	#endregion
	#region Tangens
	// Tangent of 30 and 60, used in WorldToCubic
	private static readonly float tan30 = Mathf.Tan((1.0f / 6.0f) * Mathf.PI);
	private static readonly float tan60 = Mathf.Tan((2.0f / 6.0f) * Mathf.PI);
	#endregion
	#endregion

	#region Matrices
	#region Variables and Accessors
	/// <summary>Matrix that transforms from world to local.</summary>
	private Matrix4x4 _wlMatrix = Matrix4x4.identity;
	/// <summary>Matrix that transforms from local to world.</summary>
	private Matrix4x4 _lwMatrix = Matrix4x4.identity;

	/// <summary> Matrix that transforms from upwards rhombic to world. </summary>
	private Matrix4x4 _ruwMatrix = Matrix4x4.identity;

	/// <summary> Matrix that transforms from world to upwards rhombic. </summary>
	private Matrix4x4 _wruMatrix = Matrix4x4.identity;

	/// <summary> Matrix that transforms from upwards rhombic to world. </summary>
	private Matrix4x4 _rdwMatrix = Matrix4x4.identity;

	/// <summary> Matrix that transforms from world to upwards rhombic. </summary>
	private Matrix4x4 _wrdMatrix = Matrix4x4.identity;

	/// <summary>Matrix that transforms from world to local.</summary>
	private Matrix4x4 wlMatrix {get {MatricesUpdate();return _wlMatrix;}}
	/// <summary>Matrix that transforms from local to world.</summary>
	private Matrix4x4 lwMatrix {get {MatricesUpdate();return _lwMatrix;}}

	/// <summary> Matrix that transforms from upwards rhombic to world. </summary>
	private Matrix4x4 ruwMatrix {get {MatricesUpdate(); return _ruwMatrix;}}
	/// <summary> Matrix that transforms from world to upwards rhombic. </summary>
	private Matrix4x4 wruMatrix {get {MatricesUpdate(); return _wruMatrix;}}
	/// <summary> Matrix that transforms from upwards rhombic to world. </summary>
	private Matrix4x4 rdwMatrix {get {MatricesUpdate(); return _rdwMatrix;}}
	/// <summary> Matrix that transforms from world to upwards rhombic. </summary>
	private Matrix4x4 wrdMatrix {get {MatricesUpdate(); return _wrdMatrix;}}
	#endregion

	protected override void MatricesUpdate() {
		if (!_matricesMustUpdate && !_TransformNeedsUpdate()) {
			return;
		}

		// Shearing for rhombic coordinates
		var shearMatrix = Matrix4x4.identity;
		if (hexSideMode == HexOrientation.PointySides) {
			shearMatrix[1,0] = Mathf.Sqrt(3.0f) / 3.0f; // 0.5h/s
		} else {
			shearMatrix[0,1] = Mathf.Sqrt(3.0f) / 3.0f; // 0.5h/s
		}

		// Spacing for rhombic coordinates
		var spacing = new Vector3(s, h, d);
		Swap<float>(ref spacing.x, ref spacing.y, hexSideMode == HexOrientation.FlatSides);

		_ruwMatrix = Matrix4x4.TRS(_Transform.position, _Transform.rotation, Vector3.one)
			* LayeredMatrix
			* shearMatrix
			* Matrix4x4.TRS(originOffset, Quaternion.identity, spacing)
			* LayeredMatrix;

		shearMatrix = Matrix4x4.identity;
		if (hexSideMode == HexOrientation.PointySides) {
			shearMatrix[1,0] = -Mathf.Sqrt(3.0f) / 3.0f; // 0.5h/s
		} else {
			shearMatrix[0,1] = -Mathf.Sqrt(3.0f) / 3.0f; // 0.5h/s
		}

		_rdwMatrix = Matrix4x4.TRS(_Transform.position, _Transform.rotation, Vector3.one)
			* LayeredMatrix
			* shearMatrix
			* Matrix4x4.TRS(originOffset, Quaternion.identity, spacing)
			* LayeredMatrix;

		_lwMatrix.SetTRS(_Transform.position, _Transform.rotation, Vector3.one);
		_lwMatrix *= Matrix4x4.TRS(originOffset, Quaternion.identity, Vector3.one);

		_wruMatrix = _ruwMatrix.inverse;
		_wrdMatrix = _rdwMatrix.inverse;
		_wlMatrix  = _lwMatrix.inverse;

		if (hexSideMode == HexOrientation.PointySides) { // right and 30° upwards
			_cubicColumnBasis = side * Units[idxS[0]] + 0.5f * h * Units[idxS[1]];
			_cubicRowBasis    = h * Units[idxS[1]];
		} else { // straight right
			_cubicColumnBasis = h * Units[idxS[1]];
			_cubicRowBasis    = side * Units[idxS[0]] + 0.5f * h * Units[idxS[1]];
		}
	}

	#region Basis Vectors
	private Vector3 _cubicColumnBasis = Vector3.zero;

	/// <summary>Cubic X-basis vector (column)</summary>
	/// <value>Gets the column basis.</value>
	/// Each basis-vector is orthogonal to its opposite cubic axis
	private Vector3 cubicColumnBasis { // the X-basis vector
		get {MatricesUpdate(); return _cubicColumnBasis;}
	}

	private Vector3 _cubicRowBasis = Vector3.zero;

	/// <summary>Cubic Y-basis vector (row)</summary>
	/// <value>Gets the row basis.</value>
	/// Each basis-vector is orthogonal to its opposite cubic axis
	private Vector3 cubicRowBasis { // the Y -basis vector
		get {MatricesUpdate(); return _cubicRowBasis;}
	}
	#endregion
	#endregion

	#region Coordinate Conversion
	// each of the following regions contains the conversions from that system into the others
	#region Grid

	/// <summary>Converts world coordinates to grid coordinates.</summary>
	/// <param name="worldPoint">Point in world space.</param>
	/// <returns>Grid coordinates of the world point (upwards herringbone coordinate system).</returns>
	///
	/// This is the same as calling <c>#WorldToHerringU</c>, because upwards
	/// herringbone is the default grid coordinate system.
	public override Vector3 WorldToGrid(Vector3 worldPoint) {
		return WorldToHerringU(worldPoint);
	}

	/// <summary>Converts grid coordinates to world coordinates</summary>
	/// <param name="gridPoint">Point in grid space (upwards herringbone coordinate system).</param>
	/// <returns>World coordinates of the grid point.</returns>
	///
	/// This is the same as calling <c>#HerringUToWorld</c>, because upwards
	/// herringbone is the default grid coordinate system.
	public override Vector3 GridToWorld(Vector3 gridPoint) {
		return HerringUToWorld(gridPoint);
	}
	#endregion

	#region World
	/// <summary>Returns the upwards herringbone coordinates of a point in world space.</summary>
	/// <param name="world">Point in world coordinates.</param>
	/// <returns>Point in upwards herringbone coordinates.</returns>
	///
	/// This method takes a point in world space and returns the corresponding
	/// upwards herringbone coordinates. Every odd numbered column is offset
	/// upwards, giving this coordinate system the herringbone pattern. This
	/// means that the Y coordinate directly depends on the X coordinate. The Z
	/// coordinate is simply which layer of the grid is on, relative to the
	/// grid's central layer.
	public Vector3 WorldToHerringU(Vector3 world) {
		return CubicToHerringU(WorldToCubic(world));
	}

	/// <summary>Returns the downwards herringbone coordinates of a point in world space.</summary>
	/// <param name="world">Point in world coordinates.</param>
	/// <returns>Point in downwards herringbone coordinates.</returns>
	///
	/// This method takes a point in world space and returns the corresponding
	/// downwards herringbone coordinates.  Every odd numbered column is offset
	/// downwards, giving this coordinate system the herringbone pattern. This
	/// means that the Y coordinate directly depends on the X coordinate. The Z
	/// coordinate is simply which layer of the grid is on, relative to the
	/// grid's central layer.
	public Vector3 WorldToHerringD(Vector3 world) {
		return CubicToHerringD(WorldToCubic(world));
	}

	/// <summary>Returns the rhombic coordinates of a point in world space.</summary>
	/// <param name="world">Point in world coordinates.</param>
	/// <returns>Point in rhombic coordinates.</returns>
	///
	/// This method takes a point in world space and returns the corresponding
	/// rhombic coordinates. The rhombic coordinate system uses three axes; the
	/// X-axis rotated 30° counter-clockwise, the regular Y-axis, and the Z
	/// coordinate is which layer of the grid the point is on, relative to the
	/// grid's central layer.
	public Vector3 WorldToRhombic(Vector3 world) {
		Vector3 rhombic = wruMatrix.MultiplyPoint3x4(world);
		return  rhombic;
	}

	/// <summary>Returns the downwards rhombic coordinates of a point in world space.</summary>
	/// <param name="world">Point in world coordinates.</param>
	/// <returns>Point in downwards rhombic coordinates.</returns>
	///
	/// This method takes a point in world space and returns the corresponding
	/// downwards rhombic coordinates. The downwards rhombic coordinate system
	/// uses three axes; the X-axis rotated 300° counter-clockwise, the regular
	/// Y-axis, and the Z coordinate is which layer of the grid the point is
	/// on, relative to the grid's central layer.
	public Vector3 WorldToRhombicD(Vector3 world) {
		Vector3 rhombic = wrdMatrix.MultiplyPoint3x4(world);
		return  rhombic;
	}

	/// <summary>Returns the cubic coordinates of a point in world space.</summary>
	/// <param name="world">Point in world coordinates.</param>
	/// <returns>Point in cubic coordinates.</returns>
	///
	/// This method takes a point in world space and returns the corresponding
	/// rhombic coordinates.  The cubic coordinate system uses four axes; X, Y
	/// and Z are used to fix the point on the layer while W is which layer of
	/// the grid the point is on, relative to the grid's central layer. The
	/// central hex has coordinates (0, 0, 0, 0) and the sum of the first three
	/// coordinates is always 0.
	public Vector4 WorldToCubic(Vector3 world) {
		float x, y, z, w;
		Vector3 local;

		local = wlMatrix.MultiplyPoint3x4(world);
		if (hexSideMode == HexOrientation.PointySides) {
			x = local[idx[0]] / side;
			y = (local[idx[1]] - local[idx[0]] * tan30) / height;
			z = -1.0f * (x + y);
		} else {
			z = -local[idx[1]] / side;
			y = (local[idx[1]] - local[idx[0]] * tan60) / (3.0f * radius);
			x = -1.0f * (z + y);
		}
		w = local[idx[2]] / depth;
		return new Vector4(x, y, z, w);
	}

	/// <summary>Returns the barycentric coordinates of a point in world space.</summary>
	/// <param name="world">Point in world coordinates.</param>
	/// <returns>Point in barycentric coordinates.</returns>
	///
	/// This method takes a point in world space and returns the corresponding
	/// barycentric coordinates. (subject to change?) Barycentric coordinates
	/// are similar to cubic ones, except the sum of the first three
	/// coordinates is 1.  The central hex has coordinates (0, 0, -1, 0), its
	/// north-eastern neighbour has coordinates (1, 0, 0, 0) and its northern
	/// neighbour has coordinates (0, 1, 0, 0).  In other words, it is the
	/// cubic coordinate system with +1 added to the Z-coordinate.
	private Vector4 WorldToBarycentric(Vector3 world) {
		return CubicToBarycentric(WorldToCubic(world));
	}

	#endregion

	#region Herring Up
	/// <summary>Returns the world coordinates of a point in upwards herringbone coordinates.</summary>
	/// <param name="herring">Point in upwards herringbone coordinates.</param>
	/// <returns>Point in world coordinates.</returns>
	///
	/// Takes a point in upwards herringbone coordinates and returns its world position.
	public Vector3 HerringUToWorld(Vector3 herring) {
		return CubicToWorld(HerringUToCubic(herring));
	}

	/// <summary>Returns the downwards herringbone coordinates of a point in upwards- coordinates.</summary>
	/// <param name="herring">Point in upwards herringbone coordinates.</param>
	/// <returns>Point in downwards herringbone coordinates.</returns>
	///
	/// Takes a point in upwards herringbone coordinates and returns its downwards herringbone position.
	public Vector3 HerringUToHerringD(Vector3 herring) {
		// This could be done directly without going to cubic
		return CubicToHerringD(HerringUToCubic(herring));
	}

	/// <summary>Returns the rhombic coordinates of a point in upwards herringbone coordinates.</summary>
	/// <param name="herring">Point in upwards herringbone coordinates.</param>
	/// <returns>Point in rhombic coordinates.</returns>
	///
	/// Takes a point in upwards herringbone coordinates and returns its
	/// rhombic position.
	public Vector3 HerringUToRhombic(Vector3 herring) {
		return CubicToRhombic(HerringUToCubic(herring));
	}

	/// <summary>Returns the downwards rhombic coordinates of a point in upwards herringbone coordinates.</summary>
	/// <param name="herring">Point in upwards herringbone coordinates.</param>
	/// <returns>Point in downwards rhombic coordinates.</returns>
	///
	/// Takes a point in upwards herringbone coordinates and returns its
	/// downwards rhombic position.
	public Vector3 HerringUToRhombicD(Vector3 herring) {
		return CubicToRhombicD(HerringUToCubic(herring));
	}

	/// <summary>Returns the cubic coordinates of a point in upwards herringbone coordinates.</summary>
	/// <param name="herring">Point in upwards herringbone coordinates.</param>
	/// <returns>Point in cubic coordinates.</returns>
	///
	/// Takes a point in upwards herringbone coordinates and returns its cubic
	/// position.
	public Vector4 HerringUToCubic(Vector3 herring) {
		return HerringToCubic(herring, true);
	}

	/// <summary>Returns the barycentric coordinates of a point in upwards herringbone coordinates.</summary>
	/// <param name="herring">Point in upwards herringbone coordinates.</param>
	/// <returns>Point in barycentric coordinates.</returns>
	///
	/// Takes a point in upwards herringbone coordinates and returns its
	/// barycentric position.
	private Vector4 HerringUToBarycentric(Vector3 herring) {
		return CubicToBarycentric(HerringUToCubic(herring));
	}
	#endregion

	#region Herring Down
	/// <summary>Returns the world coordinates of a point in downwards herringbone coordinates.</summary>
	/// <param name="herring">Point in downwards herringbone coordinates.</param>
	/// <returns>Point in world coordinates.</returns>
	///
	/// Takes a point in downwards herringbone coordinates and returns its
	/// world position.
	public Vector3 HerringDToWorld(Vector3 herring) {
		return CubicToWorld(HerringDToCubic(herring));
	}

	/// <summary>Returns the upwards herringbone coordinates of a point in downwards- coordinates.</summary>
	/// <param name="herring">Point in downwards herringbone coordinates.</param>
	/// <returns>Point in upwards herringbone coordinates.</returns>
	///
	/// Takes a point in downwards herringbone coordinates and returns its
	/// upwards herringbone position.
	public Vector3 HerringDToHerringU(Vector3 herring) {
		// This could be done directly without going to cubic
		return CubicToHerringU(HerringDToCubic(herring));
	}

	/// <summary>Returns the rhombic coordinates of a point in downwards herringbone coordinates.</summary>
	/// <param name="herring">Point in downwards herringbone coordinates.</param>
	/// <returns>Point in rhombic coordinates.</returns>
	///
	/// Takes a point in downwards herringbone coordinates and returns its
	/// rhombic position.
	public Vector3 HerringDToRhombic(Vector3 herring) {
		return CubicToRhombic(HerringDToCubic(herring));
	}

	/// <summary>Returns the downwards rhombic coordinates of a point in downwards herringbone coordinates.</summary>
	/// <param name="herring">Point in downwards herringbone coordinates.</param>
	/// <returns>Point in downwards rhombic coordinates.</returns>
	///
	/// Takes a point in downwards herringbone coordinates and returns its
	/// downwards rhombic position.
	public Vector3 HerringDToRhombicD(Vector3 herring) {
		return CubicToRhombicD(HerringDToCubic(herring));
	}

	/// <summary>Returns the cubic coordinates of a point in downwards herringbone coordinates.</summary>
	/// <param name="herring">Point in downwards herringbone coordinates.</param>
	/// <returns>Point in cubic coordinates.</returns>
	///
	/// Takes a point in downwards herringbone coordinates and returns its
	/// cubic position.
	public Vector4 HerringDToCubic(Vector3 herring) {
		return HerringToCubic(herring, false);
	}

	/// <summary>Returns the barycentric coordinates of a point in downwards herringbone coordinates.</summary>
	/// <param name="herring">Point in downwards herringbone coordinates.</param>
	/// <returns>Point in barycentric coordinates.</returns>
	///
	/// Takes a point in downwards herringbone coordinates and returns its
	/// barycentric position.
	private Vector4 HerringDToBarycentric(Vector3 herring) {
		return CubicToBarycentric(HerringDToCubic(herring));
	}

	#endregion

	#region Rhombic Up
	/// <summary>Returns the world coordinates of a point in rhombic coordinates.</summary>
	/// <param name="rhombic">Point in rhombic coordinates.</param>
	/// <returns>Point in world coordinates.</returns>
	///
	/// Takes a point in rhombic coordinates and returns its world position.
	public Vector3 RhombicToWorld(Vector3 rhombic) {
		Vector3 world = ruwMatrix.MultiplyPoint3x4(rhombic);
		return world;
	}

	/// <summary>Returns the upwards herringbone coordinates of a point in rhombic coordinates.</summary>
	/// <param name="rhombic">Point in rhombic coordinates.</param>
	/// <returns>Point in upwards herring coordinates.</returns>
	///
	/// Takes a point in rhombic coordinates and returns its upwards herring
	/// position.
	public Vector3 RhombicToHerringU(Vector3 rhombic) {
		return CubicToHerringU(RhombicToCubic(rhombic));
	}

	/// <summary>Returns the downwards rhombic coordinates of a point in rhombic coordinates.</summary>
	/// <param name="rhombic">Point in rhombic coordinates.</param>
	/// <returns>Point in downwards rhombic coordinates.</returns>
	///
	/// Takes a point in rhombic coordinates and returns its downwards rhombic
	/// position.
	public Vector3 RhombicToRhombicD(Vector3 rhombic) {
		return CubicToRhombicD(RhombicToCubic(rhombic));
	}

	/// <summary>Returns the downwards herringbone coordinates of a point in
	/// rhombic coordinates.</summary> <param name="rhombic">Point in rhombic
	/// coordinates.</param> <returns>Point in downwards herring
	/// coordinates.</returns>
	///
	/// Takes a point in rhombic coordinates and returns its downwards herring
	/// position.
	public Vector3 RhombicToHerringD(Vector3 rhombic) {
		return CubicToHerringD(RhombicToCubic(rhombic));
	}

	/// <summary>Returns the cubic coordinates of a point in rhombic coordinates.</summary>
	/// <param name="rhombic">Point in rhombic coordinates.</param>
	/// <returns>Point in cubic coordinates.</returns>
	///
	/// Takes a point in rhombic coordinates and returns its cubic position.
	public Vector4 RhombicToCubic(Vector3 rhombic) {
		bool pointy = hexSideMode == HexOrientation.PointySides;
		float r_x = rhombic[idx[0]], r_y = rhombic[idx[1]];

		float x = pointy ?   r_x        :  r_x + r_y;
		float y = pointy ?   r_y        : -r_x      ;
		float z = pointy ? -(r_x + r_y) : -r_y      ;
		return new Vector4(x, y, z, rhombic.z);
	}

	/// <summary>Returns the barycentric coordinates of a point in rhombic coordinates.</summary>
	/// <param name="rhombic">Point in rhombic coordinates.</param>
	/// <returns>Point in barycentric coordinates.</returns>
	///
	/// Takes a point in rhombic coordinates and returns its barycentric
	/// position.
	private Vector4 RhombicToBarycentric(Vector3 rhombic) {
		return CubicToBarycentric(RhombicToCubic(rhombic));
	}
	#endregion

	#region Rhombic Down
	/// <summary>Returns the world coordinates of a point in downwards rhombic coordinates.</summary>
	/// <param name="rhombic">Point in downwards rhombic coordinates.</param>
	/// <returns>Point in world coordinates.</returns>
	///
	/// Takes a point in downwards rhombic coordinates and returns its world
	/// position.
	public Vector3 RhombicDToWorld(Vector3 rhombic) {
		Vector3 world = rdwMatrix.MultiplyPoint3x4(rhombic);
		return  world;
	}

	/// <summary>Returns the upwards herringbone coordinates of a point in downwards rhombic coordinates.</summary>
	/// <param name="rhombic">Point in downwards rhombic coordinates.</param>
	/// <returns>Point in upwards herring coordinates.</returns>
	///
	/// Takes a point in downwards rhombic coordinates and returns its upwards
	/// herring position.
	public Vector3 RhombicDToHerringU(Vector3 rhombic) {
		return CubicToHerringU(RhombicDToCubic(rhombic));
	}

	/// <summary>Returns the downwards herringbone coordinates of a point in downwards rhombic coordinates.</summary>
	/// <param name="rhombic">Point in downwards rhombic coordinates.</param>
	/// <returns>Point in downwards herring coordinates.</returns>
	///
	/// Takes a point in downwards rhombic coordinates and returns its
	/// downwards herring position.
	public Vector3 RhombicDToHerringD(Vector3 rhombic) {
		return CubicToHerringD(RhombicDToCubic(rhombic));
	}

	/// <summary>Returns the downwards herringbone coordinates of a point in downwards rhombic coordinates.</summary>
	/// <param name="rhombic">Point in downwards rhombic coordinates.</param>
	/// <returns>Point in downwards herring coordinates.</returns>
	///
	/// Takes a point in downwards rhombic coordinates and returns its
	/// downwards herring position.
	public Vector3 RhombicDToRhombic(Vector3 rhombic) {
		return CubicToRhombic(RhombicDToCubic(rhombic));
	}

	/// <summary>Returns the cubic coordinates of a point in downwards rhombic coordinates.</summary>
	/// <param name="rhombic">Point in downwards rhombic coordinates.</param>
	/// <returns>Point in cubic coordinates.</returns>
	///
	/// Takes a point in downwards rhombic coordinates and returns its cubic
	/// position.
	public Vector4 RhombicDToCubic(Vector3 rhombic) {
		bool pointy = hexSideMode == HexOrientation.PointySides;
		float r_x = rhombic[idx[0]], r_y = rhombic[idx[1]];

		float c_x = pointy ?  r_x       :  r_x      ;
		float c_y = pointy ? -r_x + r_y : -r_x - r_y;
		float c_z = pointy ? -r_y       :  r_y      ;
		return new Vector4(c_x, c_y, c_z, rhombic.z);
	}

	/// <summary>Returns the barycentric coordinates of a point in downwards rhombic coordinates.</summary>
	/// <param name="rhombic">Point in downwards rhombic coordinates.</param>
	/// <returns>Point in barycentric coordinates.</returns>
	///
	/// Takes a point in downwards rhombic coordinates and returns its
	/// barycentric position.
	private Vector4 RhombicDToBarycentric(Vector3 rhombic) {
		return CubicToBarycentric(RhombicDToCubic(rhombic));
	}
	#endregion

	#region Cubic
	/*
	 *     \_____/
	 *     /\   /\         The X-axis goes from the western vertex through the eastern vertex
	 *    /  \ /  \        The Y-axis goes from the south-eastern vertex through the north-western vertex
	 * --(----X----)-->    The Z-axis goes from the north-eastern vertex through the south-western vertex
	 *    \  / \  /
	 *     \/___\/         The X-axis goes from the western vertex through the eastern vertex
	 *     /     \
	 *
	 *     |
	 *    /|\
	 * \ / | \ /    The X-axis goes from the south-western vertex through the north-eastern vertex
	 *  X  |  X     The Y-axis goes from the south-eastern vertex through the north-western vertex
	 *  |\ | /|     The Z-axis goes from the northern vertex through the southern vertex
	 *  | \|/ |
	 *  |  X  |     It's the same as above, except rotated 30° counter-clockwise
	 *  | /|\ |
	 *  |/ | \|
	 *  X  |  X
	 * / \ | / \
	 *    \|/
	 *     |
	 *     V
	 */


	/// <summary>Returns the world coordinates of a point in cubic coordinates.</summary>
	/// <param name="cubic">Point in cubic coordinates.</param>
	/// <returns>Point in world coordinates.</returns>
	/// Takes a point in cubic coordinates and returns its world position.
	public Vector3 CubicToWorld(Vector4 cubic) {
		Vector3 local; // first local space
		if (hexSideMode == HexOrientation.PointySides) {
			local =  cubic.x * cubicColumnBasis + cubic.y * cubicRowBasis + cubic.w * depth * Units[idxS[2]];
		} else {
			local = -cubic.y * cubicColumnBasis - cubic.z * cubicRowBasis + cubic.w * depth * Units[idxS[2]];
		}
		//Debug.Log (local);
		return lwMatrix.MultiplyPoint3x4(local);
	}

	/// <summary>Returns the upwards herring coordinates of a point in cubic coordinates.</summary>
	/// <param name="cubic">Point in cubic coordinates.</param>
	/// <returns>Point in upwards herring coordinates.</returns>
	/// Takes a point in cubic coordinates and returns its upwards herring
	/// position.
	public Vector3 CubicToHerringU(Vector4 cubic) {
		return CubicToHerring(cubic, true);
	}

	/// <summary>Returns the downwards herring coordinates of a point in cubic coordinates.</summary>
	/// <param name="cubic">Point in cubic coordinates.</param>
	/// <returns>Point in downwards herring coordinates.</returns>
	/// Takes a point in cubic coordinates and returns its downwards herring
	/// position.
	private Vector3 CubicToHerringD(Vector4 cubic) {
		return CubicToHerring(cubic, false);
	}

	/// <summary>Converts cubic- to any herring coordinates.</summary>
	/// <param name="cubic">Cubic coordinates.</param>
	/// <param name="up">True for upwards herring, false for downwards.</param>
	/// Takes a point in cubic coordinates and returns its herring position,
	/// either up- or downwards. Use this method only internally and expost the
	/// up- and downwards case individually.
	private Vector3 CubicToHerring(Vector4 cubic, bool up) {
		// the left (or lower) border
		int index = hexSideMode == HexOrientation.PointySides ? Mathf.FloorToInt(cubic.x) : -Mathf.CeilToInt(cubic.z);
		float column, row; // column and row
		if (hexSideMode == HexOrientation.PointySides) {
			column = cubic.x;
			if (up) {
				row = (index & 1) == 0 ? cubic.y + index / 2 : -cubic.z - (index + 1) / 2; // row
			} else {
				row = (index & 1) == 0 ? -cubic.z - index / 2 : cubic.y + (index + 1) / 2; // row
			}
		} else {
			row = -cubic.z;
			if (up) {
				column = (index & 1) == 0 ? -cubic.y + index / 2 : cubic.x - (index + 1) / 2;
			} else {
				column = (index & 1) == 0 ? cubic.x - index / 2 : -cubic.y + (index + 1) / 2;
			}
		}
		return column * Units[idx[0]] + row * Units[idx[1]] + cubic.w * Units[idx[2]];
	}

	/// <summary>Returns the rhombic coordinates of a point in cubic coordinates.</summary>
	/// <param name="cubic">Point in cubic coordinates.</param>
	/// <returns>Point in rhombic coordinates.</returns>
	///
	/// Takes a point in cubic coordinates and returns its rhombic position.
	public Vector3 CubicToRhombic(Vector4 cubic) {
		float x = hexSideMode == HexOrientation.PointySides ? cubic.x : -cubic.y;
		float y = hexSideMode == HexOrientation.PointySides ? cubic.y : -cubic.z;
		return x * Units[idx[0]] + y * Units[idx[1]] + cubic.w * Units[idx[2]];
	}

	/// <summary>Returns the downwards rhombic coordinates of a point in cubic coordinates.</summary>
	/// <param name="cubic">Point in cubic coordinates.</param>
	/// <returns>Point in downwards rhombic coordinates.</returns>
	///
	/// Takes a point in cubic coordinates and returns its rhombic position.
	public Vector3 CubicToRhombicD(Vector4 cubic) {
		float x = hexSideMode == HexOrientation.PointySides ?  cubic.x :  cubic.x;
		float y = hexSideMode == HexOrientation.PointySides ? -cubic.z :  cubic.z;
		return x * Units[idx[0]] + y * Units[idx[1]] + cubic.w * Units[idx[2]];
	}

	/// <summary>Returns the barycentric coordinates of a point in cubic coordinates.</summary>
	/// <param name="cubic">Point in cubic coordinates.</param>
	/// <returns>Point in world coordinates.</returns>
	///
	/// Takes a point in cubic coordinates and returns its barycentric position.
	private Vector4 CubicToBarycentric(Vector4 cubic) {
		return new Vector4(cubic.x, cubic.y, cubic.z + 1.0f, cubic.w);
	}
	#endregion

	#region Barycentric
	/// <summary>Returns the world coordinates of a point in barycentric coordinates.</summary>
	/// <param name="barycentric">Point in barycentric coordinates.</param>
	/// <returns>Point in world coordinates.</returns>
	///
	/// Takes a point in barycentric coordinates and returns its world position.
	private Vector3 BarycentricToWorld(Vector4 barycentric) {
		return CubicToWorld(BarycentricToCubic(barycentric));
	}

	/// <summary>Returns the upwards herring coordinates of a point in barycentric coordinates.</summary>
	/// <param name="barycentric">Point in barycentric coordinates.</param>
	/// <returns>Point in upwards herring coordinates.</returns>
	///
	/// Takes a point in barycentric coordinates and returns its upwards
	/// herring position.
	private Vector3 BarycentricToHerringU(Vector4 barycentric) {
		return CubicToHerringU(BarycentricToCubic(barycentric));
	}

	/// <summary>Returns the downwards herring coordinates of a point in barycentric coordinates.</summary>
	/// <param name="barycentric">Point in barycentric coordinates.</param>
	/// <returns>Point in downwards herring coordinates.</returns>
	///
	/// Takes a point in barycentric coordinates and returns its downwards
	/// herring position.
	private Vector3 BarycentricToHerringD(Vector4 barycentric) {
		return CubicToHerringD(BarycentricToCubic(barycentric));
	}

	/// <summary>Returns the rhombic coordinates of a point in barycentric coordinates.</summary>
	/// <param name="barycentric">Point in barycentric coordinates.</param>
	/// <returns>Point in rhombic coordinates.</returns>
	///
	/// Takes a point in barycentric coordinates and returns its rhombic position.
	private Vector3 BarycentricToRhombic(Vector4 barycentric) {
		return CubicToRhombic(BarycentricToCubic(barycentric));
	}

	/// <summary>Returns the cubic coordinates of a point in barycentric coordinates.</summary>
	/// <param name="barycentric">Point in barycentric coordinates.</param>
	/// <returns>Point in cubic coordinates.</returns>
	///
	/// Takes a point in barycentric coordinates and returns its cubic position.
	private Vector4 BarycentricToCubic(Vector4 barycentric) {
		return new Vector4(barycentric.x, barycentric.y, barycentric.z - 1.0f, barycentric.w);
	}
	#endregion

	#region Helper
	/// <summary>Converts any herring- to cubic coordinates.</summary>
	/// <param name="herring">Herring coordinates.</param>
	/// <param name="up">True for upwards herring, false for downwards.</param>
	/// <returns>Cubic coordinates.</returns>
	private Vector4 HerringToCubic(Vector3 herring, bool up) {
		float x, y, z;
		int index;

		// odd or even (idxS[0] means X-axis for pointy sides and Y-axis for
		// flat sides)
		index = Mathf.FloorToInt(herring[idxS[0]]);
		if (hexSideMode == HexOrientation.PointySides) {
			x = herring[idx[0]];
			if (up) {
				if ((index & 1) == 0) { // even
					y = herring[idx[1]] - index / 2;
					z = -(x + y);
				} else { // odd
					z = -herring[idx[1]] - (index + 1) / 2;
					y = -(x + z);
				}
			} else {
				if ((index & 1) == 0) { // even
					z = -herring[idx[1]] - index / 2;
					y = -(x + z);
				} else { // odd
					y = herring[idx[1]] - (index + 1) / 2;
					z = -(x + y);
				}
			}
		} else {
			z = -herring[idx[1]];
			if (up) {
				if ((index & 1) == 0) { // even
					y = -herring[idx[0]] + index / 2;
					x = -(z + y);
				} else { // odd
					x = herring[idx[0]] + (index + 1) / 2;
					y = -(z + x);
				}
			} else {
				if ((index & 1) == 0) { // even
					x = herring[idx[0]] + index / 2;
					y = -(z + x);
				} else { // odd
					y = -herring[idx[0]] + (index + 1) / 2;
					x = -(z + y);
				}
			}
		}
		return new Vector4(x, y, z, herring[idx[2]]);
	}
	#endregion
	#endregion

	#region Nearest
	#region World
	/// <summary>Returns the world coordinates of the nearest vertex.</summary>
	/// <param name="world">Point in world space.</param>
	/// <param name="doDebug">If set to @c true draw a sphere at the destination.</param>
	/// <returns>World position of the nearest vertex.</returns>
	/// Returns the world position of the nearest vertex from a given point in
	/// world space. If <c>doDebug</c> is set a small gizmo sphere will be
	/// drawn at that position.
	public override Vector3 NearestVertexW(Vector3 world, bool doDebug = false) { // documentation taken from parent class
		Vector3 vertex = CubicToWorld(NearestVertexC(world));

		if (doDebug) {
			Gizmos.DrawSphere(vertex, 0.3f);
		}
		return vertex;
	}

	/// <summary>Returns the world coordinates of the nearest face.</summary>
	/// <param name="world">Point in world space.</param>
	/// <param name="doDebug">If set to @c true draw a sphere at the destination.</param>
	/// <returns>World position of the nearest vertex.</returns>
	///
	/// Returns the world position of the nearest vertex from a given point in
	/// world space.  If <c>doDebug</c> is set a small gizmo sphere will be
	/// drawn at that position.
	public override Vector3 NearestFaceW(Vector3 world, bool doDebug) {
		Vector3 face = CubicToWorld(NearestFaceC(world));
		if (doDebug) {
			Gizmos.DrawSphere(face, height / 5);
		}
		return face;
	}

	/// <summary>Returns the world coordinates of the nearest box.</summary>
	/// <param name="fromPoint">Point in world space.</param>
	/// <param name="doDebug">If set to @c true draw a sphere at the destination.</param>
	/// <returns>World position of the nearest box.</returns>
	///
	/// Returns the world position of the nearest box from a given point in
	/// world space.  Since the box is enclosed by several vertices, the
	/// returned value is the point in between all of the vertices.  If
	/// <c>doDebug</c> is set a gizmo sphere will be drawn at that position.
	public override Vector3 NearestBoxW(Vector3 fromPoint, bool doDebug) {
		Vector3 box = CubicToWorld(NearestBoxC(fromPoint));
		if (doDebug) {
			Gizmos.DrawSphere(box, height / 2);
		}
		return box;
	}
	#endregion

	#region Grid
	/// <summary>Returns the grid position of the nearest vertex.</summary>
	/// <param name="world">Point in world space.</param>
	/// <returns>Grid position of the nearest vertex.</returns>
	///
	/// This is just a shortcut for <c>#NearestVertexHO</c>.
	public override Vector3 NearestVertexG(Vector3 world) {
		return NearestVertexHO(world);
	}

	/// <summary>Returns the grid position of the nearest face.</summary>
	/// <param name="world">Point in world space.</param>
	/// <returns>Grid position of the nearest face.</returns>
	///
	/// This is just a shortcut for <c>#NearestFaceHO</c>.
	public override Vector3 NearestFaceG(Vector3 world) {
		return NearestFaceHO(world);
	}

	/// <summary>Returns the grid position of the nearest box.</summary>
	/// <param name="world">Point in world space.</param>
	/// <returns>Grid position of the nearest box.</returns>
	///
	/// This is just a shortcut for <c>#NearestBoxHO</c>.
	public override Vector3 NearestBoxG(Vector3 world) {
		return NearestBoxHO(world);
	}
	#endregion

	#region Herring Odd
	/// <summary>Returns the upwards herring position of the nearest vertex.</summary>
	/// <param name="world">Point in world space.</param>
	/// <returns>Grid position of the nearest vertex.</returns>
	///
	/// This method takes in a point in world space and returns the upwards
	/// herring coordinates of the nearest vertex.
	public Vector3 NearestVertexHO(Vector3 world) {
		return CubicToHerringU(NearestVertexC(world));
	}

	/// <summary>Returns the upwards herring position of the nearest face.</summary>
	/// <param name="world">Point in world space.</param>
	/// <returns>Grid position of the nearest face.</returns>
	///
	/// This method takes in a point in world space and returns the upwards
	/// herring coordinates of the nearest face.
	public Vector3 NearestFaceHO(Vector3 world) {
		return CubicToHerringU(NearestFaceC(world));
	}

	/// <summary>Returns the grid position of the nearest box.</summary>
	/// <param name="world">Point in world space.</param>
	/// <returns>Grid position of the nearest box.</returns>
	///
	/// Returns the world position of the nearest box from a given point in
	/// upwards herring coordinates.
	public Vector3 NearestBoxHO(Vector3 world) {
		return CubicToHerringU(NearestBoxC(world));
	}
	#endregion

	#region Rhombic
	/// <summary>Returns the rhombic position of the nearest vertex.</summary>
	/// <param name="world">Point in world space.</param>
	/// <returns>Rhombic position of the nearest vertex.</returns>
	///
	/// This method takes in a point in world space and returns the rhombic
	/// coordinates of the nearest vertex.
	public Vector3 NearestVertexR(Vector3 world) {
		return CubicToRhombic(NearestVertexC(world));
	}

	/// <summary>Returns the rhombic position of the nearest face.</summary>
	/// <param name="world">Point in world space.</param>
	/// <returns>Rhombic position of the nearest face.</returns>
	///
	/// This method takes in a point in world space and returns the rhombic
	/// coordinates of the nearest face.
	public Vector3 NearestFaceR(Vector3 world) {
		return CubicToRhombic(NearestFaceC(world));
	}

	/// <summary>Returns the rhombic position of the nearest box.</summary>
	/// <param name="world">Point in world space.</param>
	/// <returns>Rhombic position of the nearest box.</returns>
	///
	/// Returns the rhombic position of the nearest box from a given point in
	/// upwards herring coordinates.
	public Vector3 NearestBoxR(Vector3 world) {
		return CubicToRhombic(NearestBoxC(world));
	}
	#endregion

	#region Cubic
	// HOW TO FIND VERTICES: Vertices can be found relative to their closest
 	// face. Assume the face has cubic coordinates (x, y, z) with x+y+z=0, then
 	// the vertex has coordinates (x + a/3, y + b/3, z + c/3) where either a, b
 	// or c = ±2 and the other two are ±(-1). The larger of the three always
 	// gravitates towards its direction, i.e. a=2 for E and
	// -2 for E, b=2 for NW and b=-2 for SE, c=2 for SW and c=-2 for NE.
	//
	// To find vertices reverse the strategy. First find the face, then the abs
	// of the largest of the coordinates (relative to the face of course),
	// that's where the vertex is gravitating towards. Then just add the
	// appropriate values (±2/3 and ±1/3) to the face coordinates.

	/// <summary>Returns the cubic position of the nearest vertex.</summary>
	/// <param name="world">Point in world space.</param>
	/// <returns>Cubic position of the nearest vertex.</returns>
	///
	/// This method takes in a point in world space and returns the cubic
	/// coordinates of the nearest vertex.
	public Vector4 NearestVertexC(Vector3 world) {
		Vector4 cubic = WorldToCubic(world); // cubic coordinates of the world point
		Vector4 face = NearestFaceC(world); // the face closes to the point
		Vector4 vertex = cubic - face; // point's cubic coordinates *relative* to the face

		int max = 0; // we will now look for the largest coordinate (using abs value) to decide between X, Y and Z
		for (int i = 0; i < 3; i++) {
			if (Mathf.Abs(vertex[i]) > Mathf.Abs(vertex[max])) { // no need for %1.0f because the values are already relative and <= 1
				max = i;
			}
		}
		//Debug.Log (max + ": "+ vertex[max] + " from " + cubic);
		int sign = vertex[max] % 1.0f >= 0 ? 1 : -1; // next we need to decide if the vertex is in the positive or negative direction
		vertex[max] = sign * 2.0f / 3.0f; // assign the vertex coordinates
		vertex[(max + 1) % 3] = -sign * 1.0f / 3.0f; // the other values are 1/3 and have the opposite sign
		vertex[(max + 2) % 3] = -sign * 1.0f / 3.0f; // using (max+i)%3 is a handy way to wrap around through 1, 2 and 3
		return vertex + face; // return the face coordinates plus the vertex offset
	}

	/// <summary>Returns the cubic position of the nearest face.</summary>
	/// <param name="world">Point in world space.</param>
	/// <returns>Cubic position of the nearest face.</returns>
	///
	/// This method takes in a point in world space and returns the cubic
	/// coordinates of the nearest face.
	public Vector4 NearestFaceC(Vector3 world) {
		Vector4 cubic = WorldToCubic(world);
		return RoundCubic(cubic);
	}

	/// <summary>Returns the cubic position of the nearest box.</summary>
	/// <param name="world">Point in world space.</param>
	/// <returns>Cubic position of the nearest box.</returns>
	///
	/// Returns the cubic position of the nearest box from a given point in
	/// upwards herring coordinates.
	public Vector4 NearestBoxC(Vector3 world) {
		Vector4 cubic = WorldToCubic(world); // first to cubic space
		Vector4 rounded = RoundCubic(cubic); // then the face
		rounded.w = Mathf.Floor(cubic.w) + 0.5f; // now correct the height
		return rounded;
	}
	#endregion

	#region Barycentric
	/// <summary>Returns the barycentric position of the nearest vertex.</summary>
	/// <param name="world">Point in world space.</param>
	/// <returns>Barycentric position of the nearest vertex.</returns>
	///
	/// This method takes in a point in world space and returns the barycentric
	/// coordinates of the nearest vertex.
	private Vector4 NearestVertexB(Vector3 world) {
		return CubicToBarycentric(NearestVertexC(world));
	}

	/// <summary>Returns the barycentric position of the nearest face.</summary>
	/// <param name="world">Point in world space.</param>
	/// <returns>Barycentric position of the nearest face.</returns>
	///
	/// This method takes in a point in world space and returns the barycentric
	/// coordinates of the nearest face.
	private Vector4 NearestFaceB(Vector3 world) {
		return CubicToBarycentric(NearestFaceC(world));
	}

	/// <summary>Returns the barycentric position of the nearest box.</summary>
	/// <param name="world">Point in world space.</param>
	/// <returns>Barycentric position of the nearest box.</returns>
	///
	/// Returns the barycentric position of the nearest box from a given point
	/// in upwards herring coordinates.
	private Vector4 NearestBoxB(Vector3 world) {
		return CubicToBarycentric(NearestBoxC(world));
	}
	#endregion
	#endregion

	#region Align Scale Methods
	/// <summary>Fits a position vector into the grid.</summary>
	/// <param name="pos">The position to align.</param>
	/// <param name="scale">A simulated scale to decide how exactly to fit the position into the grid.</param>
	/// <param name="lockAxis">Which axes should be ignored.</param>
	/// <returns>The <c>Vector3</c>.</returns>
	///
	/// Aligns a position vector to the grid by positioning it on the centre of
	/// the nearest face. Please refer to the user manual for more information.
	/// The parameter <c>lockAxis</c> makes the function not touch the corresponding
	/// coordinate.
	public override Vector3 AlignVector3(Vector3 pos, Vector3 scale, BoolVector3 lockAxis) {
		var newPos = NearestFaceW(pos);
		for (int i = 0; i < 3; i++) {
			if (lockAxis[i]) {
				newPos[i] = pos[i];
			}
		}
		return newPos;
	}

	/// <summary>Scales a size vector to fit inside a grid.</summary>
	/// <returns>The re-scaled vector.</returns>
	/// <param name="scale">The vector to scale.</param>
	/// <param name="lockAxis">The axes to ignore.</param>
	///
	/// This method takes in a vector representing a size and scales it to the
	/// nearest multiple of the grid’s radius and depth.  The <c>lockAxis</c>
	/// parameter lets you ignore individual axes.
	public override Vector3 ScaleVector3(Vector3 scale, BoolVector3 lockAxis) {
		var spacing = new Vector3();
		for (int i = 0; i < 2; i++) {
			spacing[idxS[i]] = height;
		}
		spacing[idxS[2]] = depth;
		var relScale = new Vector3(scale.x % spacing.x, scale.y % spacing.y, scale.z % spacing.z);
		var newScale = new Vector3();

		for (int i = 0; i <= 2; i++) {
			newScale[i] = scale[i];
			if (relScale[i] >= 0.5f * spacing[i]) {
				//Debug.Log ("Grow by " + (spacing.x - relScale.x));
				newScale[i] = newScale[i] - relScale[i] + spacing[i];
			} else {
				//Debug.Log ("Shrink by " + relativeScale.x);
				newScale[i] = newScale[i] - relScale[i];
				//if we went too far default to the spacing
				if (newScale[i] < spacing[i]) {
					newScale[i] = spacing[i];
				}
			}
		}

		for (int i = 0; i < 3; i++) {
			if (lockAxis[i]) {
				newScale[i] = scale[i];
			}
		}

		return newScale;
	}

	#endregion

	#region Draw Points
	#region Draw Points Helpers
	/// <summary>
	///   Converts a cardinal direction from the centre of a hex to a
	///   corresponding world direction.
	/// </summary>
	/// <param name="direction">Cardinal direction.</param>
	/// <param name="origin">World position of the grid's origin (actual position and origin offset).</param>
	/// This method is intended for internal use when calculating the herring
	/// position of a vertex to draw.
	private Vector3 cardinalToDirection(HexDirection direction, Vector3 origin) {
		Vector3 result = Vector3.zero;
		switch (direction) {
			case HexDirection.E  : result[idxS[0]] =  2f/3f; result[idxS[1]] = -1f/3f; break;
			case HexDirection.NE : result[idxS[0]] =  1f/3f; result[idxS[1]] =  1f/3f; break;
			case HexDirection.NW : result[idxS[0]] = -1f/3f; result[idxS[1]] =  1f/3f; break;
			case HexDirection.W  : result[idxS[0]] = -2f/3f; result[idxS[1]] = -1f/3f; break;
			case HexDirection.SW : result[idxS[0]] = -1f/3f; result[idxS[1]] = -2f/3f; break;
			case HexDirection.SE : result[idxS[0]] =  1f/3f; result[idxS[1]] = -2f/3f; break;
		}
		return HerringUToWorld(result) - origin;
	}


	/// <summary>Contributes a line to a given line set.</summary>
	/// <param name="lineSet">Line set.</param>
	/// <param name="hex">Centre of the hex relative to which the line will be contributed.</param>
	/// <param name="point1">Starting point of the line, added to the hex.</param>
	/// <param name="point2">End point of the line, added to the hex.</param>
	/// <param name="iterator">Iterator for the line set.</param>
	/// This method is only intended for internal use to contribute lines to the
	/// draw points.
	private int contributeLine(Vector3[][] lineSet, Vector3 hex, Vector3 point1, Vector3 point2, int iterator) {
		lineSet[iterator][0] = hex + point1;
		lineSet[iterator][1] = hex + point2;
		return ++iterator;
	}

	/// <summary>Contributes a layer line to a given line set.</summary>
	/// <param name="lineSet">Line set.</param>
	/// <param name="hex">Centre of the hex relative to which the line will be contributed.</param>
	/// <param name="vertex">Vertex, added to the hex.</param>
	/// <param name="back">Backwards pointing vector, added to the vertex.</param>
	/// <param name="front">Forward pointing vector, added to the vertex.</param>
	/// <param name="iterator">Iterator for the line set.</param>
	/// This method is only intended for internal use to contribute lines to the
	/// draw points. Similar to its other overload, but this one takes only one
	/// point but two layers. It's intended for layer lines.
	private int contributeLine(Vector3[][] lineSet, Vector3 hex, Vector3 vertex, Vector3 back, Vector3 front, int iterator) {
		lineSet[iterator][0] = hex + vertex + back;
		lineSet[iterator][1] = hex + vertex + front;
		return ++iterator;
	}
	#endregion

	#region Draw Points Count
	protected override void DrawPointsCount(ref int countX, ref int countY, ref int countZ, ref Vector3 from, ref Vector3 to, bool condition = true) {
		if (!condition)
			return;
		switch (gridStyle) {
		case HexGridShape.Rectangle:
		case HexGridShape.RectangleDown:
			drawPointsCountRect(ref countX, ref countY, ref countZ, ref from, ref to);
			break;
		case HexGridShape.CompactRectangle:
			drawPointsCountCompactRect(ref countX, ref countY, ref countZ, ref from, ref to);
			break;
		case HexGridShape.Rhombus:
		case HexGridShape.RhombusDown:
			drawPointsCountRhomb(ref countX, ref countY, ref countZ, ref from, ref to);
			break;
		case HexGridShape.HerringboneUp:
		case HexGridShape.HerringboneDown:
			DrawPointsCountHerring(ref countX, ref countY, ref countZ, ref from, ref to);
			break;
		case HexGridShape.Circle:
			DrawPointsCountCircle(ref countX, ref countY, ref countZ, ref from, ref to);
			break;
		}
	}

	/// @internal<summary>Counts the amount of draw points for rectangular style drawings.</summary>
	private void drawPointsCountRect(ref int countX, ref int countY, ref int countZ, ref Vector3 from, ref Vector3 to, bool compact = false) {
		if (!relativeSize) { // Convert to rhombic coordinates for easier calculation of amounts.
			from[idxS[0]] /= s; from[idxS[1]] /= h; from[idxS[2]] /= d;
			  to[idxS[0]] /= s;   to[idxS[1]] /= h;   to[idxS[2]] /= d;
		}
		// First we need to count the amount of hexes. Starting int the lower
		// left corner each hex always adds one horizontal and two vertical
		// lines: S, SW, NW. On top of this there are edge cases:
		//    _   _
		//  _/ \_/ \_  (1) Every top hex needs a N line.
		// / \_/ \_/ \ (2) Every right hex needs a NE and SE line.
		// \_/ \_/ \_/ (3) Every even bottom hex needs a SE line, unless it is
		// / \_/ \_/ \     in the rightmost column, in which case see point 2.
		// \_/ \_/ \_/ (4) Every odd top hex needs a NE line, unless it is in
		// / \_/ \_/ \     the rightmost column, in which case see point 2.
		// \_/ \_/ \_/
		//
		// Since the even bottom hexes and odd top hexes add one one diagonal
		// line each we can simply add one line total per column, then subtract
		// the one line that satisfies two conditions (point 2 and either point
		// 3 or point 4). Since only one such line can exist we always subtract
		// one.
		//
		// As for the straight lines, each column always adds one 1 line, so
		// nothing special there.
		//
		// Downwards grids have the same number of lines, but slightly
		// different rules:
		//  _   _   _
		// / \_/ \_/ \  (a) Every top hex needs a N line.
		// \_/ \_/ \_/  (b) Every right hex needs a NE and SE line.
		// / \_/ \_/ \  (c) Every even top hex needs a NE line, unless it is in
		// \_/ \_/ \_/      the rightmost column, in which case see point (b).
		// / \_/ \_/ \  (d) Every odd bottom hex needs a SE line, unless it is in
		// \_/ \_/ \_/      the rightmost column, in which case see point (b).
		//   \_/ \_/
		//
		// These are exactly the same rules, except the roles of (3) and (4)
		// are swapped.
		//  _   _   _
		// / \_/ \_/ \  Clipped rectangles are the same both up- and downwards. We'll
		// \_/ \_/ \_/  use an upwards pattern and then remove certain lines from it.
		// / \_/ \_/ \
		// \_/ \_/ \_/
		// / \_/ \_/ \
		// \_/ \_/ \_/

		int hexesH = Mathf.FloorToInt(to[idx[0]]) - Mathf.CeilToInt(from[idx[0]]) + 1; // columns
		int hexesV = Mathf.FloorToInt(to[idx[1]]) - Mathf.CeilToInt(from[idx[1]]) + 1; // rows
		int layers = Mathf.FloorToInt(to[idx[2]]) - Mathf.CeilToInt(from[idx[2]]) + 1;
		//Debug.Log("Hexes: " + hexesH + ", " + hexesV + ", layers: " + layers);

		int[] count = new int[3]; // store the three counts independent of the grid's orientation (straight, slanted, layer)
		// swap the role of horizontal and vertical for flat sides
		Swap<int>(ref hexesH, ref hexesV, hexSideMode == HexOrientation.FlatSides);

		// regular cases + 1 top line per column
		count[0] = layers * (1 * hexesH * hexesV + 1 * hexesH);
		// regular cases + 2 diagonal lines per row + 1 diagonal line per
		// column - 1 line that's either even-bottom-right-SE or odd-top-right
		// NE
		count[1] = layers * (2 * hexesH * hexesV + 2 * hexesV + 1 * hexesH - 1);

		// The blue Z-lines depend on the amount of hexes as well. Each hex
		// adds two lines, a SW and a W line. The edge cases are as follows:
		//
		//  (1') Every top hex needs a NW lines           (a') Every top hex needs a NW lines
		//  (2') Every right hex needs an E line          (b') Every right hex needs an E line
		//  (3') Every even bottom hex needs a SE line    (c') Every even top hex needs a NE line
		//  (4') Every odd top hex needs a NE line        (d') Every odd bottom hex needs a SE line
		//  (5') Every right hex needs, if it is even,    (e') Every right hex needs, if it is even,
		//       a NE line, or, if it is odd, a SE line        a SE line, or, if it is odd, a NE line
		//
		// Point one adds on line per column, point two adds one line per row,
		// point three and four add together one line per column and point four
		// adds one line per row.

		// regular case + 1 NW top per column + 1 E right per row + 1 SE (even
		// bottom) or NE (odd top) per column + 1 NE (even) or SE (odd) right
		// per row
		count[2] = 2 * hexesH * hexesV + 1 * hexesH + 1 * hexesV + 1 * hexesH + 1 * hexesV;

		// If the rectangle is compact we have to subtract some lines.
		if (compact) {
			// First count the number of odd columns. There are two cases:
			// either the total number of columns is even, or it is odd. If
			// it's even then exactly half of the colums are odd. If the total
			// is odd, then half of (total - 1) columns are odd and if the
			// first column is odd add it as well, otherwise we are done
			int firstColumn = Mathf.CeilToInt(from[idxS[0]]), lastColumn = Mathf.FloorToInt(to[idxS[0]]);
			int oddColumns = hexesH % 2 == 0 ? hexesH / 2 : (hexesH - 1) / 2 + (firstColumn % 2 != 0 ? 1 : 0);
			//Debug.Log(oddColumns);
			// Every odd top north lines needs to be removed. Every odd top NE
			// and NW line needs to be removed. The first odd top SW and last
			// top SE line need to be removed.
			count[0] -= (1 * oddColumns                                                                 ) * layers;
			count[1] -= (2 * oddColumns + (firstColumn % 2 != 0 ? 1 : 0) + (lastColumn % 2 != 0 ? 1 : 0)) * layers;
			count[2] -=  2 * oddColumns + (firstColumn % 2 != 0 ? 1 : 0) + (lastColumn % 2 != 0 ? 1 : 0)          ;
		}

		// The three counts are currently ordered as follows: straight lines,
		// slanted lines and layer lines. In order to map them to their
		// respective axes we have to apply the inverse of the `idx` (pointy
		// sides) or `idxS` permutation. The inverse of `idx` is `idx` itsef,
		// but the inverse or `idxS` is `idxS(idxS)`.
		int i = hexSideMode == HexOrientation.PointySides ? idx[0] : idxS[idxS[0]];
		int j = hexSideMode == HexOrientation.PointySides ? idx[1] : idxS[idxS[1]];
		int k = hexSideMode == HexOrientation.PointySides ? idx[2] : idxS[idxS[2]];
		countX = count[i]; countY = count[j]; countZ = count[k];
		// However, the XY plane is an exception, because then `idx` is the
		// identity permutation, thus `idxS(idxS)` is the identity again. We
		// need to force-swap:
		Swap(ref countX, ref countY, gridPlane == GridPlane.XY && hexSideMode == HexOrientation.FlatSides);
		//Debug.Log(countX + ", " + countY + ", " + countZ);
	}

	private void drawPointsCountCompactRect(ref int countX, ref int countY, ref int countZ, ref Vector3 from, ref Vector3 to) {
		drawPointsCountRect(ref countX, ref countY, ref countZ, ref from, ref to, true);
	}

	private void drawPointsCountRhomb(ref int countX, ref int countY, ref int countZ, ref Vector3 from, ref Vector3 to) {
		if (!relativeSize) { // Convert to rhombic coordinates for easier calculation of amounts.
			from[idxS[0]] /= s; from[idxS[1]] /= h; from[idxS[2]] /= d;
			  to[idxS[0]] /= s;   to[idxS[1]] /= h;   to[idxS[2]] /= d;
		}
 		//      _
		//    _/ \ The rhombic points are easier to count than herring points:
		//  _/ \_/
		// / \_/ \  - The number of horizontal lines is the number of columns times (number of rows + 1).
		// \_/ \_/  - The number of angled lines is two times the number of rows times times number
		// / \_/ \    of columns+1 plus two times the number of columns-1.
		// \_/ \_/  - The number of cylindric lines is two times the number of rows times number of
		// / \_/      columns+1 plus two times the number of columns.
		// \_/

		int c = Mathf.FloorToInt(to[idxS[0]]) - Mathf.CeilToInt(from[idxS[0]]) + 1; // columns
		int r = Mathf.FloorToInt(to[idxS[1]]) - Mathf.CeilToInt(from[idxS[1]]) + 1; // rows
		int l = Mathf.FloorToInt(to[idxS[2]]) - Mathf.CeilToInt(from[idxS[2]]) + 1; // layers

		int[] count = new int[3] {c * (r+1) * l, (2 * r * (c+1) + c-1) * l, 2 * r * (c+1) + 2*c};

		int i = hexSideMode == HexOrientation.PointySides ? idx[0] : idxS[idxS[0]];
		int j = hexSideMode == HexOrientation.PointySides ? idx[1] : idxS[idxS[1]];
		int k = hexSideMode == HexOrientation.PointySides ? idx[2] : idxS[idxS[2]];
		countX = count[i]; countY = count[j]; countZ = count[k];
		// However, the XY plane is an exception, because then `idx` is the
		// identity permutation, thus `idxS(idxS)` is the identity again. We
		// need to force-swap:
		Swap(ref countX, ref countY, gridPlane == GridPlane.XY && hexSideMode == HexOrientation.FlatSides);
		//Debug.Log("X: " + countX + "; Y " + countY + "; Z " + countZ);
	}

	private void DrawPointsCountHerring(ref int countX, ref int countY, ref int countZ, ref Vector3 from, ref Vector3 to) {
		if (!relativeSize) { // Convert to herringbone coordinates for easier calculation of amounts.
			from[idxS[0]] /= s; from[idxS[1]] /= h; from[idxS[2]] /= d;
			  to[idxS[0]] /= s;   to[idxS[1]] /= h;   to[idxS[2]] /= d;
		}
		// | | | | |  | | | | |  Vertical lines are the same as for rectangular grids
		// |\|/|\|/|  |/|\|/|\|
		// | | | | |  | | | | |  Horizontal lines are same as for rectangular grids, times the
		// |\|/|\|/|  |/|\|/|\|  number of vertical strips, plus to for the outside strips
		// | | | | |  | | | | |
		// |\|/|\|/|  |/|\|/|\|  Layer lines are the same as for rectangular grids

		int x = Mathf.FloorToInt(to[idxS[0]]) - Mathf.CeilToInt(from[idxS[0]]) + 1;
		int y = Mathf.FloorToInt(to[idxS[1]]) - Mathf.CeilToInt(from[idxS[1]]) + 1;
		int z = Mathf.FloorToInt(to[idxS[2]]) - Mathf.CeilToInt(from[idxS[2]]) + 1;

		int[] count = new int[] {
			(x+1) * y * z,
			 x    * 1 * z,
			(x+1) * y    ,
		};
		//Debug.Log("Red = " + count[0] + ", green = " + count[1] + ", blue = " + count[2]);

		countX = count[hexSideMode == HexOrientation.PointySides ? idx[0] : idxS[idxS[0]]];
		countY = count[hexSideMode == HexOrientation.PointySides ? idx[1] : idxS[idxS[1]]];
		countZ = count[hexSideMode == HexOrientation.PointySides ? idx[2] : idxS[idxS[2]]];
		// However, the XY plane is an exception, because then `idx` is the
		// identity permutation, thus `idxS(idxS)` is the identity again. We
		// need to force-swap:
		Swap(ref countX, ref countY, gridPlane == GridPlane.XY && hexSideMode == HexOrientation.FlatSides);
		//Debug.Log("Red = " + countX + ", green = " + countY + ", blue = " + countZ);
	}

	private void DrawPointsCountCircle(ref int countX, ref int countY, ref int countZ, ref Vector3 from, ref Vector3 to) {
		/* Drawing and counting hexagonal cones
		 * ====================================
		 * Draw a cone-shape around a given hexagon, that hexagon is not
		 * necessarily the origin. The rendering range consist of a distance
		 * and angle. The distance is from the central hex, if it is 0 the
		 * central hex is included. The angle is between 0 and 6 and describes
		 * which cones to render.
		 *
		 * Counting the lines in a circle
		 * ------------------------------
		 * The central hex has six points and thus six lines, no exception. The
		 * radial hexes always have four points and four lines. Hexes in
		 * between have three vertices and three lines.
		 *
		 *         Distances:              Points:
		 *             _                      _
		 *           _/4\_                  _/4\_
		 *         _/4\_/4\_              _/3\_/3\_
		 *       _/4\_/3\_/4\           _/3\_/4\_/3\_
		 *     _/4\_/3\_/3\_/4\_      _/3\_/3\_/3\_/3\_
		 *    /4\_/3\ /2\_/3\_/4\    /4\_/3\ /4\_/3\_/4\
		 *    \_/3\_/2\_/2\_/3\_/    \_/4\_/3\_/3\_/4\_/
		 *    /4\_/2\_/1\_/2\_/4\    /3\_/4\_/4\_/4\_/3\
		 *    \_/3\_/1\_/1\_/3\_/    \_/3\_/4\_/4\_/3\_/
		 *    /4\_/2\_/0\_/2\_/4\    /3\_/3\_/6\_/3\_/3\
		 *    \_/3\_/1\_/1\_/3\_/    \_/3\_/4\_/4\_/3\_/
		 *    /4\_/2\_/1\_/2\_/4\    /3\_/4\_/4\_/4\_/3\
		 *    \_/3\_/2\_/2\_/3\_/    \_/4\_/3\_/3\_/4\_/
		 *    /4\_/3\_/2\_/3\_/4\    /4\_/3\_/4\_/3\_/4\
		 *    \_/4\_/3\_/3\_/4\_/    \_/3\_/3\_/3\_/3\_/
		 *      \_/4\_/3\_/4\_/        \_/3\_/4\_/3\_/
		 *        \_/4\_/4\_/            \_/3\_/3\_/
		 *          \_/4\_/                \_/4\_/
		 *            \_/                    \_/
		 *
		 * Radial hexes are hexes directly on a line through the edges of the
		 * central hex.
		 *
		 *
		 * Drawing the cone
		 * ----------------
		 * 1) To draw a cone start out with the six lines around the central hex.
		 * 2) For ever layer do:
		 *    2.1) Do for every radial hex:
		 *         2.1.1) For the radial hex draw four lines: one adjacent to
		 *                the inner hex, two zig-zag sides, one top
		 *         2.1.2) If the current radial index is the last or 6, quit
		 *         2.1.3) For (layer - 1) inner hexes do:
		 *                2.1.3.1) Draw three lines: two sides, one top.
		 *         2.1.4) If the cone is not a closed circle the last radial hex
		 *                needs a closing line
		 *
		 * Example: cone from 0 to 2
		 * -------------------------
		 * For starters let's look at a complete circle. Start out with the
		 * central hex:
		 *       _
		 *      /0\
		 *      \_/
		 *
		 * Add the four lines of the radial hex:
		 *                _          _          _          _          _
		 *         _     /1\_      _/1\_      _/1\_      _/1\_      _/1\_
		 *       _ 1\     _/1\    /1\_/1\    /1\_/1\    /1\_/1\    /1\_/1\
		 *      /0\_/ -> /0\_/ -> \ /0\_/ -> \_/0\_/ -> \_/0\_/ -> \_/0\_/
		 *      \_/      \_/        \_/      /1\_/      /1\_/      /1\_/1\
		 *                                   \_         \_/1       \_/1\_/
		 *                                                \_/        \_/
		 *
		 * Now start with the next layer, one radial hex and the inner hexes:
		 *                                 _                     _
		 *                      _        _/2\_                 _/2\_
		 *       _   _        _ 2\_     /2\_/2\_             _/2\_/2\_
		 *     _/1\_ 2\     _/1\_/2\     _/1\_/2\           /2\_/1\_/2\
		 *    /1\_/1\_/    /1\_/1\_/    /1\_/1\_/           \_/1\_/1\_/
		 *    \_/0\_/   -> \_/0\_/   -> \_/0\_/   -> ... -> /2\_/0\_/2\
		 *    /1\_/1\      /1\_/1\      /1\_/1\             \_/1\_/1\_/
		 *    \_/1\_/      \_/1\_/      \_/1\_/             /2\_/1\_/2\
		 *      \_/          \_/          \_/               \_/2\_/2\_/
		 *                                                    \_/2\_/
		 *                                                      \_/
		 *
		 * Note that the last radial hex is the same as the first. This is
		 * different from cones where there is an explicit radial hex. Let's
		 * look at the cone now:
		 *
		 *                                      _              _
		 *                                    _/2\_          _/2\_
		 *              _          _        _/2\_/2\_      _/2\_/2\_
		 *            _/1\_      _/1\_     /2\_/1\_/2\    /2\_/1\_/2\
		 *     _     /1\_/1\    /1\_/1\    \ /1\_/1\_/    \_/1\_/1\_/
		 *    /0\ -> \ /0\_/ -> \_/0\_/ ->   \_/0\_/   ->   \_/0\_/
		 *    \_/      \_/        \_/          \_/            \_/
		 *
		 * We start at the radial hex of the first radial index and end at the
		 * radial hex of the last radial index. We only draw the inner hexes
		 * with radial index *between* these two indices. Finally, we draw the
		 * closing line.
		 *
		 * If the last radial index is 6 (or more precisely ``first index +
		 * last index = 0 mod 6``) we don't draw that, radial hex, since that
		 * would be a closed circle.
		 *
		 * Drawing not from the centre
		 * ---------------------------
		 * If the first radius index is not 0 we skip over the inner-most hex
		 * and any hexes before that. However, that does leave some undrawn
		 * edges:
		 *                                 _                      _
		 *                               _/4\_                  _/4\_
		 *                             _/4\ /4\_              _/4\_/4\_
		 *             3             _/4\  3  /4\_          _/4\_/3\_/4\_
		 *           3   3         _/4\  3   3  /4\_      _/4\_/3   3\_/4\_
		 *         3   2   3      /4\  3   2   3  /4\    /4\_/3   2   3\_/4\
		 *       3   2   2   3    \_ 3   2   2   3 _/    \_/3   2   2   3\_/
		 *         2   1   2      /4   2   1   2   4\    /4\  2   1   2  /4\
		 *       3   1   1   3    \_ 3   1   1   3 _/    \_/3   1   1   3\_/
		 *         2   0   2   -> /4   2   0   2   4\ -> /4\  2   0   2  /4\
		 *       3   1   1   3    \_ 3   1   1   3 _/    \_/3   1   1   3\_/
		 *         2   1   2      /4   2   1   2   4\    /4\  2   1   2  /4\
		 *       3   2   2   3    \_ 3   2   2   3 _/    \_/3   2   2   3\_/
		 *         3   2   3      /4   3   2   3   4\    /4\_ 3   2   3 _/4\
		 *           3   3        \_/4   3   3   4\_/    \_/4\_ 3   3 _/4\_/
		 *             3            \_/4   3   4\_/        \_/4\_ 3 _/4\_/
		 *                            \_/4   4\_/            \_/4\_/4\_/
		 *                              \_/4\_/                \_/4\_/
		 *                                \_/                    \_/
		 *
		 * As we can see the radial hexes are missing one line, the line shared
		 * with the previous circle, and the inner hexes are missing two lines.
		 *
		 * Layer lines
		 * -----------
		 * Layer lines are needed where the verices are. To cover all vertices
		 * start out with the six inner vertices. Then, for every other hex add
		 * a vertex at the end point of every line, except for the second side
		 * line.
		 *
		 * If the cone is a ring we also add the end point of every closing
		 * line.
		 *
		 * For a non-closed cone we must add the start and end point of the
		 * closing line of the last hex.
		 */

		// Convert to herringbone coordinates for easier calculation of amounts.
		if (!relativeSize) {
			from[idx[0]] /= this.radius; to[idx[0]] /= this.radius;
			from[idx[2]] /= d; to[idx[2]] /= d;
		}

		// inner and outer radius; start and end angle
		int[] radius = {Mathf.RoundToInt(from[idx[0]]), Mathf.RoundToInt(to[idx[0]])};
		int[] angle  = {Mathf.RoundToInt(from[idx[1]]), Mathf.RoundToInt(to[idx[1]])};
		int   layers = Mathf.FloorToInt(to[idx[2]]) - Mathf.CeilToInt(from[idx[2]] ) + 1;
		int[] count = {0, 0, 0};

		if (!useCustomRenderRange) {
			radius[0] = 0; angle[ 0] = 0;
			angle[ 1] = Mathf.Min(angle[1], 6);
		}

		// It makes no sense for the radius to be negative
		radius[0] = Mathf.Max(0, radius[0]);
		radius[1] = Mathf.Max(0, radius[1]);

		if (angle[0] < 0 ) {
			angle[0]  = 6 + (angle[0]%6);
			if (angle[0] > angle[1]) {angle[1] += 6;}
		}

		// Formula for inner hexes per radius: radius - 1

		// We must map the lines of each angle to an axis.
		// 0 is x-axis, 1 is y-axis
		int[] index = {1, 1, 0, 1, 1, 0};

		// If the central hex is drawn add its lines, otherwise add the closing lines
		if (radius[0] == 0) {
			count[0] += 2; count[1] += 4; count[2] += 6;
			// if the radius is from 0 to 0 we are done here
			if (radius[1] == 0) {
				count[0] *= layers; count[1] *= layers;
				countX = count[hexSideMode == HexOrientation.PointySides ? idx[0] : idxS[idxS[0]]];
				countY = count[hexSideMode == HexOrientation.PointySides ? idx[1] : idxS[idxS[1]]];
				countZ = count[hexSideMode == HexOrientation.PointySides ? idx[2] : idxS[idxS[2]]];
				return;
			}
			// otherwise we move on to the first actual ring
			++radius[0];
		} else {
			// for every radial hex add one line, for inner hexes add two lines
			for (int i = angle[0]; i < angle[1]; ++i){
				// the closing line is opposite of the first side line
				count[index[(i+1)%6]] += radius[0];
				count[index[(i+2)%6]] += radius[0] - 1;
				count[2] += 2 * radius[0] - 1; // 2 * for inner, + 1 radial
			}
			// add the closing line for the last radial hex
			if ((angle[1] - angle[0]) % 6 != 0 || angle[1] == angle[0]) {
				++count[index[(angle[1]+1)%6]];
				count[2] += 2;
			}
		}

		// Now start the actual loop...

		for (int i = radius[0]; i <= radius[1]; ++i) {
			for (int j = angle[0]; j < angle[1]; ++j) {
				// connection, botton and two sides
				// the connection is opposite of the 2nd side
				for (int k = 0; k < i; ++k) {
					if (k == 0) {
						count[index[(j+2)%6]] += 1;
						++count[2];
					}
					// now the inner hexes
					count[index[(j+0)%6]] += 1;
					count[index[(j+1)%6]] += 1;
					count[index[(j+2)%6]] += 1;
					count[2] += 2;
				}
			}
			// The last radial hex, unless the cone is a circle
			if ((angle[1] - angle[0]) % 6 != 0 || angle[1] == angle[0]) {
				// close hex, line opposite of bottom
				if ((angle[1] - angle[0]) % 6 != 5 || i > 1) {
					count[index[(angle[1]+0)%6]] += 1;
				}
				count[index[(angle[1]+0)%6]] += 1;
				count[index[(angle[1]+1)%6]] += 1;
				count[index[(angle[1]+2)%6]] += 2;
				count[2] += 4;
			}
		}

		// finally multiply all this by the number of layers.
		count[0] *= layers; count[1] *= layers;
		countX = count[hexSideMode == HexOrientation.PointySides ? idx[0] : idxS[idxS[0]]];
		countY = count[hexSideMode == HexOrientation.PointySides ? idx[1] : idxS[idxS[1]]];
		countZ = count[hexSideMode == HexOrientation.PointySides ? idx[2] : idxS[idxS[2]]];
		// However, the XY plane is an exception, because then `idx` is the
		// identity permutation, thus `idxS(idxS)` is the identity again. We
		// need to force-swap:
		Swap(ref countX, ref countY, gridPlane == GridPlane.XY && hexSideMode == HexOrientation.FlatSides);
		//Debug.Log("Radius: " + radius[0] + " to " + radius[1] + ": Layers: " + layers);
		//Debug.Log("X: " + countX + "; Y: " + countY + "; Z: " + countZ);
	}
	#endregion

	#region Draw Points Calculate
	protected override void DrawPointsCalculate(Vector3[][][] points, ref int[] amount, Vector3 from, Vector3 to) {
		switch (gridStyle) {
		case HexGridShape.Rectangle        : drawPointsCalculateRectUp(     points, amount, from, to); break;
		case HexGridShape.CompactRectangle : drawPointsCalculateRectCompact(points, amount, from, to); break;
		case HexGridShape.RectangleDown    : drawPointsCalculateRectDown(   points, amount, from, to); break;
		case HexGridShape.Rhombus          : drawPointsCalculateRhombUp(    points, amount, from, to); break;
		case HexGridShape.RhombusDown      : drawPointsCalculateRhombDown(  points, amount, from, to); break;
		case HexGridShape.HerringboneUp    : DrawPointsCalculateHerringUp(  points, amount, from, to); break;
		case HexGridShape.HerringboneDown  : DrawPointsCalculateHerringDown(points, amount, from, to); break;
		case HexGridShape.Circle           : drawPointsCalculateCircle(     points, amount, from, to); break;
		}
	}

	private void drawPointsCalculateRectUp(Vector3[][][] points, int[] amount, Vector3 from, Vector3 to) {
		drawPointsCalculateRect(points, amount, from, to, false, true);
	}

	private void drawPointsCalculateRectDown(Vector3[][][] points, int[] amount, Vector3 from, Vector3 to) {
		drawPointsCalculateRect(points, amount, from, to, false, false);
	}

	private void drawPointsCalculateRectCompact(Vector3[][][] points, int[] amount, Vector3 from, Vector3 to) {
		drawPointsCalculateRect(points, amount, from, to, true, true);
	}

	private void drawPointsCalculateRhombUp(Vector3[][][] points, int[] amount, Vector3 from, Vector3 to) {
		drawPointsCalculateRhomb(points, amount, from, to, true);
	}
	private void drawPointsCalculateRhombDown(Vector3[][][] points, int[] amount, Vector3 from, Vector3 to) {
		drawPointsCalculateRhomb(points, amount, from, to, false);
	}

	private void DrawPointsCalculateHerringUp(Vector3[][][] points, int[] amount, Vector3 from, Vector3 to) {
		drawPointsCalculateHerring(points, amount, from, to, true);
	}

	private void DrawPointsCalculateHerringDown(Vector3[][][] points, int[] amount, Vector3 from, Vector3 to) {
		drawPointsCalculateHerring(points, amount, from, to, false);
	}


	private void drawPointsCalculateRect(Vector3[][][] points, int[] amount, Vector3 from, Vector3 to, bool compact, bool upwards) {
		// from and to are already in herring space
		int maxWidth  = Mathf.FloorToInt(to[idxS[0]]), minWidth  = Mathf.CeilToInt(from[idxS[0]]);
		int maxHeight = Mathf.FloorToInt(to[idxS[1]]), minHeight = Mathf.CeilToInt(from[idxS[1]]);
		int maxDepth  = Mathf.FloorToInt(to[idxS[2]]), minDepth  = Mathf.CeilToInt(from[idxS[2]]);

		// iterator variables
		int i = minWidth, j = 0, k = 0, iterator_x = 0, iterator_y = 0, iterator_z = 0;

		/* We loop in the following order: column -> row -> layer. First we
 		 * take a column, then a row and then go through the layers of that
 		 * hex. The tree layers of the loop do the following:
		 *  - inner: Every hex on every layer contributes a S, SW and W line. The rightmost hexes
		 *           contribute also a NE and SE line.
		 *
		 *  - middle: Before the first layer is set the original from layer value is used to
		 *            contribute the first points of the SW and W layer line. If the column is
		 *            rightmost the first point of the E layer line is contributed. Also, if it is an
		 *            even column a NE point is contributed, otherwise a SE point is contributed.
		 *            Then the layer is set and the inner loop starts. After exiting the loop the
		 *            variable `k` is set to the original `t` value and the above rules are applied
		 *            again to contribute the second point of each layer line.
		 *
		 *  - outer: If the column is even and not rightmost a bottom SE line is contributed.
		 *           Afterwards we contribute a complete SE bottom layer line if the column is even.
		 *           Then the middle loop starts. After exiting the loop we are in the top row, so we
		 *           contribute a N line. If the column is odd and not rightmost we contribute a top
		 *           NE line. Finally, if the column is odd we contribute a complete top NE layer line.
		 */

		/* In order to keep performance high we need to stay in world-space as
 		 * much as possible, ideally the amount of conversions from herring
 		 * space to world space should be constant. To achieve this we will
 		 * only convert the position of the very first hex from herring- to
 		 * world space and from there on add world-space direction vectors.
 		 * These direction vectors will be created from herring space as well
 		 * and prepared before the loop starts. While int he loop the
 		 * methodology will be the same as if we were using herring
 		 * coordinates, except there will be no conversions taking place.
		 *
		 * We need three hex coordinates. The first will be be column hex, it
		 * will be initialized once once entering the outer loop and
		 * incremented every time we move to another column. The row hex will
		 * be initialized before entering the middle loop from the current
		 * column hex and incremented on every row iteration. The layer hex is
		 * initialized before entering the inner loop and incremented on every
		 * layer iteration. It is the hex that's actually used for contributing
		 * lines.
		 *
		 * We need incrementation vectors: right, up and down. Right is an
		 * array, so we can use `i % 2` to pick either the even or odd value
		 * for each column.
		 *
		 * Finally we need six vertex vectors, one for each vertex possibility.
		 * There are always two variants of each vector, one for pointy sides
		 * and one for flat sides.  Since the rules are the same for both types
		 * we don't need any other adjustments. We also need two layer vectors
		 * for front and back layer lines
		 */

		Vector3 origin = lwMatrix.MultiplyPoint(Vector3.zero); // the local origin transformed to world space

		Vector3 EE = cardinalToDirection(HexDirection.E , origin), NE = cardinalToDirection(HexDirection.NE, origin);
		Vector3 NW = cardinalToDirection(HexDirection.NW, origin), WW = cardinalToDirection(HexDirection.W , origin);
		Vector3 SW = cardinalToDirection(HexDirection.SW, origin), SE = cardinalToDirection(HexDirection.SE, origin);

		Vector3   up      = h * locUnits[idxS[1]];
		Vector3   forward = d * locUnits[idxS[2]];
		Vector3[] right   = new Vector3[2] {
			s * locUnits[idxS[0]] - 0.5f*h * locUnits[idxS[1]],
			s * locUnits[idxS[0]] + 0.5f*h * locUnits[idxS[1]]
		};
		Swap<Vector3>(ref right[0], ref right[1], !upwards);

		Vector3 hexColumn, hexRow, hexLayer;

		Vector3 back  = (from[idxS[2]] - minDepth) * d * locUnits[idxS[2]];
		Vector3 front = (to  [idxS[2]] - minDepth) * d * locUnits[idxS[2]];

		hexColumn  = origin + minHeight * up + s * minWidth * locUnits[idxS[0]] + minDepth * forward;
		hexColumn += Mod(minWidth, 2) * 0.5f * up * (upwards ? 1 : -1);
		// simply `i%2` is not enough because it might evaluate to `-1`
		for (i = minWidth; i <= maxWidth; ++i, hexColumn += right[Mod(i, 2)]) {
			if ((i%2 ==  0 && upwards) || (i%2 != 0 && !upwards)) { // rule (3') & (d')
				// SE bottom layer line
				iterator_z = contributeLine(points[idxS[2]], hexColumn, SE, back, front, iterator_z);
			}

			hexRow = hexColumn;
			for (j = minHeight; j <= maxHeight; ++j, hexRow += up) {
				// SW layer line (rule (0'))
				iterator_z = contributeLine(points[idxS[2]], hexRow, SW, back, front, iterator_z);
				// W layer line (rule (0'))
				if (!compact || i != minWidth || j != maxHeight){ //fails on compact left-most top hexes
					iterator_z = contributeLine(points[idxS[2]], hexRow, WW, back, front, iterator_z);
				}
				if (i == maxWidth) {
					// E layer line (rule (2'))
					if (!compact || i != maxWidth || j != maxHeight){ // fails on compact right-most top hexes
						iterator_z = contributeLine(points[idxS[2]], hexRow, EE, back, front, iterator_z);
					}
					if ((i%2 == 0 && upwards) || (i%2 != 0 && !upwards)) { // rule (5') and (e')
						// NE layer line
						if (!compact || j != maxHeight || i % 2 == 0){
							iterator_z = contributeLine(points[idxS[2]], hexRow, NE, back, front, iterator_z);
						}
					} else {
						// SE layer line
						iterator_z = contributeLine(points[idxS[2]], hexRow, SE, back, front, iterator_z);
					}
				}

				hexLayer = hexRow;
				for (k = minDepth; k <= maxDepth; ++k, hexLayer += forward) {
					// S line
					iterator_x = contributeLine(points[idxS[0]], hexLayer, SW, SE, iterator_x);
					if (!compact || i % 2 == 0 || j != maxHeight || i != minWidth) {
						// SW line
						iterator_y = contributeLine(points[idxS[1]], hexLayer, SW, WW, iterator_y);
					}
					if (!compact || i % 2 == 0 || j != maxHeight) {
						// NW line
						iterator_y = contributeLine(points[idxS[1]], hexLayer, NW, WW, iterator_y);
					}
					if (i == maxWidth) { // rule (2)
						if (!compact || i%2 == 0 || j != maxHeight){
							// NE line
							iterator_y = contributeLine(points[idxS[1]], hexLayer, NE, EE, iterator_y);
							// SE line
							iterator_y = contributeLine(points[idxS[1]], hexLayer, SE, EE, iterator_y);
						}
					}
					if (j == minHeight) {
						if (i != maxWidth && ((i%2 == 0 && upwards) || (i%2 != 0 && !upwards))) { // rule (3) and (d)
							// SE bottom line
							iterator_y = contributeLine(points[idxS[1]], hexLayer, EE, SE, iterator_y);
						}
					}
					if (j == maxHeight) {
						if (!compact || i%2 == 0) {
							// N line (rule (1))
							iterator_x = contributeLine(points[idxS[0]], hexLayer, NW, NE, iterator_x);
						}
						if (i != maxWidth && ((i%2 != 0 && upwards) || (i%2 == 0 && !upwards))) { // rule (4) and (c)
							if (!compact) {
								// NE line
								iterator_y = contributeLine(points[idxS[1]], hexLayer, NE, EE, iterator_y);
							}
						}
					}
				}
			}
			--j;
			hexRow -= up;
			// NW layer line (rule (1'))
			if (!compact || j != maxHeight || i % 2 == 0){
				iterator_z = contributeLine(points[idxS[2]], hexRow, NW, back, front, iterator_z);
			}

			if ((i%2 !=  0 && upwards) || (i%2 == 0 && !upwards)) { //rule (4') and (d')
				// NE top layer line
				if (!compact) {
					iterator_z = contributeLine(points[idxS[2]], hexRow, NE, back, front, iterator_z);
				}
			}
		}
	}

	private void drawPointsCalculateRhomb(Vector3[][][] points, int[] amount, Vector3 from, Vector3 to, bool upwards) {
		// from and to are already in rhombic space
		int maxWidth  = Mathf.FloorToInt(to[idxS[0]]), minWidth  = Mathf.CeilToInt(from[idxS[0]]);
		int maxHeight = Mathf.FloorToInt(to[idxS[1]]), minHeight = Mathf.CeilToInt(from[idxS[1]]);
		int maxDepth  = Mathf.FloorToInt(to[idxS[2]]), minDepth  = Mathf.CeilToInt(from[idxS[2]]);

		Vector3 origin  = lwMatrix.MultiplyPoint(Vector3.zero); // the local origin transformed to world space

		Vector3 EE = cardinalToDirection(HexDirection.E , origin), NE = cardinalToDirection(HexDirection.NE, origin);
		Vector3 NW = cardinalToDirection(HexDirection.NW, origin), WW = cardinalToDirection(HexDirection.W , origin);
		Vector3 SW = cardinalToDirection(HexDirection.SW, origin), SE = cardinalToDirection(HexDirection.SE, origin);

		Vector3 up      = h * locUnits[idxS[1]];
		Vector3 right   = s * locUnits[idxS[0]] + 0.5f*h * locUnits[idxS[1]] * (upwards ? 1.0f : -1.0f);
		Vector3 forward = d * locUnits[idxS[2]];

		Vector3 back  = (from[idxS[2]] - minDepth) * d * locUnits[idxS[2]];
		Vector3 front = (to  [idxS[2]] - minDepth) * d * locUnits[idxS[2]];

		Vector3 hexColumn, hexRow, hexLayer;
		hexLayer = origin + minWidth * right + minHeight * up + minDepth * forward;

		// iterator variables
		int iterator_x = 0, iterator_y = 0, iterator_z = 0;

		hexColumn = origin + minWidth * right + minHeight * up + minDepth * forward;
		for (int i = minWidth; i <= maxWidth; ++i, hexColumn += right) {
			hexRow = hexColumn;
			for (int j = minHeight; j <= maxHeight; ++j, hexRow += up) {

				iterator_z = contributeLine(points[idxS[2]], hexRow, SW, back, front, iterator_z);
				iterator_z = contributeLine(points[idxS[2]], hexRow, WW, back, front, iterator_z);
				if (j == maxHeight) {
					iterator_z = contributeLine(points[idxS[2]], hexRow, NW, back, front, iterator_z);
				}
				if (i != maxWidth && j == minHeight) {
					iterator_z = contributeLine(points[idxS[2]], hexRow, SE, back, front, iterator_z);
				}
				if (i == maxWidth) {
					iterator_z = contributeLine(points[idxS[2]], hexRow, SE, back, front, iterator_z);
					iterator_z = contributeLine(points[idxS[2]], hexRow, EE, back, front, iterator_z);
				}
				if (i == maxWidth && j == maxHeight) {
					iterator_z = contributeLine(points[idxS[2]], hexRow, NE, back, front, iterator_z);
				}

				hexLayer = hexRow;
				for (int k = minDepth; k <= maxDepth; ++k, hexLayer += forward) {
					if (upwards && i != maxWidth && j == minHeight) {
						iterator_y = contributeLine(points[idxS[1]], hexLayer, SE, EE, iterator_y);
					} else if (!upwards && i != maxWidth && j == maxHeight) {
						iterator_y = contributeLine(points[idxS[1]], hexLayer, NE, EE, iterator_y);
					}

					iterator_x = contributeLine(points[idxS[0]], hexLayer, SW, SE, iterator_x);
					iterator_y = contributeLine(points[idxS[1]], hexLayer, NW, WW, iterator_y);
					iterator_y = contributeLine(points[idxS[1]], hexLayer, WW, SW, iterator_y);

					if (i == maxWidth) {
						iterator_y = contributeLine(points[idxS[1]], hexLayer, SE, EE, iterator_y);
						iterator_y = contributeLine(points[idxS[1]], hexLayer, EE, NE, iterator_y);
					}

					if (j == maxHeight) {
						hexRow -= up;
						iterator_x = contributeLine(points[idxS[0]], hexLayer, NE, NW, iterator_x);
					}
				}
			}
		}
	}

	private void drawPointsCalculateHerring(Vector3[][][] points, int[] amount, Vector3 from, Vector3 to, bool upwards) {
		int maxWidth  = Mathf.FloorToInt(to[idxS[0]]), minWidth  = Mathf.CeilToInt(from[idxS[0]]);
		int maxHeight = Mathf.FloorToInt(to[idxS[1]]), minHeight = Mathf.CeilToInt(from[idxS[1]]);
		int maxDepth  = Mathf.FloorToInt(to[idxS[2]]), minDepth  = Mathf.CeilToInt(from[idxS[2]]);

		Vector3   up      = h * locUnits[idxS[1]];
		Vector3   forward = d * locUnits[idxS[2]];
		Vector3[] right   = new Vector3[3] {
			s * locUnits[idxS[0]] + 0.5f * h * locUnits[idxS[1]],
			s * locUnits[idxS[0]] - 0.5f * h * locUnits[idxS[1]],
			s * locUnits[idxS[0]]                               ,
		};
		Swap<Vector3>(ref right[0], ref right[1], !upwards);

		Vector3 origin  = lwMatrix.MultiplyPoint(Vector3.zero); // the local origin transformed to world space

		int[] iterator = new int[]{0, 0, 0};

		for (int i = minWidth; i <= maxWidth; ++i) {
			int u = i % 2 == 0 ? 0 : 1; // index to use for the `right` array

			for (int j = minHeight; j <= maxHeight; ++j) {
				for (int k = minDepth; k <= maxDepth; ++k) {
					// draw the straight lines here if j == minHeight
					if (j == minHeight) {
						points[idxS[1]][iterator[idxS[1]]][0]  = origin + i*right[2] + k*forward + from[idxS[1]]*up;
						points[idxS[1]][iterator[idxS[1]]][0] += (u == 1 ? 1 : 0) * 0.5f*up * (upwards ? 1 : -1);
						points[idxS[1]][iterator[idxS[1]]][1]  = points[idxS[1]][iterator[idxS[1]]][0];
						points[idxS[1]][iterator[idxS[1]]][1] += (to[idxS[1]]-from[idxS[1]])*up;
						++iterator[idxS[1]];
					}
					// draw the zig-zag lines
					if (i != maxWidth) {
						points[idxS[0]][iterator[idxS[0]]][0]  = origin + i*right[2] + j*up + k*forward;
						points[idxS[0]][iterator[idxS[0]]][0] += (u == 1 ? 1 : 0) * 0.5f*up * (upwards ? 1 : -1);
						points[idxS[0]][iterator[idxS[0]]][1]  = points[idxS[0]][iterator[idxS[0]]][0] + right[u];
						++iterator[idxS[0]];
					}
					if (i == minWidth) { // Leftmost zig-zag lines
						float factor = (minWidth - from[idxS[0]]);
						points[idxS[0]][iterator[idxS[0]]][0]  = origin + i*right[2] + j*up + k*forward;
						points[idxS[0]][iterator[idxS[0]]][0] += (u == 1 ? 1 : 0) * 0.5f*up * (upwards ? 1 : -1);
						points[idxS[0]][iterator[idxS[0]]][1]  = points[idxS[0]][iterator[idxS[0]]][0] - right[(u+1)%2] * factor;
						++iterator[idxS[0]];
					}
					if (i == maxWidth) { // Rightmost zig-zag lines
						float factor = (to[idxS[0]] - maxWidth);
						points[idxS[0]][iterator[idxS[0]]][0]  = origin + i*right[2] + j*up + k*forward;
						points[idxS[0]][iterator[idxS[0]]][0] += (u == 1 ? 1 : 0) * 0.5f*up * (upwards ? 1 : -1);
						points[idxS[0]][iterator[idxS[0]]][1]  = points[idxS[0]][iterator[idxS[0]]][0] + right[u] * factor;
						++iterator[idxS[0]];
					}

					//draw layer line
					if (k == minDepth) {
						points[idxS[2]][iterator[idxS[2]]][0]  = origin + i*right[2] + j*up + from[idxS[2]]*forward;
						points[idxS[2]][iterator[idxS[2]]][0] += (u == 1 ? 1 : 0) * 0.5f*up * (upwards ? 1 : -1);
						points[idxS[2]][iterator[idxS[2]]][1]  = points[idxS[2]][iterator[idxS[2]]][0];
						points[idxS[2]][iterator[idxS[2]]][1] += (to[idxS[2]]-from[idxS[2]])*forward;
						++iterator[idxS[2]];
					}
				}
			}
		}
	}

	private void drawPointsCalculateCircle(Vector3[][][] points, int[] amount, Vector3 from, Vector3 to) {
		// NOTE: The computations work for flat sides the same as for pointy
		// sides, except every direction vector is rotated 30 degrees clockwise
		// (330 degrees counter-clockwise)

		///<summary>The local origin transformed to world space.</summary>
		Vector3 origin;

		int maxDepth, minDepth;

		int iterator_x = 0, iterator_y = 0, iterator_z = 0;

		///<summary>Current hex.</summary>
		Vector3 cHex;

		///<summary>Vectors from hex to vertices (world space).</summary>
		Vector3[] verts;
		///<summary>Vectors from origin to radial hexes (world space).</summary>
		Vector3[] radials;
		///<summary>Vectors from one layer to the next (world space).</summary>
		Vector3   forward = depth * locUnits[idxS[2]];

		///<summary>Maps cardinal direction to direction origin hex to hex.</summary>
		Func<int, Vector3> cardinalToHex;
		///<summary>Maps cardinal direction to direction hex to vertex.</summary>
		Func<int, Vector3> cardinalToVert;
		///<summary>Contributes a hex line.</summary>
		Func<int, Vector3, int, int, int> contribute;
		/// <summary>Contributes a layer line.</summary>
		Action<Vector3, int, int> contributeL;

		///<summary>Contribute connecting line of hex at angle.</summary>
		Action<Vector3, int> connecting;
		///<summary>Contribute bottom line of hex at angle.</summary>
		Action<Vector3, int> bottom;
		///<summary>Contribute both side lines of hex at angle.</summary>
		Action<Vector3, int> sides;
		///<summary>Contribute closing line of hex at angle.</summary>
		Action<Vector3, int> closing;
		///<summary>Contribute inner line of hex at angle.</summary>
		Action<Vector3, int> inner;



		origin = lwMatrix.MultiplyPoint(Vector3.zero);
		maxDepth = Mathf.FloorToInt(to[idxS[2]]); minDepth  = Mathf.CeilToInt(from[idxS[2]]);

		cardinalToHex = (d) => {
			Vector3 result = Vector3.zero;
			switch (d) {
				case 0: result[idx[0]] =  1f; result[idx[1]] =  0f; break;
				case 1: result[idx[0]] =  0f; result[idx[1]] =  1f; break;
				case 2: result[idx[0]] = -1f; result[idx[1]] =  0f; break;
				case 3: result[idx[0]] = -1f; result[idx[1]] = -1f; break;
				case 4: result[idx[0]] =  0f; result[idx[1]] = -1f; break;
				case 5: result[idx[0]] =  1f; result[idx[1]] = -1f; break;
			}
			// The conversion has to be applied in pointy side mode
			if (hexSideMode == HexOrientation.FlatSides) {
				_hexSideMode = HexOrientation.PointySides;
				result = HerringUToWorld(result) - origin;
				_hexSideMode = HexOrientation.FlatSides;
				return result;
			}
			return HerringUToWorld(result) - origin;
		};

		cardinalToVert = (d) => {
			Vector3 result = Vector3.zero;
			switch (d) {
				case 0 : result[idx[0]] =  2f/3f; result[idx[1]] = -1f/3f; break;
				case 1 : result[idx[0]] =  1f/3f; result[idx[1]] =  1f/3f; break;
				case 2 : result[idx[0]] = -1f/3f; result[idx[1]] =  1f/3f; break;
				case 3 : result[idx[0]] = -2f/3f; result[idx[1]] = -1f/3f; break;
				case 4 : result[idx[0]] = -1f/3f; result[idx[1]] = -2f/3f; break;
				case 5 : result[idx[0]] =  1f/3f; result[idx[1]] = -2f/3f; break;
			}
			// The conversion has to be applied in pointy side mode
			if (hexSideMode == HexOrientation.FlatSides) {
				_hexSideMode = HexOrientation.PointySides;
				result = HerringUToWorld(result) - origin;
				_hexSideMode = HexOrientation.FlatSides;
				return result;
			}
			return HerringUToWorld(result) - origin;
		};

		verts = new Vector3[] {
			cardinalToVert(0), // [-]    2_____1
			cardinalToVert(1), // [/]    /     \
			cardinalToVert(2), // [\]   /       \
			cardinalToVert(3), // [-]  3    #    0
			cardinalToVert(4), // [/]   \       /
			cardinalToVert(5), // [\]    \_____/
			                   //        4     5
		};

		radials = new Vector3[] {
			cardinalToHex(0), // [/]     _____
			cardinalToHex(1), // [|]    /  1  \
			cardinalToHex(2), // [\]   2       0
			cardinalToHex(3), // [/]  (    #    )
			cardinalToHex(4), // [|]   3       5
			cardinalToHex(5), // [\]    \__4__/
		};
		// The directions from a radial hex to an adjacent inner
		// hex are the same, except shifted by 2 indices.

		// If the sides are flat rotate every direction 330 degrees
		if (hexSideMode == HexOrientation.FlatSides) {
			Vector3 around = (gridPlane == GridPlane.XY ? 1 : -1) * locUnits[idx[2]];
			Matrix4x4 mat = Matrix4x4.TRS(Vector3.zero, Quaternion.AngleAxis(330f, around), Vector3.one);
			for (int i = 0; i < 6; ++i) {
				verts[i] = mat.MultiplyVector(verts[i]);
				radials[i] = mat.MultiplyVector(radials[i]);
			}
		}

		contribute = (lineSet, hex, i, iterator) => {
			points[idxS[lineSet]][iterator][0] = hex + verts[(i + 0) % 6];
			points[idxS[lineSet]][iterator][1] = hex + verts[(i + 1) % 6];
			return ++iterator;
		};

		contributeL = (hex, a, indx) => {
			points[idxS[2]][iterator_z][0] = hex + verts[(indx+a)%6] + from[idxS[2]] * forward;
			points[idxS[2]][iterator_z][1] = hex + verts[(indx+a)%6] +   to[idxS[2]] * forward;
			++iterator_z;
		};

		sides = (hex, a) => {
			a %= 6;
			int ix1 = (a == 1 || a == 4) ? 0 : 1;
			int ix2 = (a == 0 || a == 3) ? 0 : 1;
			if (ix1 == 0) {
				iterator_x = contribute(ix1, hex, a + 0, iterator_x);
			} else {
				iterator_y = contribute(ix1, hex, a + 0, iterator_y);
			}
			if (ix2 == 0) {
				iterator_x = contribute(ix2, hex, a + 1, iterator_x);
			} else {
				iterator_y = contribute(ix2, hex, a + 1, iterator_y);
			}
		};

		closing = (hex, a) => {
			a %= 6;
			int ix = (a == 2 || a == 5) ? 0 : 1;
			if (ix == 0) {
				iterator_x = contribute(ix, hex, a + 2, iterator_x);
			} else {
				iterator_y = contribute(ix, hex, a + 2, iterator_y);
			}
		};

		inner = (hex, a) => {
			a %= 6;
			int ix = (a == 1 || a == 4) ? 0 : 1;
			if (ix == 0) {
				iterator_x = contribute(ix, hex, a + 3, iterator_x);
			} else {
				iterator_y = contribute(ix, hex, a + 3, iterator_y);
			}
		};

		connecting = (hex, a) => {
			a %= 6;
			int ix = (a == 0 || a == 3) ? 0 : 1;
			if (ix == 0) {
				iterator_x = contribute(ix, hex, a + 4, iterator_x);
			} else {
				iterator_y = contribute(ix, hex, a + 4, iterator_y);
			}
		};

		bottom = (hex, a) => {
			a %= 6;
			int ix = (a == 2 || a == 5) ? 0 : 1;
			if (ix == 0) {
				iterator_x = contribute(ix, hex, a + 5, iterator_x);
			} else {
				iterator_y = contribute(ix, hex, a + 5, iterator_y);
			}
		};

		// Now move the origin over to the renderAround
		origin = GridToWorld(new Vector3(renderAround[0], renderAround[1], renderAround[2]));
		cHex   = origin + minDepth * forward;

		int[] radius = {Mathf.RoundToInt(from[idx[0]]), Mathf.RoundToInt(to[idx[0]])}; // inner and outer radius
		int[] angle  = {Mathf.RoundToInt(from[idx[1]]), Mathf.RoundToInt(to[idx[1]])}; // start and end angle

		if (!useCustomRenderRange) {
			radius[0] = 0; angle[ 0] = 0;
			angle[ 1] = Mathf.Min(angle[1], 6);
		}

		// It makes no sense for the radius to be negative
		radius[0] = Mathf.Max(0, radius[0]);
		radius[1] = Mathf.Max(0, radius[1]);

		if (angle[0] < 0 ) {
			angle[0]  = 6 + (angle[0] % 6);
			if (angle[0] > angle[1]) {angle[1] += 6;}
		}

		if (radius[0] == 0) {
			for (int i = 0; i < 6; ++i) {contributeL(origin, 0, i);}
			for (int i = minDepth; i <= maxDepth; ++i) {
				iterator_y = contribute(1, cHex, 0, iterator_y);
				iterator_x = contribute(0, cHex, 1, iterator_x);
				iterator_y = contribute(1, cHex, 2, iterator_y);
				iterator_y = contribute(1, cHex, 3, iterator_y);
				iterator_x = contribute(0, cHex, 4, iterator_x);
				iterator_y = contribute(1, cHex, 5, iterator_y);
				cHex += forward;
			}

			// if the radius is from 0 to 0 we are done here
			if (radius[1] == 0) {
				/*Debug.Log(
					"Delta X:" + (amount[idxS[0]]-iterator_x) +
					"Delta Y:" + (amount[idxS[1]]-iterator_y) +
					"Delta Z:" + (amount[idxS[2]]-iterator_z)
				);*/
				return;
			}
			// otherwise we move on to the first actual ring
			++radius[0];
		} else {
			// for every radial hex add one line, for inner hexes add two lines
			for (int i = angle[0]; i < angle[1]; ++i) {
				cHex = origin + radius[0] * radials[i%6];
				// one inner line for the radial hex, opposite of side 1
				cHex += minDepth * forward;
				for (int j = 0; j < radius[0]; ++j) {
					// two inner lines for the inner hex
					for (int k = minDepth; k <= maxDepth; ++k) {
						// every hex gets an inner line
						// every inner hex a connecting line
						if (j != 0) {connecting(cHex, i);}
						inner(cHex, i);
						cHex += forward;
					}
					cHex -= (maxDepth+1) * forward; // back to the origin layer
					if (j != 0) {contributeL(cHex, i, 5);}
					contributeL(cHex, i, 4);
					cHex += minDepth * forward; // back forward
					cHex += radials[(i+2)%6];
				}
			}
			// add the last radial hex
			if ((angle[1] - angle[0]) % 6 != 0 || angle[1] == angle[0]) {
				cHex = origin + radius[0] * radials[angle[1]%6];
				cHex += minDepth * forward;
				for (int j = minDepth; j <= maxDepth; ++j) {
					inner(cHex, angle[1]);
					cHex += forward;
				}
				cHex -= (maxDepth+1) * forward;
				contributeL(cHex, angle[1], 3);
				contributeL(cHex, angle[1], 4);
			}
		}

		// Now start the actual loop...

		for (int i = radius[0]; i <= radius[1]; ++i) {
			for (int j = angle[0]; j < angle[1]; ++j) {
				// connection, bottom and two sides
				cHex = origin + i * radials[j%6];
				// the connection is opposite of the 2nd side
				// now the inner hexes
				cHex += minDepth * forward;
				for (int k = 0; k < i; ++k) {
					for (int l = minDepth; l <= maxDepth; ++l) {
						if (k == 0) {connecting(cHex, j);}
						bottom(cHex, j);
						sides( cHex, j);
						cHex += forward;
					}
					// now the inner hexes
					cHex -= (maxDepth+1) * forward; // back to origin layer
					if (k == 0) {contributeL(cHex, j, 5);}
					contributeL(cHex, j, 0);
					contributeL(cHex, j, 1);
					cHex += minDepth * forward; // back forward

					cHex += radials[(j+2)%6];
				}
			}
			// The last radial hex, unless the cone is a circle
			if ((angle[1] - angle[0]) % 6 != 0 || angle[1] == angle[0]) {
				cHex = origin + i * radials[angle[1]%6];
				cHex += minDepth * forward;
				for (int j = minDepth; j <= maxDepth; ++j) {
					connecting(cHex, angle[1]);
					bottom( cHex, angle[1]);
					sides(  cHex, angle[1]);
					// If we don't do this a line will be drawn twice
					if ((angle[1] - angle[0]) % 6 != 5 || i > 1) {
						closing(cHex, angle[1]);
					}
					cHex += forward;
				}
					cHex -= (maxDepth+1) * forward; // back to origin layer
					contributeL(cHex, angle[1], 0);
					contributeL(cHex, angle[1], 1);
					contributeL(cHex, angle[1], 2);
					contributeL(cHex, angle[1], 5);
			}
		}
		/*Debug.Log(
			"Delta X:" + (amount[idxS[0]]-iterator_x) +
			"Delta Y:" + (amount[idxS[1]]-iterator_y) +
			"Delta Z:" + (amount[idxS[2]]-iterator_z)
		);*/
	}
	#endregion
	#endregion

	#region helper functions
	/// <summary>Transforms from quasi-axis to real-axis and swaps if needed.</summary>
	/// <returns>The real-indices from quasi-indices.</returns>
	/// <param name="plane">Plane.</param>
	///
	/// Similar to the base class, except these ones swap quasi-X and quasi-Y
	/// when hexes have flat sides.
	private int[] TransformIndicesS(GridPlane plane) {
		int[] indices = TransformIndices(plane);
		Swap<int>(ref indices[0], ref indices[1], hexSideMode == HexOrientation.FlatSides);
		return indices;
	}

	/// <summary>Get-accessor for the result of TransformIndicesS.</summary>
	/// <value>The quasi-indices transformed to real indices and swapped.</value>
	private int[] idxS { get { return TransformIndicesS(gridPlane); } }

	/// <summary>rounds cubic coordinates to the nearest face.</summary>
	/// <returns>Cubic coordinates rounded to nearest face.</returns>
	/// <param name="cubic">Point in cubic coordinates.</param>
	protected Vector4 RoundCubic(Vector4 cubic) {
		float x, y, z;
		Vector4 rounded;

		//first round all components
		rounded = new Vector4(Mathf.Round(cubic.x), Mathf.Round(cubic.y), Mathf.Round(cubic.z), Mathf.Round(cubic.w));
		x = Mathf.Abs(cubic.x - rounded.x);
		y = Mathf.Abs(cubic.y - rounded.y);
		z = Mathf.Abs(cubic.z - rounded.z);

		if (x > Mathf.Max(y, z)) {
			rounded.x = -(rounded.y + rounded.z);
		} else if (y > Mathf.Max(x, z)) {
			rounded.y = -(rounded.x + rounded.z);
		} else {
			rounded.z = -(rounded.x + rounded.y);
		}

		return rounded;
	}
	#endregion

	#region Legacy
	[System.Obsolete("Deprecated, use `WorldToHerringU` instead")]
	public Vector3 WorldToHerringOdd(Vector3 world) {
		return WorldToHerringU(world);
	}
	[System.Obsolete("Deprecated, use `HerringUToWorld` instead")]
	public Vector3 HerringOddToWorld(Vector3 herring) {
		return HerringUToWorld(herring);
	}
	[System.Obsolete("Deprecated, use `HerringUToRhombic` instead")]
	public Vector3 HerringOddToRhombic(Vector3 herring) {
		return HerringUToRhombic(herring);
	}
	[System.Obsolete("Deprecated, use `HerringUToCubic` instead")]
	public Vector4 HerringOddToCubic(Vector3 herring) {
		return HerringUToCubic(herring);
	}
	[System.Obsolete("Deprecated, use `CubicToHerringU` instead")]
	public Vector3 CubicToHerringOdd(Vector4 cubic) {
		return CubicToHerringU(cubic);
	}
	[System.Obsolete("Deprecated, use `RhombicToHerringU` instead")]
	public Vector3 RhombicToHerringOdd(Vector3 rhombic) {
		return WorldToHerringU(rhombic);
	}
	#endregion
}
