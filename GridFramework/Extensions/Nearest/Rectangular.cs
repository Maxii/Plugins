using UnityEngine;
using RectGrid = GridFramework.Grids.RectGrid;
using CSystem = GridFramework.Grids.RectGrid.CoordinateSystem;

namespace GridFramework.Extensions.Nearest {
	/// <summary>
	///   Extension methods for finding the nearest vertex or cell in a
	///   rectangular grid.
	/// </summary>
	public static class Rectangular {
#region  Nearest vertex
		/// <summary>
		///   Returns the position of the nearest vertex.
		/// </summary>
		/// <returns>
		///   Grid position of the nearest vertex.
		/// </returns>
		///
		/// <param name="grid">
		///   The rectangular grid instance.
		/// </param>
		/// <param name="point">
		///   Point in world space.
		/// </param>
		/// <param name="system">
		///   Coordinate system to use.
		/// </param>
		///
		/// <remarks>
		///   Returns the position of the nearest vertex from a given point in
		///   either grid- or world space.
		/// </remarks>
		public static Vector3 NearestVertex(this RectGrid grid, Vector3 point, CSystem system) {
			var gridPoint    = grid.WorldToGrid(point);
			var roundedPoint = RoundVector3(gridPoint);

			return system == CSystem.Grid ? roundedPoint : grid.GridToWorld(roundedPoint);
		}
#endregion  // Nearest Vertex

#region  Nearest cell
		/// <summary>
		///   Returns the grid position of the nearest box.
		/// </summary>
		/// <returns>
		///   Grid position of the nearest box.
		/// </returns>
		/// <param name="grid">
		///   The rectangular grid instance.
		/// </param>
		/// <param name="point">
		///   Point in world space.
		/// </param>
		/// <param name="system">
		///   Coordinate system to use.
		/// </param>
		/// <remarks>
		///   <para>
		///     returns the coordinates of a cell in the grid. Since cell lies
		///     between vertices all three values will always have +0.5
		///     compared to vertex coordinates.
		///   </para>
		/// </remarks>
		/// <example>
		///   <code>
		///     GFRectGrid myGrid;
		///     Vector3 worldPoint;
		///     // something like (2.5, -1.5, 3.5)
		///     Vector3 box = myGrid.NearestBoxG(worldPoint);
		///   </code>
		/// </example>
		public static Vector3 NearestCell(this RectGrid grid, Vector3 point, CSystem system) {
			var shift     = .5f * Vector3.one;

			var gridPoint = grid.WorldToGrid(point);
			var shifted   = gridPoint - shift;
			var rounded   = RoundVector3(shifted);
			var gridCell  = rounded + shift;

			return system == CSystem.Grid ? gridCell : grid.GridToWorld(gridCell);
		}
#endregion  // Nearest cell

#region  Helpers
		private static Vector3 RoundVector3(Vector3 point) {
			for (var i = 0; i < 3; ++i) {
				point[i] = Mathf.Round(point[i]);
			}
			return point;
		}
#endregion  // Helpers
	}
}
