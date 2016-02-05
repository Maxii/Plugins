using UnityEngine;
using GridFramework;
using GridFramework.Vectors;

/// <summary>
///   A spherical grid: latitude, longitude and altitude.
/// </summary>
///
/// <remarks>
///   <para>
///   Spherical grids are round 3D grids defined by the radius of the sphere,
///   the number of parallel circles running orthogonal to the sphere's polar
///   axis and the number or meridians running from one pole to the other along
///   the surface of the sphere.
///   </para>
///   <para>
///     This type of grid as three coordinate systems: spherical, grid and
///     geographical. Spherical- and geographic coordinates are similar to
///     each other, except in how they thread the meridian coordinate.
///   </para>
///   <para>
///     The north pole is in positive Y-direction from the origin of the grid,
///     the south pole is opposite of it, the vector from south- to north pole
///     is the axis of rotation. The direction of rotation for longitude is
///     counter-clockwise when the rotation axis is pointing towards the
///     observer, or in other words eastwards. The primary meridian alignes
///     with the object's Z-axis (forward).
///   </para>
///   <list type="table">
///     <listheader>
///       <term>
///         Coordinate systems
///       </term>
///       <description>
///         Description
///       <description>
///     </listheader>
///
///     <item>
///       <term>
///         Spherical coordinates
///       </term>
///       <description>
///         Vectors are given as radius, polar angle and azimuth angle. The
///         radius is the distance from the origin of the grid, and the polar
///         angle is the angle between the vector and the polar axis. The azimuth
///         angle is the angle from the prime-meridian, between 0 and 2π, similar
///         to the angle in polar coordinates.
///       </description>
///     </item>
///     <item>
///       <term>
///         Geographic coordinates
///       </term>
///       <description>
///         Vectors are given as altitude, latitude and longitude. The altitude
///         is the distance from the surface of the first sphere, the latitude is
///         the same as the polar angle in spherical coordinates. The longitude
///         is positive to the right of the primary meridian and negative to the
///         left.  The sign of the longitude opposite of the prime meridian is
///         undefined.
///       </description>
///     </item>
///     <item>
///       <term>
///         Grid coordinates
///       </term>
///       <description>
///         This is similar to the spheric grid, the only difference is that
///         values are not in absolute distance and angles, but in relative
///         number of spheres, parallels and meridians.
///       </description>
///     </item>
///   </list>
/// </remarks>
public class GFSphereGrid : GFGrid {

#region  Class Members
	/// <summary>
	///   Radius of each sphere.
	/// </summary>
	[SerializeField] private float _radius = 1.0f;

	/// <summary>
	///   Number of meridian lines.
	/// </summary>
	[SerializeField] private int _meridians = 19;

	/// <summary>
	///   Number of parallel lines.
	/// </summary>
	[SerializeField] private int _parallels = 36;

	/// <summary>
	///   The amount of segments within a parallel, more looks smoother.
	/// </summary>
	[SerializeField] private int _smoothP = 2;

	/// <summary>
	///   The amount of segments within a meridian, more looks smoother.
	/// </summary>
	[SerializeField] private int _smoothM = 2;

	/// <summary>
	///   Radius of each sphere.
	/// </summary>
	///
	/// <remarks>
	///   The radius has to be greater than zero.
	/// </remarks>
	public float radius {
		get {return _radius;}
		set {
			SetMember(value, ref _radius, restrictor: Mathf.Max, limit: Mathf.Epsilon);
		}
	}

	/// <summary>
	///   Number of meridian lines.
	/// </summary>
	/// <value>
	///   Number of parallels, at least 1.
	/// </value>
	/// <remarks>
	///   There has to be at least one meridian, which is also the prime
	///   meridian.
	/// </remarks>
	public int meridians {
		get {return _meridians;}
		set {
			SetMember<int>(value, ref _meridians, restrictor: Mathf.Max, limit: 1);
		}
	}

	/// <summary>
	///   Number of parallel lines.
	/// </summary>
	/// <value>
	///   Number of parallels, at least 1.
	/// </value>
	/// <remarks>
	///   There has to be at least one parallel, which is also the equator.
	/// </remarks>
	public int parallels {
		get {return _parallels;}
		set {
			SetMember<int>(value, ref _parallels, restrictor: Mathf.Max, limit: 1);
		}
	}
	
	/// <summary>
	///   Divides the parallels to create a smoother look.
	/// </summary>
	/// <value>
	///   Smoothness of the parallels, at least 1.
	/// </value>
	///
	/// <remarks>
	///   <para>
	///     Unity's GL class can only draw straight lines, so in order to get
	///     the parallels to look round this value breaks each segment into
	///     smaller segment. The number of smoothness tells how many segments
	///     the parallel line has been broken into. The amount of end points
	///     used is smoothness + 1, because we count both edges of the sector.
	///   </para>
	/// </remarks>
	public int smoothP {
		get {return _smoothP;}
		set {
			SetMember<int>(value, ref _smoothP, restrictor: Mathf.Max, limit: 1, updateMatrix: false);
		}
	}

