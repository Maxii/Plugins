using UnityEngine;
using GridFramework;
using GridFramework.Vectors;

/// <summary>A polar grid based on cylindrical coordinates.</summary>
/// 
/// A grid based on cylindrical coordinates. The caracterizing values are <see cref="radius"/>, <see cref="sectors"/> and <see cref="depth"/>. The angle values are
/// derived from <see cref="sectors"/> and we use radians internally. The coordinate systems used are either a grid-based coordinate system based on the defining values
/// or a regular cylindrical coordinate system. If you want polar coordinates just ignore the height component of the cylindrical coordinates.
/// 
/// It is important to note that the components of polar and grid coordinates represent the radius, radian angle and height. Which component represents what depends on
/// the gridPlane. The user manual has a handy table for that purpose.
/// 
/// The members <see cref="size"/>, <see cref="renderFrom"/> and <see cref="renderTo"/> are inherited from <see cref="GFGrid"/>, but slightly different. The first
/// component of all three cannot be lower than 0 and in the case of <see cref="renderFrom"/> it cannot be larger than the first component of @c #renderTo, and vice-versa.
/// The second component is an angle in radians and wraps around as described in the user manual, no other restrictions. The third component is the height, it’s bounded
/// from below by 0.01 and in the case of <see cref="renderFrom"/> it cannot be larger than <see cref="renderTo"/> and vice-versa.
public class GFPolarGrid : GFLayeredGrid {
	#region class members
	#region overriding inherited accessors
	/// <summary>Overrides the size of GFGrid to make sure the angular coordinate is clamped appropriately between 0 and 2π (360°) or 0 and <see cref="sectors"/>.</summary>
	/// <value>The size of the grid's visual representation.</value>
	/// <see cref="relativeSize"></see>
	/// <see cref="useCustomRenderRange"></see>
	/// Aside from the additional constraint for the angular value the same rules apply as for the base class, meaning no component can be less than 0.
	public override Vector3 size {
		get{ return _size;}
		set {
			base.size = value;
			_size[idx[1]] = Mathf.Max(0.0f, Mathf.Min(value[idx[1]], relativeSize ? sectors : 2.0f * Mathf.PI));
		}
	}

	/// <summary>Overrides the property of GFGrid to make sure that the radial value is positive and the angular value is wrapped around properly.</summary>
	/// <value>Custom lower limit for drawing and rendering.</value>
	/// <see cref="relativeSize"></see>
	/// <see cref="useCustomRenderRange"></see>
	/// <see cref="renderTo"/>
	/// Aside from the additional constraint for the radial and angular value the same rules apply as for the base class, meeaning the vector cannot be greater than
	/// <see cref="renderTo"/>.
	public override Vector3 renderFrom {
		get{ return _renderFrom;}
		set {
			base.renderFrom = value;
			_renderFrom[idx[0]] = Mathf.Max(_renderFrom[idx[0]], 0); // prevent negative value
			_renderFrom[idx[1]] = relativeSize ? Float2Sector(value[idx[1]]) : Float2Rad(value[idx[1]]); // convert to sector or angle (wrap around and handle < 0)
		}
	}

	/// <summary>Overrides the property of GFGrid to make sure that the angular value is wrapped around properly.</summary>
	/// <value>Custom upper limit for drawing and rendering.</value>
	/// <see cref="relativeSize"></see>
	/// <see cref="useCustomRenderRange"></see>
	/// <see cref="renderFrom"/>
	/// Aside from the additional constraint for the angular value the same rules apply as for the base class, meeaning the vector cannot be lower than
	/// <see cref="renderFrom"/>.
	public override Vector3 renderTo {
		get{ return _renderTo;}
		set {
			base.renderTo = value;
			_renderTo[idx[1]] = relativeSize ? Float2Sector(value[idx[1]]) : Float2Rad(value[idx[1]]); // convert to sector or angle (wrap around and handle < 0)
		}
	}
	#endregion
	
	#region members
	[SerializeField]
	private float _radius = 1;
	
	/// <summary>The radius of the inner-most circle of the grid.</summary>
	/// <value>The radius.</value>
	/// The radius of the innermost circle and how far apart the other circles are. The value cannot go below 0.01.
	public float radius {
		get{ return _radius;}
		set {
			if (value == _radius) {// needed because the editor fires the setter even if this wasn't changed
				return;
			}
			_radius = Mathf.Max(0.01f, value);
			_matricesMustUpdate = true;
			_drawPointsMustUpdate = true;
			_drawPointsCountMustUpdate |= !relativeSize; // if size is relative, the amount of points does not change
			GridChanged();
		}
	}
	
	[SerializeField]
	private int	_sectors = 8;
	
