using UnityEngine;
using Grid = GridFramework.Grids.Grid;

namespace GridFramework.Grids {
	/// <summary>
	///   A spherical grid: latitude, longitude and altitude.
	/// </summary>
	/// <remarks>
	///   <para>
	///   Spherical  grids are  round 3D  grids defined  by the  radius of  the
	///   sphere,  the number  of parallel  circles running  orthogonal to  the
	///   sphere's polar axis and the number or meridians running from one pole
	///   to the other along the surface of the sphere.
	///   </para>
	///   <para>
	///     This type of grid as three coordinate systems:  spherical, grid and
	///     geographical.  Spherical- and geographic coordinates are similar to
	///     each other, except in how they thread the meridian coordinate.
	///   </para>
	///   <para>
	///     The north pole  is in positive  Y-direction from the  origin of the
	///     grid,  the south pole is opposite of it,  the vector from south- to
	///     north pole is the  axis of rotation.  The direction of rotation for
	///     longitude is counter-clockwise  when the rotation  axis is pointing
	///     towards the  observer,  or in  other words  eastwards.  The primary
	///     meridian alignes with the object's Z-axis (forward).
	///   </para>
	///   <list type="table">
	///     <listheader>
	///       <term>
	///         Coordinate systems
	///       </term>
	///       <description>
	///         Description
	///       </description>
	///     </listheader>
	///     <item>
	///       <term>
	///         Spherical coordinates
	///       </term>
	///       <description>
	///         Vectors are given as radius, polar angle and azimuth angle. The
	///         radius is the  distance from the  origin of  the grid,  and the
	///         polar angle is the angle between the vector and the polar axis.
	///         The azimuth angle is the angle from the prime-meridian, between
	///         0 and 2π, similar to the angle in polar coordinates.
	///       </description>
	///     </item>
	///     <item>
	///       <term>
	///         Geographic coordinates
	///       </term>
	///       <description>
	///         Vectors are  given as  altitude,  latitude and  longitude.  The
	///         altitude is the distance  from the surface of the first sphere,
	///         the  latitude  is the  same as  the  polar  angle in  spherical
	///         coordinates.  The longitude  is positive  to the  right of  the
	///         primary meridian  and negative  to the  left.  The sign  of the
	///         longitude opposite of the prime meridian is undefined.
	///       </description>
	///     </item>
	///     <item>
	///       <term>
	///         Grid coordinates
	///       </term>
	///       <description>
	///         This is  similar to the  spheric grid,  the  only difference is
	///         that values  are not  in absolute distance  and angles,  but in
	///         relative number of spheres, parallels and meridians.
	///       </description>
	///     </item>
	///   </list>
	/// </remarks>
	[AddComponentMenu("Grid Framework/Grids/SphereGrid")]
	public sealed class SphereGrid : Grid {

#region  Types
		/// <summary>
		///   The various coordinate systems supported by spherical grids.
		/// </summary>
		public enum CoordinateSystem {
			/// <summary>
			///   World-space coordinates.
			/// </summary>
			World,

			/// <summary>
			///   Grid coordinates, multiples of <see cref="Radius"/>,
			///   <see cref="Meridians"/> and <see cref="Parallels"/>.
			/// </summary>
			Grid,

			/// <summary>
			///   Spherical coordinates given by distance from the origin,
			///   angle from the polar axis and angle around the polar axis.
			/// </summary>
			Spherical,

			/// <summary>
			///   Geographic coordinates given by distance from the surface,
			///   angle from the equatorial plane and angle from the null
			///   meridian plane.
			/// </summary>
			Geographic,
		}

		/// <summary>
		///   Arguments object of an radius event.
		/// </summary>
		public class RadiusEventArgs : System.EventArgs {
			private readonly float _difference;

			/// <summary>
			///   Instantiate a new object form previous and current value.
			/// </summary>
			/// <para name="previous">
			///   Previous value of the radius.
			/// </para>
			/// <para name="current">
			///   Current value of the radius.
			/// </para>
			public RadiusEventArgs(float previous, float current) {
				_difference = current - previous;
			}

			/// <summary>
			///   Change in radius, new value minus old value.
			/// </summary>
			public float Difference {
				get {
					return _difference;
				}
			}
		}