	/// <summary>
	///   Divides the meridians to create a smoother look.
	/// </summary>
	/// <value>
	///   Smoothness of the meridians, at least 1.
	/// </value>
	///
	/// <remarks>
	///   <para>
	///     Unity's GL class can only draw straight lines, so in order to get
	///     the meridians to look round this value breaks each sector into
	///     smaller segments. The number of smoothness tells how many segments
	///     the meridian has been broken into. The amount of end points used is
	///     smoothness + 1, because we count both edges of the sector.
	///   </para>
	/// </remarks>
	public int smoothM {
		get {return _smoothM;}
		set {
			SetMember<int>(value, ref _smoothM, restrictor: Mathf.Max, limit: 1, updateMatrix: false);
		}
	}


#region  Overriding Inherited Accessors
	/// <summary>
	///   Overrides the size of GFGrid to make sure the angular coordinates are
	///   clamped appropriately between 0 and 2π (360°) or 0 and
	///   <see cref="parallels"/> or <see cref="meridians"/> respectively.
	/// </summary>
	///
	/// <value>
	///   The size of the grid's visual representation.
	/// </value>
	///
	/// Aside from the additional constraint for the angular value the same
	/// rules apply as for the base class, meaning no component can be less
	/// than 0.
	///
	/// <seealso cref="relativeSize"         />
	/// <seealso cref="useCustomRenderRange" />
	public override Vector3 size { get{ return _size;}
		set {
			base.size = value;
			if (relativeSize) {
				_size[1] = Mathf.Min(_size[1], 1.0f * parallels - 1);
				_size[2] = Mathf.Min(_size[2],        meridians);
			} else {
				_size[1] = Mathf.Min(_size[1], 1.0f * Mathf.PI);
				_size[2] = Mathf.Min(_size[2], 2.0f * Mathf.PI);
			}
		}
	}

	/// <summary>
	///   Overrides the property of GFGrid to make sure that the radial value
	///   is positive and the angular value is wrapped around properly.
	/// </summary>
	/// <value>
	///   Custom lower limit for drawing and rendering.
	/// </value>
	///
	/// Aside from the additional constraint for the radial and angular value
	/// the same rules apply as for the base class, meeaning the vector cannot
	/// be greater than <see cref="renderTo"/>.
	///
	/// <seealso cref="relativeSize"         />
	/// <seealso cref="useCustomRenderRange" />
	/// <seealso cref="renderTo"             />
	public override Vector3 renderFrom {
		set {
			base.renderFrom = value;
			_renderFrom[0] = Mathf.Max(_renderFrom[0], 0); // prevent negative value
			_renderFrom[1] = Mathf.Max(0, _renderFrom[1]);
			_renderFrom[2] = Float2Sector(value[2], relativeSize ? meridians : 2.0f * Mathf.PI);
		}
	}

	/// <summary>
	///   Overrides the property of GFGrid to make sure that the angular value
	///   is wrapped around properly.
	/// </summary>
	///
	/// <value>
	///   Custom upper limit for drawing and rendering.
	/// </value>
	///
	/// Aside from the additional constraint for the angular value the same
	/// rules apply as for the base class, meeaning the vector cannot be lower
	/// than <see cref="renderFrom"/>.
	///
	/// <seealso cref="relativeSize"         />
	/// <seealso cref="useCustomRenderRange" />
	/// <seealso cref="renderFrom"           />
	public override Vector3 renderTo {
		get{ return _renderTo;}
		set {
			base.renderTo = value;
			// convert to sector or angle (wrap around and handle < 0)
			_renderTo[1] = Mathf.Min(_renderTo[1], relativeSize ? parallels-1 : Mathf.PI);
			_renderTo[2] = Float2Sector(value[2], relativeSize ? meridians : 2.0f * Mathf.PI);
		}
	}

	/// @internal
	/// <summary>
	///   Interprets a float as sector; loops if value exceeds [sectors] and
	///   runs in reverse for negative values.
	/// </summary>
	private static float Float2Sector(float number, float amount) {
		return number >= 0 ? number % amount : amount + (number % amount);
	}
#endregion  // Overriding Inherited Accessors
#endregion

#region  Properties
	/// <summary>
	///   Polar angle between two parallels in radians.
	/// </summary>
	/// <value>
	///   Polar angle between two parallels in radians.
	/// </value>
	public float polar {
		get {return Mathf.PI / (parallels-1);}
	}

