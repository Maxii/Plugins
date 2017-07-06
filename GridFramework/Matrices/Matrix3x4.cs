using UnityEngine;
using System;

namespace GridFramework.Matrices {
	/// <summary>
	///   A 3x4 matrix similar to Unity's own <c>Matrix4x4</c> structure.
	/// </summary>
	[Serializable]
	public struct Matrix3x4 : IEquatable<Matrix3x4> {
#region  Constants
		/// <summary>
		///   Height of the matrix type.
		/// </summary>
		public const int Height = 3;

		/// <summary>
		///   Width of the matrix type.
		/// </summary>
		public const int Width  = 4;
#endregion  // Constants

#region  Properties
		/// <summary>
		///   The first row and first column value.
		/// </summary>
        public float M11 {get; set;}

		/// <summary>
		///   The first row and second column value.
		/// </summary>
        public float M12 {get; set;}

		/// <summary>
		///   The first row and third column value.
		/// </summary>
        public float M13 {get; set;}

		/// <summary>
		///   The first row and fourth column value.
		/// </summary>
        public float M14 {get; set;}

		/// <summary>
		///   The second row and first column value.
		/// </summary>
        public float M21 {get; set;}

		/// <summary>
		///   The second row and second column value.
		/// </summary>
        public float M22 {get; set;}

		/// <summary>
		///   The second row and third column value.
		/// </summary>
        public float M23 {get; set;}

		/// <summary>
		///   The second row and fourth column value.
		/// </summary>
        public float M24 {get; set;}

		/// <summary>
		///   The third row and first column value.
		/// </summary>
        public float M31 {get; set;}

		/// <summary>
		///   The third row and second column value.
		/// </summary>
        public float M32 {get; set;}

		/// <summary>
		///   The third row and third column value.
		/// </summary>
        public float M33 {get; set;}

		/// <summary>
		///   The third row and fourth column value.
		/// </summary>
        public float M34 {get; set;}
#endregion  // Properties

#region  Constructors
		/// <summary>
		///   Instantiate a new matrix with given components.
		/// </summary>
		public Matrix3x4(float m11, float m12, float m13, float m14,
		                 float m21, float m22, float m23, float m24,
		                 float m31, float m32, float m33, float m34) {
		    M11 = m11; M12 = m12; M13 = m13; M14 = m14;
		    M21 = m21; M22 = m22; M23 = m23; M24 = m24;
		    M31 = m31; M32 = m32; M33 = m33; M34 = m34;
		}

		/// <summary>
		///   Instantiate a new matrix from row vectors.
		/// </summary>
		public Matrix3x4(Vector4 r1, Vector4 r2, Vector4 r3) {
		    M11 = r1.x; M12 = r1.y; M13 = r1.z; M14 = r1.w;
		    M21 = r2.x; M22 = r2.y; M23 = r2.z; M24 = r2.w;
		    M31 = r3.x; M32 = r3.y; M33 = r3.z; M34 = r3.w;
		}

		/// <summary>
		///   Instantiate a new matrix from column vectors.
		/// </summary>
		public Matrix3x4(Vector3 c1, Vector3 c2, Vector3 c3, Vector3 c4) {
		    M11 = c1.x; M12 = c2.x; M13 = c3.x; M14 = c4.x;
		    M21 = c1.y; M22 = c2.y; M23 = c3.y; M24 = c4.y;
		    M31 = c1.z; M32 = c2.z; M33 = c3.z; M34 = c4.z;
		}
#endregion  // Constructors

#region  Indexers
		/// <summary>
		///   Index the matrix row-major.
		/// </summary>
		public float this[int index] {
			get {
				switch (index) {
					case  0: return M11;
					case  1: return M12;
					case  2: return M13;
					case  3: return M14;
					case  4: return M21;
					case  5: return M22;
					case  6: return M23;
					case  7: return M24;
					case  8: return M31;
					case  9: return M32;
					case 10: return M33;
					case 11: return M34;
					default: throw new ArgumentOutOfRangeException();
				}
			} set {
				switch (index) {
					case  0: M11 = value; break;
					case  1: M12 = value; break;
					case  2: M13 = value; break;
					case  3: M14 = value; break;
					case  4: M21 = value; break;
					case  5: M22 = value; break;
					case  6: M23 = value; break;
					case  7: M24 = value; break;
					case  8: M31 = value; break;
					case  9: M32 = value; break;
					case 10: M33 = value; break;
					case 11: M34 = value; break;
					default: throw new ArgumentOutOfRangeException();
				}
			}
		}

