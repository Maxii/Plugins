using UnityEngine;
using System.Collections;

/**
 * @brief A standard three-dimensional rectangular grid.
 * 
 * Your standard rectangular grid, the characterising values is its spacing, which can be set for each axis individually.
 */

public class GFRectGrid: GFGrid{
	
	#region Class Members
	[SerializeField]
	private Vector3 _spacing = Vector3.one;
	/**
	 * @brief How large the grid boxes are.
	 * 
	 * How far apart the lines of the grid are.
	 * You can set each axis separately, but none may be less than 0.1, in order to prevent values that don't make any sense.
	 */
	public Vector3 spacing{
		get{return _spacing;}
		set{if(value == _spacing)// needed because the editor fires the setter even if this wasn't changed
				return;
			_gridChanged = true;
			_spacing = Vector3.Max(value, 0.1f*Vector3.one);
		}
	}

	#region Helper Values (read-only)
	/**
	 * @brief Direction along the X-axis of the grid in world space.
	 * 
	 * The X-axis of the grid in world space. This is a shorthand writing for <c>spacing.x * transform.right</c>.
	 * The value is read-only.
	 */
	public Vector3 right { get { return spacing.x * _transform.right; } }
	/**
	 * @brief Direction along the Y-axis of the grid in world space.
	 * 
	 * The Y-axis of the grid in world space. This is a shorthand writing for <c>spacing.y * transform.up</c>.
	 * The value is read-only.
	 */
	public Vector3 up { get { return spacing.y * _transform.up; } }
	/**
	 * @brief Direction along the Z-axis of the grid in world space.
	 * 
	 * The Z-axis of the grid in world space. This is a shorthand writing for <c>spacing.z * transform.forward</c>.
	 * The value is read-only.
	 */
	public Vector3 forward { get { return spacing.z * _transform.forward; } }
	#endregion
	#endregion
	
	#region grid to world
	/**
	 * @brief Converts world coordinates to grid coordinates.
	 * @param worldPoint Point in world space.
	 * @return Grid coordinates of the world point.
	 * 
	 * Takes in a position in wold space and calculates where in the grid that position is.
	 * The origin of the grid is the world position of its GameObject and its axes lie on the corresponding axes of the Transform.
	 * Rotation is taken into account for this operation.
	 */
	public override Vector3 WorldToGrid(Vector3 worldPoint){
		return gwMatrix.inverse.MultiplyPoint3x4(worldPoint);
	}
	
	/**
	 * @brief Converts grid coordinates to world coordinates.
	 * @param worldPoint Point in grid space.
	 * @return World coordinates of the Grid point.
	 * 
	 * The opposite of @c #WorldToGrid, this returns the world position of a point in the grid.
	 * The origin of the grid is the world position of its GameObject and its axes lie on the corresponding axes of the Transform.
	 * Rotation is taken into account for this operation.
	 */
	public override Vector3 GridToWorld(Vector3 gridPoint){
		return gwMatrix.MultiplyPoint(gridPoint);
		//return _transform.GFTransformPointFixed(Vector3.Scale(gridPoint, spacing));
	}
	#endregion

	#region nearest in world space
	/**
	 * @brief Returns the grid position of the nearest vertex.
	 * @param world Point in world space.
	 * @param doDebug Whether to draw a small debug sphere at the vertex.
	 * @return World coordinates of the nearest vertex.
	 * 
	 * Returns the world position of the nearest vertex from a given point in world space.
	 * If @c doDebug is set a small gizmo sphere will be drawn at the vertex position.
	 */
	public override Vector3 NearestVertexW(Vector3 world, bool doDebug = false){
		//convert fromPoint to grid coordinates first
		Vector3 toPoint = WorldToGrid(world);
		
		// each coordinate has to be set to a multiple of spacing
		for(int i = 0; i<=2; i++){
			toPoint[i] = Mathf.Round(toPoint[i]);
		}
		
		//back to World coordinates
		toPoint = GridToWorld(toPoint);
		
		if(doDebug){
			Gizmos.DrawSphere(GridToWorld(NearestVertexG(world)), 0.3f);
			//Gizmos.DrawSphere(toPoint, 0.3f);
		}
		//return toPoint;
		return GridToWorld(NearestVertexG(world));
	}

