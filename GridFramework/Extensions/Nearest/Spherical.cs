using UnityEngine;
using SphereGrid = GridFramework.Grids.SphereGrid;
using CoordinateSystem = GridFramework.Grids.SphereGrid.CoordinateSystem;

namespace GridFramework.Extensions.Nearest {
	/// <summary>
	///   Extension methods for finding the nearest vertex, face or cell in a
	///   spherical grid.
	/// </summary>
	public static class Spherical {
#region  Nearest vertex
		/// <summary>
		///   Returns the position of the nearest vertex.
		/// </summary>
		/// <returns>
		///   Position of the nearest vertex.
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
		public static Vector3 NearestVertex(this SphereGrid grid, Vector3 point, CoordinateSystem system) {
			var gridVertex = RoundPoint(grid.WorldToGrid(point));
			if (system == CoordinateSystem.Grid) {
				return gridVertex;
			}
			return system == CoordinateSystem.World ? grid.GridToWorld(gridVertex)
				                                    : grid.GridToSpheric(gridVertex);
		}
#endregion  // Nearest vertex

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
		public static Vector3 NearestCell(this SphereGrid grid,
			                                    Vector3 point,
			                           CoordinateSystem system) {
			var gridCell = RoundPoint(grid.WorldToGrid(point) - 0.5f * Vector3.one) + 0.5f * Vector3.one;

			if (system == CoordinateSystem.Grid) {
				return gridCell;
			}
			return system == CoordinateSystem.World ? grid.GridToWorld(gridCell)
				                                    : grid.GridToSpheric(gridCell);
		}
#endregion  // Nearest cell

#region  Helpers
		private static Vector3 RoundPoint(Vector3 point) {
			return RoundPoint(point, Vector3.one);
		}

		private static Vector3 RoundPoint(Vector3 point, Vector3 multi) {
			for (int i = 0; i < 3; i++) {
				point[i] = RoundMultiple(point[i], multi[i]);
			}
			return point;
		}

		// returns the a number rounded to the nearest multiple of anothr number (rounds up or down)
		private static float RoundMultiple(float number, float multiple) {
			// could use Ceil or Floor to always round up or down
			return Mathf.Round(number / multiple) * multiple;
		}
		/// The normal X-, Y- and Z- vectors in world-space.
		private static Vector3[] Units {
			get {
				return new []{Vector3.right, Vector3.up, Vector3.forward};
			}
		}
#endregion  // Helpers
	}
}