	/// <summary>The amount of sectors per circle.</summary>
	/// <value>Ampunt of sectors.</value>
	/// The amount of sectors the circles are divided into. The minimum values is 1, which means one full circle.
	public int sectors {
		get {return _sectors;}
		set {
			if (value == _sectors) {// needed because the editor fires the setter even if this wasn't changed
				return;
			}
			_sectors = Mathf.Max(1, value);
			_matricesMustUpdate = true;
			_drawPointsMustUpdate = true;
			_drawPointsCountMustUpdate = true;
			GridChanged();
		}
	}
			
	// the amount of segments within a segment, more looks smoother
	[SerializeField]
	private int _smoothness = 5;
	
	/// <summary>Divides the sectors to create a smoother look.</summary>
	/// <value>Smoothness of the grid segments.</value>
	/// Unity's GL class can only draw straight lines, so in order to get the sectors to look round this value breaks each sector into smaller sectors. The number of
	/// smoothness tells how many segments the circular line has been broken into. The amount of end points used is smoothness + 1, because we count both edges of the
	/// sector.
	public int smoothness {
		get {return _smoothness;}
		set {
			if (value == _smoothness) {// needed because the editor fires the setter even if this wasn't changed
				return;
			}
			_smoothness = Mathf.Max(1, value);
			_drawPointsMustUpdate = true;
			_drawPointsCountMustUpdate = true;
		}
	}
	#endregion

	#region Matrices
	// <summary>Matrix that transforms from world to local.</summary>
	private Matrix4x4 _wlMatrix = Matrix4x4.identity;
	/// <summary>Matrix that transforms from local to world.</summary>
	private Matrix4x4 _lwMatrix = Matrix4x4.identity;

	/// @internal<summary>Matrix that converts from polar coordinates to grid coordinates.</summary>
	private Matrix4x4 _pgMatrix = Matrix4x4.identity;
	/// @internal<summary>Matrix that converts from grid coordinates to polar coordinates.</summary>
	private Matrix4x4 _gpMatrix = Matrix4x4.identity;

	protected override void MatricesUpdate() {
		if (!_matricesMustUpdate && !_TransformNeedsUpdate()) {
			return;
		}

		Vector3 scale = new Vector3();
		scale[idx[0]] = angle;
		scale[idx[1]] = radius;
		scale[idx[2]] = depth;

		_gpMatrix.SetTRS(Vector3.zero, Quaternion.identity, scale);
		_pgMatrix = _gpMatrix.inverse;
		_matricesMustUpdate = false;

		_lwMatrix.SetTRS(_Transform.position, _Transform.rotation, Vector3.one);
		_lwMatrix *= Matrix4x4.TRS(originOffset, Quaternion.identity, Vector3.one);
		_wlMatrix = _lwMatrix.inverse;
	}

	protected Matrix4x4 gpMatrix {
		get {
			MatricesUpdate();
			return _gpMatrix;
		}
	}

	protected Matrix4x4 pgMatrix {
		get {
			MatricesUpdate();
			return _pgMatrix;
		}
	}

	private Matrix4x4 wlMatrix {
		get {
			MatricesUpdate();
			return _wlMatrix;
		}
	}
	
	private Matrix4x4 lwMatrix {
		get {
			MatricesUpdate();
			return _lwMatrix;
		}
	}
	#endregion
	
	#region helper values (read only)
	/// <summary>The angle of a sector in radians.</summary>
	/// This is a read-only value derived from @c #sectors. It gives you the angle within a sector in radians and it’s a shorthand writing for
	/// <code>(2.0f * Mathf.PI) / sectors</code>
	public float angle { get { return (2 * Mathf.PI) / sectors; } }
	
	/// <summary>The angle of a sector in degrees.</summary>
	/// The same as @c #angle except in degrees, it’s a shorthand writing for
	/// <code>360.0f / sectors</code>
	public float angleDeg { get { return 360.0f / sectors; } }
	#endregion

	#region Drawing helper values
	/// <summary>Number of red arcs (circles).</summary>
	private int arc_count;
	/// <summary>Number of red segments (+2 for over- and underflow).</summary>
	private int segment_count;
	/// <summary>Number of green radial lines.</summary>
	private int sector_count;
	/// <summary>Number of layers.</summary>
	private int layer_count;
	#endregion
	#endregion
		
	#region Grid <-> World coordinate transformation
	/// <summary>Converts from world to grid coordinates.</summary>
	/// <returns>Grid coordinates of the world point.</returns>
	/// <param name="worldPoint">Point in world space.</param>
	/// 
	/// Converts a point from world space to grid space. The first coordinate represents the distance from the radial axis as multiples of <see cref="radius"/>, the
	/// second one the sector and the thrid one the distance from the main plane as multiples of <see cref="depth"/>. This order applies to XY-grids only, for the other
	/// two orientations please consult the manual.
	public override Vector3 WorldToGrid(Vector3 worldPoint) {
		return PolarToGrid(WorldToPolar(worldPoint));
	}
	
