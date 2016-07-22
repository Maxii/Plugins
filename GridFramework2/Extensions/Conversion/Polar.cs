using UnityEngine;
using PolarGrid = GridFramework.Grids.PolarGrid;

namespace GridFramework.Extensions.Conversion {
	/// <summary>
	///   Extension methods for converting values in a polar grid grid.
	/// </summary>
	public static class Polar {
		/// <summary>
		///   Converts an angle in radians to the corresponding sector
		///   coordinate.
		/// </summary>
		/// <returns>
		///   Sector value of the angle.
		/// </returns>
		/// <param name="grid">
		///   The polar grid instance.
		/// </param>
		/// <param name="rad">
		///   Angle in radians.
		/// </param>
		/// <remarks>
		///   <para>
		///     This method takes in an angle and returns in which sector the
		///     angle lies. If the angle exceeds 2π it wraps around, negative
		///     angles are automatically subtracted from 2π.
		///   </para>
		/// </remarks>
		/// <example>
		///   <para>
		///     Let's take a grid with six sectors, then one sector has an
		///     angle of 2π / 6 = 1/3 π, so a 2/3 π angle corresponds to a
		///     sector value of 2.
		///   </para>
		/// </example>
		public static float Rad2Sector(this PolarGrid grid, float rad) {
			const float fullCircle = 2f * Mathf.PI;

			rad %= fullCircle;
			if (rad < 0f) {
				rad += fullCircle;
			}
			var sector = rad / grid.Radians;

			return sector;
		}

		/// <summary>
		///   Converts an angle in degrees to the corresponding sector
		///   coordinate.
		/// </summary>
		/// <returns>
		///   Sector value of the angle.
		/// </returns>
		/// <param name="grid">
		///   The polar grid instance.
		/// </param>
		/// <param name="deg">
		///   Angle in degrees.
		/// </param>
		/// <remarks>
		///   <para>
		///     This method takes in an angle and returns in which sector the
		///     angle lies. If the angle exceeds 360° it wraps around, negative
		///     angles are automatically subtracted from 360°.
		///   </para>
		/// </remarks>
		/// <example>
		///   <para>
		///     Let's take a grid with six sectors, then one sector has an
		///     angle of 360° / 6 = 60°, so a 135° angle corresponds to a
		///     sector value of 130° / 60° = 2.25.
		///   </para>
		/// </example>
		public static float Deg2Sector(this PolarGrid grid, float deg) {
			const float fullCircle = 360f;

			deg %= fullCircle;
			if (deg < 0f) {
				deg += fullCircle;
			}
		var sector = deg / grid.Degrees;

			return sector;
		}

		/// <summary>
		///   Converts a sector to the corresponding angle coordinate in
		///   radians.
		/// </summary>
		/// <returns>
		///   Radians value of the sector.
		/// </returns>
		/// <param name="grid">
		///   The polar grid instance.
		/// </param>
		/// <param name="sector">
		///   Sector number.
		/// </param>
		/// <remarks>
		///   <para>
		///     This method takes in a sector coordinate and returns the
		///     corresponding angle around the origin. If the sector exceeds
		///     the amount of sectors of the grid it wraps around, negative
		///     sectors are subtracted from the maximum.
		///   </para>
		/// </remarks>
		/// <example>
		///   <para>
		///     Let's take a grid with six sectors, then one sector has an
		///     angle of 2π / 6 = 1/3 π, so a 2/3 π angle corresponds to a
		///     sector value of 2.
		///   </para>
		/// </example>
		public static float Sector2Rad(this PolarGrid grid, float sector) {
			sector %= grid.Sectors;
			if (sector < 0) {
				sector += grid.Sectors;
			}
			var rad = sector * grid.Radians;
			return rad;
		}

		/// <summary>
		///   Converts a sector to the corresponding angle coordinate in
		///   degrees.
		/// </summary>
		/// <returns>
		///   Angle value of the sector.
		/// </returns>
		/// <param name="grid">
		///   The polar grid instance.
		/// </param>
		/// <param name="sector">
		///   Sector number.
		/// </param>
		/// <remarks>
		///   <para>
		///     This method takes in a sector coordinate and returns the
		///     corresponding angle around the origin. If the sector exceeds
		///     the amount of sectors of the grid it wraps around, negative
		///     sectors are automatically subtracted from the maximum.
		///   </para>
		/// </remarks>
		/// <example>
		///   <para>
		///     Let's take a grid with six sectors, then one sector has an
		///     angle of 360° / 6 = 60°, so a 135° angle corresponds to a
		///     sector value of 130° / 60° = 2.25.
		///   </para>
		/// </example>
		public static float Sector2Deg(this PolarGrid grid, float sector) {
			sector %= grid.Sectors;
			if (sector < 0) {
				sector += grid.Sectors;
			}
			var deg = sector * grid.Degrees;
			return deg;
		}

		/// <summary>
		///   Converts an angle around the origin to a rotation.
		/// </summary>
		/// <returns>
		///   Rotation quaternion which rotates around the origin by
		///   <paramref name="rad"/> radians.
		/// </returns>
		/// <param name="grid">
		///   The polar grid instance.
		/// </param>
		/// <param name="rad">
		///   Angle in radians.
		/// </param>
		/// <remarks>
		///   <para>
		///     This method returns a quaternion which represents a rotation
		///     within the grid. The result is a combination of the grid's own
		///     rotation and the rotation from the angle. Since we use an
		///     angle, this method is more suitable for polar coordinates than
		///     grid coordinates. See <see cref="Sector2Rotation"/> for a
		///     similar method that uses sectors.
		///   </para>
		/// </remarks>
		public static Quaternion Rad2Rotation(this PolarGrid grid, float rad) {
			var deg = rad * Mathf.Rad2Deg;
			return Deg2Rotation(grid, deg);
		}

