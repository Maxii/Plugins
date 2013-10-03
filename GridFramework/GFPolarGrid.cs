using UnityEngine;
using System.Collections;

// format of polar (cylindrical) coordinates (radius, angle, height), where:
//	- radius as distance from centre of the grid
//	- angle in radians in [0, 2π)
//	- height as distance from centre of the grid
//	- (plus index transformation due to gridPlane)

// format of grid coordinates (radius, sector, height), where:
//	- radius as multiple of "radius"
//	- sector as multiple of "angle"
//	- height as multiple of "depth"
//	- (plus index transformation due to gridPlane)

/**
 * @brief A polar grid based on cylindrical coordinates.
 * 
 * A grid based on cylindrical coordinates.
 * The caracterizing values are @c #radius, @c #sectors and @c #depth.
 * The angle values are derived from @c #sectors and we use radians internally.
 * The coordinate systems used are either a grid-based coordinate system based on the defining values or a regular cylindrical coordinate system.
 * If you want polar coordinates just ignore the height component of the cylindrical coordinates.
 * 
 * It is important to note that the components of polar and grid coordinates represent the radius, radian angle and height.
 * Which component represents what depends on the gridPlane. The user manual has a handy table for that purpose.
 * 
 * The members @c #size, @c #renderFrom and @c #renderTo are inherited from @c #GFGrid, but slightly different.
 * The first component of all three cannot be lower than 0 and in the case of @c #renderFrom it cannot be larger than the first component of @c #renderTo, and vice-versa.
 * The second component is an angle in radians and wraps around as described in the user manual, no other restrictions.
 * The third component is the height, it’s bounded from below by 0.01 and in the case of @c #renderFrom it cannot be larger than @c #renderTo and vice-versa.
 */
public class GFPolarGrid : GFLayeredGrid {

	#region class members
	#region overriding inherited accessors
	public override Vector3 size{
		get{return _size;}
		set{if(value == _size)// needed because the editor fires the setter even if this wasn't changed
				return;
			_gridChanged = true;
			//_size = Vector3.Max(Vector3.Min(value, value[idx[0]] * units[idx[0]] + 2 * Mathf.PI * units[idx[1]] + value[idx[2]] * units[idx[2]]), Vector3.zero);
			_size[idx[0]] = Mathf.Max(value[idx[0]], 0);
			_size[idx[1]] = Mathf.Max(Mathf.Min(value[idx[1]], 2 * Mathf.PI), 0);
			_size[idx[2]] = Mathf.Max(value[idx[2]], 0);
		}
	}
	
	public override Vector3 renderFrom{
		get{return _renderFrom;}
		set{if(value == _renderFrom)// needed because the editor fires the setter even if this wasn't changed
				return;
			_gridChanged = true;
			//_renderFrom = Vector3.Min(value, renderTo);
			_renderFrom[idx[0]] = Mathf.Max(Mathf.Min(value[idx[0]], renderTo[idx[0]]), 0); // prevent negative quasi-X and keep lower than renderTo
			_renderFrom[idx[1]] = Float2Rad(value[idx[1]]);
			_renderFrom[idx[2]] = Mathf.Min(renderTo[idx[2]], value[idx[2]]); // keep lower than renderTo
		}
	}
	
	public override Vector3 renderTo{
		get{return _renderTo;}
		set{if(value == _renderTo)// needed because the editor fires the setter even if this wasn't changed
				return;
			_gridChanged = true;
			//_renderTo = Vector3.Max(value, _renderFrom);
			_renderTo[idx[0]] = Mathf.Max(renderFrom[idx[0]], value[idx[0]]); // prevent negative quasi-X
			_renderTo[idx[1]] = Float2Rad(value[idx[1]]);
			_renderTo[idx[2]] = Mathf.Max(renderFrom[idx[2]], value[idx[2]]); // keep lower than renderTo
		}
	}
	#endregion
	
