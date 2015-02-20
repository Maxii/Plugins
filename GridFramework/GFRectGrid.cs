using UnityEngine;
using GridFramework;
using GridFramework.Vectors;

/// <summary>A standard three-dimensional rectangular grid.</summary>
/// 
/// Your standard rectangular grid, the characterising values is its spacing, which can be set for each axis individually.
public class GFRectGrid: GFGrid {
	
	#region Class Members
	[SerializeField]
	private Vector3	_spacing = Vector3.one;

	/// <summary>How large the grid boxes are.</summary>
	/// <value>The spacing of the grid.</value>
	/// How far apart the lines of the grid are. You can set each axis separately, but none may be less than 0.1, in order
	/// to prevent values that don't make any sense.
	public Vector3 spacing {
		get {return _spacing;}
		set {
			SetMember<Vector3>(value, ref _spacing, restrictor: Vector3.Max, limit: 0.1f*Vector3.one);
		}
	}

	[SerializeField]
	public Vector6 _shearing = Vector6.zero;
	/// <summary>How the axes are sheared.</summary>
	/// <value>Shearing vector of the grid.</value>
	/// How much the individual axes of the grid are skewed towards each other. For instance, this means the if _XY_ is set
	/// to _2_, then for each point with grid coordinates _(x, y)_ will be mapped to _(x, y + 2x)_, while the uninvolved _Z_
	/// coordinate remains the same. For more information refer to the manual.
	public Vector6 shearing {
		get {return _shearing;}
		set {
			SetMember<Vector6>(value, ref _shearing);
		}
	}

	#region Helper Values (read-only)
	/// <summary>Direction along the X-axis of the grid in world space.</summary>
	/// <value>Unit vector in grid scale along the grid's X-axis.</value>
	/// 
	/// The X-axis of the grid in world space. This is a shorthand writing for <c>spacing.x * transform.right</c>. The value is read-only.
	public Vector3 right {get{MatricesUpdate(); return _right;}}

	/// <summary>Direction along the Y-axis of the grid in world space.</summary>
	/// <value>Unit vector in grid scale along the grid's Y-axis.</value>
	/// 
	/// The Y-axis of the grid in world space. This is a shorthand writing for <c>spacing.y * transform.up</c>. The value is read-only.
	public Vector3 up {get{MatricesUpdate(); return _up;}}

	/// <summary>Direction along the Z-axis of the grid in world space.</summary>
	/// <value>Unit vector in grid scale along the grid's Z-axis.</value>
	/// 
	/// The Z-axis of the grid in world space. This is a shorthand writing for <c>spacing.z * transform.forward</c>. The value is read-only.
	public Vector3 forward {get{MatricesUpdate(); return _forward;}}
	#endregion
	#endregion

	#region Matrices
	/// <summary>Matrix that converts from grid- to world space.</summary>
	private Matrix4x4 _gwMatrix = Matrix4x4.identity;
	/// <summary>Matrix that converts from world- to grid space.</summary>
	private Matrix4x4 _wgMatrix = Matrix4x4.identity;

	private Vector3 _right, _up, _forward;
	
	/// <summary>Updates the coordinate conversion matrices of the grid.</summary>
	/// <para>After updating the matrices the <c>_matricesMustUpdate</c> is set back to <c>false</c>.</para>
	protected override void MatricesUpdate() {
		if (_TransformNeedsUpdate() || _matricesMustUpdate) {
			Matrix4x4 shearMatrix = new Matrix4x4();
			shearMatrix.SetRow(0, new Vector4(1          , shearing.yx, shearing.zx, 0));
			shearMatrix.SetRow(1, new Vector4(shearing.xy, 1          , shearing.zy, 0));
			shearMatrix.SetRow(2, new Vector4(shearing.xz, shearing.yz, 1          , 0));
			shearMatrix.SetRow(3, new Vector4(0          , 0          , 0          , 1));

			//Matrix4x4 scleM = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, spacing);
			//Matrix4x4 tranM = Matrix4x4.TRS(_Transform.position, Quaternion.identity, Vector3.one);
			//Matrix4x4 offsM = Matrix4x4.TRS(originOffset, Quaternion.identity, Vector3.one);
			//Matrix4x4 rotaM = Matrix4x4.TRS(Vector3.zero, _Transform.rotation, Vector3.one);

			//_gwMatrix = tranM * rotaM * shearMatrix * offsM * scleM; // <-- This is the right order, from right to left
			_gwMatrix = Matrix4x4.TRS(_Transform.position, _Transform.rotation, Vector3.one) * shearMatrix * Matrix4x4.TRS(originOffset, Quaternion.identity, spacing);
			_wgMatrix = _gwMatrix.inverse;

			Vector3 origin = _gwMatrix.MultiplyPoint3x4(Vector3.zero);
			_right   = _gwMatrix.MultiplyPoint3x4(Vector3.right  ) - origin;
			_up      = _gwMatrix.MultiplyPoint3x4(Vector3.up     ) - origin;
			_forward = _gwMatrix.MultiplyPoint3x4(Vector3.forward) - origin;

			_matricesMustUpdate = false;
		}
	}
	
