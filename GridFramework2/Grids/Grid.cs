using UnityEngine;

namespace GridFramework.Grids {
	/// <summary>
	///   Abstract base class for all Grid Framework grids.
	/// </summary>
	/// <remarks>
	///   <para>
	///     This is the standard class all grids are based on. Aside from
	///     providing a common set of variables and a template for what methods
	///     to use, this class has no practical meaning for end users. Use this
	///     as reference for what can be done without having to specify which
	///     type of grid you are using. For anything more specific you have to
	///     look at the child classes.
	///   </para>
	///   <para>
	///     If you wish to create your own grids you will have to inherit this
	///     class and implement all abstract methods. See the detailed
	///     documentation of each method for details.
	///   </para>
	/// </remarks>
	[System.Serializable]
	public abstract class Grid : MonoBehaviour {
		/// <summary>
		///   Set this flag if the grid has changed, but cached properties have
		///   not been re-computed yet.
		/// </summary>
		/// <remarks>
		///   <para>
		///     An example for this is the spacing of a rectangular grid: if
		///     the spacing changes this affects the grid-coordinates of a
		///     point. This flag can be used within
		///     <see cref="UpdateCachedMembers">UpdateConversions</see> to
		///     decide whether it is necessary to re-compute cached members.
		///   </para>
		/// </remarks>
		protected bool _cacheIsDirty = true;

#region Matrices
		/// <summary>
		///   Update all the coordinate conversion members of the grid.
		/// </summary>
		/// <remarks>
		///   <para>
		///     Coordinate conversion computations consist of multiple steps
		///     that can be merged together for performance. For example a
		///     conversion could be implemented as a series of matrix
		///     multiplications that can be combined into a single matrix.
		///   </para>
		///   <para>
		///     Every time the grid changes we have to re-compute these
		///     conversion members. That's when this method is to be called.
		///     When exactly you want to call it is up to you; you could call
		///     it immediately after the grid has been changed, or call it
		///     lazily only when a conversion method is called.
		///   </para>
		///   <para>
		///     Personally I use this method lazily: whenever a conversion
		///     requires a computed result (matrix, scalar) this method is
		///     called beforehand. To avoid redundant computations I also use
		///   </para>
		/// </remarks>
		protected abstract void UpdateCachedMembers();
#endregion

#region Transform
		/// <summary>
		///   Caching the transform for performance.
		/// </summary>
		private Transform  _transform; //this is the real cache
		private Vector3    _position;
		private Quaternion _rotation;

		/// <summary>
		///   Whether the Transform_ has been changed since the last time this
		///   was checked.
		/// </summary>
		/// <returns>
		///   <c>true</c>, if the <c>Transform_</c> has been changed since the
		///   last time, <c>false</c> otherwise.
		/// </returns>
		protected bool TransfromHasChanged() {
			var alteredTransform = false;
			if (_position != Transform_.position) {
				alteredTransform = true;
				_position = Transform_.position;
			}
			if (_rotation != Transform_.rotation) {
				alteredTransform = true;
				_rotation = Transform_.rotation;
			}
			return alteredTransform;
		}

		/// <summary>
		///   This is used for access, if there is nothing cached it performs the
		///   cache first, then return the component.
		/// </summary>
		protected Transform Transform_ {
			get {
				if (!_transform) {
					_transform = transform;
					_position = _transform.position;
					_rotation = _transform.rotation;
				}
				return _transform;
			}
		}
#endregion
	}
}
