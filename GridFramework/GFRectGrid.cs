using UnityEngine;
using System.Collections;

/// <summary>A standard three-dimensional rectangular grid.</summary>
/// 
/// Your standard rectangular grid, the characterising values is its spacing, which can be set for each axis individually.
public class GFRectGrid: GFGrid{
	
	#region Class Members
	[SerializeField]
	private Vector3 _spacing = Vector3.one;

	/// <summary>How large the grid boxes are.</summary>
	/// <value>The spacing of the grid.</value>
	/// 
	/// How far apart the lines of the grid are. You can set each axis separately, but none may be less than 0.1, in order to prevent values that don't make any sense.
	public Vector3 spacing{
		get{return _spacing;}
		set{if(value == _spacing)// needed because the editor fires the setter even if this wasn't changed
				return;
			hasChanged = true;
			_spacing = Vector3.Max(value, 0.1f*Vector3.one);
		}
	}

	#region Helper Values (read-only)
	/// <summary>Direction along the X-axis of the grid in world space.</summary>
	/// <value>Unit vector in grid scale along the grid's X-axis.</value>
	/// 
	/// The X-axis of the grid in world space. This is a shorthand writing for <c>spacing.x * transform.right</c>. The value is read-only.
	public Vector3 right { get { return spacing.x * _transform.right; } }

	/// <summary>Direction along the Y-axis of the grid in world space.</summary>
	/// <value>Unit vector in grid scale along the grid's Y-axis.</value>
	/// 
	/// The Y-axis of the grid in world space. This is a shorthand writing for <c>spacing.y * transform.up</c>. The value is read-only.
	public Vector3 up { get { return spacing.y * _transform.up; } }

	/// <summary>Direction along the Z-axis of the grid in world space.</summary>
	/// <value>Unit vector in grid scale along the grid's Z-axis.</value>
	/// 
	/// The Z-axis of the grid in world space. This is a shorthand writing for <c>spacing.z * transform.forward</c>. The value is read-only.
	public Vector3 forward { get { return spacing.z * _transform.forward; } }
	#endregion
	#endregion
	
	#region grid to world
	/// <summary>Converts world coordinates to grid coordinates.</summary>
	/// <returns>Grid coordinates of the world point.</returns>
	/// <param name="worldPoint">Point in world space.</param>
	/// 
	/// Takes in a position in wold space and calculates where in the grid that position is. The origin of the grid is the world position of its GameObject and its axes
	/// lie on the corresponding axes of the Transform. Rotation is taken into account for this operation.
	public override Vector3 WorldToGrid(Vector3 worldPoint){
		return gwMatrix.inverse.MultiplyPoint3x4(worldPoint);
	}

	/// <summary>Converts grid coordinates to world coordinates.</summary>
	/// <returns>World coordinates of the Grid point.</returns>
	/// <param name="gridPoint">Point in grid space.</param>
	/// 
	/// The opposite of <see cref="WorldToGrid"/>, this returns the world position of a point in the grid. The origin of the grid is the world position of its GameObject
	/// and its axes lie on the corresponding axes of the Transform. Rotation is taken into account for this operation.
	public override Vector3 GridToWorld(Vector3 gridPoint){
		return gwMatrix.MultiplyPoint(gridPoint);
		//return _transform.GFTransformPointFixed(Vector3.Scale(gridPoint, spacing));
	}
	#endregion

	#region nearest in world space

	/// Returns the world position of the nearest vertex from a given point in world space. If <c>doDebug</c> is set a small gizmo sphere will be drawn at the vertex
	/// position.
	public override Vector3 NearestVertexW(Vector3 worldPoint, bool doDebug){
		//convert fromPoint to grid coordinates first
		Vector3 toPoint = WorldToGrid(worldPoint);
		
		// each coordinate has to be set to a multiple of spacing
		for(int i = 0; i<=2; i++){
			toPoint[i] = Mathf.Round(toPoint[i]);
		}
		
		//back to World coordinates
		toPoint = GridToWorld(toPoint);
		
		if(doDebug){
			Gizmos.DrawSphere(GridToWorld(NearestVertexG(worldPoint)), 0.3f);
			//Gizmos.DrawSphere(toPoint, 0.3f);
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
	public override Vector3 NearestFaceW(Vector3 worldPoint, GridPlane plane, bool doDebug){
		//debugging
		if(doDebug){
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
	public override Vector3 NearestBoxW(Vector3 worldPoint, bool doDebug){		
		if(doDebug){
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
	public override Vector3 NearestVertexG(Vector3 worldPoint){
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
	public override Vector3 NearestFaceG(Vector3 worldPoint, GridPlane plane){
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
	public override Vector3 NearestBoxG(Vector3 worldPoint){
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
	/// This method aligns a point to the grid. The <param name="scale"> parameter is needed to simulate the “size” of point, which influences the resulting position like
	/// the scale of a Transform would do above. By default it’s set to one on all axes, placing the point at the centre of a box. If a component of @c scale is odd that
	/// component of the vector will be placed between edges, otherwise it will be placed on the nearest edge. The <c>lockAxis</c> parameter lets you ignore individual axes.
	public override Vector3 AlignVector3(Vector3 pos, Vector3 scale, GFBoolVector3 ignoreAxis){
		Vector3 currentPosition = WorldToGrid(pos);
		Vector3 newPositionB = WorldToGrid (NearestBoxW(pos));
		Vector3 newPositionV = WorldToGrid (NearestVertexW (pos));
		Vector3 newPosition = new Vector3();
		
		for (int i = 0; i <=2; i++){
			// vertex or box, depends on whether scale is a multiple of spacing
			newPosition [i] = (scale [i] / spacing [i]) % 2f <= 0.5f ? newPositionV [i] : newPositionB [i];
		}
		
		// don't apply aligning if the axis has been locked+
		for (int i = 0; i < 3; i++) {
			if(ignoreAxis[i])
				newPosition[i] = currentPosition[i];
		}

		return GridToWorld(newPosition);
	}

	/// <summary>Scales a size to fit inside the grid.</summary>
	/// <returns>The re-scaled vector.</returns>
	/// <param name="scl">The vector to scale.</param>
	/// <param name="ignoreAxis">The axes to ignore.</param>
	/// 
	/// Scales a size to the nearest multiple of the grid’s spacing. The parameter <param name="ignoreAxis"> makes the function not touch the corresponding coordinate.
	public override Vector3 ScaleVector3(Vector3 scl, GFBoolVector3 ignoreAxis){
		Vector3 relScale = scl.GFModulo3(spacing);
		Vector3 newScale = Vector3.zero;
		
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
			if(ignoreAxis[i])
				newScale[i] = scl[i];
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
		if( RecyclePoints( from, to ) )
			return _drawPoints;
		
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