		/// <summary>
		///   Arguments object of a parallels- or meridians event.
		/// </summary>
		public class LinesEventArgs : System.EventArgs {
			private readonly int _parallelsDifference;
			private readonly int _meridiansDifference;

			/// <summary>
			///   Instantiate a new object form previous and current values.
			/// </summary>
			/// <para name="previousParallels">
			///   Previous amount of parallel lines.
			/// </para>
			/// <para name="currentParallels">
			///   Current amount of parallel lines.
			/// </para>
			/// <para name="previousMeridians">
			///   Previous amount of meridian lines.
			/// </para>
			/// <para name="currentMeridians">
			///   Current amount of meridian lines.
			/// </para>
			public LinesEventArgs(int previousParallels, int currentParallels, 
				                  int previousMeridians, int currentMeridians) {
				_parallelsDifference = currentParallels - previousParallels;
				_meridiansDifference = currentMeridians - previousMeridians;
			}

			/// <summary>
			///   Change in parallel lines, new value minus old value.
			/// </summary>
			public int ParallelsDifference {
				get {
					return _parallelsDifference;
				}
			}

			/// <summary>
			///   Change in meridian lines, new value minus old value.
			/// </summary>
			public int MeridiansDifference {
				get {
					return _meridiansDifference;
				}
			}
		}

		/// <summary>
		///   Arguments object of a smoothness for parallel or meridian lines
		///   event.
		/// </summary>
		public class SmoothnessEventArgs : System.EventArgs {
			private readonly int _parallelsDifference;
			private readonly int _meridiansDifference;

			/// <summary>
			///   Instantiate a new object form previous and current values.
			/// </summary>
			/// <param name="previousParallels">
			///   Previous smoothness of parallel lines.
			/// </param>
			/// <param name="currentParallels">
			///   Current smoothness of parallel lines.
			/// </param>
			/// <param name="previousMeridians">
			///   Previous smoothness of meridian lines.
			/// </param>
			/// <param name="currentMeridians">
			///   Current smoothness of meridian lines.
			/// </param>
			public SmoothnessEventArgs(int previousParallels, int currentParallels, 
				                  int previousMeridians, int currentMeridians) {
				_parallelsDifference = currentParallels - previousParallels;
				_meridiansDifference = currentMeridians - previousMeridians;
			}

			/// <summary>
			///   Change in parallel smoothness, new value minus old value.
			/// </summary>
			public int ParallelsDifference {
				get {
					return _parallelsDifference;
				}
			}

			/// <summary>
			///   Change in meridian smoothness, new value minus old value.
			/// </summary>
			public int MeridiansDifference {
				get {
					return _meridiansDifference;
				}
			}
		}

		/// <summary>
		///   Event raised when the radius of the grid changes.
		/// </summary>
		public event System.EventHandler<RadiusEventArgs> RadiusChanged;

		/// <summary>
		///   Event raised when the number of parallel or meridian lines of the
		///   grid changes.
		/// </summary>
		public event System.EventHandler<LinesEventArgs> LinesChanged;

		/// <summary>
		///   Event raised when the smoothness of parallel or meridian lines of
		///   the grid changes.
		/// </summary>
		public event System.EventHandler<SmoothnessEventArgs> SmoothnessChanged;
#endregion  // Types

#region  Private member variables
		[SerializeField] private float _radius = 1.0f;

		[SerializeField] private int _meridians = 19;
		[SerializeField] private int _parallels = 36;
#endregion  // Private member variables

#region  Accessors
		/// <summary>
		///   Radius of each sphere.
		/// </summary>
		/// <remarks>
		///   <para>
		///     The radius has to be greater than zero.
		///   </para>
		/// </remarks>
		public float Radius {
			get {
				return _radius;
			} set {
				var previousRadius = _radius;
				_radius = Mathf.Max(value, Mathf.Epsilon);
				OnRadiusChanged(previousRadius, _radius);
			}
		}

		/// <summary>
		///   Number of meridian lines.
		/// </summary>
		/// <value>
		///   Number of parallels, at least 1.
		/// </value>
		/// <remarks>
		///   There has to be at least one meridian, which is also the prime
		///   meridian.
		/// </remarks>
		public int Parallels {
			get {
				return _parallels;
			} set {
				var previousParallels = _parallels;
				_parallels = Mathf.Max(value, 1);
				OnLinesChanged(previousParallels, _parallels, Meridians, Meridians);
			}
		}