	/// <summary>Converts from grid to world coordinates.</summary>
	/// <returns>World coordinates of the grid point.</returns>
	/// <param name="gridPoint">Point in grid space.</param>
	/// 
	/// Converts a point from grid space to world space.
	public override Vector3 GridToWorld(Vector3 gridPoint) {
		return PolarToWorld(GridToPolar(gridPoint));
	}
	#endregion
	
	#region Polar <-> World coordinate transformation
	/// <summary>Converts from world to polar coordinates.</summary>
	/// <returns>Point in polar space.</returns>
	/// <param name="worldPoint">Point in world space.</param>
	/// 
	/// Converts a point from world space to polar space. The first coordinate represents the distance from the radial axis, the second one the angle in radians and the
	/// thrid one the distance from the main plane. This order applies to XY-grids only, for the other two orientations please consult the manual.
	public Vector3 WorldToPolar(Vector3 worldPoint) {
		// first transform the point into local coordinates
		Vector3 localPoint = _Transform.GFInverseTransformPointFixed(worldPoint) - originOffset;
		// now turn the point from Cartesian coordinates into polar coordinates
		return Mathf.Sqrt(Mathf.Pow(localPoint[idx[0]], 2) + Mathf.Pow(localPoint[idx[1]], 2)) * units[idx[0]]
			+ Atan3(localPoint[idx[1]], localPoint[idx[0]]) * units[idx[1]]
			+ localPoint[idx[2]] * units[idx[2]];
	}
	
	/// <summary>Converts from polar to world coordinates.</summary>
	/// <returns>Point in world space.</returns>
	/// <param name="polarPoint">Point in polar space.</param>
	/// 
	/// Converts a point from polar space to world space.
	public Vector3 PolarToWorld(Vector3 polarPoint) {
		return _Transform.GFTransformPointFixed(
			polarPoint[idx[0]] * Mathf.Cos(Float2Rad(polarPoint[idx[1]])) * units[idx[0]]
			+ polarPoint[idx[0]] * Mathf.Sin(Float2Rad(polarPoint[idx[1]])) * units[idx[1]]
			+ polarPoint[idx[2]] * units[idx[2]])
			+ _Transform.TransformDirection(originOffset);
	}
	#endregion
	
	#region Grid <-> Polar coordinate transformation
	/// <summary>Converts a point from grid to polar space.</summary>
	/// <returns>Point in polar space.</returns>
	/// <param name="gridPoint">Point in grid space.</param>
	/// 
	/// Converts a point from grid to polar space. The main difference is that grid coordinates are dependent on the grid's parameters, while polar coordinates are not.
	public Vector3 GridToPolar(Vector3 gridPoint) {
		gridPoint[idx[1]] = Float2Sector(gridPoint[idx[1]]);
		Vector3 polar = Vector3.Scale(gridPoint, radius * units[idx[0]] + Float2Rad(angle) * units[idx[1]] + depth * units[idx[2]]);
		polar[idx[1]] = Float2Rad(polar[idx[1]]);
		return polar;
	}
	
	/// <summary>Converts a point from polar to grid space.</summary>
	/// <returns>Point in grid space.</returns>
	/// <param name="polarPoint">Point in polar space.</param>
	/// 
	/// Converts a point from polar to grid space. The main difference is that grid coordinates are dependent on the grid's parameters, while polar coordinates are not.
	public Vector3 PolarToGrid(Vector3 polarPoint) {
		return polarPoint.GFReverseScale(radius * units[idx[0]] + Float2Rad(angle) * units[idx[1]] + depth * units[idx[2]]);
	}
	#endregion

	#region Conversions
	#region Publicz
	/// <summary>Converts an angle (radians or degree) to the corresponding sector coordinate.</summary>
	/// <returns>Sector value of the angle.</returns>
	/// <param name="angle">Angle in either radians or degress.</param>
	/// <param name="mode">The mode of the angle, defaults to radians.</param>
	/// 
	/// This method takes in an angle and returns in which sector the angle lies. If the angle exceeds 2π or 360° it wraps around, nagetive angles are automatically
	/// subtracted from 2π or 360°.
	/// 
	/// <example>Let's take a grid with six sectors for example, then one sector has an agle of 360° / 6 = 60°, so a 135° angle corresponds to a sector value of 130° /
	/// 60° = 2.25.</example>
	public float Angle2Sector(float angle, AngleMode mode = AngleMode.radians) {
		angle = Float2Rad(angle * (mode == AngleMode.degrees ? Mathf.Deg2Rad : 1.0f));
		return angle / this.angle * (mode == AngleMode.degrees ? Mathf.Rad2Deg : 1.0f);
	}
	