	/// <summary>
	///   Polar angle between two parallels in degrees.
	/// </summary>
	/// <value>
	///   Polar angle between two parallels in degrees.
	/// </value>
	public float polarDeg {
		get {return 180.0f * polar / Mathf.PI;}
	}

	/// <summary>
	///   Azimuth angle between two meridians in radians.
	/// </summary>
	/// <value>
	///   Azimuth angle between two meridians in radians.
	/// </value>
	public float azimuth {
		get {return 2 * Mathf.PI / meridians;}
	}

	/// <summary>
	///   Azimuth angle between two meridians in degrees.
	/// </summary>
	/// <value>
	///   Azimuth angle between two meridians in degrees.
	/// </value>
	public float azimuthDeg {
		get {return 180.0f * azimuth / Mathf.PI;}
	}
#endregion

#region  Matrices
	/// <summary>
	///   World to aligned matrix.
	/// </summary>
	private Matrix4x4 _waMatrix = Matrix4x4.identity;

	/// <summary>
	///   Aligned to world matrix.
	/// </summary>
	private Matrix4x4 _awMatrix = Matrix4x4.identity;

	/// <summary>
	///   Grid to spheric matrix.
	/// </summary>
	private Matrix4x4 _gsMatrix = Matrix4x4.identity;

	/// <summary>
	///   Spheric to grid matrix.
	/// </summary>
	private Matrix4x4 _sgMatrix = Matrix4x4.identity;

	/// <summary>
	///   Reads the value of the World->Aligned matrix (read only).
	/// </summary>
	///
	/// <value>
	///   The World->Aligned matrix.
	/// </value>
	///
	/// <remarks>
	///   <para>
	///     If the Matrices have to be updated they will do so first.
	///   </para>
	/// </remarks>
	private Matrix4x4 waMatrix {
		get {MatricesUpdate(); return _waMatrix;}
	}

	/// <summary>
	///   Reads the value of the Aligned->World matrix (read only).
	/// </summary>
	/// <value>
	///   The Aligned->World matrix.
	/// </value>
	/// <remarks>
	///   <para>
	///     If the Matrices have to be updated they will do so first.
	///   </para>
	/// </remarks>
	private Matrix4x4 awMatrix {
		get {MatricesUpdate(); return _awMatrix;}
	}

	/// <summary>
	///   Reads the value of the Grid->Spheric matrix (read only).
	/// </summary>
	/// <value>
	///   The Grid->Spheric matrix.
	/// </value>
	/// <remarks>
	///   <para>
	///     If the Matrices have to be updated they will do so first.
	///   </para>
	/// </remarks>
	private Matrix4x4 gsMatrix {
		get {MatricesUpdate(); return _gsMatrix;}
	}

	/// <summary>
	///   Reads the value of the Spheric->Grid matrix (read only).
	/// </summary>
	/// <value>
	///   The Spheric->Grid matrix.
	/// </value>
	/// <remarks>
	///   <para>
	///     If the Matrices have to be updated they will do so first.
	///   </para>
	/// </remarks>
	private Matrix4x4 sgMatrix {
		get {MatricesUpdate(); return _sgMatrix;}
	}

	/// <summary>
	///   Update all the matrices for coordinate conversion when necessary.
	/// </summary>
	protected override void MatricesUpdate() {
		if (_TransformNeedsUpdate() || _matricesMustUpdate) {
			_awMatrix = Matrix4x4.TRS(_Transform.position, _Transform.rotation, Vector3.one);
			_awMatrix = _awMatrix * Matrix4x4.TRS(originOffset, Quaternion.identity, Vector3.one);
			_waMatrix = _awMatrix.inverse;

			var spacing = new Vector3(radius, polar, azimuth);
			_gsMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, spacing);
			_sgMatrix = _gsMatrix.inverse;

			_matricesMustUpdate = false;
		}
	}
#endregion