	#region members
	[SerializeField]
	private float _radius = 1;
	/**
	 * @brief The radius of the inner-most circle of the grid.
	 * 
	 * The radius of the innermost circle and how far apart the other circles are. The value cannot go below 0.01.
	 */
	public float radius {
		get{return _radius;}
		set{if(value == _radius)// needed because the editor fires the setter even if this wasn't changed
				return;
			_radius = Mathf.Max(0.01f, value);
			_gridChanged = true;
		}
	}
	
	[SerializeField]
	private int _sectors = 8;
	/**
	 * @brief The amount of sectors per circle.
	 * 
	 * The amount of sectors the circles are divided into. The minimum values is 1, which means one full circle.
	 */
	public int sectors{
		get{return _sectors;}
		set{if(value == _sectors)// needed because the editor fires the setter even if this wasn't changed
				return;
			_sectors = Mathf.Max(1, value);
			_gridChanged = true;
		}
	}
			
	// the amount of segments within a segment, more looks smoother
	[SerializeField]
	private int _smoothness = 5;
	/**
	 * @brief Divides the sectors to create a smoother look.
	 * 
	 * The GL class can only draw straight lines, so in order to get the sectors to look round this value breaks each sector into smaller sectors.
	 * The number of smoothness tells how many segments the circular line has been broken into.
	 * The amount of end points used is smoothness + 1, because we count both edges of the sector.
	 */
	public int smoothness{
		get{return _smoothness;}
		set{if(value == _smoothness)// needed because the editor fires the setter even if this wasn't changed
				return;
			_smoothness = Mathf.Max(1, value);
			_gridChanged = true;
		}
	}
	#endregion
	
	#region helper values (read only)
	/**
	 * @brief The angle of a sector in radians.
	 * 
	 * This is a read-only value derived from @c #sectors. It gives you the angle within a sector in radians and it’s a shorthand writing for
	 * @code
	 * (2.0f * Mathf.PI) / sectors
	 * @endcode
	 */
	public float angle { get { return (2 * Mathf.PI) / sectors;} }

	/**
	 * @brief The angle of a sector in degrees.
	 * 
	 * The same as @c #angle except in degrees, it’s a shorthand writing for
	 * @code
	 * 360.0f / sectors
	 * @endcode
	 */
	public float angleDeg { get { return 360.0f / sectors;} }
	#endregion
	#endregion
		
	#region Grid <-> World coordinate transformation
	/**
	 * @brief Converts from world to grid coordinates.
	 * @param worldPoint Point in world space.
	 * @return Point in grid space.
	 * 
	 * Converts a point from world space to grid space.
	 * The first coordinate represents the distance from the radial axis as multiples of @c #radius, the second one the sector and the thrid one the distance from the main plane as multiples of @c #height.
	 * This order applies to XY-grids only, for the other two orientations please consult the manual.
	 */
	public override Vector3 WorldToGrid(Vector3 worldPoint){
		return PolarToGrid(WorldToPolar(worldPoint));
	}

	/**
	 * @brief Converts from grid to world coordinates.
	 * @param gridPoint Point in grid space.
	 * @return Point in world space.
	 * 
	 * Converts a point from grid space to world space.
	 */
	public override Vector3 GridToWorld(Vector3 gridPoint){
		return PolarToWorld(GridToPolar(gridPoint));
	}
	#endregion
	
	#region Polar <-> World coordinate transformation
	/**
	 * @brief Converts from world to polar coordinates.
	 * @param worldPoint Point in world space.
	 * @return Point in polar space.
	 * 
	 * Converts a point from world space to polar space.
	 * The first coordinate represents the distance from the radial axis, the second one the angle in radians and the thrid one the distance from the main plane.
	 * This order applies to XY-grids only, for the other two orientations please consult the manual.
	 */
	public Vector3 WorldToPolar ( Vector3 worldPoint) {
		// first transform the point into local coordinates
		Vector3 localPoint = _transform.GFInverseTransformPointFixed(worldPoint) - originOffset;
		// now turn the point from Cartesian coordinates into polar coordinates
		return Mathf.Sqrt(Mathf.Pow(localPoint[idx[0]], 2) + Mathf.Pow(localPoint[idx[1]], 2)) * units[idx[0]]
			+ Atan3(localPoint[idx[1]], localPoint[idx[0]]) * units[idx[1]]
			+ localPoint[idx[2]] * units[idx[2]];
	}