	/// <summary>Converts a sector to the corresponding angle coordinate (radians or degree).</summary>
	/// <returns>Angle value of the sector.</returns>
	/// <param name="sector">Sector number.</param>
	/// <param name="mode">The mode of the angle, defaults to radians.</param>
	/// 
	/// This method takes in a sector coordinate and returns the corresponding angle around the origin. If the sector exceeds the amount of sectors of the grid it wraps
	/// around, nagetive sctors are automatically subtracted from the maximum.
	/// 
	/// <example>Let's take a grid with six sectors for example, then one sector has an agle of 360° / 6 = 60°, so a 2.25 sector corresponds to an angle of 2.25 * 60° =
	/// 135°.</example>
	public float Sector2Angle(float sector, AngleMode mode = AngleMode.radians) {
		sector = Float2Sector(sector);
		return sector * angle * (mode == AngleMode.degrees ? Mathf.Rad2Deg : 1.0f);
	}
	/// <summary>Converts an angle around the origin to a rotation.</summary>
	/// <returns>Rotation quaterion which rotates arround the origin by <paramref name="angle"/>.</returns>
	/// <param name="angle">Angle in either radians or degrees.</param>
	/// <param name="mode">The mode of the angle, defaults to radians.</param>
	/// 
	/// This method returns a quaternion which represents a rotation within the grid. The result is a combination of the grid's own rotation and the rotation from the
	/// angle. Since we use an angle, this method is more suitable for polar coordinates than grid coordinates. See <see cref="Sector2Rotation"/> for a similar method
	/// that uses sectors.
	public Quaternion Angle2Rotation(float angle, AngleMode mode = AngleMode.radians) {
		return Quaternion.AngleAxis(angle * (mode == AngleMode.radians ? Mathf.Rad2Deg : 1.0f), locUnits[idx[2]] * (gridPlane == GridPlane.XY ? 1.0f : -1.0f)) * _Transform.rotation;
	}

	/// <summary>Converts a sector around the origin to a rotation.</summary>
	/// <returns>Rotation quaterion which rotates arround the origin.</returns>
	/// <param name="sector">Sector coordinate inside the grid.</param>
	/// 
	/// This is basically the same as <see cref="Angle2Rotation"/>, excpet with sectors, which makes this method more suitable for grid coordinates than polar coordinates.
	public Quaternion Sector2Rotation(float sector) {
		return Angle2Rotation(Sector2Angle(sector));
	}

	/// <summary>Converts a world position to a rotation around the origin.</summary>
	/// <returns>The rotation.</returns>
	/// <param name="worldPoint">Point in world space.</param>
	/// 
	/// This method compares the point's position in world space to the grid and then returns which rotation an object should have if it was at that position and rotated
	/// around the grid.
	public Quaternion World2Rotation(Vector3 worldPoint) {
		return Angle2Rotation(World2Angle(worldPoint));
	}

	/// <summary>Converts a world position to an angle around the origin.</summary>
	/// <returns>Angle between the point and the grid's "right" axis.</returns>
	/// <param name="worldPoint">Point in world space.</param>
	/// <param name="mode">The mode of the angle, defaults to radians.</param>
	/// 
	/// This method returns which angle around the grid a given point in world space has.
	public float World2Angle(Vector3 worldPoint, AngleMode mode = AngleMode.radians) {
		return WorldToPolar(worldPoint)[idx[1]] * (mode == AngleMode.radians ? 1.0f : Mathf.Rad2Deg);
	}

	/// <summary>Converts a world position to the sector of the grid it is in.</summary>
	/// <returns>Sector the point is in.</returns>
	/// <param name="worldPoint">Point in world space.</param>
	/// 
	/// This method returns which which sector a given point in world space is in.
	public float World2Sector(Vector3 worldPoint) {
		return WorldToGrid(worldPoint)[idx[1]];
	}

	/// <summary>Converts a world position to the radius from the origin.</summary>
	/// <returns>Radius of the point from the grid.</returns>
	/// <param name="worldPoint">Point in world space.</param>
	/// 
	/// This method returns the distance of a world point from the grid's radial axis. This is not the same as the point's distance from the grid's origin, because it doesn't
	/// take "height" into account. Thus it is always less or equal than the distance from the origin.
	public float World2Radius(Vector3 worldPoint) {
		return WorldToPolar(worldPoint)[idx[0]];
	}
	#endregion
	#region Private
	/// @internal <summary>Interprets a float as radians; loops if value exceeds 2π and runs in reverse for negative values.</summary>
	private static float Float2Rad(float number) {
		return number >= 0 ? number % (2 * Mathf.PI) : 2 * Mathf.PI + (number % Mathf.PI);
	}
	/// @internal <summary>Interprets a float as degree; loops if value exceeds 360 and runs in reverse for negative values.</summary>
	private static float Float2Deg(float number) {
		return number >= 0 ? number % 360 : 360 + (number % 360);
	}
	/// @internal <summary>Interprets a float as sector; loops if value exceeds [sectors] and runs in reverse for negative values.</summary>
	private float Float2Sector(float number) {
		return number >= 0 ? number % sectors : sectors + (number % sectors);
	}
	#endregion
	#endregion
	