#region  World coordinate system
	/// <summary>
	///   Converts a point from spheric- to world coordinates.
	/// </summary>
	///
	/// <param name="spheric">
	///   Spherical coordinates of the point to convert.
	/// </param>
	///
	/// <remarks>
	///   <para>
	///     Takes in a position in world space and computes where in the sphere
	///     that position is. The origin of the grid is the world position of
	///     its <c>GameObject</c> and its axes lie on the corresponding axes of
	///     the Transform.  Rotation is taken into account for this operation.
	///   </para>
	/// </remarks>
	public Vector3 SphericToWorld(Vector3 spheric) {
		// Aligned point
		// r * (sin(theta) sin(phi), cos(theta), sin(theta) cos(phi))
		spheric.z = 2.0f * Mathf.PI - spheric.z;
		var a = new Vector3(Mathf.Sin(spheric.y) * Mathf.Sin(spheric.z),
		                    Mathf.Cos(spheric.y)                       ,
		                    Mathf.Sin(spheric.y) * Mathf.Cos(spheric.z)
		                   );
		a *= spheric.x;
		return awMatrix.MultiplyPoint3x4(a);
	}

	/// <summary>
	///   Converts a point from geographic- to world coordinates.
	/// </summary>
	///
	/// <param name="geographic">
	///   Geographic coordinates of the point to convert.
	/// </param>
	///
	/// <remarks>
	///   <para>
	///     Takes in a position in geographic space and computes where in the
	///     sphere that position is. The origin of the grid is the world
	///     position of its <c>GameObject</c> and its axes lie on the
	///     corresponding axes of the Transform.  Rotation is taken into
	///     account for this operation.
	///   </para>
	/// </remarks>
	public Vector3 GeographicToWorld(Vector3 geographic) {
		var s = GeographicToSpheric(geographic);
		return SphericToWorld(s);
	}

	/// <summary>
	///   Converts a point from geographic- to world coordinates.
	/// </summary>
	///
	/// <param name="grid">
	///   Grid coordinates of the point to convert.
	/// </param>
	///
	/// <remarks>
	///   <para>
	///     Takes in a position in grid space and computes where in the sphere
	///     that position is. The origin of the grid is the world position of
	///     its <c>GameObject</c> and its axes lie on the corresponding axes of
	///     the Transform.  Rotation is taken into account for this operation.
	///   </para>
	/// </remarks>
	public override Vector3 GridToWorld(Vector3 grid) {
		return SphericToWorld(GridToSpheric(grid));
	}
#endregion

#region  Spheric coordinate system
	/// <summary>
	///   Converts a point from world- to spheric coordinates.
	/// </summary>
	///
	/// <param name="world">
	///   World coordinates of the point to convert.
	/// </param>
	///
	/// <remarks>
	///   <para>
	///     Takes in a position in world space and computes where in the sphere
	///     that position is. The origin of the grid is the world position of
	///     its <c>GameObject</c> and its axes lie on the corresponding axes of
	///     the Transform. Rotation is taken into account for this operation.
	///   </para>
	/// </remarks>
	public Vector3 WorldToSpheric(Vector3 world) {
		// Aligned point
		var a = waMatrix.MultiplyPoint3x4(world);

		var r     = a.magnitude;
		var theta = r != 0.0f ? Mathf.Acos(a.y / r) : 0.0f;
		var phi   = Mathf.Atan2(a.x , a.z);

		if (phi < 0) {
			phi = 2*Mathf.PI + phi;
		}
		phi = 2.0f * Mathf.PI - phi;

		return new Vector3(r, theta, phi);
	}

	/// <summary>
	///   Converts a point from geographic- to spheric coordinates.
	/// </summary>
	///
	/// <param name="geographic">
	///   World coordinates of the point to convert.
	/// </param>
	///
	/// <remarks>
	///   <para>
	///     Takes in a position in geographic space and computes where in the
	///     sphere that position is. The origin of the grid is the world
	///     position of its <c>GameObject</c> and its axes lie on the
	///     corresponding axes of the Transform. Rotation is taken into account
	///     for this operation.
	///   </para>
	/// </remarks>
	public Vector3 GeographicToSpheric(Vector3 geographic) {
		geographic.x += radius;
		geographic.y = Mathf.PI / 2.0f - geographic.y;
		if (geographic.z < 0.0f) {
			geographic.z = 2.0f * Mathf.PI + geographic.z;
		}
		return geographic;
	}

	/// <summary>
	///   Converts a point from grid- to spheric coordinates.
	/// </summary>
	///
	/// <param name="grid">
	///   Grid coordinates of the point to convert.
	/// </param>
	///
	/// <remarks>
	///   <para>
	///     Takes in a position in grid space and computes where in the sphere
	///     that position is. The origin of the grid is the world position of
	///     its <c>GameObject</c> and its axes lie on the corresponding axes of
	///     the Transform. Rotation is taken into account for this operation.
	///   </para>
	/// </remarks>
	public Vector3 GridToSpheric(Vector3 grid) {
		return gsMatrix.MultiplyPoint3x4(grid);
	}
#endregion