	/**
	 * @brief Returns the world position of the nearest face.
	 * @param world Point in world space.
	 * @param thePlane Plane on which the face lies.
	 * @param doDebug Whether to draw a small debug sphere at the vertex.
	 * @return World coordinates of the nearest face.
	 * 
	 * Similar to @c #NearestVertexW, it returns the world coordinates of a face on the grid.
	 * Since the face is enclosed by four vertices, the returned value is the point in between all four of the vertices.
	 * You also need to specify on which plane the face lies.
	 * If @c doDebug is set a small gizmo face will drawn inside the face.
	 */
	public override Vector3 NearestFaceW(Vector3 world, GridPlane thePlane, bool doDebug = false){
		//debugging
		if(doDebug){
			Vector3 debugCube = spacing;
			debugCube[(int)thePlane] = 0.0f;
			
			//store the old matrix and create a new one based on the grid's roation and the point's position
			Matrix4x4 oldRotationMatrix = Gizmos.matrix;
			//Matrix4x4 newRotationMatrix = Matrix4x4.TRS(toPoint, transform.rotation, Vector3.one);
			Matrix4x4 newRotationMatrix = Matrix4x4.TRS(GridToWorld(NearestFaceG(world, thePlane)), transform.rotation, Vector3.one);
			
			Gizmos.matrix = newRotationMatrix;
			Gizmos.DrawCube(Vector3.zero, debugCube);//Position zero because the matrix already contains the point
			Gizmos.matrix = oldRotationMatrix;
		}
		
		//return toPoint;
		return GridToWorld(NearestFaceG(world, thePlane));
	}

	/**
	 * @brief Returns the world position of the nearest box.
	 * @param world Point in world space.
	 * @param doDebug Whether to draw a small debug sphere at the vertex.
	 * @return World coordinates of the nearest box.
	 * 
	 * Similar to @c #NearestVertexW, it returns the world coordinates of a box in the grid.
	 * Since the box is enclosed by eight vertices, the returned value is the point in between all eight of them.
	 * If @c doDebug is set a small gizmo box will drawn inside the box.
	 */
	public override Vector3 NearestBoxW(Vector3 fromPoint, bool doDebug = false){		
		if(doDebug){
			//store the old matrix and create a new one based on the grid's roation and the point's position
			Matrix4x4 oldRotationMatrix = Gizmos.matrix;
			//Matrix4x4 newRotationMatrix = Matrix4x4.TRS(toPoint, transform.rotation, Vector3.one);
			Matrix4x4 newRotationMatrix = Matrix4x4.TRS(GridToWorld(NearestBoxG(fromPoint)), transform.rotation, Vector3.one);
			
			
			Gizmos.matrix = newRotationMatrix;
			Gizmos.DrawCube(Vector3.zero, spacing);
			Gizmos.matrix = oldRotationMatrix;
		}
		//convert back to world coordinates
		//return toPoint;
		return GridToWorld(NearestBoxG(fromPoint));
	}
	
	#endregion

	#region nearest in grid space
	
	/**
	 * @brief Returns the grid position of the nearest vertex.
	 * @param world Point in world space.
	 * @return Grid coordinates of the nearest vertex.
	 * 
	 * Similar to @c #NearestVertexW, except you get grid coordinates instead of world coordinates.
	 */
	public override Vector3 NearestVertexG(Vector3 world){
		return RoundPoint(WorldToGrid(world));
	}

	/**
	 * @brief Returns the grid position of the nearest face.
	 * @param world Point in world space.
	 * @param thePlane Plane on which the face lies.
	 * @return Grid coordinates of the nearest face.
	 * 
	 * Similar to @c #NearestFaceW, except you get grid coordinates instead of world coordinates.
	 * Since faces lie between vertices two values will always have +0.5 compared to vertex coordinates, while the values that lies on the plane will have a round number.
	 * Example:
	 * @code
	 * var myGrid: GFRectGrid;
	 * var worldPoint: Vector3;
	 * var face = myGrid.NearestFaceG (worldPoint, GFGrid.GridPlane.XY); // something like (2.5, -1.5, 3)
	 * @endcode
	 */
	public override Vector3 NearestFaceG(Vector3 fromPoint, GridPlane thePlane){
		return RoundPoint(WorldToGrid(fromPoint) - 0.5f * Vector3.one + 0.5f * units[(int)thePlane]) + 0.5f * Vector3.one - 0.5f * units[(int)thePlane];
	}

