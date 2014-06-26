using UnityEngine;
using System.Collections;

/// <summary>
/// 	A grid consting of flat hexagonal grids stacked on top of each other
/// </summary>
/// <remarks>
/// 	A regular hexagonal grid that forms a honeycomb pattern.
/// 	It is characterized by the <c>radius</c> (distance from the centre of a hexagon to one of its vertices) and the <c>depth</c> (distance between two honeycomb layers).
/// 	Hex grids use a herringbone pattern for their coordinate system, please refer to the user manual for information about how that coordinate system works.
/// </remarks>
public class GFHexGrid : GFLayeredGrid {

	#region Enums
	/// <summary>orientation of hexes, pointy or flat sides.</summary>
	/// 
	/// There are two ways a hexagon can be rotated: <c>PointySides</c> has flat tops, parallel to the grid's X-axis,
	/// and <c>FlatSides</c> has pointy tops, parallel to the grid's Y-axis.
	public enum HexOrientation {PointySides, FlatSides};
	/// <summary>orientation of hexes, flat or pointy tops.</summary>
	/// 
	/// There are two ways a hexagon can be rotated: <c>FlatTops</c> has flat tops, parallel to the grid's X-axis,
	/// and <c>PointyTops</c> has pointy tops, parallel to the grid's Y-axis. This enum is best used to complement <c>HexOrientation</c>.
	public enum HexTopOrientation {FlatTops, PointyTops};

	/// <summary>Different coordinate systems for hexagonal grids.</summary>
	/// 
	/// This is an enumeration of all the currently supported coordinate systems for hexagonal grids.
	/// Cubic and barycentric coordinates are four-dimensional, the rest are three-dimensional.
	private enum HexCoordinateSystem {HerringOdd, Rhombic, Cubic, Barycentric};
	//protected enum HexCoordinateSystem {HerringOdd, HerringEven, Rhombic, Cubic, Barycentric};

	///<summary>Shape of the drawing and rendering.</summary>
	/// 
	/// Different shapes of hexagonal grids: <c>Rectangle</c> looks like a rectangle with every odd-numbered column offset,
	/// <c>CompactRectangle</c> is similar with the odd-numbered colums one hex shorter.
	public enum HexGridShape {Rectangle, CompactRectangle}
	//public enum HexGridShape {Rectangle, CompactRectangle, Rhombus, BigHex, Triangle}

	/// <summary>Cardinal direction of a vertex.</summary>
	/// 
	/// The cardinal position of a vertex relative to the centre of a given hex.
	/// Note that using N and S for pointy sides, as well as E and W for flat sides does not make sense, but it is still possible.
	public enum HexDirection { N, NE, E, SE, S, SW, W, NW };
	#endregion

	#region class members
	[SerializeField]
	private float _radius = 1.0f;
	/// <summary>Distance from the centre of a hex to a vertex.</summary>
	/// <value>
	/// 	This refers to the distance between the centre of a hexagon and one of its vertices.
	/// 	Since the hexagon is regular all vertices have the same distance from the centre.
	/// 	In other words, imagine a circumscribed circle around the hexagon, its radius is the radius of the hexagon.
	/// 	The value may not be less than 0.1 (please contact me if you really need lower values).
	/// </value>
	public float radius{
		get { return _radius; }
		set {
			if (value == _radius)// needed because the editor fires the setter even if this wasn't changed
				return;
			_radius = Mathf.Max(value, 0.1f);
			hasChanged = true;
		}
	}
	
	#region helper values (read only)
	#region Agnostic Helper Values
	// These helper values are orientation-agnostic, meaning they are normal for pointy  sides, and flipped 90° for flat sides (keeps formulas simple)
	private float h {
		get{
			return Mathf.Sqrt (3.0f) * radius;
		}
	}
	private float w {
		get{
			return 2.0f * radius;
		}
	}
	#endregion
	//use these helper values to keep the formulae simple (everything depends on radius)

	/// <summary>1.5 times the radius.</summary>
	/// <value>Shorthand writing for <c>1.5f * #radius</c> (read-only).</value>
	public float side {
		get {
			return 1.5f * radius;
		}
	}
	/// <summary>Full width of the hex.</summary>
	/// This is the full vertical height of a hex.
	/// For pointy side hexes this is the distance from one edge to its opposite (<c>sqrt(3) * radius</c>) and for flat side hexes it is the distance between two opposite vertixes (<c>2 * radius</c>).
	public float height {
		get {
			return hexSideMode == HexOrientation.PointySides ? h : w;
		}
	}
	/// <summary>Distance between vertices on opposite sides.</summary>
	/// This is the full horizontal width of a hex.
	/// For pointy side hexes this is the distance from one vertex to its opposite (<c>2 * radius</c>) and for flat side hexes it is the distance between to opposite edges (<c>sqrt(3) * radius</c>).
	public float width {
		get {
			return hexSideMode == HexOrientation.PointySides ? w : h;
		}
	}

	#region Basis Vectors
	/// <summary>Cubic X-basis vector (column)</summary>
	/// <value>Gets the column basis.</value>
	/// 
	/// Each basis-vector is orthogonal to its opposite cubic axis
	private Vector3 cubicColumnBasis { // the X-basis vector
		get {
			if (hexSideMode == HexOrientation.PointySides) { // right and 30° upwards
				return side * units [idxS [0]] + 0.5f * h * units [idxS [1]];
			} else { // straight right
				return h * units [idxS [1]];
			}
		}
	}

	/// <summary>Cubic Y-basis vector (row)</summary>
	/// <value>Gets the row basis.</value>
	/// 
	/// Each basis-vector is orthogonal to its opposite cubic axis
	private Vector3 cubicRowBasis { // the Y -basis vector
		get {
			if (hexSideMode == HexOrientation.PointySides) { // straight up
				return h * units [idxS [1]];
			} else { // right and 60° upwards
				return side * units [idxS [0]] + 0.5f * h * units [idxS [1]];
			}
		}
	}
	#endregion
	#region Tangens
	// tangens of 30 and 60, used in WoldToCubic
	private static readonly float tan30 = Mathf.Tan( (1.0f / 6.0f) * Mathf.PI );
	private static readonly float tan60 = Mathf.Tan( (2.0f / 6.0f) * Mathf.PI );
	#endregion
	#endregion

	[SerializeField]
	protected HexOrientation _hexSideMode;
	/// <summary>Pointy sides or flat sides.</summary>
	/// <value>
	/// 	Whether the grid has pointy sides or flat sides. This affects both the drawing and the calculations.
	/// </value>
	public HexOrientation hexSideMode {
		get {
			return _hexSideMode;
		} set {
			if (value == _hexSideMode) {
				return;
			}
			_hexSideMode = value;
			hasChanged = true;
		}
	}
	/// <summary>Flat tops or pointy tops.</summary>
	/// Whether the grid has flat tops or pointy tops.
	/// This is directly connected to <c>::hexSideMode</c>, in fact this is just an accessor that gets and sets the appropriate value for it.
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
	protected static HexGridShape _gridStyle = HexGridShape.Rectangle;
	/// <summary>The shape of the overall grid, affects only drawing and rendering, not the calculations.</summary>
	/// <value>The shape when drawing or rendering the grid. This only affects the grid’s appearance, but not how it works.</value>
	public HexGridShape gridStyle {get{return _gridStyle;}set{if(value == _gridStyle){return;} _gridStyle = value; hasChanged = true;}}
	
