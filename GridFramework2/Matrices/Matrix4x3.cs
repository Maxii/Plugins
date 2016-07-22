using UnityEngine;
using System;

namespace GridFramework.Matrices {
	/// <summary>
	///   A 4x3 matrix similar to Unity's own <c>Matrix4x4</c> structure.
	/// </summary>
	[Serializable]
	public struct Matrix4x3 : IEquatable<Matrix4x3> {
#region  Constants
		/// <summary>
		///   Height of the matrix type.
		/// </summary>
		public const int Height = 4;

		/// <summary>
		///   Width of the matrix type.
		/// </summary>
		public const int Width  = 3;
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
		///   The fourth row and first column value.
		/// </summary>
        public float M41 {get; set;}

		/// <summary>
		///   The fourth row and second column value.
		/// </summary>
        public float M42 {get; set;}

		/// <summary>
		///   The fifth row and third column value.
		/// </summary>
        public float M43 {get; set;}
#endregion  // Properties

#region  Constructors
		/// <summary>
		///   Instantiate a new matrix with given components.
		/// </summary>
		public Matrix4x3(float m11, float m12, float m13,
		                 float m21, float m22, float m23,
		                 float m31, float m32, float m33,
		                 float m41, float m42, float m43) {
		    M11 = m11; M12 = m12; M13 = m13;
		    M21 = m21; M22 = m22; M23 = m23;
		    M31 = m31; M32 = m32; M33 = m33;
		    M41 = m41; M42 = m42; M43 = m43;
		}

		/// <summary>
		///   Instantiate a new matrix from row vectors.
		/// </summary>
		public Matrix4x3(Vector3 r1, Vector3 r2, Vector3 r3, Vector3 r4) {
		    M11 = r1.x; M12 = r1.y; M13 = r1.z;
		    M21 = r2.x; M22 = r2.y; M23 = r2.z;
		    M31 = r3.x; M32 = r3.y; M33 = r3.z;
		    M41 = r4.x; M42 = r4.y; M43 = r4.z;
		}

