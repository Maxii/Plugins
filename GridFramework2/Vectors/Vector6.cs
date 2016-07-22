using UnityEngine;
using System;

namespace GridFramework.Vectors {
	/// <summary>
	///   Class representing six adjacent float values for the shearing of a
	///   rectangular grid.
	/// </summary>
	/// <remarks>
	///   <para>
	///     This struct is based on Unity's own <c>Vector3</c> struct and can
	///     be used in a similar way. It resides in Grid Framework's own
	///     namespace to prevent collision with similar types from other
	///     plugins or a future official Unity <c>Vector6</c> type.
	///   </para>
	///   <para>
	///     The API is mostly the same as for <c>Vector3</c>, except in places
	///     where it wouldn't make sense. The individual components are named
	///     to reflect their shearing nature according to the following
	///     pattern: <c>ab</c> where <c>a</c> is either <c>X</c>, <c>Y</c> or
	///     <c>Z</c> and <c>b</c> is another axis different from <c>a</c>. Two
	///     adjacent values belonging to the same axis can also be accessed as
	///     a <c>Vector2</c>, like for example <c>YX</c> and <c>YZ</c>, but not
	///     <c>XZ</c> and <c>YX</c>.
	///   </para>
	///   <para>
	///     The individual components are indexed as follows: <c>XY</c>=0,
	///     <c>XZ</c>=1, <c>YX</c>=2, <c>YZ</c>=3, <c>ZX</c>=4, <c>ZY</c>=5.
	///   </para>
	/// </remarks>
	[System.Serializable]
	public struct Vector6 : IEquatable<Vector6> {

#region  Types
		/// <summary>
		///   Enumeration of the possible axis combinations.
		/// </summary>
		/// <remarks>
		///   <para>
		///     The enumeration's corresponding integer numbers start at 0 and
		///     increment by one.
		///   </para>
		/// </remarks>
		public enum Axis2 {YZ, ZX, XY, ZY, YX, XZ};

		/// <summary>
		///   Enumeration of the three possible axes that can be sheared.
		/// </summary>
		public enum Axis {X, Y, Z};
#endregion  // Types

#region  Private variables
		/// <summary>
		///   The number of items in the vector.
		/// </summary>
		const int SIZE = 6;

		/// <summary>
		///   Shearing of the <c>Y</c> axis in <c>Z</c> direction.
		/// </summary>
		[SerializeField] private float _yz;

		/// <summary>
		///   Shearing of the <c>Z</c> axis in <c>X</c> direction.
		/// </summary>
		[SerializeField] private float _zx;

		/// <summary>
		///   Shearing of the <c>X</c> axis in <c>Y</c> direction.
		/// </summary>
		[SerializeField] private float _xy;

		/// <summary>
		///   Shearing of the <c>Z</c> axis in <c>Y</c> direction.
		/// </summary>
		[SerializeField] private float _zy;

		/// <summary>
		///   Shearing of the <c>X</c> axis in <c>Z</c> direction.
		/// </summary>
		[SerializeField] private float _xz;

		/// <summary>
		///   Shearing of the <c>Y</c> axis in <c>X</c> direction.
		/// </summary>
		[SerializeField] private float _yx;
#endregion  // Private variables

#region  Accessors
		/// <summary>
		///   Shearing of the <c>Y</c> axis in <c>Z</c> direction.
		/// </summary>
		public float YZ {
			get {
				return _yz;
			} set {
				_yz = value;
			}
		}

		/// <summary>
		///   Shearing of the <c>Z</c> axis in <c>X</c> direction.
		/// </summary>
		public float ZX {
			get {
				return _zx;
			} set {
				_zx = value;
			}
		}

		/// <summary>
		///   Shearing of the <c>X</c> axis in <c>Y</c> direction.
		/// </summary>
		public float XY {
			get {
				return _xy;
			} set {
				_xy = value;
			}
		}

		/// <summary>
		///   Shearing of the <c>Z</c> axis in <c>Y</c> direction.
		/// </summary>
		public float ZY {
			get {
				return _zy;
			} set {
				_zy = value;
			}
		}

		/// <summary>
		///   Shearing of the <c>X</c> axis in <c>Z</c> direction.
		/// </summary>
		public float XZ {
			get {
				return _xz;
			} set {
				_xz = value;
			}
		}