	#region nearest in world space
	/// <summary>Returns the world position of the nearest vertex.</summary>
	/// <returns>World position of the nearest vertex.</returns>
	/// <param name="worldPoint">Point in world space.</param>
	/// <param name="doDebug">If set to <c>true</c> do debug.</param>
	/// 
	/// Returns the world position of the nearest vertex from a given point in world space. If <paramref name="doDebug"/> is set a small gizmo sphere will be drawn at
	/// that position.
	public override Vector3 NearestVertexW(Vector3 worldPoint, bool doDebug) {
		Vector3 dest = PolarToWorld(NearestVertexP(worldPoint));
		if (doDebug) {
			DrawSphere(dest);
		}
		return dest;
	}

	/// <summary>Returns the world position of the nearest face.</summary>
	/// <returns>World position of the nearest face.</returns>
	/// <param name="world">Point in world space.</param>
	/// <param name="doDebug">If set to <c>true</c> draw a sphere at the destination.</param>
	/// 
	/// Similar to <see cref="NearestVertexW"/>, it returns the world coordinates of a face on the grid. Since the face is enclosed by several vertices, the returned
	/// value is the point in between all of the vertices. If <paramref name="doDebug"/> is set a small gizmo face will drawn there.
	public override Vector3 NearestFaceW(Vector3 world, bool doDebug) {
		Vector3 dest = PolarToWorld(NearestFaceP(world));
		if (doDebug) {
			DrawSphere(dest);
		}
		return dest;
	}

	/// <summary>Returns the world position of the nearest box.</summary>
	/// <returns>World position of the nearest box.</returns>
	/// <param name="worldPoint">Point in world space.</param>
	/// <param name="doDebug">If set to <c>true</c> draw a sphere at the destination.</param>
	/// 
	/// Similar to <see cref="NearestVertexW"/>, it returns the world coordinates of a box in the grid. Since the box is enclosed by several vertices, the returned value
	/// is the point in between all of the vertices. If <paramref name="doDebug"/> is set a small gizmo box will drawn there.
	public override Vector3 NearestBoxW(Vector3 worldPoint, bool doDebug) {
		Vector3 dest = PolarToWorld(NearestBoxP(worldPoint));
		if (doDebug) {
			DrawSphere(dest);
		}
		return dest;
	}
	#endregion
	
	#region nearest in grid space
	/// <summary>Returns the grid position of the nearest vertex.</summary>
	/// <returns>Grid position of the nearest vertex.</returns>
	/// <param name="worldPoint">Point in world space.</param>
	/// 
	/// Returns the position of the nerest vertex in grid coordinates from a given point in world space.
	public override Vector3 NearestVertexG(Vector3 worldPoint) {
		Vector3 local = WorldToGrid(worldPoint);
		return RoundGridPoint(local);
	}

	/// <summary>Returns the grid position of the nearest Face.</summary>
	/// <returns>Grid position of the nearest face.</returns>
	/// <param name="world">Point in world space.</param>
	/// 
	/// Similar to <see cref="NearestVertexG"/>, it returns the grid coordinates of a face on the grid. Since the face is enclosed by several vertices, the returned value
	/// is the point in between all of the vertices.
	public override Vector3 NearestFaceG(Vector3 world) {
		return PolarToGrid(NearestFaceP(world));
	}

	/// <summary>Returns the grid position of the nearest box.</summary>
	/// <returns>Grid position of the nearest box.</returns>
	/// <param name="worldPoint">Point in world space.</param>
	/// 
	/// Similar to <see cref="NearestVertexG"/>, it returns the grid coordinates of a box in the grid. Since the box is enclosed by several vertices, the returned value
	/// is the point in between all of the vertices.
	public override Vector3 NearestBoxG(Vector3 worldPoint) {
		return PolarToGrid(NearestBoxP(worldPoint));
	}
	
	private Vector3 RoundGridPoint(Vector3 point) {
		return Mathf.Round(point[idx[0]]) * units[idx[0]] + Mathf.Round(point[idx[1]]) * units[idx[1]] + Mathf.Round(point[idx[2]]) * units[idx[2]];
	}
	#endregion
	
	#region nearest in polar space
	/// <summary>Returns the grid position of the nearest vertex.</summary>
	/// <returns>Polar position of the nearest vertex.</returns>
	/// <param name="worldPoint">Point in world space.</param>
	/// 
	/// Returns the position of the nerest vertex in polar coordinates from a given point in world space.
	public Vector3 NearestVertexP(Vector3 worldPoint) {
		Vector3 polar = WorldToPolar(worldPoint);
		return RoundPolarPoint(polar);
	}