		/// <summary>
		///   Index the matrix by row and column entry.
		/// </summary>
		public float this[int row, int column] {
			get {
				return this[Width * row + column];
			} set {
				this[Width * row + column] = value;
			}
		}
#endregion  // Indexers

#region  Setters
		/// <summary>
		///   Set the column of a matrix.
		/// </summary>
		/// <param name="column">
		///   Index of the column (0-based).
		/// </param>
		/// <param name="value">
		///   The values to copy.
		/// </param>
		public void SetColumn(int column, Vector3 value) {
			if (column < 0 || column >= Width) {
				throw new ArgumentOutOfRangeException();
			}
			for (int i = 0; i < Height; ++i) {
				this[i, column] = value[i];
			}
		}

		/// <summary>
		///   Set the row of a matrix.
		/// </summary>
		/// <param name="row">
		///   Index of the row (0-based).
		/// </param>
		/// <param name="value">
		///   The values to copy.
		/// </param>
		public void SetRow(int row, Vector4 value) {
			if (row < 0 || row >= Height) {
				throw new ArgumentOutOfRangeException();
			}
			for (int i = 0; i < Width; ++i) {
				this[row, i] = value[i];
			}
		}
#endregion  // Setters

#region  Getters
		/// <summary>
		///   Transposed matrix.
		/// </summary>
		public Matrix4x3 Transpose {
			get {
				var transpose = new Matrix4x3();
				for (int r = 0; r < Height; ++r) {
					for (int c = 0; c < Width; ++c) {
						transpose[c, r] = this[r, c];
					}
				}
				return transpose;
			}
		}

		/// <summary>
		///   Compute the pseudoinverse matrix.
		/// </summary>
		/// <remarks>
		///   <para>
		///     This property works only for matrices with a rank of 3 at the
		///     moment.
		///   </para>
		/// </remarks>
		public Matrix4x3 PseudoInverse {
			get {
				// This formula assumes that the matrix has a full rank of 4.
				// This is the case when using it for cubic coordinates in Grid
				// Framework, but it does not have to hold true in general.
				//
				// Given a rank of 4 we can compute the pseudoinverse as 'A+ =
				// A* (A A*)^(-1)', where 'A+' is the pseudoinverse, 'A*' the
				// transpose and 'A^(-1)' the inverse.
				var transpose = Transpose;
				var product   = this * transpose;
				var inverse   = product.Inverse;
				var pseudo    = transpose * inverse;
				return pseudo;
			}
		}
#endregion  // Getters

#region  Multiplication
		/// <summary>
		///   Matrix-multiplication for a 3x4- and a 4x3 matrix.
		/// </summary>
		/// <param name="lhs">
		///   Left-hand side of the multiplication.
		/// </param>
		/// <param name="rhs">
		///   Right-hand side of the multiplication.
		/// </param>
		/// <returns>
		///   The matrix-product of <paramref name="lhs"><c>lhs</c></paramref>
		///   and <paramref name="rhs"><c>rhs</c></paramref>.
		/// </returns>
		public static Matrix3x3 Multiply(Matrix3x4 lhs, Matrix4x3 rhs) {
			const int m = Matrix3x4.Height;  // Height of the product
			const int n = Matrix4x3.Width;   // Width of the product
			const int o = Matrix3x4.Width;   // Same as lhs.Height

			var product = new Matrix3x3();
			for (int i = 0; i < m; ++i) {
				for (int j = 0; j < n; ++j) {
					var sum = 0f;
					for (int k = 0; k < o; ++k) {
						sum += lhs[i, k] * rhs[k, j];
					}
					product[i, j] = sum;
				}
			}
			return product;
		}

		/// <summary>
		///   Matrix-multiplication for a 3x4- and a 4x4 matrix.
		/// </summary>
		/// <param name="lhs">
		///   Left-hand side of the multiplication.
		/// </param>
		/// <param name="rhs">
		///   Right-hand side of the multiplication.
		/// </param>
		/// <returns>
		///   The matrix-product of <paramref name="lhs"><c>lhs</c></paramref>
		///   and <paramref name="rhs"><c>rhs</c></paramref>.
		/// </returns>
		public static Matrix3x4 Multiply(Matrix3x4 lhs, Matrix4x4 rhs) {
			const int m = Matrix3x4.Height;  // Height of the product
			const int n =                4;  // Width of the product
			const int o = Matrix3x4.Width;   // Same as rhs.Width

			var product = new Matrix3x4();
			for (int i = 0; i < m; ++i) {
				for (int j = 0; j < n; ++j) {
					var sum = 0f;
					for (int k = 0; k < o; ++k) {
						sum += lhs[i, k] * rhs[k, j];
					}
					product[i, j] = sum;
				}
			}
			return product;
		}

