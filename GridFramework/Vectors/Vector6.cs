using UnityEngine;
using System;

namespace GridFramework {
	namespace Vectors {
		/// <summary>Enumeration of the possible axis combinations.</summary>
		/// The enumeraion's corresponding integer numers start at 0 and increment by one.
		public enum Axis2 {
			xy, ///< integer value 0
			xz, ///< integer value 1
			yx, ///< integer value 2
			yz, ///< integer value 3
			zx, ///< integer value 4
			zy  ///< integer value 5
		};

		/// <summary>Enumeration of the three possible axes.</summary>
		public enum Axis {
			x, ///< integer value 0
			y, ///< integer value 1
			z  ///< integer value 2
		};
		
		/// <summary>Class representing six adjacent float values for the shearing of a rectangular grid.</summary>
		/// This class is based on Unity's own _Vector3_ struct and can be used in a similar way. It resides in Grid
		/// Framework's own namespace to prevent collision with similar types from other plugins or a future official
		/// Unity _Vector6_ type.
		/// 
		/// The API is mostly the same as for _Vector3_, except in places where it wouldn't make sense. The individual
		/// components are named to reflect their shearing nature accorrding to the following pattern: "ab" where _a_
		/// is either _x_, _y_ or _z_ and _b_ is another axis different from _a_. Two adjacent values belonging to the
		/// same axis can also be accessed as a _Vector2_, like for example _yx_ and _yz_, but not _xz_ and _yx_.
		/// 
		/// Note that _Vector6_ is a class, not a struct, unlike Unity's Vector3. This was done for technical reasons due
		/// to Unity. It is generally not an issue, but you should avoid throwing out _Vector6_ left and right like you can
		/// do with _Vector3_. Also, keep in mind that classes are always passed by reference, while structs are passed by
		/// value. If you need to assign new values use the `Set` methods instead of assigning a new instance to a variable.
		/// If you need arithmetic or comparison methods there are always two available: one that operates on an instance
		/// and mutates it rather than creating a new instance, and a static one that does return a new instance but does
		/// not mutate its arguments. Also note that the operators create new instances rather than mutating existing ones.
		///
		/// The individual components are indexed as follows: _xy_=0, _xz_=1, _yx_=2, _yz_=3, _zx_=4, _zy_=5.
		[System.Serializable]
		public class Vector6 : IEquatable<Vector6> {
	
			[SerializeField]
			private float[] values;
			/// <summary>Shearing of the _X_ axis in _Y_ direction.</summary>
			public float xy {
				get {return values[0];}
				set {values[0] = value;}
			}
			/// <summary>Shearing of the _X_ axis in _Z_ direction.</summary>
			public float xz {
				get {return values[1];}
				set {values[1] = value;}
			}
			/// <summary>Shearing of the _Y_ axis in _X_ direction.</summary>
			public float yx {
				get {return values[2];}
				set {values[2] = value;}
			}
			/// <summary>Shearing of the _Y_ axis in _Z_ direction.</summary>
			public float yz {
				get {return values[3];}
				set {values[3] = value;}
			}
			/// <summary>Shearing of the _Z_ axis in _X_ direction.</summary>
			public float zx {
				get {return values[4];}
				set {values[4] = value;}
			}
			/// <summary>Shearing of the _Z_ axis in _Y_ direction.</summary>
			public float zy {
				get {return values[5];}
				set {values[5] = value;}
			}
	
			/// <summary>Shearing value at a specific index.</summary>
			public float this[int index] {
				get {return values[index];}
				set {values[index] = value;}
			}

			#region Constructors
			/// <summary>Creates a new vector with given values.</summary>
			public Vector6(float xy, float xz, float yx, float yz, float zx, float zy) {
				values = new float[6] {xy, xz, yx, yz, zx, zy};
			}
			/// <summary>Creates a new vector with all values set to the same value.</summary>
			public Vector6(float value) {
				values = new float[6] {value, value, value, value, value, value};
			}
			/// <summary>Creates a new vector from an existing vector.</summary>
			public Vector6(Vector6 original) {
				values = new float[6] {original.xy, original.xz, original.yx, original.yz, original.zx, original.zy};
			}
			/// <summary>Creates a new vector with undefined, but allocated values.</summary>
			public Vector6 () {
				values = new float[6];
			}
			/// <summary>Creates a new vector with all values set to zero.</summary>
			public static Vector6 zero {
				get {return new Vector6(0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f);}
			}

