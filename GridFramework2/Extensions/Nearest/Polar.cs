using UnityEngine;
using PolarGrid = GridFramework.Grids.PolarGrid;
using CoordinateSystem = GridFramework.Grids.PolarGrid.CoordinateSystem;

namespace GridFramework.Extensions.Nearest {
	/// <summary>
	///   Extension methods for finding the nearest vertex, face or cell in a
	///   polar grid.
	/// </summary>
	public static class Polar {
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
		public static Vector3 NearestVertex(this PolarGrid grid, Vector3 point, CoordinateSystem system) {
			var gridPoint = grid.WorldToGrid(point);
			for (var i = 0; i < 3; ++i) {
				gridPoint[i] = Mathf.Round(gridPoint[i]);
			}

			return GridToCoordinateSystem(grid, gridPoint, system);
		}
#endregion  // Nearest vertex

#region  Nearest face
		/// <summary>
		///   Returns the position of the nearest face.
		/// </summary>
		/// <returns>
		///   Position of the nearest face.
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
		///
		/// <remarks>
		///   <para>
		///     Returns the coordinates of a face on the grid closest to a
		///     given point. Since the face is enclosed by four vertices, the
		///     returned value is the point in between all four of the
		///     vertices. You also need to specify on which plane the face
		///     lies.
		///   </para>
		/// </remarks>
		public static Vector3 NearestFace(this PolarGrid grid,
			                                    Vector3 point,
			                           CoordinateSystem system) {
			Vector3 gridPoint = grid.WorldToGrid(point);

			gridPoint.x = Mathf.Floor(gridPoint.x) + .5f;
			gridPoint.y = Mathf.Floor(gridPoint.y) + .5f;
			gridPoint.z = Mathf.Round(gridPoint.z);

			return GridToCoordinateSystem(grid, gridPoint, system);
		}
#endregion  // Nearest face

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
		public static Vector3 NearestCell(this PolarGrid grid,
			                                    Vector3 point,
			                           CoordinateSystem system) {
			Vector3 gridPoint = grid.WorldToGrid(point);

			gridPoint.x = Mathf.Floor(gridPoint.x) + .5f;
			gridPoint.y = Mathf.Floor(gridPoint.y) + .5f;
			gridPoint.z = Mathf.Floor(gridPoint.z) + .5f;

			return GridToCoordinateSystem(grid, gridPoint, system);
		}
#endregion  // Nearest cell

#region  Helpers
		private static Vector3 GridToCoordinateSystem(PolarGrid grid, Vector3 gridPoint, CoordinateSystem system) {
			switch (system) {
				case CoordinateSystem.Grid:
					return gridPoint;
				case CoordinateSystem.World:
					return grid.GridToWorld(gridPoint);
				case CoordinateSystem.Cylindric:
					return grid.GridToPolar(gridPoint);
				default:
					var error = string.Format("Error: Coordinate system \"{0}\" unimplemented", system);
					throw new System.ComponentModel.InvalidEnumArgumentException(error);
			}
		}
#endregion  // Helpers
	}
}