	/// <summary>Returns the polar position of the nearest Face.</summary>
	/// <returns>Polar position of the nearest face.</returns>
	/// <param name="worldPoint">Point in world space.</param>
	/// 
	/// Similar to <see cref="NearestVertexP"/>, it returns the polar coordinates of a face on the grid. Since the face is enclosed by several vertices, the returned
	/// value is the point in between all of the vertices.
	public Vector3 NearestFaceP(Vector3 worldPoint) {
		Vector3 polar = WorldToPolar(worldPoint);
		int i, j; // i radius, j angle
		if (gridPlane == GridPlane.XY) {
			i = 0;
			j = 1;
		} else if (gridPlane == GridPlane.XZ) {
			i = 0;
			j = 2;
		} else {
			i = 2;
			j = 1;
		}
		polar -= 0.5f * radius * units[idx[i]] + 0.5f * angle * units[idx[j]]; // virtually shift the point half an angle and half a radius down, this will simulate the shifted coordinates
		polar[idx[j]] = Mathf.Max(0, polar[idx[j]]); // prevent the angle from becoming negative
		polar = RoundPolarPoint(polar); // round the point
		polar += 0.5f * radius * units[idx[i]] + 0.5f * angle * units[idx[j]];
		//Debug.Log (polar);
		return polar;
	}

	/// <summary>Returns the polar position of the nearest box.</summary>
	/// <returns>Polar position of the nearest box.</returns>
	/// <param name="worldPoint">Point in world space.</param>
	/// 
	/// Similar to <see cref="NearestVertexP"/>, it returns the polar coordinates of a box in the grid. Since the box is enclosed by several vertices, the returned value
	/// is the point in between all of the vertices.
	public Vector3 NearestBoxP(Vector3 worldPoint) {
		Vector3 polar = WorldToPolar(worldPoint);
		// virtually shift the point half an angle, half a radius and half the depth down, this will simulate the shifted coordinates
		polar -= 0.5f * radius * units[idx[0]] + 0.5f * angle * units[idx[1]] + 0.5f * depth * units[idx[2]];
		polar[idx[1]] = Mathf.Max(0, polar[idx[1]]);  // prevent the angle from becoming negative
		polar = RoundPolarPoint(polar);
		polar += 0.5f * radius * units[idx[0]] + 0.5f * angle * units[idx[1]] + 0.5f * depth * units[idx[2]];
		return polar;
	}
	
	private Vector3 RoundPolarPoint(Vector3 point) {
		return RoundMultiple(point[idx[0]], radius) * units[idx[0]] + RoundMultiple(point[idx[1]], angle) * units[idx[1]] + RoundMultiple(point[idx[2]], depth) * units[idx[2]];
	}
	#endregion

	#region Align Methods
	/// <summary>Aligns and rotates a Transform.</summary>
	/// <param name="transform">The Transform to align.</param>
	/// <param name="lockAxis">Axis to ignore.</param>
	/// 
	/// Aligns a Transform and the rotates it depending on its position inside the grid. This method cobines two steps in one call for convenience.
	public void AlignRotateTransform(Transform transform, BoolVector3 lockAxis) {
		AlignTransform(transform, true, lockAxis);
		transform.rotation = World2Rotation(transform.position);
	}

	#region overload
	/// @overload
	/// Use this overload when you want to appy the alignment on all axis, then you don't have to specity them.
	public void AlignRotateTransform(Transform theTransform) {
		AlignRotateTransform(theTransform, new BoolVector3(false));
	}
	#endregion

	/// <summary>Fits a position vector into the grid.</summary>
	/// <param name="pos">The position to align.</param>
	/// <param name="scale">A simulated scale to decide how exactly to fit the poistion into the grid.</param>
	/// <param name="ignoreAxis">Which axes should be ignored.</param>
	/// <returns>Aligned position vector.</returns>
	/// 
	/// Fits a position inside the grid by using the object’s transform. Currently the object will snap either on edges or between, depending on which is closer, ignoring
	/// the <paramref name="scale"/> passed, but I might add an optionfor this in the future.
	public override Vector3 AlignVector3(Vector3 pos, Vector3 scale, BoolVector3 ignoreAxis) {
		float fracAngle = World2Angle(pos) / angle - Mathf.Floor(World2Angle(pos) / angle);
		float fracRad = World2Radius(pos) / radius - Mathf.Floor(World2Radius(pos) / radius);

		Vector3 vertex = NearestVertexP(pos);
		Vector3 box = NearestBoxP(pos);
		Vector3 final = Vector3.zero;

		//final += (scale [idx[0]] % 2.0f >= 0.5f || scale [idx[0]] < 1.0f ? box [idx[0]] : vertex [idx[0]]) * units[idx[0]]; % <-- another idea based on scale
		final += (0.25f < fracRad && fracRad < 0.75f ? box[idx[0]] : vertex[idx[0]]) * units[idx[0]];
		final += (0.25f < fracAngle && fracAngle < 0.75f ? box[idx[1]] : vertex[idx[1]]) * units[idx[1]];
		final += (scale[idx[2]] % 2.0f >= 0.5f || scale[idx[0]] < 1.0f ? box[idx[2]] : vertex[idx[2]]) * units[idx[2]];

		for (int i = 0; i <= 2; i++) {
			final[i] = ignoreAxis[i] ? pos[i] : final[i];
		}
		return PolarToWorld(final);
	}
	#endregion
	
