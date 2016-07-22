using UnityEngine;
using Grid = GridFramework.Grids.Grid;

namespace GridFramework.Grids {
	/// <summary>
	///   The parent class for all layered grids.
	/// </summary>
	/// <remarks>
	///   <para>
	///     This class serves as a parent for all grids composed out of
	///     two-dimensional grids stacked on top of each other (currently only
	///     hex- and polar grids).  These grids have a plane (orientation) and
	///     a "depth" (how densely stacked they are). Other than keeping common
	///     values and internal methods in one place, this class has not much
	///     practical use. I recommend yu ignore it, it is documented just for
	///     the sake of completion.
	///   </para>
	/// </remarks>
	public abstract class LayeredGrid : Grid {
#region  Types
		/// <summary>
		///   Arguments object of a depth event.
		/// </summary>
		public class DepthEventArgs : System.EventArgs {
			private readonly float _difference;

			/// <summary>
			///   Instantiate a new object form previous and current value.
			/// </summary>
			/// <para name="previous">
			///   Previous value of the depth.
			/// </para>
			/// <para name="current">
			///   Current value of the depth.
			/// </para>
			public DepthEventArgs(float previous, float current) {
				_difference = current - previous;
			}

			/// <summary>
			///   Change in depth, new value minus old value.
			/// </summary>
			public float Difference {
				get {
					return _difference;
				}
			}
		}

		/// <summary>
		///   Event raised when the depth of the grid changes.
		/// </summary>
		public event System.EventHandler<DepthEventArgs> DepthChanged;
#endregion  // Types

#region  Private variables
		[SerializeField]
		private float _depth = 1.0f;
#endregion  // Private variables

#region  Accessors
		/// <summary>
		///   How far apart layers of the grid are.
		/// </summary>
		/// <value>
		///   Depth of grid layers.
		/// </value>
		/// <remarks>
		///   <para>
		///     Layered grids are made of an infinite number of two-dimensional
		///     grids stacked on top of each other. This determines how far
		///     apart those layers are. The value cannot be lower than
		///     <c>Mathf.Epsilon</c> in order to prevent contradictory values.
		///   </para>
		/// </remarks>
		public float Depth {
			get {
				return _depth;
			} set {
				var previousDepth = _depth;
				_depth = Mathf.Max(value, Mathf.Epsilon);
				OnDepthChanged(previousDepth, _depth);
			}
		}
#endregion  // Accessors

#region  Computed properties
		/// <summary>
		///   Vector between two layers of the grid.
		/// </summary>
		public Vector3 Forward {
			get {
				var axis = transform.forward;
				return Depth * axis;
			}
		}
#endregion  // Computed properties

#region  Hook methods
		protected virtual void OnDepthChanged(float previous, float current) {
			var delta = Mathf.Abs(previous - current);
			_cacheIsDirty |= delta > Mathf.Epsilon;

			if (DepthChanged == null) {
				return;
			}
			var args = new DepthEventArgs(previous, current);
			DepthChanged(this, args);
		}
#endregion
	}
}