	/**
	 * @brief Returns the grid position of the nearest box.
	 * @param world Point in world space.
	 * @return Grid coordinates of the nearest box.
	 * 
	 * Similar to @c #NearestBoxW, except you get grid coordinates instead of world coordinates.
	 * Since faces lie between vertices all three values will always have +0.5 compared to vertex coordinates.
	 * Example:
	 * @code
	 * var myGrid: GFRectGrid;
	 * var worldPoint: Vector3;
	 * var box = myGrid.NearestBoxG (worldPoint); // something like (2.5, -1.5, 3.5)
	 * @endcode
	 */
	public override Vector3 NearestBoxG(Vector3 fromPoint){
		return RoundPoint(WorldToGrid(fromPoint) - 0.5f * Vector3.one) + 0.5f * Vector3.one;
	}	
	#endregion

	#region AlignScaleMethods

	/**
	 * @brief Aligns a point to fit inside the grid
	 * @param position The world position of the point.
	 * @param scale The scale determines how exactly to fit in the point.
	 * @param lockAxis Which of the axes to ignore.
	 * @return A re-positioned point vector.
	 * 
	 * This method aligns a point to the grid.
	 * The @c scale parameter is needed to simulate the “size” of point, which influences the resulting position like the scale of a Transform would do above.
	 * By default it’s set to one on all axes, placing the point at the centre of a box.
	 * If a component of @c scale is odd that component of the vector will be placed between edges, otherwise it will be placed on the nearest edge.
	 * The @c lockAxis parameter lets you ignore individual axes.
	 */
	public override Vector3 AlignVector3(Vector3 position, Vector3 scale, GFBoolVector3 lockAxis){
		Vector3 currentPosition = WorldToGrid(position);
		Vector3 newPositionB = WorldToGrid (NearestBoxW(position));
		Vector3 newPositionV = WorldToGrid (NearestVertexW (position));
		Vector3 newPosition = new Vector3();
		
		for (int i = 0; i <=2; i++){
			// vertex or box, depends on whether scale is a multiple of spacing
			newPosition [i] = (scale [i] / spacing [i]) % 2f <= 0.5f ? newPositionV [i] : newPositionB [i];
		}
		
		// don't apply aligning if the axis has been locked+
		for (int i = 0; i < 3; i++) {
			if(lockAxis[i])
				newPosition[i] = currentPosition[i];
		}

		return GridToWorld(newPosition);
	}
	
	/**
	 * @brief Scales a size to fit inside the grid
	 * @param scale Will be rounded to match the grid.
	 * @param lockAxis Which of the axes to ignore.
	 * @return A re-scaled size vector.
	 * 
	 * Scales a size to the nearest multiple of the grid’s spacing.
	 * The parameter @c #lockAxis makes the function not touch the corresponding coordinate.
	 */
	public override Vector3 ScaleVector3(Vector3 scale, GFBoolVector3 lockAxis){
		Vector3 relScale = scale.GFModulo3(spacing);
		Vector3 newScale = Vector3.zero;
		
		for (int i = 0; i <= 2; i++){
			newScale[i] = scale[i];
			
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
				newScale[i] = scale[i];
		}
		
		return  newScale;
	}
	
	#endregion

	#region Draw Gizoms
	
	void OnDrawGizmos(){
		if(useCustomRenderRange){
			DrawGrid(renderFrom, renderTo);
		} else{
			DrawGrid();
		}
	}
	
	#endregion
	