		/// <summary>
		///   Number of parallel lines.
		/// </summary>
		/// <value>
		///   Number of parallels, at least 1.
		/// </value>
		/// <remarks>
		///   There has to be at least one parallel, which is also the equator.
		/// </remarks>
		public int Meridians {
			get {
				return _meridians;
			} set {
				var previousMeridians = _meridians;
				_meridians = Mathf.Max(value, 1);
				OnLinesChanged(Parallels, Parallels, previousMeridians, _meridians);
			}
		}

#endregion  // Accessors
	
#region  Computed properties
		/// <summary>
		///   Polar angle between two parallels in radians.
		/// </summary>
		/// <value>
		///   Polar angle between two parallels in radians.
		/// </value>
		/// <remarks>
		///   <para>
		///     When assigning a value only values of the form <c>π/n</c> are
		///     valid, everything else will be rounded to the nearest possible
		///     value.
		///   </para>
		/// </remarks>
		public float Polar {
			get {
				return Mathf.PI / (Parallels - 1);
			} set {
				Parallels = Mathf.RoundToInt(Mathf.PI / value) + 1;
			}
		}
	
		/// <summary>
		///   Polar angle between two parallels in degrees.
		/// </summary>
		/// <value>
		///   Polar angle between two parallels in degrees.
		/// </value>
		/// <remarks>
		///   <para>
		///     When assigning a value only values of the form <c>360 / 2^n</c>
		///     are valid, everything else will be rounded to the nearest
		///     possible value.
		///   </para>
		/// </remarks>
		public float PolarDeg {
			get {
				return 180f * Polar / Mathf.PI;
			} set {
				Polar = value * Mathf.PI / 180f;
			}
		}
	
		/// <summary>
		///   Azimuth angle between two meridians in radians.
		/// </summary>
		/// <value>
		///   Azimuth angle between two meridians in radians.
		/// </value>
		/// <remarks>
		///   <para>
		///     When assigning a value only values of the form <c>2π / 2^n</c>
		///     are valid, everything else will be rounded to the nearest
		///     possible value.
		///   </para>
		/// </remarks>
		public float Azimuth {
			get {
				return 2f * Mathf.PI / Meridians;
			} set {
				Meridians = Mathf.RoundToInt(2f * Mathf.PI / value);
			}
		}
	
		/// <summary>
		///   Azimuth angle between two meridians in degrees.
		/// </summary>
		/// <value>
		///   Azimuth angle between two meridians in degrees.
		/// </value>
		/// <remarks>
		///   <para>
		///     When assigning a value only values of the form <c>360 / 2^n</c>
		///     are valid, everything else will be rounded to the nearest
		///     possible value.
		///   </para>
		/// </remarks>
		public float AzimuthDeg {
			get {
				return 180f * Azimuth / Mathf.PI;
			} set {
				Azimuth = value * Mathf.PI / 180f;
			}
		}
#endregion  // Computed properties
	
#region  Cached members
		// World <-> Aligned
		private Matrix4x4 _waMatrix = Matrix4x4.identity;
		private Matrix4x4 _awMatrix = Matrix4x4.identity;
	
		// Grid <-> Spheric
		private Matrix4x4 _gsMatrix = Matrix4x4.identity;
		private Matrix4x4 _sgMatrix = Matrix4x4.identity;
	
		/// <summary>
		///   Reads the value of the World->Aligned matrix (read only).
		/// </summary>
		/// <value>
		///   The World->Aligned matrix.
		/// </value>
		/// <remarks>
		///   <para>
		///     If the Matrices have to be updated they will do so first.
		///   </para>
		/// </remarks>
		private Matrix4x4 waMatrix {
			get {UpdateCachedMembers(); return _waMatrix;}
		}
	
		/// <summary>
		///   Reads the value of the Aligned->World matrix (read only).
		/// </summary>
		/// <value>
		///   The Aligned->World matrix.
		/// </value>
		/// <remarks>
		///   <para>
		///     If the Matrices have to be updated they will do so first.
		///   </para>
		/// </remarks>
		private Matrix4x4 awMatrix {
			get {UpdateCachedMembers(); return _awMatrix;}
		}
	