	/**
	 * @brief Converts from polar to world coordinates.
	 * @param gridPoint Point in polar space.
	 * @return Point in world space.
	 * 
	 * Converts a point from polar space to world space.
	 */
	public Vector3 PolarToWorld ( Vector3 polarPoint) {
		return _transform.GFTransformPointFixed (
			polarPoint [idx [0]] * Mathf.Cos (Float2Rad (polarPoint [idx [1]])) * units [idx [0]]
			+ polarPoint [idx [0]] * Mathf.Sin (Float2Rad (polarPoint [idx [1]])) * units [idx [1]]
			+ polarPoint [idx [2]] * units [idx [2]])
			+ originOffset;
	}
	#endregion
	
	#region Grid <-> Polar coordinate transformation
	/**
	 * @brief Converts a point from grid to polar space.
	 * @param gridPoint Point in grid space.
	 * 
	 * Converts a point from grid to polar space. The main difference is that grid coordinates are dependent on the grid's parameters, while polar coordinates are not.
	 */
	public Vector3 GridToPolar ( Vector3 gridPoint) {
		gridPoint [idx[1]] = Float2Sector (gridPoint[idx[1]]);
		Vector3 polar = Vector3.Scale(gridPoint, radius * units[idx[0]] + Float2Rad(angle) * units[idx[1]] + depth * units[idx[2]]);
		polar[idx[1]] = Float2Rad(polar[idx[1]]);
		return polar;
	}

	/**
	 * @brief Converts a point from polar to grid space.
	 * @param gridPoint Point in polar space.
	 * 
	 * Converts a point from polar to grid space. The main difference is that grid coordinates are dependent on the grid's parameters, while polar coordinates are not.
	 */
	public Vector3 PolarToGrid ( Vector3 polarPoint) {
		return polarPoint.GFReverseScale(radius * units[idx[0]] + Float2Rad(angle) * units[idx[1]] + depth * units[idx[2]]);
	}
	#endregion

	#region Conversions
	#region Public
	/**
	 * @brief Converts an angle (radians or degree) to the corresponding sector coordinate.
	 * @param angle Angle in either radians or degress.
	 * @param mode The mode of the angle, defaults to radians.
	 * @return Sector coordinate of the angle.
	 * 
	 * This method takes in an angle and returns in which sector the angle lies.
	 * If the angle exceeds 2π or 360° it wraps around, nagetive angles are automatically subtracted from 2π or 360°.
	 * 
	 * Let's take a grid with six sectors for example, then one sector has an agle of 360° / 6 = 60°, so a 135° angle corresponds to a sector value of 130° / 60° = 2.25.
	 */
	public float Angle2Sector (float angle, GFAngleMode mode = GFAngleMode.radians) {
		angle = Float2Rad (angle * (mode == GFAngleMode.degrees ? Mathf.Deg2Rad : 1.0f));
		return angle / this.angle * (mode == GFAngleMode.degrees ? Mathf.Rad2Deg : 1.0f);
	}

	/**
	 * @brief Converts a sector to the corresponding angle coordinate (radians or degree).
	 * @param sector Sector number.
	 * @param mode The mode of the angle, defaults to radians.
	 * @return angle coordinate of the sector.
	 * 
	 * This method takes in a sector coordinate and returns the corresponding angle around the origin.
	 * If the sector exceeds the amount of sectors of the grid it wraps around, nagetive sctors are automatically subtracted from the maximum.
	 * 
	 * Let's take a grid with six sectors for example, then one sector has an agle of 360° / 6 = 60°, so a 2.25 sector corresponds to an angle of 2.25 * 60° = 135°.
	 */
	public float Sector2Angle (float sector, GFAngleMode mode = GFAngleMode.radians) {
		sector = Float2Sector (sector);
		return sector * angle * (mode == GFAngleMode.degrees ? Mathf.Rad2Deg : 1.0f);
	}