	#endregion

	#region Coordinate Conversion
	// each of the following regions contains the conversions from that system into the others

	/// <summary>Converts world coordinates to grid coordinates.</summary>
	/// <param name="worldPoint">Point in world space.</param>
	/// <returns>Grid coordinates of the world point (odd herringbone coordinate system).</returns>
	/// 
	/// This is the same as calling <c>#WorldToHerringOdd</c>, because HerringOdd is the default grid coordinate system.
	public override Vector3 WorldToGrid (Vector3 worldPoint) {
		return WorldToHerringOdd (worldPoint);
	}

	/// <summary>Converts grid coordinates to world coordinates</summary>
	/// <param name="gridPoint">Point in grid space (odd herringbone coordinate system).</param>
	/// <returns>World coordinates of the grid point.</returns>
	/// 
	/// This is the same as calling <c>#HerringOddToWorld</c>, because HerringOdd is the default grid coordinate system.
	public override Vector3 GridToWorld (Vector3 gridPoint)	{
		return HerringOddToWorld (gridPoint);
	}
	#region World
	/// <summary>Returns the odd herringbone coordinates of a point in world space.</summary>
	/// <param name="world">Point in world coordinates.</param>
	/// <returns>Point in odd herringbone coordinates.</returns>
	/// 
	/// This method takes a point in world space and returns the corresponding odd herringbone coordinates.
	/// Every odd numbered column is offset upwards, giving this coordinate system the herringbone pattern.
	/// This means that the Y coordinate directly depends on the X coordinate.
	/// The Z coordinaate is simply which layer of the grid is on, relative to the grid's central layer.
	public Vector3 WorldToHerringOdd(Vector3 world){
		return CubicToHerringOdd (WorldToCubic (world));
	}

	/// <summary>Returns the rhombic coordinates of a point in world space.</summary>
	/// <param name="world">Point in world coordinates.</param>
	/// <returns>Point in rhombic coordinates.</returns>
	/// 
	/// This method takes a point in world space and returns the corresponding rhombic coordinates.
	/// The rhombic coordinate system uses three axes; the X-axis rotated 30° counter-clockwise, the regular Y-axis,
	/// and the Z coordinate is which layer of the grid the point is on, relative to the grid's central layer.
	public Vector3 WorldToRhombic (Vector3 world) {
		return CubicToRhombic (WorldToCubic (world));
	}

	/// <summary>Returns the cubic coordinates of a point in world space.</summary>
	/// <param name="world">Point in world coordinates.</param>
	/// <returns>Point in cubic coordinates.</returns>
	/// 
	/// This method takes a point in world space and returns the corresponding rhombic coordinates.
	/// The cubic coordinate system uses four axes; X, Y and Z are used to fix the point on the layer while W is which layer of the grid the point is on, relative to the grid's central layer.
	/// The central hex has coordinates (0, 0, 0, 0) and the sum of the first three coordinates is always 0.
	public Vector4 WorldToCubic (Vector3 world) {
		Vector3 local = _transform.GFInverseTransformPointFixed(world) - originOffset;
		float x, y, z, w;
		if (hexSideMode == HexOrientation.PointySides) {
			x = local [idx [0]] / side;
			y = (local [idx [1]] - local [idx [0]] * tan30) / height;
			z = -1.0f * (x + y);
		} else {
			z = -local [idx [1]] / side;
			y = (local [idx [1]] - local [idx [0]] * tan60) / (3.0f * radius);
			x = -1.0f * (z + y);
		}
		w = local [idx [2]] / depth;
		return new Vector4 (x, y, z, w);
	}

	/// <summary>Returns the barycentric coordinates of a point in world space.</summary>
	/// <param name="world">Point in world coordinates.</param>
	/// <returns>Point in barycentric coordinates.</returns>
	/// This method takes a point in world space and returns the corresponding barycentric coordinates. (subject to change?)
	/// Barycentric coordinates are similar to cubic ones, except the sum of the first three coordinates is 1.
	/// The central hex has coordinates (0, 0, -1, 0), its north-eastern neighbour has coordinates (1, 0, 0, 0) and its northern neighbour has coordinates (0, 1, 0, 0).
	/// In other words, it is the cubic coordinate system with +1 added to the Z-coordinate.
	private Vector4 WorldToBarycentric (Vector3 world) {
		return CubicToBarycentric (WorldToCubic (world));
	}

	#endregion

	#region HerringOdd
	/// <summary>Returns the world coordinates of a point in odd herringbone coordinates.</summary>
	/// <param name="herring">Point in odd herringbone coordinates.</param>
	/// <returns>Point in world coordinates.</returns>
	/// 
	/// Takes a point in odd herringbone coordinates and returns its world position.
	public Vector3 HerringOddToWorld(Vector3 herring){
		return CubicToWorld (HerringOddToCubic (herring));
	}

	/// <summary>Returns the rhombic coordinates of a point in odd herringbone coordinates.</summary>
	/// <param name="herring">Point in odd herringbone coordinates.</param>
	/// <returns>Point in rhombic coordinates.</returns>
	/// 
	/// Takes a point in odd herringbone coordinates and returns its rhombic position.
	public Vector3 HerringOddToRhombic (Vector3 herring) {
		return CubicToRhombic (HerringOddToCubic (herring));
	}

	/// <summary>Returns the cubic coordinates of a point in odd herringbone coordinates.</summary>
	/// <param name="herring">Point in odd herringbone coordinates.</param>
	/// <returns>Point in cubic coordinates.</returns>
	/// 
	/// Takes a point in odd herringbone coordinates and returns its cubic position.
	public Vector4 HerringOddToCubic (Vector3 herring) {
		int index = Mathf.FloorToInt (herring [idxS [0]]); // odd or even (idxS[0] means X-axis for pointy sides and Y-axis for flat sides)
		float x, y, z;
		if (hexSideMode == HexOrientation.PointySides) {
			x = herring [idx [0]];
			if ((index & 1) == 0) { // even
				y = herring [idx [1]] - index / 2;
				z = -(x + y);
			} else { // odd
				z = -herring [idx [1]] - (index + 1) / 2;
				y = -(x + z);
			}
		} else {
			z = -herring[idx[1]];
			if ((index & 1) == 0) { // even
				y = -herring [idx [0]] + index / 2;
				x = -(z + y);
			} else { // odd
				x = herring [idx [0]] + (index + 1) / 2;
				y = -(z + x);
			}
		}
		return new Vector4 (x, y, z, herring[idx[2]]);
	}

	/// <summary>Returns the barycentric coordinates of a point in odd herringbone coordinates.</summary>
	/// <param name="herring">Point in odd herringbone coordinates.</param>
	/// <returns>Point in bearycentric coordinates.</returns>
	/// 
	/// Takes a point in odd herringbone coordinates and returns its barycentric position.
	private Vector4 HerringOddToBarycentric (Vector3 herring) {
		return CubicToBarycentric (HerringOddToCubic (herring));
	}
	#endregion