#region  Geographic coordinate conversion
	/// <summary>
	///   Converts a point from world- to geometric coordinates.
	/// </summary>
	///
	/// <param name="world">
	///   World coordinates of the point to convert.
	/// </param>
	///
	/// <remarks>
	///   <para>
	///     Takes in a position in world space and computes where on the sphere
	///     that position is. The origin of the grid is the world position of
	///     its <c>GameObject</c> and its axes lie on the corresponding axes of
	///     the Transform. Rotation is taken into account for this operation.
	///   </para>
	/// </remarks>
	public Vector3 WorldToGeographic(Vector3 world) {
		var s = WorldToSpheric(world);
		return SphericToGeographic(s);
	}

	/// <summary>
	///   Converts a point from spheric- to geometric coordinates.
	/// </summary>
	///
	/// <param name="spheric">
	///   Spheric coordinates of the point to convert.
	/// </param>
	///
	/// <remarks>
	///   <para>
	///     Takes in a position in spheric space and computes where on the
	///     sphere that position is. The origin of the grid is the world
	///     position of its <c>GameObject</c> and its axes lie on the
	///     corresponding axes of the Transform. Rotation is taken into account
	///     for this operation.
	///   </para>
	/// </remarks>
	public Vector3 SphericToGeographic(Vector3 spheric) {
		spheric.x -= radius;
		spheric.y = Mathf.PI / 2.0f - spheric.y;
		if (spheric.z > Mathf.PI) {
			spheric.z  = -1.0f * (2.0f * Mathf.PI - spheric.z);
		}

		return spheric;
	}

	/// <summary>
	///   Converts a point from grid- to geometric coordinates.
	/// </summary>
	///
	/// <param name="grid">
	///   Grid coordinates of the point to convert.
	/// </param>
	///
	/// <remarks>
	///   <para>
	///     Takes in a position in grid space and computes where on the sphere
	///     that position is. The origin of the grid is the world position of
	///     its <c>GameObject</c> and its axes lie on the corresponding axes of
	///     the Transform. Rotation is taken into account for this operation.
	///   </para>
	/// </remarks>
	public Vector3 GridToGeographic(Vector3 grid) {
		Vector3 s = GridToSpheric(grid);
		return SphericToGeographic(s);
	}
#endregion

#region  Grid coordinate system
	/// <summary>
	///   Converts a point from world- to grid coordinates.
	/// </summary>
	///
	/// <param name="world">
	///   World coordinates of the point to convert.
	/// </param>
	///
	/// <remarks>
	///   <para>
	///     Takes in a position in world space and computes where in the grid
	///     that position is. The origin of the grid is the world position of
	///     its <c>GameObject</c> and its axes lie on the corresponding axes of
	///     the Transform. Rotation is taken into account for this operation.
	///   </para>
	/// </remarks>
	public override Vector3 WorldToGrid(Vector3 world) {
		var s = WorldToSpheric(world);
		return SphericToGrid(s);
	}

	/// <summary>
	///   Converts a point from spheric- to grid coordinates.
	/// </summary>
	///
	/// <param name="spheric">
	///   Spheric coordinates of the point to convert.
	/// </param>
	///
	/// <remarks>
	///   <para>
	///     Takes in a position in spheric space and computes where in the grid
	///     that position is. The origin of the grid is the world position of
	///     its <c>GameObject</c> and its axes lie on the corresponding axes of
	///     the Transform. Rotation is taken into account for this operation.
	///   </para>
	/// </remarks>
	public Vector3 SphericToGrid(Vector3 spheric) {
		return sgMatrix.MultiplyPoint3x4(spheric);
	}

	/// <summary>
	///   Converts a point from geographic- to grid coordinates.
	/// </summary>
	///
	/// <param name="geographic">
	///   Geographic coordinates of the point to convert.
	/// </param>
	///
	/// <remarks>
	///   <para>
	///     Takes in a position in geographic space and computes where in the
	///     grid that position is. The origin of the grid is the world position
	///     of its <c>GameObject</c> and its axes lie on the corresponding axes
	///     of the Transform. Rotation is taken into account for this
	///     operation.
	///   </para>
	/// </remarks>
	public Vector3 GeographicToGrid(Vector3 geographic) {
		Vector3 s = GeographicToSpheric(geographic);
		return SphericToGrid(s);
	}
#endregion