	/**
	 * @brief Converts an angle around the origin to a rotation.
	 * @param angle Angle in either radians or degress.
	 * @param mode The mode of the angle, defaults to radians.
	 * @return Rotation quaterion which rotates arround the origin.
	 * 
	 * This method returns a quaternion which represents a rotation within the grid.
	 * The result is a combination of the grid's own rotation and the rotation from the angle.
	 * Since we use an angle, this method is more suitable for polar coordinates than grid coordinates.
	 * Look at @c #Sector2Rotation for a similar method that uses sectors.
	 */ 
	public Quaternion Angle2Rotation (float angle, GFAngleMode mode = GFAngleMode.radians) {
		return Quaternion.AngleAxis(angle * (mode == GFAngleMode.radians ? Mathf.Rad2Deg : 1.0f), locUnits[idx[2]] * (gridPlane == GridPlane.XY ? 1.0f : -1.0f)) * _transform.rotation;
	}

	/**
	 * @brief Converts a sector around the origin to a rotation
	 * @param sector Sector coordinate inside the grid.
	 * @return Rotation quaterion which rotates arround the origin.
	 * 
	 * This is basically the same as @c #Angle2Rotation, excpet with sectors, which makes this method more suitable for grid coordinates than polar coordinates.
	 */
	public Quaternion Sector2Rotation (float sector) {
		return Angle2Rotation(Sector2Angle(sector,GFAngleMode.radians), GFAngleMode.radians);
	}

	/**
	 * @brief Converts a world position to a rotation around the origin.
	 * @param world Point in world space.
	 * @return Rotation quaterion which rotates arround the origin.
	 * 
	 * This method compares the point's position in world space to the grid and then returns which rotation an object should have if it was at that position and rotated around the grid.
	 */
	public Quaternion World2Rotation (Vector3 world) {
		return Angle2Rotation(World2Angle(world));
	}

	/**
	 * @brief Converts a world position to an angle around the origin.
	 * @param world Point in world space.
	 * @param mode The mode of the angle, defaults to radians.
	 * @return Angle between the point and the grid's "right" axis.
	 * 
	 * This method returns which angle around the grid a given point in world space has.
	 */
	public float World2Angle (Vector3 world, GFAngleMode mode = GFAngleMode.radians) {
		return WorldToPolar(world)[idx[1]] * (mode == GFAngleMode.radians ? 1.0f : Mathf.Rad2Deg);
	}

	/**
	 * @brief Converts a world position to the sector of the grid it is in.
	 * @param world Point in world space.
	 * @return Sector the point is in.
	 * 
	 * This method returns which which sector a given point in world space is in.
	 */
	public float World2Sector (Vector3 world) {
		return WorldToGrid(world)[idx[1]];
	}

	/**
	 * @brief Converts a world position to the radius from the origin.
	 * @param world Point in world space.
	 * @return Radius of the point from the grid.
	 * 
	 * This method returns the distance of a world point from the grid's radial axis.
	 * This is not the same as the point's distance from the grid's origin, because it diesn't take "height" into account.
	 * Thus it is always less or equal than the distance from the origin.
	 */
	public float World2Radius (Vector3 world) {
		return WorldToPolar (world) [idx[0]];
	}
	#endregion
	#region Private
	// interprets a float as radians; loops if value exceeds 2π and runs in reverse for negative values
	private static float Float2Rad (float number){
		return number >= 0 ? number % (2* Mathf.PI) : 2 * Mathf.PI + (number % Mathf.PI);
	}
	// interprets a float as degree; loops if value exceeds 360 and runs in reverse for negative values
	private static float Float2Deg (float number){
		return number >= 0 ? number % 360 : 360 + (number % 360);
	}
	// interprets a float as sector; loops if value exceeds [sectors] and runs in reverse for negative values
	private float Float2Sector (float number) {
		return number >= 0 ? number % sectors : sectors + (number % sectors);
	}
	#endregion
	#endregion
	
	#region nearest in world space
	/**
	 * @brief Returns the world position of the nearest vertex.
	 * @param worldPoint Point in world space.
	 * @param doDebug If set to @c true draw a sphere at the destination.
	 * @return World position of the nearest vertex.
	 * 
	 * Returns the world position of the nearest vertex from a given point in world space.
	 * If <c>doDebug</c> is set a small gizmo sphere will be drawn at that position.
	 */
	public override Vector3 NearestVertexW(Vector3 fromPoint, bool doDebug = false){
		Vector3 dest = PolarToWorld(NearestVertexP(fromPoint));
		if(doDebug)
			DrawSphere(dest);
		return dest;
	}