	/// <summary>Reads the value of the Grid->World matrix (read only).</summary>
	/// <value>The Grid->World matrix.</value>
	/// <para>If the Matrices have to be updated they will do so first.</para>
	private Matrix4x4 gwMatrix {
		get {
			MatricesUpdate();
			return _gwMatrix;
		}
	}
	
	/// <summary>Reads the value of the World->Grid matrix (read only).</summary>
	/// <value>The World->Grid matrix.</value>
	/// <para>If the Matrices have to be updated they will do so first.</para>
	private Matrix4x4 wgMatrix {
		get {
			MatricesUpdate();
			return _wgMatrix;
		}
	}
	#endregion
	
	#region grid to world
	/// <summary>Converts world coordinates to grid coordinates.</summary>
	/// <returns>Grid coordinates of the world point.</returns>
	/// <param name="worldPoint">Point in world space.</param>
	/// 
	/// Takes in a position in wold space and calculates where in the grid that position is. The origin of the grid is the world position of its GameObject and its axes
	/// lie on the corresponding axes of the Transform. Rotation is taken into account for this operation.
	public override Vector3 WorldToGrid(Vector3 worldPoint) {
		return wgMatrix.MultiplyPoint3x4(worldPoint);
	}

	/// <summary>Converts grid coordinates to world coordinates.</summary>
	/// <returns>World coordinates of the Grid point.</returns>
	/// <param name="gridPoint">Point in grid space.</param>
	/// 
	/// The opposite of <see cref="WorldToGrid"/>, this returns the world position of a point in the grid. The origin of the grid is the world position of its GameObject
	/// and its axes lie on the corresponding axes of the Transform. Rotation is taken into account for this operation.
	public override Vector3 GridToWorld(Vector3 gridPoint) {
		return gwMatrix.MultiplyPoint3x4(gridPoint);
	}
	#endregion

	#region nearest in world space

	/// Returns the world position of the nearest vertex from a given point in world space. If <c>doDebug</c> is set a small gizmo sphere will be drawn at the vertex
	/// position.
	public override Vector3 NearestVertexW(Vector3 worldPoint, bool doDebug) {
		if (doDebug) {
			Gizmos.DrawSphere(GridToWorld(NearestVertexG(worldPoint)), 0.3f);
		}
		//return toPoint;
		return GridToWorld(NearestVertexG(worldPoint));
	}