		/// <summary>
		///   Reads the value of the Grid->Spheric matrix (read only).
		/// </summary>
		/// <value>
		///   The Grid->Spheric matrix.
		/// </value>
		/// <remarks>
		///   <para>
		///     If the Matrices have to be updated they will do so first.
		///   </para>
		/// </remarks>
		private Matrix4x4 gsMatrix {
			get {UpdateCachedMembers(); return _gsMatrix;}
		}
	
		/// <summary>
		///   Reads the value of the Spheric->Grid matrix (read only).
		/// </summary>
		/// <value>
		///   The Spheric->Grid matrix.
		/// </value>
		/// <remarks>
		///   <para>
		///     If the Matrices have to be updated they will do so first.
		///   </para>
		/// </remarks>
		private Matrix4x4 sgMatrix {
			get {UpdateCachedMembers(); return _sgMatrix;}
		}
	
		/// <summary>
		///   Update all the matrices for coordinate conversion when necessary.
		/// </summary>
		protected override void UpdateCachedMembers() {
			if (!_cacheIsDirty && !TransfromHasChanged()) {
				return;
			}
			_awMatrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
			_waMatrix = _awMatrix.inverse;

			var spacing = new Vector3(Radius, Polar, Azimuth);
			_gsMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, spacing);
			_sgMatrix = _gsMatrix.inverse;

			_cacheIsDirty = false;
		}
#endregion  // Cached members
	
#region  Coordinate conversion to World
		/// <summary>
		///   Converts a point from spheric- to world coordinates.
		/// </summary>
		///
		/// <param name="spheric">
		///   Spherical coordinates of the point to convert.
		/// </param>
		///
		/// <remarks>
		///   <para>
		///     Takes in a position in world space and computes where in the sphere
		///     that position is. The origin of the grid is the world position of
		///     its <c>GameObject</c> and its axes lie on the corresponding axes of
		///     the Transform.  Rotation is taken into account for this operation.
		///   </para>
		/// </remarks>
		public Vector3 SphericToWorld(Vector3 spheric) {
			// Aligned point
			// r * (sin(theta) sin(phi), cos(theta), sin(theta) cos(phi))
			spheric.z = 2.0f * Mathf.PI - spheric.z;
			var x = spheric.x * Mathf.Sin(spheric.y) * Mathf.Sin(spheric.z);
			var y = spheric.x * Mathf.Cos(spheric.y);
			var z = spheric.x * Mathf.Sin(spheric.y) * Mathf.Cos(spheric.z);
			var local = new Vector3(x, y, z);

			return awMatrix.MultiplyPoint3x4(local);
		}
	
		/// <summary>
		///   Converts a point from geographic- to world coordinates.
		/// </summary>
		/// <param name="geographic">
		///   Geographic coordinates of the point to convert.
		/// </param>
		/// <remarks>
		///   <para>
		///     Takes in a position in geographic space and computes where in the
		///     sphere that position is. The origin of the grid is the world
		///     position of its <c>GameObject</c> and its axes lie on the
		///     corresponding axes of the Transform.  Rotation is taken into
		///     account for this operation.
		///   </para>
		/// </remarks>
		public Vector3 GeographicToWorld(Vector3 geographic) {
			var s = GeographicToSpheric(geographic);
			return SphericToWorld(s);
		}
	