#region  Nearest in world space
	/// <summary>Returns the world position of the nearest vertex.</summary>
	/// <returns>World position of the nearest face.</returns>
	/// <param name="worldPoint">Point in world space.</param>
	/// <param name="doDebug">Whether to draw a small debug sphere at the vertex.</param>
	/// 
	/// Returns the world position of the nearest vertex from a given point in
	/// world space. If <c>doDebug</c> is set a small gizmo sphere will be
	/// drawn at the vertex position.
	public override Vector3 NearestVertexW(Vector3 worldPoint, bool doDebug) {
		if (doDebug) {
			Gizmos.DrawSphere(GridToWorld(NearestVertexG(worldPoint)), 0.05f);
		}
		return GridToWorld(NearestVertexG(worldPoint));
	}

	/// <summary>Returns the world position of the nearest face.</summary>
	/// <returns>World position of the nearest face.</returns>
	/// <param name="worldPoint">Point in world space.</param>
	/// <param name="plane">Plane on which the face lies.</param>
	/// <param name="doDebug">Whether to draw a small debug sphere at the vertex.</param>
	/// 
	/// Similar to <see cref="NearestVertexW"/>, it returns the world
	/// coordinates of a face on the grid. Since the face is enclosed by four
	/// vertices, the returned value is the point in between all four of the
	/// vertices. You also need to specify on which plane the face lies. If
	/// <c>doDebug</c> is set a small gizmo face will drawn inside the face.
	public override Vector3 NearestFaceW(Vector3 worldPoint, GridPlane plane, bool doDebug) {
		//debugging
		if (doDebug) {
			Vector3 debugCube = 0.05f * Vector3.one;
			debugCube[(int)plane] = 0.0f;
			
			//store the old matrix and create a new one based on the grid's roation and the point's position
			Matrix4x4 oldRotationMatrix = Gizmos.matrix;
			//Matrix4x4 newRotationMatrix = Matrix4x4.TRS(toPoint, transform.rotation, Vector3.one);
			Matrix4x4 newRotationMatrix = Matrix4x4.TRS(GridToWorld(NearestFaceG(worldPoint, plane)), transform.rotation, Vector3.one);
			
			Gizmos.matrix = newRotationMatrix;
			Gizmos.DrawCube(Vector3.zero, debugCube);//Position zero because the matrix already contains the point
			Gizmos.matrix = oldRotationMatrix;
		}
		
		//return toPoint;
		return GridToWorld(NearestFaceG(worldPoint, plane));
	}

	/// <summary>Returns the world position of the nearest box.</summary>
	/// <returns>World position of the nearest box.</returns>
	/// <param name="worldPoint">Point in world space.</param>
	/// <param name="doDebug">Whether to draw a small debug sphere at the box.</param>
	/// 
	/// Similar to <see cref="NearestVertexW"/>, it returns the world
	/// coordinates of a box in the grid. Since the box is enclosed by eight
	/// vertices, the returned value is the point in between all eight of them.
	/// If <c>doDebug</c> is set a small gizmo box will drawn inside the box.
	public override Vector3 NearestBoxW(Vector3 worldPoint, bool doDebug) {		
		if (doDebug) {
			//store the old matrix and create a new one based on the grid's roation and the point's position
			Matrix4x4 oldRotationMatrix = Gizmos.matrix;
			//Matrix4x4 newRotationMatrix = Matrix4x4.TRS(toPoint, transform.rotation, Vector3.one);
			Matrix4x4 newRotationMatrix = Matrix4x4.TRS(GridToWorld(NearestBoxG(worldPoint)), transform.rotation, Vector3.one);
			
			
			Gizmos.matrix = newRotationMatrix;
			Gizmos.DrawCube(Vector3.zero, 0.05f * Vector3.one);
			Gizmos.matrix = oldRotationMatrix;
		}
		//convert back to world coordinates
		//return toPoint;
		return GridToWorld(NearestBoxG(worldPoint));
	}
#endregion

#region  Nearest in grid space
	/// <summary>Returns the grid position of the nearest vertex.</summary>
	/// <returns>Grid position of the nearest vertex.</returns>
	/// <param name="worldPoint">Point in world space.</param>
	/// 
	/// Similar to @c #NearestVertexW, except you get grid coordinates instead
	/// of world coordinates.
	public override Vector3 NearestVertexG(Vector3 worldPoint) {
		return RoundPoint(WorldToGrid(worldPoint));
	}

	/// <summary>Returns the grid position of the nearest face.</summary>
	/// <returns>Grid position of the nearest face.</returns>
	/// <param name="worldPoint">Point in world space.</param>
	/// <param name="plane">Plane on which the face lies.</param>
	/// 
	/// Similar to <see cref="NearestFaceW"/>, except you get grid coordinates
	/// instead of world coordinates. Since faces lie between vertices two
	/// values will always have +0.5 compared to vertex coordinates, while the
	/// values that lies on the plane will have a round number.
	/// <example>
	/// Example:
	/// <code>
	/// GFRectGrid myGrid;
	/// Vector3 worldPoint;
	/// Vector3 face = myGrid.NearestFaceG(worldPoint, GFGrid.GridPlane.XY); // something like (2.5, -1.5, 3)
	/// </code>
	/// </example>
	public override Vector3 NearestFaceG(Vector3 worldPoint, GridPlane plane) {
		return RoundPoint(WorldToGrid(worldPoint) - 0.5f * Vector3.one + 0.5f * Units[(int)plane]) + 0.5f * Vector3.one - 0.5f * Units[(int)plane];
	}

	/// <summary>Returns the grid position of the nearest box.</summary>
	/// <returns>Grid position of the nearest box.</returns>
	/// <param name="worldPoint">Point in world space.</param>
	/// 
	/// Similar to @c #NearestBoxW, except you get grid coordinates instead of
	/// world coordinates. Since faces lie between vertices all three values
	/// will always have +0.5 compared to vertex coordinates.
	/// <example>
	/// Example:
	/// <code>
	/// GFRectGrid myGrid;
	/// Vector3 worldPoint;
	/// Vector3 box = myGrid.NearestBoxG(worldPoint); // something like (2.5, -1.5, 3.5)
	/// </code>
	/// </example>
	public override Vector3 NearestBoxG(Vector3 worldPoint) {
		return RoundPoint(WorldToGrid(worldPoint) - 0.5f * Vector3.one) + 0.5f * Vector3.one;
	}
