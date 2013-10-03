using UnityEngine;
using System.Collections;

/**
 * @brief The parent class for all layered grids.
 * 
 * This class serves as a parent for all grids composed out of two-dimensional grids stacked on top of each other (currently only hex- and polar grids).
 * These grids have a plane (orientation) and a "depth" (how densely stacked they are).
 * Other than keeping common values and internal methods in one place, this class has not much practical use.
 * I recommend yu ignore it, it is documented just for the sake of completion.
 */
public abstract class GFLayeredGrid : GFGrid {
	
	#region class members
	[SerializeField]
	private float _depth = 1.0f;
	/**
	 * @brief How far apart layers of the grid are
	 * 
	 * Layered grids are made of an infinite number of two-dimensional grids stacked on top of each other.
	 * This determines how far apart those layers are.
	 * The value cannot be lower than 0.1 in order to prevent contradictory values.
	 */
	public float depth{
		get{return _depth;}
		set{if(value == _depth)// needed because the editor fires the setter even if this wasn't changed
				return;
			_depth = Mathf.Max(value, 0.1f);
			_gridChanged = true;
		}
	}
	
	// the layers will be parallel the the specified plane
	[SerializeField]
	protected GridPlane _gridPlane = GridPlane.XY;
	/**
	 * @brief What plane the layers are on.
	 * 
	 * Layered grids are made of an infinite number of two-dimensional grids stacked on top of each other.
	 * This determines the orientation of these layers, i. e. if they are XY-, XZ- or YZ-layers.
	 */
	public virtual GridPlane gridPlane {
		get {
			return _gridPlane;}
		set {
			if(value == _gridPlane)
				{return;}
			_gridPlane = value;
			_gridChanged = true;
		}
	}
	
	#region helper values (read only)
	// the indices of the axes transformed to quasi-spcae (i.e. the Z-axis works like the Y-axis in XZ-grids)
	protected int[] idx {get {return TransformIndices(gridPlane);}}

	/// <summary>right, up and forward relative to the grid's Transform (i.e. in local space)</summary>
	protected Vector3[] locUnits {get { return new Vector3[3] { _transform.right, _transform.up, _transform.forward } ; } }
	#endregion
	#endregion

	#region Methods
	public Vector3 NearestFaceW (Vector3 worldPoint) {
		return NearestFaceW (worldPoint, false);
	}
	public override Vector3 NearestFaceW (Vector3 worldPoint, GridPlane plane, bool doDebug) {
		return NearestFaceW (worldPoint, doDebug);
	}

	public override Vector3 NearestFaceG (Vector3 worldPoint, GridPlane plane) {
		return NearestFaceG (worldPoint);
	}

	public abstract Vector3 NearestFaceW (Vector3 world, bool doDebug);
	public abstract Vector3 NearestFaceG (Vector3 world);
	#endregion

	#region helper functions
	//transforms from quasi axis to real axis. Quasi axis is the relative X, Y and Z n the current grid plane,
	// all calculations are done in quasi space, so there is only one calculation, and then transformed into real space
	protected virtual int[] TransformIndices(GridPlane plane){
		if(plane == GridPlane.YZ){
			return new int[3] {2, 1, (int)gridPlane};
		} else if(plane == GridPlane.XZ){
			return new int[3] {0, 2, (int)gridPlane};
		} else{
			return new int[3] {0, 1, (int)gridPlane};
		}
	}
	#endregion
}