		/// <summary>
		///   Converts a point from geographic- to world coordinates.
		/// </summary>
		/// <param name="grid">
		///   Grid coordinates of the point to convert.
		/// </param>
		/// <remarks>
		///   <para>
		///     Takes in a position in grid space and computes where in the sphere
		///     that position is. The origin of the grid is the world position of
		///     its <c>GameObject</c> and its axes lie on the corresponding axes of
		///     the Transform.  Rotation is taken into account for this operation.
		///   </para>
		/// </remarks>
		public Vector3 GridToWorld(Vector3 grid) {
			return SphericToWorld(GridToSpheric(grid));
		}
#endregion  // Coordinate conversion from World
	
#region  Coordinate conversion to Spheric
		/// <summary>
		///   Converts a point from world- to spheric coordinates.
		/// </summary>
		/// <param name="world">
		///   World coordinates of the point to convert.
		/// </param>
		/// <remarks>
		///   <para>
		///     Takes in a position in world space and computes where in the sphere
		///     that position is. The origin of the grid is the world position of
		///     its <c>GameObject</c> and its axes lie on the corresponding axes of
		///     the Transform. Rotation is taken into account for this operation.
		///   </para>
		/// </remarks>
		public Vector3 WorldToSpheric(Vector3 world) {
			// Aligned point
			var a = waMatrix.MultiplyPoint3x4(world);
	
			var r     = a.magnitude;
			var theta = r >= Mathf.Epsilon ? Mathf.Acos(a.y / r) : 0.0f;
			var phi   = Mathf.Atan2(a.x , a.z);
	
			if (phi < 0) {
				phi = 2*Mathf.PI + phi;
			}
			phi = 2.0f * Mathf.PI - phi;
	
			return new Vector3(r, theta, phi);
		}
	
		/// <summary>
		///   Converts a point from geographic- to spheric coordinates.
		/// </summary>
		/// <param name="geographic">
		///   World coordinates of the point to convert.
		/// </param>
		/// <remarks>
		///   <para>
		///     Takes in a position in geographic space and computes where in the
		///     sphere that position is. The origin of the grid is the world
		///     position of its <c>GameObject</c> and its axes lie on the
		///     corresponding axes of the Transform. Rotation is taken into account
		///     for this operation.
		///   </para>
		/// </remarks>
		public Vector3 GeographicToSpheric(Vector3 geographic) {
			geographic.x += Radius;
			geographic.y = Mathf.PI / 2.0f - geographic.y;
			if (geographic.z < 0.0f) {
				geographic.z = 2.0f * Mathf.PI + geographic.z;
			}
			return geographic;
		}
	
		/// <summary>
		///   Converts a point from grid- to spheric coordinates.
		/// </summary>
		/// <param name="grid">
		///   Grid coordinates of the point to convert.
		/// </param>
		/// <remarks>
		///   <para>
		///     Takes in a position in grid space and computes where in the sphere
		///     that position is. The origin of the grid is the world position of
		///     its <c>GameObject</c> and its axes lie on the corresponding axes of
		///     the Transform. Rotation is taken into account for this operation.
		///   </para>
		/// </remarks>
		public Vector3 GridToSpheric(Vector3 grid) {
			return gsMatrix.MultiplyPoint3x4(grid);
		}
#endregion  // Coordinate conversion to Spheric
	
#region  Coordinate conversion to Geographic
		/// <summary>
		///   Converts a point from world- to geometric coordinates.
		/// </summary>
		/// <param name="world">
		///   World coordinates of the point to convert.
		/// </param>
		/// <remarks>
		///   <para>
		///     Takes in a position in world space and computes where on the sphere
		///     that position is. The origin of the grid is the world position of
		///     its <c>GameObject</c> and its axes lie on the corresponding axes of
		///     the Transform. Rotation is taken into account for this operation.
		///   </para>
		/// </remarks>
		public Vector3 WorldToGeographic(Vector3 world) {
			var s = WorldToSpheric(world);
			return SphericToGeographic(s);
		}
	
		/// <summary>
		///   Converts a point from spheric- to geometric coordinates.
		/// </summary>
		/// <param name="spheric">
		///   Spheric coordinates of the point to convert.
		/// </param>
		/// <remarks>
		///   <para>
		///     Takes in a position in spheric space and computes where on the
		///     sphere that position is. The origin of the grid is the world
		///     position of its <c>GameObject</c> and its axes lie on the
		///     corresponding axes of the Transform. Rotation is taken into account
		///     for this operation.
		///   </para>
		/// </remarks>
		public Vector3 SphericToGeographic(Vector3 spheric) {
			spheric.x -= Radius;
			spheric.y = Mathf.PI / 2.0f - spheric.y;
			if (spheric.z > Mathf.PI) {
				spheric.z  = -1.0f * (2.0f * Mathf.PI - spheric.z);
			}
	
			return spheric;
		}
	