	#region Calculate draw points
	//This function returns a three-dimensional jagged array. The most inner layer contains
	// a pair of points for one line, the second layer contains the sets of all lines in the
	// same direction and the third layer contains all the sets.
	//The first set of lines are the horizontal X-lines, their amount depends on Y and Z. The
	// second set are the vertical lines (X and Z), the third set the forward lines (X and Y).
	protected override Vector3[][][] CalculateDrawPoints(Vector3 from, Vector3 to){
		// reuse the points if the grid hasn't changed, we already have some points and we use the same range
		if(!hasChanged && _drawPoints != null && from == renderFrom && to == renderTo){
			return _drawPoints;
		}
		
		// our old points are of no ue, so let's create a new set
		_drawPoints = new Vector3[3][][];
		
		Vector3 relFrom = relativeSize ? Vector3.Scale(from, spacing) : from;
		Vector3 relTo = relativeSize ? Vector3.Scale(to, spacing) : to;
		
		float[] length = new float[3];
		for(int i = 0; i < 3; i++){
			length[i] = relTo[i] - relFrom[i];
		}

		
		//the amount of lines for each direction
		int[] amount = new int[3];
		for(int i = 0; i < 3; i++){
			amount[i] = Mathf.FloorToInt(relTo[i] / spacing[i]) - Mathf.CeilToInt(relFrom[i] / spacing[i]) + 1;
		}
				
		//the starting point of the first pair (an iteration vector will be added to this)
		Vector3[] startPoint = new Vector3[3]{
			//everything in the right top front
			_transform.GFTransformPointFixed(new Vector3(relTo.x, spacing.y * Mathf.Floor(relTo.y / spacing.y), spacing.z * Mathf.Floor(relTo.z / spacing.z))),
			_transform.GFTransformPointFixed(new Vector3(spacing.x * Mathf.Floor(relTo.x / spacing.x), relTo.y, spacing.z * Mathf.Floor(relTo.z / spacing.z))),
			_transform.GFTransformPointFixed(new Vector3(spacing.x * Mathf.Floor(relTo.x / spacing.x), spacing.y * Mathf.Floor(relTo.y / spacing.y), relTo.z))
		};
		
		//this will be added to each first point in a pair
		Vector3[] endDirection = new Vector3[3]{
			_transform.TransformDirection(new Vector3(-Mathf.Abs(relTo.x - relFrom.x), 0.0f, 0.0f)),
			_transform.TransformDirection(new Vector3(0.0f, -Mathf.Abs(relTo.y - relFrom.y), 0.0f)),
			_transform.TransformDirection(new Vector3(0.0f, 0.0f, -Mathf.Abs(relTo.z - relFrom.z)))
		};
		
		//a multiple of this will be added to the starting point for iteration
		Vector3[] iterationVector = new Vector3[3]{
			_transform.TransformDirection(new Vector3(-spacing.x, 0.0f, 0.0f)),
			_transform.TransformDirection(new Vector3(0.0f, -spacing.y, 0.0f)),
			_transform.TransformDirection(new Vector3(0.0f, 0.0f, -spacing.z))
		};
		
		// assemble the array
		for(int i = 0; i < 3; i++){			
			//when collecting the line sets we need to know the amount of lines, it depends on
			// the other two coordinates that don't affect the line's size. Get them using modulo
			int idx1 = ((i+1)%3);
			int idx2 = ((i+2)%3);
			int iterator = 0;//j+k won't do it if one is larger than zero, use this independent iterator
			
			Vector3[][] lineSet = new Vector3[amount[idx1]*amount[idx2]][];
			
			if(relTo[i] - relFrom[i] <= 0.01f){// no need for a huge line set no one will see
				lineSet = new Vector3[0][];
			} else{
				for(int j = 0; j < amount[idx1]; ++j){
					for(int k = 0; k < amount[idx2]; ++k){
						Vector3[] line = new Vector3[2];
						line[0] = startPoint[i] + j*iterationVector[idx1] + k*iterationVector[idx2];
						line[1] = line[0] + endDirection[i];
						lineSet[iterator] = line;
						iterator++;
					}
				}
			}
			_drawPoints[i] = lineSet;
		}
		// if the points were calculated from the outside chances are they don't match the range anymore and the rendering could show the wrong range
		if(from != renderFrom || to != renderTo)
			_gridChanged = true;
		// in that case set _gridChanged to force a second calculation with the proper range

		// apply pivot offset
		ApplyDrawOffset ();

		return _drawPoints;
	}
	#endregion
	
	#region Matrices
	private Matrix4x4 gwMatrix {
		get{
			Matrix4x4 _gwMatrix = new Matrix4x4();
			_gwMatrix.SetColumn(0, _transform.right * spacing.x); // the scaled axes form the first three colums as Vector4 (the w component is 0)
			_gwMatrix.SetColumn(1, _transform.up * spacing.y);
			_gwMatrix.SetColumn(2, _transform.forward * spacing.z);
			_gwMatrix.SetColumn(3, _transform.position + originOffset); // the fourth column is the position of theorigin, used for translation)
			_gwMatrix[15] = 1; // the final matrix entry is always set to 1 by default

			return _gwMatrix;
		}
	}
	#endregion
	
	#region helper methods
	private Vector3 RoundPoint (Vector3 point) {
		return RoundPoint(point, Vector3.one);
	}
	private Vector3 RoundPoint (Vector3 point, Vector3 multi) {
		for(int i = 0; i < 3; i++){
			point[i] = RoundMultiple(point[i], multi[i]);
		}
		return point;
	}
	#endregion
}