	#region Rhombic
	/// <summary>Returns the world coordinates of a point in rhombic coordinates.</summary>
	/// <param name="rhombic">Point in rhombic coordinates.</param>
	/// <returns>Point in world coordinates.</returns>
	/// 
	/// Takes a point in rhombic coordinates and returns its world position.
	public Vector3 RhombicToWorld (Vector3 rhombic) {
		return CubicToWorld (RhombicToCubic (rhombic));
	}

	/// <summary>Returns the odd herring coordinates of a point in rhombic coordinates.</summary>
	/// <param name="rhombic">Point in rhombic coordinates.</param>
	/// <returns>Point in odd herring coordinates.</returns>
	/// 
	/// Takes a point in rhombic coordinates and returns its odd herring position.
	public Vector3 RhombicToHerringOdd (Vector3 rhombic) {
		return CubicToHerringOdd (RhombicToCubic (rhombic));
	}

	/// <summary>Returns the cubic coordinates of a point in rhombic coordinates.</summary>
	/// <param name="rhombic">Point in rhombic coordinates.</param>
	/// <returns>Point in cubic coordinates.</returns>
	/// 
	/// Takes a point in rhombic coordinates and returns its cubic position.
	public Vector4 RhombicToCubic (Vector3 rhombic) {
		float x = hexSideMode == HexOrientation.PointySides ? rhombic [idx [0]] : rhombic [idx [0]] + rhombic [idx [1]];
		float y = hexSideMode == HexOrientation.PointySides ? rhombic [idx [1]] : -rhombic [idx [0]];
		float z = hexSideMode == HexOrientation.PointySides ? -(rhombic [idx [0]] + rhombic [idx [1]]) : -rhombic [idx [1]];
		return new Vector4 (x, y, z, rhombic.z);
	}

	/// <summary>Returns the barycentric coordinates of a point in rhombic coordinates.</summary>
	/// <param name="rhombic">Point in rhombic coordinates.</param>
	/// <returns>Point in barycentric coordinates.</returns>
	/// 
	/// Takes a point in rhombic coordinates and returns its barycentric position.
	private Vector4 RhombicToBarycentric (Vector3 rhombic) {
		return CubicToBarycentric (RhombicToCubic (rhombic));
	}
	#endregion

	#region Cubic
	/// <summary>Returns the world coordinates of a point in cubic coordinates.</summary>
	/// <param name="cubic">Point in cubic coordinates.</param>
	/// <returns>Point in world coordinates.</returns>
	/// 
	/// Takes a point in cubic coordinates and returns its world position.
	public Vector3 CubicToWorld (Vector4 cubic) {
		Vector3 local; // first local space
		if (hexSideMode == HexOrientation.PointySides) {
			local = cubic.x * cubicColumnBasis + cubic.y * cubicRowBasis + cubic.w * depth * locUnits[idxS[2]];
		} else {
			local = -cubic.y * cubicColumnBasis - cubic.z * cubicRowBasis + cubic.w * depth * locUnits[idxS[2]];
		}
		//Debug.Log (local);
		return _transform.GFTransformPointFixed (local) + originOffset; // then world space
	}

	/// <summary>Returns the odd herring coordinates of a point in cubic coordinates.</summary>
	/// <param name="cubic">Point in cubic coordinates.</param>
	/// <returns>Point in odd herring coordinates.</returns>
	/// 
	/// Takes a point in cubic coordinates and returns its odd herring position.
	public Vector3 CubicToHerringOdd (Vector4 cubic) {
		float c, r; // column and row
		int index = hexSideMode == HexOrientation.PointySides ? Mathf.FloorToInt (cubic.x) : -Mathf.CeilToInt (cubic.z) ; // the left (or lower) border
		if (hexSideMode == HexOrientation.PointySides){
			c = cubic.x; // column
			r = (index & 1) == 0 ? cubic.y + index / 2 : -cubic.z - (index + 1) / 2; // row
		} else {
			r = -cubic.z;
			c = (index & 1) == 0 ? -cubic.y + index / 2 : cubic.x - (index + 1) / 2 ;
		}

		//return new Vector3 (c, r, cubic.w);
		return c * units[idx[0]] + r * units[idx[1]] + cubic.w * units[idx[2]];
	}

	/// <summary>Returns the rhombic coordinates of a point in cubic coordinates.</summary>
	/// <param name="cubic">Point in cubic coordinates.</param>
	/// <returns>Point in rhombic coordinates.</returns>
	/// 
	/// Takes a point in cubic coordinates and returns its rhombic position.
	public Vector3 CubicToRhombic (Vector4 cubic) {
		float x = hexSideMode == HexOrientation.PointySides ? cubic.x : -cubic.y;
		float y = hexSideMode == HexOrientation.PointySides ? cubic.y : -cubic.z;
		return x * units [idx [0]] + y * units [idx [1]] + cubic.w * units [idx [2]];
	}

	/// <summary>Returns the barycentric coordinates of a point in cubic coordinates.</summary>
	/// <param name="cubic">Point in cubic coordinates.</param>
	/// <returns>Point in world coordinates.</returns>
	/// 
	/// Takes a point in cubic coordinates and returns its barycentric position.
	private Vector4 CubicToBarycentric (Vector4 cubic) {
		return new Vector4 (cubic.x, cubic.y, cubic.z + 1.0f, cubic.w);
	}
	#endregion

	#region Barycentric
	/// <summary>Returns the world coordinates of a point in barycentric coordinates.</summary>
	/// <param name="barycentric">Point in barycentric coordinates.</param>
	/// <returns>Point in world coordinates.</returns>
	/// 
	/// Takes a point in barycentric coordinates and returns its world position.
	private Vector3 BarycentricToWorld (Vector4 barycentric) {
		return CubicToWorld (BarycentricToCubic (barycentric));
	}

	/// <summary>Returns the odd herring coordinates of a point in barycentric coordinates.</summary>
	/// <param name="barycentric">Point in barycentric coordinates.</param>
	/// <returns>Point in odd herring coordinates.</returns>
	/// 
	/// Takes a point in barycentric coordinates and returns its odd herring position.
	private Vector3 BarycentricToHerringOdd (Vector4 barycentric) {
		return CubicToHerringOdd (BarycentricToCubic (barycentric));
	}

	/// <summary>Returns the rhombic coordinates of a point in barycentric coordinates.</summary>
	/// <param name="barycentric">Point in barycentric coordinates.</param>
	/// <returns>Point in rhombic coordinates.</returns>
	/// 
	/// Takes a point in barycentric coordinates and returns its rhombic position.
	private Vector3 BarycentricToRhombic (Vector4 barycentric) {
		return CubicToRhombic (BarycentricToCubic (barycentric));
	}

	/// <summary>Returns the cubic coordinates of a point in barycentric coordinates.</summary>
	/// <param name="barycentric">Point in barycentric coordinates.</param>
	/// <returns>Point in cubic coordinates.</returns>
	/// 
	/// Takes a point in barycentric coordinates and returns its cubic position.
	private Vector4 BarycentricToCubic (Vector4 barycentric) {
		return new Vector4 (barycentric.x, barycentric.y, barycentric.z - 1.0f, barycentric.w);
	}
	#endregion
	#endregion