		/// <summary>
		///   Converts a point from grid- to geometric coordinates.
		/// </summary>
		/// <param name="grid">
		///   Grid coordinates of the point to convert.
		/// </param>
		/// <remarks>
		///   <para>
		///     Takes in a position in grid space and computes where on the sphere
		///     that position is. The origin of the grid is the world position of
		///     its <c>GameObject</c> and its axes lie on the corresponding axes of
		///     the Transform. Rotation is taken into account for this operation.
		///   </para>
		/// </remarks>
		public Vector3 GridToGeographic(Vector3 grid) {
			Vector3 s = GridToSpheric(grid);
			return SphericToGeographic(s);
		}
#endregion
	
#region  Coordinate conversion to Grid
		/// <summary>
		///   Converts a point from world- to grid coordinates.
		/// </summary>
		/// <param name="world">
		///   World coordinates of the point to convert.
		/// </param>
		/// <remarks>
		///   <para>
		///     Takes in a position in world space and computes where in the grid
		///     that position is. The origin of the grid is the world position of
		///     its <c>GameObject</c> and its axes lie on the corresponding axes of
		///     the Transform. Rotation is taken into account for this operation.
		///   </para>
		/// </remarks>
		public Vector3 WorldToGrid(Vector3 world) {
			var s = WorldToSpheric(world);
			return SphericToGrid(s);
		}
	
		/// <summary>
		///   Converts a point from spheric- to grid coordinates.
		/// </summary>
		/// <param name="spheric">
		///   Spheric coordinates of the point to convert.
		/// </param>
		/// <remarks>
		///   <para>
		///     Takes in a position in spheric space and computes where in the grid
		///     that position is. The origin of the grid is the world position of
		///     its <c>GameObject</c> and its axes lie on the corresponding axes of
		///     the Transform. Rotation is taken into account for this operation.
		///   </para>
		/// </remarks>
		public Vector3 SphericToGrid(Vector3 spheric) {
			return sgMatrix.MultiplyPoint3x4(spheric);
		}
	
		/// <summary>
		///   Converts a point from geographic- to grid coordinates.
		/// </summary>
		/// <param name="geographic">
		///   Geographic coordinates of the point to convert.
		/// </param>
		/// <remarks>
		///   <para>
		///     Takes in a position in geographic space and computes where in the
		///     grid that position is. The origin of the grid is the world position
		///     of its <c>GameObject</c> and its axes lie on the corresponding axes
		///     of the Transform. Rotation is taken into account for this
		///     operation.
		///   </para>
		/// </remarks>
		public Vector3 GeographicToGrid(Vector3 geographic) {
			Vector3 s = GeographicToSpheric(geographic);
			return SphericToGrid(s);
		}
#endregion  // Coordinate conversion to Grid
	
#region  Hook methods
		private void OnRadiusChanged(float previous, float current) {
			var delta = Mathf.Abs(previous - current);
			_cacheIsDirty |= delta > Mathf.Epsilon;

			if (RadiusChanged == null) {
				return;
			}

			var args = new RadiusEventArgs(previous, current);
			RadiusChanged(this, args);
		}

		private void OnLinesChanged(int previousParallels, int currentParallels,
			                        int previousMeridians, int currentMeridians) {
			var deltaParallels = Mathf.Abs(previousParallels - currentParallels);
			var deltaMeridians = Mathf.Abs(previousMeridians - currentMeridians);
			_cacheIsDirty |= deltaParallels > Mathf.Epsilon;
			_cacheIsDirty |= deltaMeridians > Mathf.Epsilon;

			if (LinesChanged == null) {
				return;
			}

			var args = new LinesEventArgs(previousParallels, currentParallels,
				                          previousMeridians, currentMeridians);
			LinesChanged(this, args);
		}

		private void OnSmoothnessChanged(int previousParallels, int currentParallels,
			                             int previousMeridians, int currentMeridians) {
			var deltaParallels = Mathf.Abs(previousParallels - currentParallels);
			var deltaMeridians = Mathf.Abs(previousMeridians - currentMeridians);
			_cacheIsDirty |= deltaParallels > Mathf.Epsilon;
			_cacheIsDirty |= deltaMeridians > Mathf.Epsilon;

			if (SmoothnessChanged == null) {
				return;
			}

			var args = new SmoothnessEventArgs(previousParallels, currentParallels,
				                               previousMeridians, currentMeridians);
			SmoothnessChanged(this, args);
		}
#endregion  // Hook methods
	}
}