		/// <summary>
		///   Shearing of the <c>Y</c> axis in <c>X</c> direction.
		/// </summary>
		public float YX {
			get {
				return _yx;
			} set {
				_yx = value;
			}
		}

		/// <summary>
		///   Shearing value at a specific index.
		/// </summary>
		public float this[int index] {
			get {
				switch (index) {
					case 0: return YZ;
					case 1: return ZX;
					case 2: return XY;
					case 3: return ZY;
					case 4: return XZ;
					case 5: return YX;
					default: throw new IndexOutOfRangeException();
				}
			} set {
				switch (index) {
					case 0: YZ = value; break;
					case 1: ZX = value; break;
					case 2: XY = value; break;
					case 3: ZY = value; break;
					case 4: XZ = value; break;
					case 5: YX = value; break;
					default: throw new IndexOutOfRangeException();
				}
			}
		}
#endregion  // Accessors

#region  Computed properties
		/// <summary>
		///   Accessor to the vector's <c>XY</c> and <c>XZ</c> components.
		/// </summary>
		public Vector2 X {
			get {
				return new Vector2(XY, XZ);
			} set {
				XY = value.x; XZ = value.y;
			}
		}

		/// <summary>
		///   Accessor to the vector's <c>YX</c> and <c>YZ</c> components.
		/// </summary>
		public Vector2 Y {
			get {
				return new Vector2(YX, YZ);
			} set {
				YX = value.x; YZ = value.y;
			}
		}

		/// <summary>
		///   Accessor to the vector's <c>ZX</c> and <c>ZY</c> components.
		/// </summary>
		public Vector2 Z {
			get {
				return new Vector2(ZX, ZY);
			} set {
				ZX = value.x; ZY = value.y;
			}
		}
#endregion  // Computed properties

#region  Constructors
		/// <summary>
		///   Creates a new vector with given values.
		/// </summary>
		public Vector6(float yz, float zx, float xy, float zy, float xz, float yx) {
			_yz = yz; _zx = zx; _xy = xy;
			_zy = zy; _xz = xz; _yx = yx;
		}

		/// <summary>
		///   Creates a new vector with all values set to the same value.
		/// </summary>
		public Vector6(float n) {
			_yz = n; _zx = n; _xy = n;
			_zy = n; _xz = n; _yx = n;
		}

		/// <summary>
		///   Creates a new vector from an existing vector.
		/// </summary>
		public Vector6(Vector6 v) {
			_yz = v.YZ; _zx = v.ZX;
			_xy = v.XY; _zy = v.ZY;
			_xz = v.XZ; _yx = v.YX;
		}

		/// <summary>
		///   Creates a new vector with all values set to zero.
		/// </summary>
		public static Vector6 Zero {
			get {return new Vector6(0f, 0f, 0f, 0f, 0f, 0f);}
		}
#endregion  // Constructors

#region  ShearMatrix
		/// <summary>
		///   Construct a shearing matrix from a vector.
		/// </summary>
		/// <remarks>
		///   <para>
		///     The shearing matrix can be used in combination with a TRS
		///     matrix via matrix multiplication. This is what the matrix looks
		///     like:
		///   </para>
		///   <code>
		///      1  YX  ZX   0
		///     XY   1  ZY   0
		///     XZ  YZ   1   0
		///      0   0   0   1
		///   </code>
		/// </remarks>
		public static Matrix4x4 ShearMatrix(Vector6 v) {
			var shearMatrix = Matrix4x4.identity;

			shearMatrix[0, 1] = v.YX; shearMatrix[0, 2] = v.ZX;
			shearMatrix[1, 0] = v.XY; shearMatrix[1, 2] = v.ZY;
			shearMatrix[2, 0] = v.XZ; shearMatrix[2, 1] = v.YZ;

			return shearMatrix;
		}

