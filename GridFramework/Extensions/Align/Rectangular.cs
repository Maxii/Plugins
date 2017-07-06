using UnityEngine;
using GridFramework.Extensions.Nearest;
using RectGrid = GridFramework.Grids.RectGrid;
using CoordinateSystem = GridFramework.Grids.RectGrid.CoordinateSystem;

namespace GridFramework.Extensions.Align {
	/// <summary>
	///   Extension methods for aligning vectors (position) and transforms to a
	///   rectangular grid.
	/// </summary>
	public static class Rectangular {
		/// <summary>
		///   Aligns a position vector onto the nearest face of the grid.
		/// </summary>
		/// <param name="grid">
		///   The grid instance to extend.
		/// </param>
		/// <param name="vector">
		///   The position in world-coordinates.
		/// </param>
		/// <returns>
		///   Position of the nearest face.
		/// </returns>
		/// <remarks>
		///   <para>
		///     The position will be interpreted to be the position of an
		///     object that happens to fit exactly into a grid cell.
		///   </para>
		/// </remarks>
		public static Vector3 AlignVector3(this RectGrid grid, Vector3 vector) {
			var scale = Vector3.one;
			return AlignVector3(grid, vector, scale);
		}

		/// <summary>
		///   Aligns a position vector onto the grid.
		/// </summary>
		/// <param name="grid">
		///   The grid instance to extend.
		/// </param>
		/// <param name="vector">
		///   The position in world-coordinates.
		/// </param>
		/// <param name="scale">
		///   Used to determine whether to align to a cell or an edge, see
		///   remarks.
		/// </param>
		/// <returns>
		///   Position of the nearest face.
		/// </returns>
		/// <remarks>
		///   <para>
		///     The position will be interpreted to be the position of an
		///     object that has the size <c>scale</c>. If the scale in an odd
		///     multiple of the grid's spacing the position will be aligned to
		///     an edge, otherwise to a cell.
		///   </para>
		/// </remarks>
		public static Vector3 AlignVector3(this RectGrid grid, Vector3 vector, Vector3 scale) {
			const CoordinateSystem system = CoordinateSystem.Grid;

			var vertex  = grid.NearestVertex(vector, system);
			var cell    = grid.NearestCell(  vector, system);
			var spacing = grid.Spacing;
			var aligned = new Vector3();

			for (var i = 0; i < 3; ++i) {
				var fraction = Mathf.Max(scale[i] / spacing[i], 1f);
				var even = Mathf.RoundToInt(fraction) % 2 == 0;

				aligned[i] = even ? vertex[i] : cell[i];
			}

			return grid.GridToWorld(aligned);
		}

		/// <summary>
		///   Aligns a <c>Transform</c> onto the grid.
		/// </summary>
		/// <param name="grid">
		///   The grid instance to extend.
		/// </param>
		/// <param name="transform">
		///   The <c>Transform</c> to align.
		/// </param>
		/// <remarks>
		///   <para>
		///     The exact position depends on the scale of the
		///     <c>Transform</c>, i.e. whether it's an even or odd multiple of
		///     the grid's spacing.
		///   </para>
		/// </remarks>
		public static void AlignTransform(this RectGrid grid, Transform transform) {
			var position = transform.position;
			var scale    = transform.lossyScale;

			transform.position = AlignVector3(grid, position, scale);
		}
	}
}