	#region Scale Methods
	/// <summary>Scales a size vector to fit inside a grid.</summary>
	/// <param name="scl">The vector to scale.</param>
	/// <param name="ignoreAxis">The axes to ignore.</param>
	/// <returns>The re-scaled vector.</returns>
	/// 
	/// Scales a given scale vector to the nearest multiple of the grid’s radius and depth, but does not change its position. The parameter <paramref name="ignoreAxis"/>
	/// makes the function not touch the corresponding coordinate.
	public override Vector3 ScaleVector3(Vector3 scl, BoolVector3 ignoreAxis) {
		Vector3 result = Vector3.Max(RoundMultiple(scl[idx[0]], radius) * locUnits[idx[0]] + scl[idx[1]] * locUnits[idx[1]] + RoundMultiple(scl[idx[2]], depth) * locUnits[idx[2]],
			radius * locUnits[idx[0]] + scl[idx[1]] * locUnits[idx[1]] + depth * locUnits[idx[2]]);
		for (int i = 0; i <= 2; i++) {
			result[i] = ignoreAxis[i] ? scl[i] : result[i];
		}
		return result;
	}
	#endregion
	
	#region Render Methods
	public override void RenderGrid(int width = 0, Camera cam = null, Transform camTransform = null) {
		RenderGrid(Vector3.zero - size[idx[2]] * units[idx[2]], size, useSeparateRenderColor ? renderAxisColors : axisColors, width, cam, camTransform);
	}
	#endregion
	
	#region Calculate Draw Points
	#region Helper
	/// <summary>Computes helper values <c>arc_count</c>, <c>segment_count</c>, <c>sector_count</c> and <c>layer_count</c>.</summary>
	/// <param name="from">From vector.</param>
	/// <param name="to">To vector.</param>
	/// Values must be in grid space.
	private void ComputePointCounts(Vector3 from, Vector3 to) {
		// Adjusted for constant amount addition (e.g. we always have at least one layer)
		segment_count = Mathf.FloorToInt(to[idx[1]] * smoothness) - Mathf.CeilToInt(from[idx[1]] * smoothness) + 2; // number of red segments (+2 for over- and underflow)
		arc_count     = Mathf.FloorToInt(to[idx[0]]             ) - Mathf.CeilToInt(from[idx[0]]             ) + 1; // number of red arcs (circles)
		sector_count  = Mathf.FloorToInt(to[idx[1]]             ) - Mathf.CeilToInt(from[idx[1]]             ) + 1; // number of green radial lines
		layer_count   = Mathf.FloorToInt(to[idx[2]]             ) - Mathf.CeilToInt(from[idx[2]]             ) + 1; // number of layers
	}
	
	private Vector3 ContributePoint(float r, float phi, float z, Vector3 origin) {
		//return GridToWorld(new Vector3(r, phi, z)); // <-- sometimes the most bizarre things can happen.
		return ContributePoint(r, Quaternion.AngleAxis(phi, locUnits[idx[2]]), z, origin);
	}

	private Vector3 ContributePoint(float r, Quaternion phi, float z, Vector3 origin) {
		Vector3 pivot = origin + z * locUnits[idx[2]];
		return pivot + phi * (r * locUnits[idx[0]]);
	}
	#endregion

	protected override void drawPointsCount(ref int countX, ref int countY, ref int countZ, ref Vector3 from, ref Vector3 to, bool condition = true) {
		if (!condition)
			return;
		if(!relativeSize) { // Convert to grid coordinates for easier calculation of amounts.
			from[idx[0]] /= radius;
			from[idx[1]] /= angle;
			from[idx[2]] /= depth;
			
			to[idx[0]] /= radius;
			to[idx[1]] /= angle;
			to[idx[2]] /= depth;
		}
		// Adjust for non-custom range: the start radius and angle have to be 0, the end angle has to be slightly less than 2π.
		if (!useCustomRenderRange) {
			from[idx[0]] = 0.0f;
			from[idx[1]] = 0.0f;
			to[idx[1]]   = Mathf.Min(to[idx[1]], sectors - 0.00001f); // prevent the connecting radial line from being drawn twice
		}

		if (from[idx[1]] > to[idx[1]]) { // If the from angle is greater than the to angle wrap the to angle around once.
			to[idx[1]] += sectors;
		}

		ComputePointCounts(from, to);

		int[] amount = new int[3] {
			segment_count * arc_count                * layer_count,
			                            sector_count * layer_count,
			                arc_count * sector_count
		};

		countX = amount[idx[0]]; // total number of segments
		countY = amount[idx[1]]; // total number of radial lines
		countZ = amount[idx[2]]; // total number of cylindrical lines
	}
	