		/// <summary>
		///   Construct a shearing matrix from this vector.
		/// </summary>
		/// <remarks>
		///   <para>
		///     The shearing matrix can be used in combination with a TRS
		///     matrix via matrix multiplication. This is what the matrix looks
		///     like:
		///   </para>
		///   <code>
		///      1  YX  ZX   0
		///     XY   1  ZY   0
		///     XZ  YZ   1   0
		///      0   0   0   1
		///   </code>
		/// </remarks>
		public Matrix4x4 ShearMatrix() {
			return ShearMatrix(this);
		}
#endregion  // ShearMatrix

#region  Set
		/// <summary>
		///   Copies the values of another vector.
		/// </summary>
		/// <param name="original">
		///   The original vector to copy values from.
		/// </param>
		public void Set(Vector6 original) {
			for (var i = 0; i < SIZE; ++i) {
				this[i] = original[i];
			}
		}
#endregion  // Set

#region  Math
		/// <summary>
		///   Adds a vector to this vector.
		/// </summary>
		public void Add(Vector6 summand) {
			for (var i = 0; i < SIZE; ++i) {
				this[i] += summand[i];
			}
		}

		/// <summary>
		///   Adds two vectors and returns the result as a new vector.
		/// </summary>
		public static Vector6 Add(Vector6 augend, Vector6 addend) {
			var sum = new Vector6(augend);
			sum.Add(addend);
			return sum;
		}

		/// <summary>
		///   Subtracts a vector from this vector.
		/// </summary>
		public void Subtract(Vector6 subtrahend) {
			for (var i = 0; i < SIZE; ++i) {
				this[i] -= subtrahend[i];
			}
		}

		/// <summary>
		///   Subtracts two vectors and returns the result as a new vector.
		/// </summary>
		public static Vector6 Subtract(Vector6 minuend, Vector6 subtrahend) {
			var difference = new Vector6(minuend);
			difference.Subtract(subtrahend);
			return difference;
		}

		/// <summary>
		///   Negates a vector.
		/// </summary>
		public void Negate() {
			for (var i = 0; i < SIZE; ++i) {
				this[i] *= -1.0f;
			}
		}

		/// <summary>
		///   Negates a vector and returns the result as a new vector.
		/// </summary>
		public static Vector6 Negate(Vector6 value) {
			var negative = new Vector6(value);
			negative.Negate();
			return negative;
		}

		/// <summary>
		///   Scales this vector component-wise with another vector.
		/// </summary>
		public void Scale(Vector6 factor) {
			for (var i = 0; i < SIZE; ++i) {
				this[i] *= factor[i];
			}
		}

		/// <summary>
		///   Scales this vector component-wise with a scalar factor.
		/// </summary>
		public void Scale(float factor) {
			for (var i = 0; i < SIZE; ++i) {
				this[i] *= factor;
			}
		}

		/// <summary>
		///   Scales a vector component-wise with another vector and
		///   returns the result as a new vector.
		/// </summary>
		public static Vector6 Scale(Vector6 multiplier, Vector6 multiplicand) {
			var product = new Vector6(multiplier);
			product.Scale(multiplicand);
			return product;
		}

		/// <summary>
		///   Scales a vector component-wise with a scalar factor and
		///   returns the result as a new vector.
		/// </summary>
		public static Vector6 Scale(Vector6 vector, float factor) {
			var product = new Vector6(vector);
			product.Scale(factor);
			return product;
		}

		/// <summary>
		///   Component-wise maximum of this vector and another one.
		/// </summary>
		public void Max(Vector6 comparedTo) {
			for (var i = 0; i < SIZE; ++i) {
				this[i] = Mathf.Max(this[i], comparedTo[i]);
			}
		}

		/// <summary>
		///   Creates a new vector as the component-wise maximum of two vectors.
		/// </summary>
		public static Vector6 Max(Vector6 lhs, Vector6 rhs) {
			var max = new Vector6(lhs);
			max.Max(rhs);
			return max;
		}

		/// <summary>
		///   Component-wise minimum of this vector and another one.
		/// </summary>
		public void Min(Vector6 comparedTo) {
			for (var i = 0; i < SIZE; ++i) {
				this[i] = Mathf.Min(this[i], comparedTo[i]);
			}
		}

		/// <summary>
		///   Creates a new vector as the component-wise minimum of two vectors.
		/// </summary>
		public static Vector6 Min(Vector6 lhs, Vector6 rhs) {
			var min = new Vector6(lhs);
			min.Min(rhs);
			return min;
		}