		/// <summary>
		///   Matrix-multiplication for a 3x4- and a 4-vector.
		/// </summary>
		/// <param name="lhs">
		///   Left-hand side of the multiplication.
		/// </param>
		/// <param name="rhs">
		///   Right-hand side of the multiplication.
		/// </param>
		/// <returns>
		///   The matrix-product of <paramref name="lhs"><c>lhs</c></paramref>
		///   and <paramref name="rhs"><c>rhs</c></paramref>.
		/// </returns>
		public static Vector3 Multiply(Matrix3x4 lhs, Vector4 rhs) {
			const int m = Matrix3x4.Height;  // Height of the product
			const int n = Matrix3x4.Width;   // Height of the vector

			var product = new Vector3();
			for (int i = 0; i < m; ++i) {
				var sum = 0f;
				for (int j = 0; j < n; ++j) {
					sum += lhs[i, j] * rhs[j];
				}
				product[i] = sum;
			}
			return product;
		}

		/// <summary>
		///   Matrix-multiplication of this and a 4x3 matrix.
		/// </summary>
		/// <param name="rhs">
		///   Right-hand side of the multiplication.
		/// </param>
		/// <returns>
		///   The matrix-product of this matrix and
		///   <paramref name="rhs"><c>rhs</c></paramref>.
		/// </returns>
		public Matrix3x3 Multiply(Matrix4x3 rhs) {
			return Multiply(this, rhs);
		}

		/// <summary>
		///   Matrix-multiplication of this and a 4x4 matrix.
		/// </summary>
		/// <param name="rhs">
		///   Right-hand side of the multiplication.
		/// </param>
		/// <returns>
		///   The matrix-product of this matrix and
		///   <paramref name="rhs"><c>rhs</c></paramref>.
		/// </returns>
		Matrix3x4 Multiply(Matrix4x4 rhs) {
			return Multiply(this, rhs);
		}

		/// <summary>
		///   Matrix-multiplication of this and a 3-vector.
		/// </summary>
		/// <param name="rhs">
		///   Right-hand side of the multiplication.
		/// </param>
		/// <returns>
		///   The matrix-product of this matrix and
		///   <paramref name="rhs"><c>rhs</c></paramref>.
		/// </returns>
		public Vector4 Multiply(Vector3 rhs) {
			return Multiply(this, rhs);
		}
#endregion  // Multiplication

#region  IEquatable<T>
		/// <summary>
		///   Compares whether current instance is equal to the specified
		///   <see cref="Matrix3x4"/>.
		/// </summary>
		/// <param name="other">
		///   The <see cref="Matrix3x4"/> to compare.
		/// </param>
		/// <returns>
		///   <c>true</c> if the instances are equal; <c>false</c> otherwise.
		/// </returns>
		public bool Equals(Matrix3x4 other) {
			var equals = true;
			for (int i = 0; i < Height * Width; ++i) {
				equals &= Mathf.Abs(this[i] - other[i]) < Mathf.Epsilon;
				if (!equals) {
					break;
				}
			}
			return equals;
		}

		/// <summary>
		///   Compares whether current instance is equal to the specified
		///   <see cref="Object"/>.
		/// </summary>
		/// <param name="obj">
		///   The <see cref="Object"/> to compare.
		/// </param>
		/// <returns>
		///   <c>true</c> if the instances are equal; <c>false</c> otherwise.
		/// </returns>
		public override bool Equals(object obj) {
			bool equals = false;
			if (obj is Matrix3x4) {
				equals = Equals((Matrix3x4)obj);
			}
			return equals;
        }

        /// <summary>
        ///   Gets the hash code of this <see cref="Matrix3x4"/>.
        /// </summary>
        /// <returns>
        ///   Hash code of this <see cref="Matrix3x4"/>.
        /// </returns>
        public override int GetHashCode() {
        	var hash = 0;
        	for (int i = 0; i < Height * Width; ++i) {
        		hash += this[i].GetHashCode();
        	}
        	return hash;
        }
#endregion // IEquatable<T>

#region  Operators
		/// <summary>
		///   Shorthand for <c>Multiply(lhs, rhs)</c>.
		/// </summary>
		public static Matrix3x3 operator *(Matrix3x4 lhs, Matrix4x3 rhs) {
			return Multiply(lhs, rhs);
		}

		/// <summary>
		///   Shorthand for <c>Multiply(lhs, rhs)</c>.
		/// </summary>
		public static Matrix3x4 operator *(Matrix3x4 lhs, Matrix4x4 rhs) {
			return Multiply(lhs, rhs);
		}

		/// <summary>
		///   Shorthand for <c>Multiply(lhs, rhs)</c>.
		/// </summary>
		public static Vector3 operator *(Matrix3x4 lhs, Vector4 rhs) {
			return Multiply(lhs, rhs);
		}
#endregion  // Operators
	}
}