	#region Nearest
	#region World
	/// <summary>Returns the world coordinates of the nearest vertex.</summary>
	/// <param name="fromPoint">Point in world space.</param>
	/// <param name="doDebug">If set to @c true draw a sphere at the destination.</param>
	/// <returns>World position of the nearest vertex.</returns>
	/// 
	/// Returns the world position of the nearest vertex from a given point in world space.
	/// If <c>doDebug</c> is set a small gizmo sphere will be drawn at that position.
	public override Vector3 NearestVertexW(Vector3 world, bool doDebug = false) { // documentation taken from parent class
		Vector3 vertex = CubicToWorld (NearestVertexC (world));
		
		if(doDebug){
			Gizmos.DrawSphere(vertex, 0.3f);
		}
		return vertex;
	}

	/// <summary>Returns the world coordinates of the nearest face.</summary>
	/// <param name="fromPoint">Point in world space.</param>
	/// <param name="doDebug">If set to @c true draw a sphere at the destination.</param>
	/// <returns>World position of the nearest vertex.</returns>
	/// 
	/// Returns the world position of the nearest vertex from a given point in world space.
	/// If <c>doDebug</c> is set a small gizmo sphere will be drawn at that position.
	public override Vector3 NearestFaceW(Vector3 world, bool doDebug) {
		Vector3 face = CubicToWorld (NearestFaceC (world));
		if(doDebug){
			Gizmos.DrawSphere(face, height / 5);
		}
		return face;
	}