		/// <summary>
		///   Linearly interpolates a vector with another one.
		/// </summary>
		public void Lerp(Vector6 towards, float t) {
			for (var i = 0; i < SIZE; ++i) {
				this[i] = Mathf.Lerp(this[i], towards[i], t);
			}
		}

		/// <summary>
		///   Linearly interpolates two vectors and returns the result as a
		///   new vector.
		/// </summary>
		public static Vector6 Lerp(Vector6 from, Vector6 to, float t) {
			var lerp = new Vector6(from);
			lerp.Lerp(to, t);
			return lerp;
		}
#endregion  // Math

#region  IEquatable
		/// <summary>
		///   Compare this vector with another one for equality.
		/// </summary>
		/// <remarks>
		///   <para>
		///     Two vectors are considered equal when all their components are
		///     equal. Two components are considered equal if their difference
		///     is less than <c>Mathf.Epsilon</c>.
		///   </para>
		/// </remarks>
		public bool Equals(Vector6 v) {
			for (var i = 0; i < SIZE; ++i) {
				var delta = Mathf.Abs(this[i] - v[i]) < Mathf.Epsilon;
				if (delta) {
					return false;
				}
			}
			return true;
		}

		/// <summary>
		///   Compare this vector with another object for equality.
		/// </summary>
		/// <remarks>
		///   <para>
		///     If <c>obj</c> is not a <c>Vector6</c> the result is
		///     <c>false</c>, otherwise the result depends on the values of the
		///     vector.
		///   </para>
		/// </remarks>
		public override bool Equals(object obj) {
			var vector = (Vector6)obj;
			return Equals(vector);
		}

		/// <summary>
		///   Get the hash code of this vector.
		/// </summary>
		/// <remarks>
		///   <para>
		///     The hash code is the sum of all components.
		///   </para>
		/// </remarks>
		public override int GetHashCode() {
			return (int)(XY + XZ + YX + YZ + ZX + ZY);
		}
#endregion  // IEquatable

#region  Overridden
		/// <summary>
		///   String-representation of this vector.
		/// </summary>
		/// <remarks>
		///   <para>
		///     The string consist of a pair of parentheses with the values of
		///     components inside, separated by a comma and a space.
		///   </para>
		///   <code>
		///     (XY, XZ, YX, YZ, ZX, ZY)
		///   </code>
		/// </remarks>
		public override string ToString() {
			return "(" + XY + ", " + XZ + ", " + YX
				+ ", " + YZ + ", " + ZX + ", " + ZY + ")";
		}
#endregion  // Overridden

#region  Operators
		/// <summary>
		///   Compares two vectors for equality.
		/// </summary>
		public static bool operator == (Vector6 lhs, Vector6 rhs) {
			return lhs.Equals(rhs);
		}

		/// <summary>
		///   Compares two vectors for inequality.
		/// </summary>
		public static bool operator != (Vector6 lhs, Vector6 rhs) {
			return !(lhs.Equals(rhs));
		}

		/// <summary>
		///   Creates a new vector as the sum of two vectors.
		/// </summary>
		public static Vector6 operator + (Vector6 lhs, Vector6 rhs) {
			return Vector6.Add(lhs, rhs);
		}

		/// <summary>
		///   Creates a new vector as the difference of two vectors.
		/// </summary>
		public static Vector6 operator - (Vector6 lhs, Vector6 rhs) {
			return Vector6.Subtract(lhs, rhs);
		}

		/// <summary>
		///   Creates a new vector as the negation of a vector.
		/// </summary>
		public static Vector6 operator - (Vector6 value) {
			return Vector6.Negate(value);
		}

		/// <summary>
		///   Creates a new vector as the component-wise product of two vectors.
		/// </summary>
		public static Vector6 operator * (Vector6 lhs, float rhs) {
			return Vector6.Scale(lhs, rhs);
		}

		/// <summary>
		///   Creates a new vector as the component-wise product of a
		///   vector and a scalar.
		/// </summary>
		public static Vector6 operator * (float lhs, Vector6 rhs) {
			return Vector6.Scale(rhs, lhs);
		}

		/// <summary>
		///   Creates a new vector as the component-wise quotient of two
		///   vectors.
		/// </summary>
		public static Vector6 operator / (Vector6 lhs, float rhs) {
			return Vector6.Scale(lhs, 1.0f / rhs);
		}
#endregion  // Operators
	}
}