	protected override void drawPointsCalculate(ref Vector3[][][] points, ref int[] amount, Vector3 from, Vector3 to) {
		float r, phi, z, delta_phi = angleDeg / smoothness;
		Vector3 origin = lwMatrix.MultiplyPoint3x4(Vector3.zero);

		/* How to fill the arc segment line array: Each segment consists of two points, the first point of a segment is the second point of the previous segment and the
		 * second point of a segment is the first point of the next segment. The very first point will be the the first point of the very first segment (segment 0). For
		 * every other segment (segment i) add only its first point as its first point and as the second point of the previous segment (segment i-1). The very last point
		 * will be added as the second point of the very last segment (segment n-1). 
		 */

		// segment lines
		z = Mathf.Ceil(from[idx[2]]) * depth;
		for (int i = 0; i < layer_count; ++i) {           // for every layer
			r = Mathf.Ceil(from[idx[0]]) * radius;
			for (int j = 0; j < arc_count; ++j) {         // for every circle/arc
				// the first point is an exception
				_drawPoints[idx[0]][i * arc_count * segment_count + j * segment_count + 0][0] = ContributePoint(r, from[idx[1]] * angleDeg, z, origin);

				phi = Mathf.Ceil(from[idx[1]] * smoothness) / smoothness * angleDeg;
				for (int k = 1; k < segment_count; ++k) { // for every segment
					Vector3 point = ContributePoint(r, phi, z, origin);
					_drawPoints[idx[0]][i * arc_count * segment_count + j * segment_count + k - 1][1] = point;
					_drawPoints[idx[0]][i * arc_count * segment_count + j * segment_count + k    ][0] = point;
					phi += delta_phi;
				}
				// the last point is an exception as well
				_drawPoints[idx[0]][i * arc_count * segment_count + j * segment_count + segment_count - 1][1] = ContributePoint(r, to[idx[1]] * angleDeg, z, origin);

				r += radius;
			}
			z += depth;
		}

		// radial lines
		z   = Mathf.Ceil(from[idx[2]]) * depth;
		for (int i = 0; i < layer_count; ++i) {
			phi = Mathf.Ceil(from[idx[1]]) * angleDeg;
			for (int j = 0; j < sector_count; ++j) {
				points[idx[1]][i * sector_count + j][0] = ContributePoint(from[idx[0]] * radius, phi, z, origin);
				points[idx[1]][i * sector_count + j][1] = ContributePoint(  to[idx[0]] * radius, phi, z, origin);
				phi += angleDeg;
			}
			z += depth;
		}

		// cylindric lines
		r = Mathf.Ceil(from[idx[0]]) * radius;
		for (int i = 0; i < arc_count; ++i) {
			phi = Mathf.Ceil(from[idx[1]]) * angleDeg;
			for (int j = 0; j < sector_count; ++j) {
				points[idx[2]][i * sector_count + j][0] = ContributePoint(r, phi, from[idx[2]], origin);
				points[idx[2]][i * sector_count + j][1] = ContributePoint(r, phi,   to[idx[2]], origin);
				phi += angleDeg;
			}
			r += radius;
		}

		/* TO DO: performance can be improved by reducing the amount of rotations computed. To this end invert the loop, i.e. iterate over the angles first, inside those
		 * iterate over the arcs and inside those over the layers. Compute the rotation once per angle and use that for drawing radial lines, segments and layer lines.
		 * PROBLEM:  handling over- and underflow ina  loop without repeating; in the current implementation we can make the two exceptions without repeating code, because
		 * they are in the inner-most loop.
		 */

		/*Quaternion rotation;
		int i = 0, j = 0, k = 0;
		phi = from[idx[2]] * angleDeg;
		r = Mathf.CeilToInt(from[idx[0]]);
		z = Mathf.CeilToInt(from[idx[2]]);
		while (r < to[idx[0]]) {
			// draw layer line here
			while (z < to[idx[2]]) {
				// draw segment here
				++k;
			}
			++j;
		}*/
	}
	#endregion

	#region helper methods		
	/// <summary>An extended version of Atan 2; defaults to 0 if x=0 and maps to [0, 2π).</summary>
	private static float Atan3(float y, float x) {
		return Mathf.Atan2(y, x) + (y >= 0 ? 0 : 2 * Mathf.PI);
	}
	#endregion
}
