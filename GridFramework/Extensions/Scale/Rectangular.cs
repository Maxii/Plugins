using UnityEngine;
using RectGrid = GridFramework.Grids.RectGrid;
using Grid = GridFramework.Grids.Grid;

namespace GridFramework.Extensions.Scale {
	/// <summary>
	///   Extension methods for scaling vectors (sizes) and transforms to a
	///   rectangular grid.
	/// </summary>
	public static class AlignAndScale {
		/// <summary>
		///   Scales a size-vector to fit inside the grid.
		/// </summary>
		/// <returns>
		///   The re-scaled vector.
		/// </returns>
		/// <param name="grid">
		///   The instance of the grid to extend.
		/// </param>
		/// <param name="vector">
		///   The vector to scale.
		/// </param>
		/// <remarks>
		///   <para>
		///     Scales a size to the nearest multiple of the grid’s spacing.
		///     This ignores the grid's shearing and the result might look
		///     wrong if shearing is used, depending on what you want to
		///     achieve.
		///   </para>
		/// </remarks>
		public static Vector3 ScaleVector3(this RectGrid grid, Vector3 vector) {
			var spacing = grid.Spacing;
			for (int i = 0; i < 3; ++i) {
				vector[i] = Mathf.Round(vector[i] / spacing[i]) * spacing[i];
				// If the vector has been rounded down to zero.
				vector[i] = Mathf.Max(vector[i], spacing[i]);
			}
			return vector;
		}

		/// <summary>
		///   Scales a <c>Transform</c> to fit inside the grid.
		/// </summary>
		/// <param name="grid">
		///   The instance of the grid to extend.
		/// </param>
		/// <param name="transform">
		///   The <c>Transform</c> to scale.
		/// </param>
		/// <remarks>
		///   <para>
		///     This method scales the <c>Transform</c> globally, which will
		///     work without issues if the <c>Transform</c> is not a child or a
		///     child but not rotated. However, if it is a rotated child of
		///     another <c>Transform</c> all the caveats of
		///     <c>Transform.lossyScale</c> apply.
		///   </para>
		/// </remarks>
		public static void ScaleTransform(this RectGrid grid, Transform transform) {
			var scale = ScaleVector3(grid, transform.lossyScale);
			var parent = transform.parent;
			if (!parent) {
				transform.localScale = scale;
				return;
			}
			var localScale = transform.localScale;
			for (int i = 0; i < 3; ++i) {
				localScale[i] = scale[i] / parent.localScale[i];
			}
			transform.localScale = localScale;
		}
	}
}