			/*public static Vector6 dimetricXY(float ratio) {
				Vector6 vector = Vector6.zero;
				vector.xy = -ratio;
				vector.yx = 1.0f / ratio;
				return vector;
			}*/
			#endregion

			#region Functions
			/// <summary>Set the values of the vector manually.</summary>
			public void Set(float xy, float xz, float yx, float yz, float zx, float zy) {
				this.xy = xy;
				this.xz = xz;
				this.yx = yx;
				this.yz = yz;
				this.zx = zx;
				this.zy = zy;
			}
			/// <summary>Set a value of the vector at a certain index.</summary>
			public void Set(float value, int index) {
				values[index] = value;
			}
			/// <summary>Set a value of the vector at a certain component.</summary>
			public void Set(float value, Axis2 component) {
				Set(value, (int)component);
			}
			/// <summary>Set two values of the vector at a certain index.</summary>
			public void Set(Vector2 values, int index) {
				this.values[index    ] = values.x;
				this.values[index + 1] = values.y;
			}
			/// <summary>Set two values of the vector at a certain axis.</summary>
			public void Set(Vector2 values, Axis axis) {
				Set(values, (int)axis * 2);
			}
			/// <summary>Copies the values of another vector.</summary>
			public void Set(Vector6 original) {
				Set(original.xy, original.xz, original.yx, original.yz, original.zx, original.zy);
			}
			/*public string ToString() {
				return "(" + this.xy + ", " + this.xz + ", " + this.yx + ", " + this.yz + ", " + this.zx + ", " + this.zy + ")";
			}
			public string ToString(string format) {

			}*/
			#endregion
			
			#region Vector2
			/// <summary>Accessor to the vector's _XY_ and _XZ_ components.</summary>
			public Vector2 x {
				get {return new Vector2(xy, xz);}
				set {
					xy = value.x;
					xz = value.y;
				}
			}
			/// <summary>Accessor to the vector's _YX_ and _YZ_ components.</summary>
			public Vector2 y {
				get {return new Vector2(yx, yz);}
				set {
					yx = value.x;
					yz = value.y;
				}
			}
			/// <summary>Accessor to the vector's _ZX_ and _ZY_ components.</summary>
			public Vector2 z {
				get {return new Vector2(zx, zy);}
				set {
					zx = value.x;
					zy = value.y;
				}
			}

			#endregion

			#region Math
			#region Arithmetic
			/// <summary>Adds a vector to this vector.</summary>
			public void Add(Vector6 summand) {
				for (int i = 0; i < 6; ++i) {
					values[i] += summand[i];
				}
			}
			/// <summary>Adds two vectors and returns the result as a new vector.</summary>
			public static Vector6 Add(Vector6 summand1, Vector6 summand2) {
				Vector6 sum = new Vector6(summand1);
				sum.Add(summand2);
				return sum;
			}
			/// <summary>Subtracts a vector from this vector.</summary>
			public void Subtract(Vector6 subtrahend) {
				for (int i = 0; i < 6; ++i) {
					values[i] -= subtrahend[i];
				}
			}
			/// <summary>Subtracts two vectors and returns the result as a new vector.</summary>
			public static Vector6 Subtract(Vector6 minuend, Vector6 subtrahend) {
				Vector6 difference = new Vector6(minuend);
				difference.Subtract(subtrahend);
				return difference;
			}
			/// <summary>Negates a vector.</summary>
			public void Negate() {
				for (int i = 0; i < 6; ++i) {
					values[i] *= -1.0f;
				}
			}
			/// <summary>Negates a vector and returns the result as a new vector.</summary>
			public static Vector6 Negate(Vector6 value) {
				Vector6 negative = new Vector6(value);
				negative.Negate();
				return negative;
			}