	/**
	 * @brief Returns the world position of the nearest face.
	 * @return World position of the nearest face.
	 * @param worldPoint Point in world space.
	 * @param doDebug If set to <c>true</c> draw a sphere at the destination.
	 * 
	 * Similar to <c>#NearestVertexW</c>, it returns the world coordinates of a face on the grid.
	 * Since the face is enclosed by several vertices, the returned value is the point in between all of the vertices.
	 * If <c>doDebug</c> is set a small gizmo face will drawn there.
	 */
	public override Vector3 NearestFaceW (Vector3 fromPoint, bool doDebug = false){
		Vector3 dest =  PolarToWorld (NearestFaceP (fromPoint));
		if(doDebug)
			DrawSphere(dest);
		return dest;
	}

	/**
	 * @brief Returns the world position of the nearest box.
	 * @return World position of the nearest box.
	 * @param worldPoint Point in world space.
	 * @param doDebug If set to <c>true</c> draw a sphere at the destination.
	 * 
	 * Similar to <c>#NearestVertexW</c>, it returns the world coordinates of a box in the grid.
	 * Since the box is enclosed by several vertices, the returned value is the point in between all of the vertices.
	 * If <c>doDebug</c> is set a small gizmo box will drawn there.
	 */
	public override Vector3 NearestBoxW (Vector3 fromPoint, bool doDebug = false){
		Vector3 dest = PolarToWorld(NearestBoxP(fromPoint));
		if(doDebug)
			DrawSphere(dest);
		return dest;
	}
	#endregion
	
	#region nearest in grid space
	/**
	 * @brief Returns the grid position of the nearest vertex.
	 * @param worldPoint Point in world space.
	 * @return Grid position of the nearest vertex.
	 * 
	 * Returns the position of the nerest vertex in grid coordinates from a given point in world space.
	 */
	public override Vector3 NearestVertexG(Vector3 world){
		Vector3 local = WorldToGrid(world);
		return RoundGridPoint(local);
	}

	/**
	 * @brief Returns the grid position of the nearest Face.
	 * @return Grid position of the nearest face.
	 * @param worldPoint Point in world space.
	 * 
	 * Similar to <c>#NearestVertexG</c>, it returns the grid coordinates of a face on the grid.
	 * Since the face is enclosed by several vertices, the returned value is the point in between all of the vertices.
	 */
	public override Vector3 NearestFaceG (Vector3 world) {
		return PolarToGrid (NearestFaceP (world));
	}

	/**
	 * @brief Returns the grid position of the nearest box.
	 * @return Grid position of the nearest box.
	 * @param worldPoint Point in world space.
	 * 
	 * Similar to <c>#NearestVertexG</c>, it returns the grid coordinates of a box in the grid.
	 * Since the box is enclosed by several vertices, the returned value is the point in between all of the vertices.
	 */
	public override Vector3 NearestBoxG(Vector3 world){
		return PolarToGrid (NearestBoxP(world));
	}
	
	private Vector3 RoundGridPoint (Vector3 point) {
		return Mathf.Round(point[idx[0]]) * units[idx[0]] + Mathf.Round(point[idx[1]]) * units[idx[1]] + Mathf.Round(point[idx[2]]) * units[idx[2]];
	}
	#endregion
	
	#region nearest in polar space
	/**
	 * @brief Returns the grid position of the nearest vertex.
	 * @param worldPoint Point in world space.
	 * @return Polar position of the nearest vertex.
	 * 
	 * Returns the position of the nerest vertex in polar coordinates from a given point in world space.
	 */
	public Vector3 NearestVertexP(Vector3 world){
		Vector3 polar = WorldToPolar(world);
		return RoundPolarPoint(polar);
	}
	