#endregion

#region  AlignScaleMethods
	/// <summary>
	///   Fits a position vector into the grid.
	/// </summary>
	///
	/// <returns>
	///   Aligned position vector.
	/// </returns>
	///
	/// <param name="pos">
	///   The position to align.
	/// </param>
	/// <param name="scale">
	///   A simulated scale to decide how exactly to fit the poistion into the
	///   grid.
	/// </param>
	/// <param name="ignoreAxis">
	///   Which axes should be ignored.
	/// </param>
	/// 
	/// <remarks>
	///   This method aligns a point to the grid. The *scale* parameter is
	///   needed for legacy API compliance and is ignored. The <c>lockAxis</c>
	///   parameter lets you ignore individual axes.
	/// </remarks>
	public override Vector3 AlignVector3(Vector3 pos, Vector3 scale, BoolVector3 ignoreAxis) {

		var vertex = NearestVertexG(pos);
		var box    = NearestBoxG(pos);
		var grid   = WorldToGrid(pos);
		var final  = vertex;

		for (int i = 0; i < 3; ++i) {
			if (Mathf.Abs(grid[i] - box[i]) < Mathf.Abs(grid[i] - vertex[i])) {
				final[i] = box[i];
			}
		}

		final = GridToWorld(final);
		for (int i = 0; i <= 2; i++) {
			final[i] = ignoreAxis[i] ? pos[i] : final[i];
		}

		return final;
	}

	/// <summary>
	///   Stub, doesn't do anything.
	/// </summary>
	/// <returns>
	///   The same vector.
	/// </returns>
	/// <param name="scl">
	///   The vector to scale.
	/// </param>
	/// <param name="ignoreAxis">
	///   The axes to ignore.
	/// </param>
	/// 
	/// <remarks>
	///   This function *would* return the scales vector, but in a spheric grid
	///   were nothing is constant this idea does not make sense. It's a
	///   leftover from earlier versions of Grid Framework and is only included
	///   for API compliance.
	/// </remarks>
	public override Vector3 ScaleVector3(Vector3 scl, BoolVector3 ignoreAxis) {
		return scl;
	}
#endregion


