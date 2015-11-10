using UnityEngine;
using GridFramework;

/// <summary>The parent class for all layered grids.</summary>
/// 
/// This class serves as a parent for all grids composed out of two-dimensional
/// grids stacked on top of each other (currently only hex- and polar grids).
/// These grids have a plane (orientation) and a "depth" (how densely stacked
/// they are). Other than keeping common values and internal methods in one
/// place, this class has not much practical use. I recommend yu ignore it, it
/// is documented just for the sake of completion.
public abstract class GFLayeredGrid : GFGrid {
	
	#region class members
	[SerializeField]
	private float _depth = 1.0f;
	/// <summary>How far apart layers of the grid are.</summary>
	/// <value>Depth of grid layers.</value>
	///
	/// Layered grids are made of an infinite number of two-dimensional grids
	/// stacked on top of each other. This determines how far apart those
	/// layers are. The value cannot be lower than `Mathf.Epsilon` in order to
	/// prevent contradictory values.
	public float depth {
		get{ return _depth;}
		set {
			SetMember<float>(value, ref _depth, restrictor: Mathf.Max, limit: Mathf.Epsilon);
		}
	}
	
	// the layers will be parallel the specified plane
	[SerializeField]
	protected GridPlane _gridPlane = GridPlane.XY;
	/// <summary>What plane the layers are on.</summary>
	/// <value>The plane on which the grid is aligned.</value>
	/// Layered grids are made of an infinite number of two-dimensional grids
	/// stacked on top of each other. This determines the orientation of these
	/// layers, i. e. if they are XY-, XZ- or YZ-layers.
	public GridPlane gridPlane {
		get {
			return _gridPlane;
		}
		set {
			SetMember<GridPlane>(value, ref _gridPlane);
		}
	}
	
	#region helper values (read only)
	/// @internal <summary>the indices of the axes transformed to quasi-spcae
	/// (i.e. the Z-axis works like the Y-axis in XZ-grids).</summary>
	protected int[] idx { get { return TransformIndices(gridPlane); } }

	/// @internal <summary>right, up and forward relative to the grid's
	/// Transform (i.e. in local space).</summary>
	protected Vector3[] locUnits {
		get {
			return new Vector3[] {_Transform.right, _Transform.up, _Transform.forward};
		}
	}
	#endregion
	#endregion

	#region Methods
	public Vector3 NearestFaceW(Vector3 worldPoint) {
		return NearestFaceW(worldPoint, false);
	}
	public override Vector3 NearestFaceW(Vector3 worldPoint, GridPlane plane, bool doDebug) {
		return NearestFaceW(worldPoint, doDebug);
	}

	public override Vector3 NearestFaceG(Vector3 worldPoint, GridPlane plane) {
		return NearestFaceG(worldPoint);
	}

	public abstract Vector3 NearestFaceW(Vector3 world, bool doDebug);
	public abstract Vector3 NearestFaceG(Vector3 world);
	#endregion

	#region helper functions
	/// <summary>transforms from quasi axis to real axis.</summary>
	/// <returns>Real indices of quasi-indices.</returns>
	/// <param name="plane">The plane.</param>
	/// 
	/// Quasi axis is the relative X, Y and Z n the current grid plane, all calculations are done in quasi space, so there is only one calculation, and then transformed into real space.
	protected virtual int[] TransformIndices(GridPlane plane) {
		if (plane == GridPlane.YZ) {
			return new int[] {2, 1, (int)gridPlane};
		}
		if (plane == GridPlane.XZ) {
			return new int[] {0, 2, (int)gridPlane};
		}
		return new int[] {0, 1, (int)gridPlane};
	}

	/// <summary> Coordinate conversion matrix for grid plane. </summary>
	///
	/// This matrix maps a point between layered- and real coordinates and back.
	protected Matrix4x4 LayeredMatrix {
		get {
			var matrix = Matrix4x4.identity; /* Identity */
			switch (gridPlane) {
				case GridPlane.XY:
					break;
				case GridPlane.YZ:
					matrix[0,0] = 0; matrix[0,2] = 1;
					matrix[2,0] = 1; matrix[2,2] = 0;
					break;
				case GridPlane.XZ:
					matrix[1,1] = 0; matrix[1,2] = 1;
					matrix[2,1] = 1; matrix[2,2] = 0;
					break;
			}
			return matrix;
		}
	}
	#endregion
}