	/**
	 * @brief Returns the polar position of the nearest Face.
	 * @return Polar position of the nearest face.
	 * @param worldPoint Point in world space.
	 * 
	 * Similar to <c>#NearestVertexP</c>, it returns the polar coordinates of a face on the grid.
	 * Since the face is enclosed by several vertices, the returned value is the point in between all of the vertices.
	 */
	public Vector3 NearestFaceP (Vector3 worldPoint) {
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
		polar = RoundPolarPoint (polar); // round the point
		polar += 0.5f * radius * units[idx[i]] + 0.5f * angle * units[idx[j]];
		//Debug.Log (polar);
		return polar;
	}

	/**
	 * @brief Returns the polar position of the nearest box.
	 * @param worldPoint Point in world space.
	 * @return Polar position of the nearest box.
	 * 
	 * Similar to <c>#NearestVertexP</c>, it returns the polar coordinates of a box in the grid.
	 * Since the box is enclosed by several vertices, the returned value is the point in between all of the vertices.
	 */
	public Vector3 NearestBoxP(Vector3 world){
		Vector3 polar = WorldToPolar(world);
		// virtually shift the point half an angle, half a radius and half the depth down, this will simulate the shifted coordinates
		polar -= 0.5f * radius * units[idx[0]] + 0.5f * angle * units[idx[1]] + 0.5f * depth * units[idx[2]];
		polar[idx[1]] = Mathf.Max(0, polar[idx[1]]);  // prevent the angle from becoming negative
		polar = RoundPolarPoint (polar);
		polar += 0.5f * radius * units[idx[0]] + 0.5f * angle * units[idx[1]] + 0.5f * depth * units[idx[2]];
		return polar;
	}
	
	private Vector3 RoundPolarPoint (Vector3 point) {
		return RoundMultiple(point[idx[0]], radius) * units[idx[0]] + RoundMultiple(point[idx[1]], angle) * units[idx[1]] + RoundMultiple(point[idx[2]], depth) * units[idx[2]];
	}
	#endregion

	#region Align Methods
	/**
	 * @brief Aligns and rotates a Transform.
	 * @param theTransform The Transform to align.
	 * @param lockAxis Axis to ignore.
	 * 
	 * Aligns a Transform and the rotates it depending on its position inside the grid.
	 * This method cobines two steps in one call for convenience.
	 */
	public void AlignRotateTransform (Transform theTransform, GFBoolVector3 lockAxis) {
		AlignTransform(theTransform, true, lockAxis);
		theTransform.rotation = World2Rotation(theTransform.position);
	}

	#region overload
	/**
	 * @overload
	 * Use this overload when you want to appy the alignment on all axis, then you don't have to specity them.
	 */
	public void AlignRotateTransform (Transform theTransform) {
		AlignRotateTransform(theTransform, new GFBoolVector3(false));
	}
	#endregion

	/// <summary>
	/// Fits a position vector into the grid.
	/// </summary>
	/// <param name="pos">The position to align.</param>
	/// <param name="scale">A simulated scale to decide how exactly to fit the poistion into the grid.</param>
	/// <param name="lockAxis">Which axes should be ignored.</param>
	/// <returns>Aligned position vector.</returns>
	/// 
	/// Fits a position inside the grid by using the object’s transform.
	/// Currently the object will snap either on edges or between, depending on which is closer, ignoring the @c scale passed, but I might add an option for this in the future.
	public override Vector3 AlignVector3 (Vector3 pos, Vector3 scale, GFBoolVector3 lockAxis) {
		float fracAngle = World2Angle(pos) / angle - Mathf.Floor (World2Angle(pos) / angle);
		float fracRad = World2Radius(pos) / radius - Mathf.Floor (World2Radius(pos) / radius);

		Vector3 vertex = NearestVertexP(pos);
		Vector3 box = NearestBoxP(pos);
		Vector3 final = Vector3.zero;

		//final += (scale [idx[0]] % 2.0f >= 0.5f || scale [idx[0]] < 1.0f ? box [idx[0]] : vertex [idx[0]]) * units[idx[0]]; % <-- another idea based on scale
		final += (0.25f < fracRad && fracRad < 0.75f ? box [idx[0]] : vertex [idx[0]]) * units[idx[0]];
		final += (0.25f < fracAngle && fracAngle < 0.75f ? box [idx[1]] : vertex [idx[1]]) * units [idx[1]];
		final += (scale [idx[2]] % 2.0f >= 0.5f || scale [idx[0]] < 1.0f ? box [idx[2]] : vertex [idx[2]]) * units[idx[2]];

		for (int i = 0; i <= 2; i++) {final[i] = lockAxis[i] ? pos[i] : final[i];}
		return PolarToWorld(final);
	}
	#endregion
	