		/// <summary>
		///   Instantiate a new matrix from column vectors.
		/// </summary>
		public Matrix4x3(Vector4 c1, Vector4 c2, Vector4 c3) {
		    M11 = c1.x; M12 = c2.y; M13 = c3.z;
		    M21 = c1.y; M22 = c2.y; M23 = c3.y;
		    M31 = c1.z; M32 = c2.z; M33 = c3.z;
		    M41 = c1.w; M42 = c2.w; M43 = c3.w;
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
					case  3: return M21;
					case  4: return M22;
					case  5: return M23;
					case  6: return M31;
					case  7: return M32;
					case  8: return M33;
					case  9: return M41;
					case 10: return M42;
					case 11: return M43;
					default: throw new ArgumentOutOfRangeException("" +index);
				}
			} set {
				switch (index) {
					case  0: M11 = value; break;
					case  1: M12 = value; break;
					case  2: M13 = value; break;
					case  3: M21 = value; break;
					case  4: M22 = value; break;
					case  5: M23 = value; break;
					case  6: M31 = value; break;
					case  7: M32 = value; break;
					case  8: M33 = value; break;
					case  9: M41 = value; break;
					case 10: M42 = value; break;
					case 11: M43 = value; break;
					default: throw new ArgumentOutOfRangeException("" +index);
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
		public void SetColumn(int column, Vector4 value) {
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
		public void SetRow(int row, Vector3 value) {
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
		public Matrix3x4 Transpose {
			get {
				var transpose = new Matrix3x4();
				for (int r = 0; r < Height; ++r) {
					for (int c = 0; c < Width; ++c) {
						transpose[c, r] = this[r, c];
					}
				}
				return transpose;
			}
		}

		// Requires the inverse of a 4x4 matrix to work
		/* public Matrix3x4 PseudoInverse { */
		/* 	get { */
		/* 		// This formula assumes that the matrix has a full rank of 4. */
		/* 		// This is the case when using it for cubic coordinates in Grid */
		/* 		// Framework, but it does not have to hold true in general. */
		/* 		// */
		/* 		// Given a rank of 4 we can compute the pseudoinverse as 'A+ = */
		/* 		// (A* A)^(-1) A*', where 'A+' is the pseudoinverse, 'A*' the */
		/* 		// transpose and 'A^(-1)' the inverse. */
		/* 		var transpose = Transpose; */
		/* 		var product   = transposeMultiply(this); */
		/* 		var inverse   = product.inverse; */
		/* 		var pseudo    = inverse.Multiply(transpose); */
		/* 	} */
		/* } */
#endregion  // Getters

#region  Multiplication
		/// <summary>
		///   Matrix-multiplication for a 4x3- and a 3x4 matrix.
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
		public static Matrix4x4 Multiply(Matrix4x3 lhs, Matrix3x4 rhs) {
			const int m = Matrix4x3.Height;  // Height of the product
			const int n = Matrix3x4.Width;   // Width of the product
			const int o = Matrix4x3.Width;   // Same as lhs.Height

			var product = new Matrix4x4();
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
		///   Matrix-multiplication for a 4x3- and a 3x3 matrix.
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
		public static Matrix4x3 Multiply(Matrix4x3 lhs, Matrix3x3 rhs) {
			const int m = Matrix4x3.Height;  // Height of the product
			const int n = Matrix3x3.Width;   // Width of the product
			const int o = Matrix4x3.Width;   // Same as lhs.Height

			var product = new Matrix4x3();
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
		///   Matrix-multiplication for a 4x3-matrix and a 3-vector.
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
		public static Vector4 Multiply(Matrix4x3 lhs, Vector3 rhs) {
			const int m = Matrix4x3.Height;  // Height of the product
			const int n = Matrix4x3.Width;   // Height of the vector

			var product = new Vector4();
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
		///   Matrix-multiplication of this and a 3x4 matrix.
		/// </summary>
		/// <param name="rhs">
		///   Right-hand side of the multiplication.
		/// </param>
		/// <returns>
		///   The matrix-product of this matrix and
		///   <paramref name="rhs"><c>rhs</c></paramref>.
		/// </returns>
		public Matrix4x4 Multiply(Matrix3x4 rhs) {
			return Multiply(this, rhs);
		}

		/// <summary>
		///   Matrix-multiplication of this and a 3x3 matrix.
		/// </summary>
		/// <param name="rhs">
		///   Right-hand side of the multiplication.
		/// </param>
		/// <returns>
		///   The matrix-product of this matrix and
		///   <paramref name="rhs"><c>rhs</c></paramref>.
		/// </returns>
		public Matrix4x3 Multiply(Matrix3x3 rhs) {
			return Multiply(this, rhs);
		}

		/// <summary>
		///   Matrix-multiplication of this and a 4-vector.
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
		///   <see cref="Matrix4x3"/>.
		/// </summary>
		/// <param name="other">
		///   The <see cref="Matrix4x3"/> to compare.
		/// </param>
		/// <returns>
		///   <c>true</c> if the instances are equal; <c>false</c> otherwise.
		/// </returns>
		public bool Equals(Matrix4x3 other) {
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
			if (obj is Matrix4x3) {
				equals = Equals((Matrix4x3)obj);
			}
			return equals;
        }

        /// <summary>
        ///   Gets the hash code of this <see cref="Matrix4x3"/>.
        /// </summary>
        /// <returns>
        ///   Hash code of this <see cref="Matrix4x3"/>.
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
		public static Matrix4x4 operator *(Matrix4x3 lhs, Matrix3x4 rhs) {
			return Multiply(lhs, rhs);
		}

		/// <summary>
		///   Shorthand for <c>Multiply(lhs, rhs)</c>.
		/// </summary>
		public static Matrix4x3 operator *(Matrix4x3 lhs, Matrix3x3 rhs) {
			return Multiply(lhs, rhs);
		}

		/// <summary>
		///   Shorthand for <c>Multiply(lhs, rhs)</c>.
		/// </summary>
		public static Vector4 operator *(Matrix4x3 lhs, Vector3 rhs) {
			return Multiply(lhs, rhs);
		}
#endregion  // Operators
	}
}