	/// <summary>Returns the world coordinates of the nearest box.</summary>
	/// <param name="fromPoint">Point in world space.</param>
	/// <param name="doDebug">If set to @c true draw a sphere at the destination.</param>
	/// <returns>World position of the nearest box.</returns>
	/// 
	/// Returns the world position of the nearest box from a given point in world space.
	/// Since the box is enclosed by several vertices, the returned value is the point in between all of the vertices.
	/// If <c>doDebug</c> is set a gizmo sphere will be drawn at that position.
	public override Vector3 NearestBoxW(Vector3 fromPoint, bool doDebug) {
		Vector3 box = CubicToWorld (NearestBoxC (fromPoint));
		if(doDebug){
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
	public override Vector3 NearestVertexG (Vector3 world) {
		return NearestVertexHO(world);
	}

	/// <summary>Returns the grid position of the nearest face.</summary>
	/// <param name="world">Point in world space.</param>
	/// <returns>Grid position of the nearest face.</returns>
	/// 
	/// This is just a shortcut for <c>#NearestFaceHO</c>.
	public override Vector3 NearestFaceG (Vector3 world) {
		return NearestFaceHO(world);
	}

	/// <summary>Returns the grid position of the nearest box.</summary>
	/// <param name="world">Point in world space.</param>
	/// <returns>Grid position of the nearest box.</returns>
	/// 
	/// This is just a shortcut for <c>#NearestBoxHO</c>.
	public override Vector3 NearestBoxG (Vector3 world) {
		return NearestBoxHO(world);
	}
	#endregion

	#region Herring Odd
	/// <summary>Returns the odd herring position of the nearest vertex.</summary>
	/// <param name="world">Point in world space.</param>
	/// <returns>Grid position of the nearest vertex.</returns>
	/// 
	/// This method takes in a point in world space and returns the odd herring coordinates of the nearest vertex.
	protected Vector3 NearestVertexHO(Vector3 world){
		return CubicToHerringOdd (NearestVertexC (world));
	}
	
	/// <summary>Returns the odd herring position of the nearest face.</summary>
	/// <param name="world">Point in world space.</param>
	/// <returns>Grid position of the nearest face.</returns>
	/// 
	/// This method takes in a point in world space and returns the odd herring coordinates of the nearest face.
	public Vector3 NearestFaceHO(Vector3 world) {
		return CubicToHerringOdd (NearestFaceC (world));
	}

	/// <summary>Returns the grid position of the nearest box.</summary>
	/// <param name="world">Point in world space.</param>
	/// <returns>Grid position of the nearest box.</returns>
	/// 
	/// Returns the world position of the nearest box from a given point in odd herring coordinates.
	protected Vector3 NearestBoxHO(Vector3 world){
		return CubicToHerringOdd (NearestBoxC (world));
	}
	#endregion

	#region Rhombic
	/// <summary>Returns the rhombic position of the nearest vertex.</summary>
	/// <param name="world">Point in world space.</param>
	/// <returns>Rhombic position of the nearest vertex.</returns>
	/// 
	/// This method takes in a point in world space and returns the rhombic coordinates of the nearest vertex.
	public Vector3 NearestVertexR (Vector3 world) {
		return CubicToRhombic (NearestVertexC (world));
	}

	/// <summary>Returns the rhombic position of the nearest face.</summary>
	/// <param name="world">Point in world space.</param>
	/// <returns>Rhombic position of the nearest face.</returns>
	/// 
	/// This method takes in a point in world space and returns the rhombic coordinates of the nearest face.
	public Vector3 NearestFaceR (Vector3 world) {
		return CubicToRhombic (NearestFaceC (world));
	}

	/// <summary>Returns the rhombic position of the nearest box.</summary>
	/// <param name="world">Point in world space.</param>
	/// <returns>Rhombic position of the nearest box.</returns>
	/// 
	/// Returns the rhombic position of the nearest box from a given point in odd herring coordinates.
	public Vector3 NearestBoxR (Vector3 world) {
		return CubicToRhombic (NearestBoxC (world));
	}
	#endregion

	#region Cubic
	/* HOW TO FIND VERTICES: Vertices can be found relative to their closest face. Assume the face has cubic coordinates (x, y, z) with x+y+z=0, then the vertex has coordinates
	 * (x + a/3, y + b/3, z + c/3) where either a, b or c = ±2 and the other two are ±(-1). The larger of the three always gravitates towards its direction, i.e. a=2 for E and -2 for E,
	 * b=2 for NW and b=-2 for SE, c=2 for SW and c=-2 for NE.
	 * 
	 * To find vertices reverse the strategy. First find the face, then the abs of the largest of the coordinates (relative to the face of course), that's where the vertex is
	 * gravitating towards. Then just add the appropriate values (±2/3 and ±1/3) to the face coordinates.
	 */

	/// <summary>Returns the cubic position of the nearest vertex.</summary>
	/// <param name="world">Point in world space.</param>
	/// <returns>Cubic position of the nearest vertex.</returns>
	/// 
	/// This method takes in a point in world space and returns the cubic coordinates of the nearest vertex.
	public Vector4 NearestVertexC (Vector3 world) {
		Vector4 cubic = WorldToCubic (world); // cubic coordinates of the world point
		Vector4 face = NearestFaceC (world); // the fce closes to the point
		Vector4 vertex = cubic - face; // point's cubic coordinates *relative* to the face

		int max = 0; // we will now look for the largest coordinate (using abs value) to decide between X, Y and Z
		for (int i = 0; i < 3; i++) {
			if (Mathf.Abs (vertex [i]) > Mathf.Abs (vertex [max])) // no need for %1.0f because the values are already relative and <= 1
				max = i;
		}
		//Debug.Log (max + ": "+ vertex[max] + " from " + cubic);
		int sign = vertex [max] % 1.0f >= 0 ? 1 : -1; // next we need to decide if the vertex is in the positive or negative direction
		vertex [max] = sign * 2.0f / 3.0f; // assign the vertex coordinates
		vertex [(max + 1) % 3] = -sign * 1.0f / 3.0f; // the other values are 1/3 and have the opposite sign
		vertex [(max + 2) % 3] = -sign * 1.0f / 3.0f; // using (max+i)%3 is a handy way to wrap around through 1, 2 and 3
		return vertex + face; // return the face coordinates plus the vertex offset
	}

	/// <summary>Returns the cubic position of the nearest face.</summary>
	/// <param name="world">Point in world space.</param>
	/// <param name="thePlane">Plane on which the face lies.</param>
	/// <returns>Cubic position of the nearest face.</returns>
	/// 
	/// This method takes in a point in world space and returns the cubic coordinates of the nearest face.
	public Vector4 NearestFaceC (Vector3 world) {
		Vector4 cubic = WorldToCubic (world);
		return RoundCubic (cubic);
	}

	/// <summary>Returns the cubic position of the nearest box.</summary>
	/// <param name="world">Point in world space.</param>
	/// <returns>Cubic position of the nearest box.</returns>
	/// 
	/// Teturns the cubic position of the nearest box from a given point in odd herring coordinates.
	public Vector4 NearestBoxC (Vector3 world) {
		Vector4 cubic = WorldToCubic (world); // first to cubic space
		Vector4 rounded = RoundCubic (cubic); // then the face
		rounded.w = Mathf.Floor (cubic.w) + 0.5f; // now correct the height
		return rounded;
	}
	#endregion

	#region Barycentric
	/// <summary>Returns the barycentric position of the nearest vertex.</summary>
	/// <param name="world">Point in world space.</param>
	/// <returns>Barycentric position of the nearest vertex.</returns>
	/// 
	/// This method takes in a point in world space and returns the barycentric coordinates of the nearest vertex.
	private Vector4 NearestVertexB (Vector3 world) {
		return CubicToBarycentric (NearestVertexC (world));
	}

	/// <summary>Returns the barycentric position of the nearest face.</summary>
	/// <param name="world">Point in world space.</param>
	/// <returns>Barycentric position of the nearest face.</returns>
	/// 
	/// This method takes in a point in world space and returns the barycentric coordinates of the nearest face.
	private Vector4 NearestFaceB (Vector3 world) {
		return CubicToBarycentric (NearestFaceC (world));
	}

	/// <summary>Returns the barycentric position of the nearest box.</summary>
	/// <param name="world">Point in world space.</param>
	/// <returns>Barycentric position of the nearest box.</returns>
	/// 
	/// Teturns the barycentric position of the nearest box from a given point in odd herring coordinates.
	private Vector4 NearestBoxB (Vector3 world) {
		return CubicToBarycentric (NearestBoxC (world));
	}
	#endregion
	#endregion

	#region Align Scale Methods
	/// <summary>Fits a position vector into the grid.</summary>
	/// <param name="pos">The position to align.</param>
	/// <param name="scale">A simulated scale to decide how exactly to fit the poistion into the grid.</param>
	/// <param name="lockAxis">Which axes should be ignored.</param>
	/// <returns>The vector3.</returns>
	/// 
	/// Aligns a poistion vector to the grid by positioning it on the centre of the nearest face.
	/// Please refer to the user manual for more information.
	/// The parameter lockAxis makes the function not touch the corresponding coordinate.
	public override Vector3 AlignVector3(Vector3 pos, Vector3 scale, GFBoolVector3 lockAxis){
		Vector3 newPos = NearestFaceW(pos);
		for(int i = 0; i < 3; i++){
			if(lockAxis[i])
				newPos[i] = pos[i];
		}
		return newPos;
	}

	/// <summary>Scales a size vector to fit inside a grid.</summary>
	/// <returns>The re-scaled vector.</returns>
	/// <param name="scl">The vector to scale.</param>
	/// <param name="lockAxis">The axes to ignore.</param>
	/// 
	/// This method takes in a vector representing a size and scales it to the nearest multiple of the grid’s radius and depth.
	/// The @c lockAxis parameter lets you ignore individual axes.
	public override Vector3 ScaleVector3(Vector3 scl, GFBoolVector3 lockAxis){
		Vector3 spacing = new Vector3();
		for(int i = 0; i < 2; i++){
			spacing[idxS[i]] = height;
		}
		spacing[idxS[2]] = depth;
		Vector3 relScale = scl.GFModulo3(spacing);
		Vector3 newScale = new Vector3();
				
		for (int i = 0; i <= 2; i++){
			newScale[i] = scl[i];			
			if(relScale[i] >= 0.5f * spacing[i]){
//				Debug.Log ("Grow by " + (spacing.x - relScale.x));
				newScale[i] = newScale[i] - relScale[i] + spacing[i];
			} else{
//				Debug.Log ("Shrink by " + relativeScale.x);
				newScale[i] = newScale[i] - relScale[i];
				//if we went too far default to the spacing
				if(newScale[i] < spacing[i])
					newScale[i] = spacing[i];
			}
		}
		
		for(int i = 0; i < 3; i++){
			if(lockAxis[i])
				newScale[i] = scl[i];
		}
		
		return newScale;
	}
	
	#endregion

	#region Drawing Methods
		
	public override void DrawGrid(Vector3 from, Vector3 to){
		DrawGridRect(from, to);		
	}

	/// <summary>Currently the same as <c>DrawGrid</c>.</summary>
	protected void DrawGridRect(Vector3 from, Vector3 to){
		if(hideGrid)
			return;
		
		Vector3[][][] lines = CalculateDrawPointsRect(from, to, gridStyle == HexGridShape.CompactRectangle);
		
		//Swap the X and Y colours if the grid has flat sides (that's because I swapped quasi-X and quasi-Y when calculating the points)
		//Swap<Color>(ref axisColors.x, ref axisColors.y, hexSideMode == HexOrientation.FlatSides);
		ColourSwap (ref axisColors, 0, 1, hexSideMode == HexOrientation.FlatSides);
		
		for(int i = 0; i < 3; i++){//looping through the two (three?) directions
			if(hideAxis[i])
				continue;
			Gizmos.color = axisColors[i];
			foreach(Vector3[] line in lines[i]){
			if(line != null)	Gizmos.DrawLine(line[0], line[1]);
			}
		}
		
		//Swap<Color>(ref axisColors.x, ref axisColors.y, hexSideMode == HexOrientation.FlatSides); //swap the colours back
		ColourSwap (ref axisColors, 0, 1, hexSideMode == HexOrientation.FlatSides); //swap the colours back
		
		//draw a sphere at the centre
		if(drawOrigin){
			Gizmos.color = Color.white;
			Gizmos.DrawSphere(_transform.position, 0.3f);
		}

		//DrawHerring ();
	}
		
	#endregion
	
	#region Render Methods
	#region overload
	public override void RenderGrid(Vector3 from, Vector3 to, GFColorVector3 colors, int width = 0, Camera cam = null, Transform camTransform = null){
		RenderGridRect(from, to, useSeparateRenderColor ? renderAxisColors : axisColors, width, cam, camTransform);
	}

	/// <summary>Currently the same as <see cref="RenderGrid"/>.</summary>
	protected void RenderGridRect(int width = 0, Camera cam = null, Transform camTransform = null){
		RenderGridRect(-size, size, useSeparateRenderColor ? renderAxisColors : axisColors, width, cam, camTransform);
	}
	#endregion
	
	protected void RenderGridRect(Vector3 from, Vector3 to, GFColorVector3 colors, int width = 0, Camera cam = null, Transform camTransform = null){
		if(!renderGrid)
			return;
		
		if(!renderMaterial)
			renderMaterial = defaultRenderMaterial;
		
		CalculateDrawPointsRect(from, to, gridStyle == HexGridShape.CompactRectangle);
		
		RenderGridLines(colors, width, cam, camTransform);
	}
	#endregion
	
	#region Draw Gizoms
	
	void OnDrawGizmos(){
		if(useCustomRenderRange){
			DrawGrid(renderFrom, renderTo);
		} else{
			DrawGrid();
		}
		//Gizmos.DrawSphere(GridToWorld(Vector3.zero), 0.3f);
		//DrawHerring();
	}
	
	protected void DrawHerring(){
		DrawHerring(-size, size);
	}
	
	protected void DrawHerring(Vector3 from, Vector3 to){
		for(int i = Mathf.FloorToInt(from[idxS[0]] / (1.5f * radius)) + 1; i < Mathf.FloorToInt(to[idxS[0]] / (1.5f * radius)); i++){
			for(int j = Mathf.FloorToInt(from[idxS[1]] / height) + 1; j < Mathf.FloorToInt(to[idxS[1]] / height); j++){
				for(int k = Mathf.FloorToInt(from[idxS[2]] / depth); k < Mathf.FloorToInt(to[idxS[2]] / depth) + 1; k++){
					Gizmos.color = Color.yellow;
					Gizmos.DrawLine(GridToWorld(i * units[idxS[0]] + j * units[idxS[1]] + k * units[idxS[2]]),
						GridToWorld((i+1) * units[idxS[0]] + j * units[idxS[1]] + k * units[idxS[2]]));
					Gizmos.color = Color.white;
					Gizmos.DrawLine(GridToWorld(i * units[idxS[0]] + j * units[idxS[1]] + k * units[idxS[2]]),
						GridToWorld(i * units[idxS[0]] + (j+1) * units[idxS[1]] + k * units[idxS[2]]));
				}
			}
		}
	}
	
	#endregion
	
	//calculates the points to be used for drawing and rendering (the result is of type Vector3[][][] where the most inner array is a pair of two points,
	//the middle array is the set of all points of the same axis and the outer array is the set of those three sets
	
	#region Calculate draw points
	
	#region overload
	protected override Vector3[][][] CalculateDrawPoints(Vector3 from, Vector3 to){
		return CalculateDrawPointsRect(from, to, gridStyle == HexGridShape.CompactRectangle);
	}
	
	protected Vector3[][][] CalculateDrawPointsRect(bool isCompact = false){
		return CalculateDrawPointsRect(-size, size, gridStyle == HexGridShape.CompactRectangle);
	}
	
	protected void CalculateDrawPointsRhomb(){
		CalculateDrawPointsRhomb(-size, size);
	}
	
	protected void CalculateDrawPointsTriangle(){
		CalculateDrawPointsTriangle(-size, size);
	}
	
	protected void CalculateDrawPointsBigHex(){
		CalculateDrawPointsBigHex(-size, size);
	}
	#endregion
	
	protected Vector3[][][] CalculateDrawPointsRect(Vector3 from, Vector3 to, bool isCompact = false){
		// reuse the points if the grid hasn't changed, we already have some points and we use the same range
		if( RecyclePoints( from, to ) )
			return _drawPoints;

		_drawPoints = new Vector3[3][][];
		Vector3 spacing = CombineRadiusDepth();
		Vector3 relFrom = relativeSize ? Vector3.Scale(from, spacing) : from;
		Vector3 relTo = relativeSize ? Vector3.Scale(to, spacing) : to;
		
		float[] length = new float[3];
		for(int i = 0; i < 3; i++){length[i] = relTo[i] - relFrom[i];}
		
		//calculate the amount of steps from the centre for the first hex (I will need these values later)
		int startX = Mathf.FloorToInt(relTo[idxS[0]] / (1.5f * radius));
		int startY = Mathf.FloorToInt(relTo[idxS[1]] / h);
		
		int endX = Mathf.CeilToInt(relFrom[idxS[0]] / (1.5f * radius));
		int endY = Mathf.CeilToInt(relFrom[idxS[1]] / h);
						
		//the starting point of the first pair (an iteration vector will be added to this)
		Vector3[] startPoint = new Vector3[1]{ // can this be expanded to use 3 points and draw incomplete lines like in RectGrid?
			//everything in the right top front
			_transform.position + locUnits[idxS[0]] * (1.5f * startX) * radius
				+ locUnits[idxS[1]] * ((startY + (Mathf.Abs(startX % 2)) * 0.5f) * h)
				+ locUnits[idxS[2]] * depth * Mathf.Floor(relTo[idxS[2]] / depth)
		};
		//Gizmos.DrawSphere(startPoint[0], 0.3f);
				
		int[] amount = new int[3]{
			startX - endX + 1,
			startY - endY + 1,
			Mathf.FloorToInt(relTo[idxS[2]] / depth) - Mathf.CeilToInt(relFrom[idxS[2]] / depth) + 1
		};
				
		//a multiple of this will be added to the starting point for iteration
		Vector3[] iterationVector = new Vector3[3]{
			locUnits[idxS[0]] * -side, locUnits[idxS[1]] * -h,	locUnits[idxS[2]] * -depth
		};
		
		Vector3[][] lineSetX = new Vector3[(amount[0] * amount[1] + amount[0])  * amount[2]][];
		Vector3[][] lineSetY = new Vector3[(amount[0] * 2 * amount[1] + 2 * amount[1] + amount[0] - 1) * amount[2]][];
		
		int[] iterator = new int[3]{0, 0, 0};
		
		for(int i = 0; i < amount[2]; i++){ //loop through the quasi-Z axis
			for(int j = 0; j < amount[0]; j++){ // loop through the quasi-X axis
				bool isShiftedUp = (startX % 2 == 0 && j % 2 == 1) || (Mathf.Abs(startX % 2) == 1 && j % 2 == 0); //is the current hex shifted upwards?
				for(int k = 0; k < amount[1]; k++){  // loop through the quasi-Y axis
					Vector3 hexCentre = startPoint[0] + j * iterationVector[0] + k * iterationVector[1] + i * iterationVector[2];
					//quasi-Y offset adjusting (can this be made into its own variable?)
					if(startX % 2 == 0 && j % 2 == 1){
						hexCentre += 0.5f * h * locUnits[idxS[1]];
					} else if(Mathf.Abs(startX % 2) == 1 && j % 2 == 1){
						hexCentre -= 0.5f * h * locUnits[idxS[1]];
					}
					
					Vector3[] lineSE = new Vector3[2]{hexCentre + locUnits[idxS[0]] * radius,
						hexCentre - locUnits[idxS[1]] * 0.5f * h + locUnits[idxS[0]] * 0.5f * radius};
					if(!(isShiftedUp && k == 0 && j == 0 && isCompact)){
						lineSetY[iterator[1]] = lineSE; iterator[1]++; //make an exception in one case
					}
					
					Vector3[] lineS = new Vector3[2]{lineSE[1], lineSE[1] - radius * locUnits[idxS[0]]};
					lineSetX[iterator[0]] = lineS; iterator[0]++;
					
					//if the grid is compact we don't need the rest from the up shifted upper lines for the first run of the inner loop
					if(isCompact && k == 0 && isShiftedUp)
						continue;
					
					Vector3[] lineNE = new Vector3[2]{lineSE[0] + 0.5f * h * locUnits[idxS[1]] - 0.5f * radius * locUnits[idxS[0]], lineSE[0]};
					lineSetY[iterator[1]] = lineNE; iterator[1]++;
					
					
					if(k == 0){ // all upper hexes get one northern line (this gives us all missing quasi-X lines)
						Vector3[] lineN = new Vector3[2]{lineS[1] + h * locUnits[idxS[1]],lineS[0] + h * locUnits[idxS[1]]};
						lineSetX[iterator[0]] = lineN; iterator[0]++;
					}
					
					if(j == amount[0] - 1){ // all the left hexes get a north-western line
						Vector3[] lineNW = new Vector3[2]{lineSE[1] - side * locUnits[idxS[0]] + 0.5f * h * locUnits[idxS[1]],
							lineSE[0] - side * locUnits[idxS[0]] + 0.5f * h * locUnits[idxS[1]]};
						lineSetY[iterator[1]] = lineNW; iterator[1]++;
					}
					
					// north-western lines for upper shifted hexes (except the most left one, that has been dealt with above)
					if(isShiftedUp && k == 0 && j != amount[0] - 1){
						Vector3[] lineNW = new Vector3[2]{lineSE[1] - side * locUnits[idxS[0]] + 0.5f * h * locUnits[idxS[1]],
							lineSE[0] - side * locUnits[idxS[0]] + 0.5f * h * locUnits[idxS[1]]};
						lineSetY[iterator[1]] = lineNW; iterator[1]++;
					}
					
					if(j == amount[0] - 1){ // all the left hexes get a south-western line
						Vector3[] lineSW = new Vector3[2]{lineSE[1] - radius * locUnits[idxS[0]], lineSE[0] - 2.0f * radius * locUnits[idxS[0]]};
						lineSetY[iterator[1]] = lineSW; iterator[1]++;
					}
					
					// south-western lines for lower unshifted hexes (except the most left one, that has been dealt with above)
					if(!isShiftedUp && k == amount[1] - 1 && j != amount[0] - 1){
						Vector3[] lineSW = new Vector3[2]{lineSE[1] - radius * locUnits[idxS[0]], lineSE[0] - 2.0f * radius * locUnits[idxS[0]]};
						lineSetY[iterator[1]] = lineSW; iterator[1]++;
					}
					
					//Gizmos.DrawSphere(hexCentre, 0.3f);
				}
			}
		}
		_drawPoints[0] = lineSetX;
		_drawPoints[1] = lineSetY;
		
		Vector3[][] lineSetZ = new Vector3[2 * amount[0] * amount[1] + 3 * amount[0] + 2 * amount[1] - 1][];
		//similar to above loop though all the hexes and add each vertex once
		for(int j = 0; j < amount[0]; j++){
			bool isShiftedUp = (startX % 2 == 0 && j % 2 == 1) || (Mathf.Abs(startX % 2) == 1 && j % 2 == 0);
			Vector3 depthShift = - locUnits[idxS[2]] * length[idxS[2]];
			Vector3 hexStart = startPoint[0] + locUnits[idxS[2]] * (relTo[idxS[2]] - depth * Mathf.Floor(relTo[idxS[2]] / depth)); //adjust quasi-Z coordinate
			for(int k = 0; k < amount[1]; k++){
				Vector3 hexCentre = hexStart + j * iterationVector[0] + k * iterationVector[1];
				
				//quasi-Y offset adjusting (can this be made into its own variable?)
				if(startX % 2 == 0 && j % 2 == 1){
					hexCentre += 0.5f * height * locUnits[idxS[1]];
				} else if(Mathf.Abs(startX % 2) == 1 && j % 2 == 1){
					hexCentre -= 0.5f * height * locUnits[idxS[1]];
				}
				
				Vector3 pointE = hexCentre + locUnits[idxS[0]] * radius;
				if(!(isShiftedUp && k == 0 && j == 0 && isCompact)){//make an exception in one case
					Vector3[] pairE = new Vector3[2]{pointE, pointE + depthShift};
					lineSetZ[iterator[2]] = pairE; iterator[2]++;
					//Gizmos.color = new Color(1.0f, 0.0f, 0.0f, 0.3f); Gizmos.DrawSphere(pointE, 0.3f);
				}
				Vector3 pointSE = hexCentre - locUnits[idxS[1]] * 0.5f * height + locUnits[idxS[0]] * 0.5f * radius;
				Vector3[] pairSE = new Vector3[2]{pointSE, pointSE + depthShift};
				lineSetZ[iterator[2]] = pairSE; iterator[2]++;
				//Gizmos.color = new Color(0.0f, 1.0f, 0.0f, 0.3f); Gizmos.DrawSphere(pointSE, 0.3f);
				
				//if the grid is compact we don't need the rest from the up shifted upper lines
				if(isCompact && k == 0 && isShiftedUp)
					continue;
				
				if(k == 0){ // all upper hexes get one north-eastern point
					Vector3 pointNE = pointSE + height * locUnits[idxS[1]];
					Vector3[] pairNE = new Vector3[2]{pointNE, pointNE + depthShift};
					lineSetZ[iterator[2]] = pairNE; iterator[2]++;
					//Gizmos.color = new Color(1, 1, 0, 0.3f); Gizmos.DrawSphere(pointNE, 0.3f);
				}
				
				if(j == amount[0] - 1){ // all the left hexes get a north-western point
					Vector3 pointNW = pointE - side * locUnits[idxS[0]] + 0.5f * height * locUnits[idxS[1]];
					Vector3[] pairNW = new Vector3[2]{pointNW, pointNW + depthShift};
					lineSetZ[iterator[2]] = pairNW; iterator[2]++;
					//Gizmos.color = new Color(0, 1, 1, 0.3f); Gizmos.DrawSphere(pointNW, 0.3f);
				}
				
				// north-western points for upper shifted hexes (except the most left one, that has been dealt with above)
				if(isShiftedUp && k == 0 && j != amount[0] - 1){
					Vector3 pointNW = pointE - side * locUnits[idxS[0]] + 0.5f * height * locUnits[idxS[1]];
					Vector3[] pairNW = new Vector3[2]{pointNW, pointNW + depthShift};
					lineSetZ[iterator[2]] = pairNW; iterator[2]++;
					//Gizmos.DrawSphere(pointNW, 0.3f); Gizmos.color = new Color(1, 0, 1, 0.3f); //Magenta
				}
				
				if(j == amount[0] - 1){ // all the left hexes get a western point (and a south-western for the lowest one)
					Vector3 pointW = pointE - 2.0f * radius * locUnits[idxS[0]];
					Vector3[] pairW = new Vector3[2]{pointW, pointW + depthShift};
					lineSetZ[iterator[2]] = pairW; iterator[2]++;
					//Gizmos.color = new Color(0.0f, 0.0f, 1.0f, 0.3f);Gizmos.DrawSphere(pointW, 0.3f);
					if(k == amount[1] - 1){ // one more for the lower left hex
						Vector3 pointSW = pointSE - radius * locUnits[idxS[0]];
						Vector3[] pairSW = new Vector3[2]{pointSW, pointSW + depthShift};
						lineSetZ[iterator[2]] = pairSW; iterator[2]++;
						//Gizmos.color = new Color(0, 0, 0, 0.3f); Gizmos.DrawSphere(pointSW, 0.3f);
					}
				}
				
				// south-western points for lower unshifted hexes (except the most left one, that has been dealt with above)
				if(!isShiftedUp && k == amount[1] - 1 && j != amount[0] - 1){
					Vector3 pointSW = pointSE - radius * locUnits[idxS[0]];
					Vector3[] pairSW = new Vector3[2]{pointSW, pointSW + depthShift};
					lineSetZ[iterator[2]] = pairSW; iterator[2]++;
					//Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.3f); Gizmos.DrawSphere(pointSW, 0.3f);
				}
			}
		}
		//Debug.Log((2 * amount[0] * amount[1] + 3 * amount[0] + 2 * amount[1] - 1 - iterator[3]) + "= " + (2 * amount[0] * amount[1] + 3 * amount[0] + 2 * amount[1] - 1) + " - " + iterator[3]);
		_drawPoints[2] = lineSetZ;		

		ApplyDrawOffset ();

		return _drawPoints;
	}
	
	protected void CalculateDrawPointsRhomb(Vector3 from, Vector3 to){
		
	}
	
	protected void CalculateDrawPointsTriangle(Vector3 from, Vector3 to){
		
	}
	
	protected void CalculateDrawPointsBigHex(Vector3 from, Vector3 to){
		
	}
	
	#endregion

	#region helper functions
	#if !DOXYGEN_SHOULD_SKIP_THIS // make doxygen skip the following lines
	/// <summary>Transforms from quasi-axis to real-ais and swaps if needed.</summary>
	/// <returns>The real-indices from quasi-indices.</returns>
	/// <param name="plane">Plane.</param>
	/// 
	/// Similar to the base class, except these ones swap quasi-X and quasi-Y when hexes have flat sides.
	private int[] TransformIndicesS(GridPlane plane){
		int[] indices = TransformIndices (plane);
		Swap<int>(ref indices[0], ref indices[1], hexSideMode == HexOrientation.FlatSides);
		return indices;
	}

	/// <summary>Get-accessor for the result of TransformIndicesS.</summary>
	/// <value>The quasi-indices transformed to real indices and swapped.</value>
	private int[] idxS {get {return TransformIndicesS(gridPlane);}}

	/// <summary>rounds cubic coordinates to the nearest face.</summary>
	/// <returns>Cubic coordinates rounded to nearest face.</returns>
	/// <param name="cubic">Point in cubic coordinates.</param>
	protected Vector4 RoundCubic (Vector4 cubic) {
		Vector4 rounded = new Vector4 (Mathf.Round (cubic.x), Mathf.Round (cubic.y), Mathf.Round (cubic.z), Mathf.Round (cubic.w)); //first round all components

		float x = Mathf.Abs (cubic.x - rounded.x);
		float y = Mathf.Abs (cubic.y - rounded.y);
		float z = Mathf.Abs (cubic.z - rounded.z);

		if (x > Mathf.Max (y, z)) {
			rounded.x = -(rounded.y + rounded.z);
		} else if (y > Mathf.Max (x, z)) {
			rounded.y = -(rounded.x + rounded.z);
		} else {
			rounded.z = -(rounded.x + rounded.y);
		}

		return rounded;
	}


	// returns the direction from a face to the specified face (world space only!)
	public Vector3 GetDirection (HexDirection dir) {
		return GetDirection (dir, hexSideMode);
	}
	public Vector3 GetDirection (HexDirection dir, HexOrientation mode) {
		Vector3 vec = Vector3.zero;
		if (mode == HexOrientation.PointySides) {
			if (dir == HexDirection.N || dir == HexDirection.S) {
				vec = height * locUnits[idxS[1]];
			} else if (dir == HexDirection.NE || dir == HexDirection.SW){
				vec = 1.5f * radius * locUnits[idxS[0]] + 0.5f * height * locUnits[idxS[1]];
			} else if (dir == HexDirection.E || dir == HexDirection.W) {
				vec = 1.5f * radius * locUnits[idxS[0]];
			} else if (dir == HexDirection.SE || dir == HexDirection.NW) {
				vec = 1.5f * radius * locUnits [idxS[0]] - 0.5f * height * locUnits [idxS[1]];
			}
		} else{
			if (dir == HexDirection.N || dir == HexDirection.S) {
				vec = 1.5f * radius * locUnits[idxS[1]];
			} else if (dir == HexDirection.NE || dir == HexDirection.SW){
				vec = 0.5f * height * locUnits[idxS[0]] + 1.5f * radius * locUnits[idxS[1]];
			} else if (dir == HexDirection.E || dir == HexDirection.W) {
				vec = height * locUnits[idxS[0]];
			} else if (dir == HexDirection.SE || dir == HexDirection.NW) {
				vec = 0.5f * height * locUnits [idxS[0]] - 1.5f * radius * locUnits [idxS[1]];
			}
		}
		if (dir == HexDirection.S || dir == HexDirection.SW || dir == HexDirection.W || dir == HexDirection.NW)
			vec *= -1.0f;
		return vec;
	}
	
	// returns the direction from a face to the specified vertex (world space only!)
	public Vector3 VertexToDirection(HexDirection vert) {
		return VertexToDirection (vert, true);
	}

	public Vector3 VertexToDirection(HexDirection vert, bool worldSpace){
		Vector3 dir = new Vector3();
				
		if(vert == HexDirection.E || vert == HexDirection.W){
			dir = locUnits[idxS[0]] * radius;
			if(vert == HexDirection.W)
				dir = -dir;
		} else if(vert == HexDirection.N || vert == HexDirection.S){
			dir = locUnits[idxS[1]] * 0.5f * height;
			if(vert == HexDirection.S)
				dir = -dir;
		} else if(vert == HexDirection.NE || vert == HexDirection.SW){
			dir = 0.5f * radius * locUnits[idxS[0]] + 0.5f * height * locUnits[idxS[1]];
			if(vert == HexDirection.SW)
				dir = -dir;
		}  else if(vert == HexDirection.NW || vert == HexDirection.SE){
			dir = -0.5f * radius * locUnits[idxS[0]] + 0.5f * height * locUnits[idxS[1]];
			if(vert == HexDirection.SE)
				dir = -dir;
		}
		
		return dir;	
	}
	
	// combines radius and depth into one vector that works like spacing for rectangular grids
	protected Vector3 CombineRadiusDepth(){
		Vector3 spacing = new Vector3();
		spacing[idxS[0]] = 1.5f * radius;
		spacing[idxS[1]] = Mathf.Sqrt(3) * radius;
		spacing[idxS[2]] = depth;
		return spacing;
	}

	// we need this because we can't pass the indexer or getter/setter as ref
	private void ColourSwap (ref GFColorVector3 col, int i, int j, bool condition = true) {
		if (!condition)
			return;
		Color temp = col[i];
		col [i] = col [j];
		col [j] = temp;
	}
	#endif
	#endregion
}