	#region Scale Methods
	/// <summary>
	/// Scales a size vector to fit inside a grid.
	/// </summary>
	/// <param name="scl">The vector to scale.</param>
	/// <param name="lockAxis">The axes to ignore.</param>
	/// <returns>The re-scaled vector.</returns>
	/// 
	/// Scales a given scale vector to the nearest multiple of the grid’s radius and depth, but does not change its position.
	/// The parameter @c lockAxis makes the function not touch the corresponding coordinate.
	public override Vector3 ScaleVector3(Vector3 scl, GFBoolVector3 lockAxis){
		Vector3 result = Vector3.Max(RoundMultiple(scl[idx[0]], radius) * locUnits[idx[0]] + scl[idx[1]] * locUnits[idx[1]] + RoundMultiple(scl[idx[2]], depth) * locUnits[idx[2]],
			radius * locUnits[idx[0]] + scl[idx[1]] * locUnits[idx[1]] + depth * locUnits[idx[2]]);
		for (int i = 0; i <= 2; i++) {result[i] = lockAxis[i] ? scl[i] : result[i];}
		return result;
	}
	#endregion
		
	#region Gizmos
	void OnDrawGizmos(){
		if(useCustomRenderRange){
			DrawGrid(renderFrom, renderTo);
		} else{
			DrawGrid();
		}
	}
	#endregion
	
	#region Draw Methods
	public override void DrawGrid () {
		DrawGrid (Vector3.zero - size[idx[2]] * units[idx[2]], size);
	}
	#endregion
	
	#region Render Methods
	public override void RenderGrid(int width = 0, Camera cam = null, Transform camTransform = null){
		RenderGrid(Vector3.zero - size[idx[2]] * units[idx[2]], size, useSeparateRenderColor ? renderAxisColors : axisColors, width, cam, camTransform);
	}
	#endregion
	