#region  Calculate draw points
	protected override void DrawPointsCount(ref int countX, ref int countY, ref int countZ, ref Vector3 from, ref Vector3 to, bool condition = true) {
		if (!condition) {
			return;
		}

		// Convert to grid coordinates for easier calculation of amounts.
		if(!relativeSize) {
			from[0] /= radius  ; to[0] /= radius  ;
			from[1] /= polar   ; to[1] /= polar   ;
			from[2] /= azimuth ; to[2] /= azimuth ;
		}

		// Adjust for non-custom range: the start radius and angle have to be 0,
		// the end angle has to be slightly less than 2π.
		if (!useCustomRenderRange) {
			from[0] = 0.0f;
			from[1] = 0.0f;

			// prevent the connecting parallels from being drawn twice
			from[2] = 0.0f; to[2] = Mathf.Min(to[2], meridians + 0 - 0.00001f);
		}

		// If the from angle is greater than the to angle wrap the to angle
		// around once.
		if (from[2] > to[2]) {
			to[2] += meridians;
		}

		// Deltas for spheres, parallels and meridians
		var deltaS = Mathf.FloorToInt(to[0]        ) - Mathf.CeilToInt(from[0]        ) + 1;
		var deltaP = Mathf.FloorToInt(to[1]*smoothM) - Mathf.CeilToInt(from[1]*smoothM) + 1;
		var deltaM = Mathf.FloorToInt(to[2]*smoothP) - Mathf.CeilToInt(from[2]*smoothP) + 1;

		countX = deltaM *  deltaP + 2;
		countY = deltaS * (deltaM + 2) * (deltaP + 1) * smoothM;
		countZ = deltaS * (deltaM + 1) * (deltaP + 2) * smoothP;
	}

	protected override void DrawPointsCalculate(Vector3[][][] points, ref int[] amount, Vector3 from, Vector3 to) {
		// Origin
		var o = _Transform.position;

		int i;  // Counter through the arrays
		int firstS = Mathf.CeilToInt(from[0]), lastS = Mathf.FloorToInt(to[0]);
		int firstP = Mathf.CeilToInt(from[1]), lastP = Mathf.FloorToInt(to[1]);
		int firstM = Mathf.CeilToInt(from[2]), lastM = Mathf.FloorToInt(to[2]);

		// -- DRAWING THE SPOKES --

		i = 0;
		for (int p = firstP; p <= lastP; ++p) {
			var v_p = Quaternion.AngleAxis(-p * polarDeg, -_Transform.right) * _Transform.up;
			for (int m = firstM; m <= lastM; ++m) {
				if (p == 0 || p == parallels - 1) {
					continue;
				}
				var v_m = Quaternion.AngleAxis(m * azimuthDeg, -_Transform.up) * v_p;
				points[0][i][0] = o + v_m * from[0] * radius;
				points[0][i][1] = o + v_m *   to[0] * radius;
				++i;
			}
		}
		// Lines at the poles
		if (lastP == parallels - 1) { // South
			points[0][i][0] = o - _Transform.up * from[0] * radius;
			points[0][i][1] = o - _Transform.up *   to[0] * radius;
			++i;
		}
		if (firstP == 0) { // North
			points[0][i][0] = o + _Transform.up * from[0] * radius;
			points[0][i][1] = o + _Transform.up *   to[0] * radius;
			++i;
		}


		// -- DRAWING THE MERIDIANS --
		firstS = Mathf.Max(firstS, 1);

		firstP = Mathf.CeilToInt(from[1]*smoothM);
		lastP  = Mathf.FloorToInt(to[1]*smoothM);

		i = 0;
		for (int s = firstS; s <= lastS; ++s) {
			// Sphere construction vector
			var v_s = _Transform.forward * s * radius;

			for (int m = firstM; m <= lastM; ++m) {
				// Meridian construction vector
				var v_m = Quaternion.AngleAxis(m * azimuthDeg, -_Transform.up) * v_s;
				var axis = Vector3.Cross(v_m, _Transform.up);
				points[1][i][0] = o + Quaternion.AngleAxis(90.0f - from[1] * polarDeg, axis) * v_m;
				for (int p = firstP; p <= lastP; ++p) {
					var v_p = Quaternion.AngleAxis(90.0f - p * polarDeg/smoothM, axis) * v_m;
					points[1][i++][1] = o + v_p;
					points[1][i  ][0] = points[1][i-1][1];
				}
				points[1][i++][1] = o + Quaternion.AngleAxis(90.0f - to[1] * polarDeg, axis) * v_m;
			}
		}

		// -- DRAWING THE PARALLELS -- : A parallel is like  the circle of a
		// polar grid, the angle between segments is the azimuth of the grid.

		firstP = Mathf.Max(Mathf.CeilToInt( from[1]), 1);
		lastP  = Mathf.Min(Mathf.FloorToInt(  to[1]),  parallels-1);

		firstM = Mathf.CeilToInt(from[2]*smoothP);
		lastM  = Mathf.FloorToInt( to[2]*smoothP);

		i = 0;
		for (int s = firstS; s <= lastS; ++s) {
			// Sphere construction vector
			var v_s = _Transform.up * s * radius;

			for (int p = firstP; p <= lastP; ++p) {
				// Parallel construction vector
				var v_p = Quaternion.AngleAxis(p * polarDeg, _Transform.right) * v_s;
				points[2][i][0] = o + Quaternion.AngleAxis(from[2] * azimuthDeg, -_Transform.up) * v_p;
				for (int m = firstM; m <= lastM; ++m) {
					var v_m = Quaternion.AngleAxis(m * azimuthDeg/smoothP, -_Transform.up) * v_p;
					points[2][i++][1] = o + v_m;
					points[2][i][0] = points[2][i-1][1];
				}
				points[2][i++][1] = o + Quaternion.AngleAxis(to[2]* azimuthDeg, -_Transform.up) * v_p;
			}
		}
	}
#endregion


#region  Helper methods
	private Vector3 RoundPoint(Vector3 point) {
		return RoundPoint(point, Vector3.one);
	}

	private Vector3 RoundPoint(Vector3 point, Vector3 multi) {
		for (int i = 0; i < 3; i++) {
			point[i] = RoundMultiple(point[i], multi[i]);
		}
		return point;
	}
#endregion
}