	/// <summary>Returns the world position of the nearest face.</summary>
	/// <returns>World position of the nearest face.</returns>
	/// <param name="worldPoint">Point in world space.</param>
	/// <param name="plane">Plane on which the face lies.</param>
	/// <param name="doDebug">Whether to draw a small debug sphere at the vertex.</param>
	/// 
	/// Similar to <see cref="NearestVertexW"/>, it returns the world coordinates of a face on the grid. Since the face is enclosed by four vertices, the returned value
	/// is the point in between all four of the vertices. You also need to specify on which plane the face lies. If <c>doDebug</c> is set a small gizmo face will drawn
	/// inside the face.
	public override Vector3 NearestFaceW(Vector3 worldPoint, GridPlane plane, bool doDebug) {
		//debugging
		if (doDebug) {
			Vector3 debugCube = spacing;
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
	/// Similar to <see cref="NearestVertexW"/>, it returns the world coordinates of a box in the grid. Since the box is enclosed by eight vertices, the returned value is the
	/// point in between all eight of them. If <c>doDebug</c> is set a small gizmo box will drawn inside the box.
	public override Vector3 NearestBoxW(Vector3 worldPoint, bool doDebug) {		
		if (doDebug) {
			//store the old matrix and create a new one based on the grid's roation and the point's position
			Matrix4x4 oldRotationMatrix = Gizmos.matrix;
			//Matrix4x4 newRotationMatrix = Matrix4x4.TRS(toPoint, transform.rotation, Vector3.one);
			Matrix4x4 newRotationMatrix = Matrix4x4.TRS(GridToWorld(NearestBoxG(worldPoint)), transform.rotation, Vector3.one);
			
			
			Gizmos.matrix = newRotationMatrix;
			Gizmos.DrawCube(Vector3.zero, spacing);
			Gizmos.matrix = oldRotationMatrix;
		}
		//convert back to world coordinates
		//return toPoint;
		return GridToWorld(NearestBoxG(worldPoint));
	}
	
	#endregion

	#region nearest in grid space
	/// <summary>Returns the grid position of the nearest vertex.</summary>
	/// <returns>Grid position of the nearest vertex.</returns>
	/// <param name="worldPoint">Point in world space.</param>
	/// 
	/// Similar to @c #NearestVertexW, except you get grid coordinates instead of world coordinates.
	public override Vector3 NearestVertexG(Vector3 worldPoint) {
		return RoundPoint(WorldToGrid(worldPoint));
	}

	/// <summary>Returns the grid position of the nearest face.</summary>
	/// <returns>Grid position of the nearest face.</returns>
	/// <param name="worldPoint">Point in world space.</param>
	/// <param name="plane">Plane on which the face lies.</param>
	/// 
	/// Similar to <see cref="NearestFaceW"/>, except you get grid coordinates instead of world coordinates. Since faces lie between vertices two values will always have
	/// +0.5 compared to vertex coordinates, while the values that lies on the plane will have a round number.
	/// <example>
	/// Example:
	/// <code>
	/// GFRectGrid myGrid;
	/// Vector3 worldPoint;
	/// Vector3 face = myGrid.NearestFaceG(worldPoint, GFGrid.GridPlane.XY); // something like (2.5, -1.5, 3)
	/// </code>
	/// </example>
	public override Vector3 NearestFaceG(Vector3 worldPoint, GridPlane plane) {
		return RoundPoint(WorldToGrid(worldPoint) - 0.5f * Vector3.one + 0.5f * units[(int)plane]) + 0.5f * Vector3.one - 0.5f * units[(int)plane];
	}

	/// <summary>Returns the grid position of the nearest box.</summary>
	/// <returns>Grid position of the nearest box.</returns>
	/// <param name="worldPoint">Point in world space.</param>
	/// 
	/// Similar to @c #NearestBoxW, except you get grid coordinates instead of world coordinates. Since faces lie between vertices all three values will always have +0.5
	/// compared to vertex coordinates.
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

	#region AlignScaleMethods
	/// <summary>Fits a position vector into the grid.</summary>
	/// <returns>Aligned position vector.</returns>
	/// <param name="pos">The position to align.</param>
	/// <param name="scale">A simulated scale to decide how exactly to fit the poistion into the grid.</param>
	/// <param name="ignoreAxis">Which axes should be ignored.</param>
	/// 
	/// This method aligns a point to the grid. The *scale* parameter is needed to simulate the “size” of point, which influences the resulting position like
	/// the scale of a Transform would do above. By default it’s set to one on all axes, placing the point at the centre of a box. If a component of @c scale is odd that
	/// component of the vector will be placed between edges, otherwise it will be placed on the nearest edge. The <c>lockAxis</c> parameter lets you ignore individual axes.
	public override Vector3 AlignVector3(Vector3 pos, Vector3 scale, BoolVector3 ignoreAxis) {
		var currentPosition = WorldToGrid(pos);
		var newPositionB = WorldToGrid(NearestBoxW(pos));
		var newPositionV = WorldToGrid(NearestVertexW(pos));
		var newPosition = new Vector3();
		
		for (int i = 0; i <=2; i++) {
			// vertex or box, depends on whether scale is a multiple of spacing
			newPosition[i] = (scale[i] / spacing[i]) % 2f <= 0.5f ? newPositionV[i] : newPositionB[i];
		}
		
		// don't apply aligning if the axis has been locked+
		for (int i = 0; i < 3; i++) {
			if (ignoreAxis[i]) {
				newPosition[i] = currentPosition[i];
			}
		}

		return GridToWorld(newPosition);
	}

	/// <summary>Scales a size to fit inside the grid.</summary>
	/// <returns>The re-scaled vector.</returns>
	/// <param name="scl">The vector to scale.</param>
	/// <param name="ignoreAxis">The axes to ignore.</param>
	/// 
	/// Scales a size to the nearest multiple of the grid’s spacing. The parameter *ignoreAxis* makes the function not touch the corresponding coordinate.
	public override Vector3 ScaleVector3(Vector3 scl, BoolVector3 ignoreAxis) {
		//Vector3 relScale = scl.GFModulo3(spacing);
		var relScale = new Vector3(scl.x % spacing.x, scl.y % spacing.y, scl.z % spacing.z);
		var newScale = Vector3.zero;
		
		for (int i = 0; i <= 2; i++) {
			newScale[i] = scl[i];
			
			if (relScale[i] >= 0.5f * spacing[i]) {
				newScale[i] = newScale[i] - relScale[i] + spacing[i];
			} else {
				newScale[i] = newScale[i] - relScale[i];
				//if we went too far default to the spacing
				if (newScale[i] < spacing[i]) {
					newScale[i] = spacing[i];
				}
			}		
		}
		
		for (int i = 0; i < 3; i++) {
			if (ignoreAxis[i]) {
				newScale[i] = scl[i];
			}
		}
		
		return  newScale;
	}
	#endregion
	
	#region Calculate draw points
	protected override void drawPointsCount(ref int countX, ref int countY, ref int countZ, ref Vector3 from, ref Vector3 to, bool condition = true) {
		if (!condition)
			return;
		from = relativeSize ? from : new Vector3(from.x / spacing.x, from.y / spacing.y, from.z / spacing.z);
		to   = relativeSize ? to   : new Vector3(  to.x / spacing.x,   to.y / spacing.y,   to.z / spacing.z);

		int x = Mathf.FloorToInt(to[0]) - Mathf.CeilToInt(from[0]) + 1;
		int y = Mathf.FloorToInt(to[1]) - Mathf.CeilToInt(from[1]) + 1;
		int z = Mathf.FloorToInt(to[2]) - Mathf.CeilToInt(from[2]) + 1;

		countX =     y * z;
		countY = x *     z;
		countZ = x * y    ;
	}

	protected override void drawPointsCalculate(ref Vector3[][][] points, ref int[] amount, Vector3 from, Vector3 to) {
		int maxWidth = Mathf.CeilToInt(from.x), maxHeight = Mathf.CeilToInt(from.y), maxDepth = Mathf.CeilToInt(from.z);

		// Amount of lines perpendicular to each axis
		int[] lines = new int[3] {
			Mathf.FloorToInt(to.x) - maxWidth  + 1,
			Mathf.FloorToInt(to.y) - maxHeight + 1,
			Mathf.FloorToInt(to.z) - maxDepth  + 1
		};

		//The starting point of the first pair (an iteration vector will be added to this), everything in the left bottom back.
		Vector3[] startPoint = new Vector3[3]{
			GridToWorld(new Vector3(from.x  , maxHeight, maxDepth)),
			GridToWorld(new Vector3(maxWidth, from.y   , maxDepth )),
			GridToWorld(new Vector3(maxWidth, maxHeight, from.z   ))
		};
		
		//this will be added to each first point in a pair
		Vector3[] endDirection = new Vector3[3]{
			GridToWorld(new Vector3(to.x, maxHeight, maxDepth)) - startPoint[0],
			GridToWorld(new Vector3(maxWidth, to.y, maxDepth)) - startPoint[1],
			GridToWorld(new Vector3(maxWidth, maxHeight, to.z)) - startPoint[2]
		};

		//a multiple of this will be added to the starting point for iteration
		Vector3[] iterationVector = new Vector3[3] {right, up, forward};
		
		// assemble the array
		for (int i = 0; i < 3; ++i) {			
			// when collecting the line sets we need to know the amount of lines, it depends on
			// the other two coordinates that don't affect the line's size. Get them using modulo
			int idx1 = ((i + 1) % 3);
			int idx2 = ((i + 2) % 3);
			int iterator = 0;//j+k won't do it if one is larger than zero, use this independent iterator

			for (int j = 0; j < lines[idx1]; ++j) {
				for (int k = 0; k < lines[idx2]; ++k) {
					points[i][iterator][0] = startPoint[i] + j * iterationVector[idx1] + k * iterationVector[idx2];
					points[i][iterator][1] = points[i][iterator][0] + endDirection[i];
					++iterator;
				}
			}
		}
	}
	#endregion
	
	#region helper methods
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