		/// <summary>
		///   Converts an angle around the origin to a rotation.
		/// </summary>
		/// <returns>
		///   Rotation quaternion which rotates around the origin by
		///   <paramref name="deg"/> degrees.
		/// </returns>
		/// <param name="grid">
		///   The polar grid instance.
		/// </param>
		/// <param name="deg">
		///   Angle in degrees.
		/// </param>
		/// <remarks>
		///   <para>
		///     This method returns a quaternion which represents a rotation
		///     within the grid. The result is a combination of the grid's own
		///     rotation and the rotation from the angle. Since we use an
		///     angle, this method is more suitable for polar coordinates than
		///     grid coordinates. See <see cref="Sector2Rotation"/> for a
		///     similar method that uses sectors.
		///   </para>
		/// </remarks>
		public static Quaternion Deg2Rotation(this PolarGrid grid, float deg) {
			var axis = -grid.Forward;
			var rot = Quaternion.AngleAxis(deg, axis);
			return rot * grid.transform.rotation;
		}

		/// <summary>
		///   Converts a sector around the origin to a rotation.
		/// </summary>
		/// <returns>
		///   Rotation quaternion which rotates around the origin.
		/// </returns>
		/// <param name="grid">
		///   The polar grid instance.
		/// </param>
		/// <param name="sector">
		///   Sector coordinate inside the grid.
		/// </param>
		/// <remarks>
		///   <para>
		///     This is basically the same as <see cref="Rad2Rotation"/> and
		///     <see cref="Deg2Rotation"/>, except with sectors, which makes
		///     this method more suitable for grid coordinates than polar
		///     coordinates.
		///   </para>
		/// </remarks>
		public static Quaternion Sector2Rotation(this PolarGrid grid, float sector) {
			var deg = Sector2Deg(grid, sector);
			return Deg2Rotation(grid, deg);
		}

		/// <summary>
		///   Converts a world position to a rotation around the origin.
		/// </summary>
		/// <returns>
		///   The rotation.
		/// </returns>
		/// <param name="grid">
		///   The polar grid instance.
		/// </param>
		/// <param name="worldPoint">
		///   Point in world space.
		/// </param>
		/// <remarks>
		///   <para>
		///     This method compares the point's position in world space to the
		///     grid and then returns which rotation an object should have if
		///     it was at that position and rotated around the grid.
		///   </para>
		/// </remarks>
		public static Quaternion World2Rotation(this PolarGrid grid, Vector3 worldPoint) {
			var polar = grid.WorldToPolar(worldPoint);
			var rad = polar.y;
			return Rad2Rotation(grid, rad);
		}

		/// <summary>
		///   Converts a world position to an angle around the origin.
		/// </summary>
		/// <returns>
		///   Angle between the point and the grid's "right" axis in radians.
		/// </returns>
		/// <param name="grid">
		///   The polar grid instance.
		/// </param>
		/// <param name="world">
		///   Point in world space.
		/// </param>
		/// <remarks>
		///   <para>
		///     This method returns which angle around the grid a given point
		///     in world space has.
		///   </para>
		/// </remarks>
		public static float World2Rad(this PolarGrid grid, Vector3 world) {
			var polar = grid.WorldToPolar(world);
			return polar.y;
		}


		/// <summary>
		///   Converts a world position to an angle around the origin.
		/// </summary>
		/// <returns>
		///   Angle between the point and the grid's "right" axis in degrees.
		/// </returns>
		/// <param name="grid">
		///   The polar grid instance.
		/// </param>
		/// <param name="world">
		///   Point in world space.
		/// </param>
		/// <remarks>
		///   <para>
		///     This method returns which angle around the grid a given point
		///     in world space has.
		///   </para>
		/// </remarks>
		public static float World2Deg(this PolarGrid grid, Vector3 world) {
			var polar = grid.WorldToPolar(world);
			return polar.y * Mathf.Rad2Deg;
		}

		/// <summary>
		///   Converts a world position to the sector of the grid it is in.
		/// </summary>
		/// <returns>
		///   Sector the point is in.
		/// </returns>
		/// <param name="grid">
		///   The polar grid instance.
		/// </param>
		/// <param name="world">
		///   Point in world space.
		/// </param>
		/// <remarks>
		///   <para>
		///     This method returns which which sector a given point in world
		///     space is in.
		///   </para>
		/// </remarks>
		public static float World2Sector(this PolarGrid grid, Vector3 world) {
			var gridPoint = grid.WorldToGrid(world);
			return gridPoint.y;
		}

		/// <summary>
		///   Converts a world position to the radius from the origin.
		/// </summary>
		/// <returns>
		///   Radius of the point from the grid.
		/// </returns>
		/// <param name="grid">
		///   The polar grid instance.
		/// </param>
		/// <param name="world">
		///   Point in world space.
		/// </param>
		/// <remarks>
		///   <para>
		///     This method returns the distance of a world point from the
		///     grid's radial axis. This is not the same as the point's
		///     distance from the grid's origin, because it doesn't take
		///     "height" into account. Thus it is always less or equal than the
		///     distance from the origin.
		///   </para>
		/// </remarks>
		public static float World2Radius(this PolarGrid grid, Vector3 world) {
			var polar = grid.WorldToPolar(world);
			return polar.x;
		}
	}
}