			/// <summary>Scales this vector component-wise with another vector.</summary>
			public void Scale(Vector6 factor) {
				for (int i = 0; i < 6; ++i) {
					values[i] *= factor[i];
				}
			}
			/// <summary>Scales this vector component-wise with a scalar factor.</summary>
			public void Scale(float factor) {
				for (int i = 0; i < 6; ++i) {
					values[i] *= factor;
				}
			}
			/// <summary>Scales a vector component-wise with another vector and returns the result as a new vector.</summary>
			public static Vector6 Scale(Vector6 factor1, Vector6 factor2) {
				Vector6 product = new Vector6(factor1);
				product.Scale(factor2);
				return product;
			}
			/// <summary>Scales a vector component-wise with a scalar factor and returns the result as a new vector.</summary>
			public static Vector6 Scale(Vector6 vector, float factor) {
				Vector6 product = new Vector6(vector);
				product.Scale(factor);
				return product;
			}
			#endregion
			#region Relation
			/// <summary>Component-wise maximum of this vector and another one.</summary>
			public void Max(Vector6 comparedTo) {
				for (int i = 0; i < 6; ++i) {
					values[i] = Mathf.Max(values[i], comparedTo[i]);
				}
			}
			/// <summary>Creates a new vector as the component-wise maximum of two vectors.</summary>
			public static Vector6 Max(Vector6 lhs, Vector6 rhs) {
				Vector6 max = new Vector6(lhs);
				max.Max(rhs);
				return max;
			}

			/// <summary>Component-wise minimum of this vector and another one.</summary>
			public void Min(Vector6 comparedTo) {
				for (int i = 0; i < 6; ++i) {
					values[i] = Mathf.Min(values[i], comparedTo[i]);
				}
			}
			/// <summary>Creates a new vector as the component-wise minimum of two vectors.</summary>
			public static Vector6 Min(Vector6 lhs, Vector6 rhs) {
				Vector6 min = new Vector6(lhs);
				min.Min(rhs);
				return min;
			}
			#endregion
			#region Interpolation
			/// <summary>Linearly interpolates a vector with another one.</summary>
			public void Lerp(Vector6 towards, float t) {
				for (int i = 0; i < 6; ++i) {
					values[i] = Mathf.Lerp(values[i], towards[i], t);
				}
			}
			/// <summary>Linearly interpolates two vectors and returns the result as a new vector.</summary>
			public static Vector6 Lerp(Vector6 from, Vector6 to, float t) {
				Vector6 lerp = new Vector6(from);
				lerp.Lerp(to, t);
				return lerp;
			}
			#endregion
			#endregion

			#region Operators
			#region Equality
			public bool Equals(Vector6 v) {
				// If parameter is null return false:
				if ((object)v == null) {
					return false;
				}
				for (int i = 0; i < 6; ++i) {
					if (this[i] != v[i]) {
						return false;
					}
				}
				return true;
			}
			public override bool Equals(object obj) {
				if (obj == null) {
					return false;
				}
				// If parameter cannot be cast to Vector6 return false.
				Vector6 v = obj as Vector6;
				if ((System.Object)v == null) {
					return false;
				}

				return Equals((Vector6 )obj);
			}
			public override int GetHashCode() {
				return (int)(xy + xz + yx + yz + zx + zy);
			}

			public static bool operator ==(Vector6 lhs, Vector6 rhs) {
				return lhs.Equals(rhs);
			}

			public static bool operator !=(Vector6 lhs, Vector6 rhs) {
				return !(lhs.Equals(rhs));
			}
			#endregion
			#region Math
			/// <summary>Creates a new vector as the sum of two vectors.</summary>
			public static Vector6 operator +(Vector6 lhs, Vector6 rhs) {
				return Vector6.Add(lhs, rhs);
			}
			/// <summary>Creates a new vector as the difference of two vectors.</summary>
			public static Vector6 operator -(Vector6 lhs, Vector6 rhs) {
				return Vector6.Subtract(lhs, rhs);
			}
			/// <summary>Creates a new vector as the negationn of a vector.</summary>
			public static Vector6 operator -(Vector6 value) {
				return Vector6.Negate(value);
			}
			/// <summary>Creates a new vector as the component-wise product of two vectors.</summary>
			public static Vector6 operator *(Vector6 vector, float scalar) {
				return Vector6.Scale(vector, scalar);
			}
			/// <summary>Creates a new vector as the component-wise product of a vector and a scalar.</summary>
			public static Vector6 operator *(float scalar, Vector6 vector) {
				return Vector6.Scale(vector, scalar);
			}
			/// <summary>Creates a new vector as the component-wise quotient of two vectors.</summary>
			public static Vector6 operator /(Vector6 vector, float scalar) {
				return Vector6.Scale(vector, 1.0f / scalar);
			}
			#endregion
			#endregion
		}
	}
}
