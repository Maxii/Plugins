using UnityEngine;
using GridFramework.Extensions.Nearest;
using HexGrid = GridFramework.Grids.HexGrid;
using CoordinateSystem = GridFramework.Grids.HexGrid.CoordinateSystem;

namespace GridFramework.Extensions.Align {
	/// <summary>
	///   Extension methods for aligning vectors (position) and transforms to a
	///   hexagonal grid.
	/// </summary>
	public static class Hexagonal {
		/// <summary>
		///   Aligns a position vector onto the nearest face of the grid.
		/// </summary>
		/// <param name="grid">
		///   Instance of the grid to extend.
		/// </param>
		/// <param name="vector">
		///   The position in world-coordinates.
		/// </param>
		/// <returns>
		///   Position of the nearest face.
		/// </returns>
		/// <remarks>
		///   <para>
		///     This is identical to the extension method for finding the
		///     nearest face.
		///   </para>
		/// </remarks>
		public static Vector3 AlignVector3(this HexGrid grid, Vector3 vector) {
			return grid.NearestFace(vector, CoordinateSystem.World);
		}

		/// <summary>
		///   Aligns a <c>Transform</c> vector onto the nearest face of the
		///   grid.
		/// </summary>
		/// <param name="grid">
		///   Instance of the grid to extend.
		/// </param>
		/// <param name="transform">
		///   The <c>Transform</c> to align.
		/// </param>
		/// <remarks>
		///   <para>
		///     This extension method is pretty dumb because it does not take
		///     the size of an object into account. There are many ways to
		///     "align" on object to a hex grid and they depend on the shape of
		///     the object, there is no general way of doing it. This method
		///     mainly serves as an example for how to make your own extension
		///     method.
		///   </para>
		/// </remarks>
		public static void AlignTransform(this HexGrid grid, Transform transform) {
			transform.position = AlignVector3(grid, transform.position);
		}
	}
}