	#region Calculate Draw Points
	#region overload
	protected override Vector3[][][] CalculateDrawPoints(){
		Debug.Log("ping");
		return CalculateDrawPoints(Vector3.zero - size[idx[2]] * units[idx[2]], size);
	}
	#endregion
	protected override Vector3[][][] CalculateDrawPoints(Vector3 from, Vector3 to){
		// reuse the points if the grid hasn't changed, we already have some points and we use the same range
		if(!hasChanged && _drawPoints != null && from == renderFrom && to == renderTo){
			return _drawPoints;
		}
		
		if (relativeSize) {
			from[idx[0]] *= radius; to[idx[0]] *= radius;
			from[idx[2]] *= depth; to[idx[2]] *= depth;
		}
		
		// fit the float values of the second component into radians
		from[idx[1]] = Float2Rad(from[idx[1]]);
		to[idx[1]] = Float2Rad(to[idx[1]]);
		
		// our old points are of no use, so let's create a new set
		_drawPoints = new Vector3[3][][];
				
		// fist we need to figure out how many of each line we require, start with the amount of layers
		//float lowerZ = relativeSize ? depth * from[idx[2]] : from[idx[2]];
		//float upperZ = relativeSize ? depth * to[idx[2]] : to[idx[2]];
		float lowerZ = from[idx[2]];
		float upperZ = to[idx[2]];
		int layers = Mathf.FloorToInt(upperZ / depth) - Mathf.CeilToInt(lowerZ / depth) + 1;

		// the amount of circles per layer
		float startR = RoundCeil(from[idx[0]], radius);
		float endR = RoundFloor(to[idx[0]], radius);
		int circles = Mathf.RoundToInt((endR - startR) / radius) + 1;
		
		// the amount of sectors
		float startA = RoundMultiple(from[idx[1]], angle);
		float endA = RoundMultiple(to[idx[1]], angle);
		if(from[idx[1]] >= to[idx[1]]) // if the to angle did a full loop
			endA += 2*Mathf.PI; // add a whole cycle to it
		int sctrs = Mathf.RoundToInt((endA - startA) / angle);
		
		// from where to start (the centre of the lowest layer) (will be shifted after each layer)
		Vector3 startPos = _transform.position + depth * Mathf.CeilToInt(lowerZ / depth) * locUnits[idx[2]];
		
		_drawPoints[0] = new Vector3[layers * circles * sctrs * smoothness][]; // the circles
		_drawPoints[1] = new Vector3[layers * sectors][]; // the radial lines
		_drawPoints[2] = new Vector3[1][] {new Vector3[2] {_transform.position + lowerZ * locUnits[idx[2]], _transform.position + upperZ * locUnits[idx[2]]}}; // the Z-line
		
		// various counters to keep track what index to use for which line
		int circleSegmentCounter = 0; //line segment of a circle (does not reset)
		int radialCounter = 0; // a radial line (does not reset)
		
		// NOTE:  * (gridPlane == GridPlane.XY ? 1 : -1) is used to make sure the drawing is always counter-clockwise

		for (int i = 0; i < layers; i++){ // loop through the layers, each layer is one iteration
			for (int j = 0; j < circles; j++) { // first draw the circles, one loop for each of the circles of the current layer
				for (int k = 0; k < sctrs * smoothness; k++) { // each circle is made of segments, sectors * smoothness
					// formula: start at starPos, then rotate around the quasi-Z-axis (add starting angle to the rotation and shift along the radial line (plus starting radius)
					_drawPoints[0][circleSegmentCounter] = new Vector3[2] { // [origin] + ([rotation by degrees] * [direction] * [distance from origin])
						startPos + (Quaternion.AngleAxis(k * 360.0f / (sectors * smoothness) + startA * Mathf.Rad2Deg, locUnits[idx[2]] * (gridPlane == GridPlane.XY ? 1 : -1)) * locUnits[idx[0]] * (j *radius +startR)),
						startPos + (Quaternion.AngleAxis((k+1) * 360.0f / (sectors * smoothness) + startA * Mathf.Rad2Deg, locUnits[idx[2]] * (gridPlane == GridPlane.XY ? 1 : -1)) * locUnits[idx[0]] * (j * radius + startR))
					};
					circleSegmentCounter++;
				}
			}
			// now draw the radial lines, one loop fills the entire layer
			for (int j = 0; j <= Mathf.Min(sctrs, sectors - 1); j++) {
				// formula: start at startPos, then rotate around the quasi-Z-axis and add the start/ending length along the radial line
				_drawPoints[1][radialCounter] = new Vector3[2] {
					startPos + (Quaternion.AngleAxis(j * 360.0f / sectors + startA * Mathf.Rad2Deg, locUnits[idx[2]] * (gridPlane == GridPlane.XY ? 1 : -1)) * locUnits[idx[0]] * from[idx[0]]),
					startPos + (Quaternion.AngleAxis(j * 360.0f / sectors + startA * Mathf.Rad2Deg, locUnits[idx[2]] * (gridPlane == GridPlane.XY ? 1 : -1)) * locUnits[idx[0]] * to[idx[0]])
				};
				radialCounter++;
			}
			// increment starting position of the current layer
			startPos += depth * locUnits[idx[2]];
		}

		ApplyDrawOffset ();

		return _drawPoints;
	}
	#endregion
	
	#region helper functions		
	// an extended version of Atan 2; defaults to 0 if x=0 and maps to [0, 2π)
	private static float Atan3 (float y, float x) {
		return Mathf.Atan2(y, x) + (y >= 0 ? 0 : 2 * Mathf.PI);
	}
	
	// the maximum radius of the drawing
	private float MaxRadius (Vector3 from, Vector3 to){
		return Mathf.Min (Mathf.Abs (from[idx[0]]), Mathf.Abs (from[idx[0]]));
	}
	#endregion
}