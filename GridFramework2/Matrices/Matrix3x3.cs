using UnityEngine;
using System;

namespace GridFramework.Matrices {
	/// <summary>
	///   A 3x3 matrix similar to Unity's own <c>Matrix4x4</c> structure.
	/// </summary>
	[Serializable]
	public struct Matrix3x3 : IEquatable<Matrix3x3> {
#region  Constants
		/// <summary>
		///   Height of the matrix type.
		/// </summary>
		public const int Height = 3;

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
#endregion  // Properties

#region  Constructors
		/// <summary>
		///   Instantiate a new matrix with given components.
		/// </summary>
		public Matrix3x3(float m11, float m12, float m13,
		                 float m21, float m22, float m23,
		                 float m31, float m32, float m33) {
		    M11 = m11; M12 = m12; M13 = m13;
		    M21 = m21; M22 = m22; M23 = m23;
		    M31 = m31; M32 = m32; M33 = m33;
		}

		/// <summary>
		///   Instantiate a new matrix from a 4x4 matrix.
		/// </summary>
		/// <remarks>
		///   The resulting matrix is the upper-left 3x3 slice.
		/// </remarks>
		public Matrix3x3(Matrix4x4 m) {
		    M11 = m[0,0]; M12 = m[0,1]; M13 = m[0,2];
		    M21 = m[1,0]; M22 = m[1,1]; M23 = m[1,2];
		    M31 = m[2,0]; M32 = m[2,1]; M33 = m[2,2];
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
					default: throw new ArgumentOutOfRangeException();
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

#region  Getters
		/// <summary>
		///   Transposed matrix.
		/// </summary>
		public Matrix3x3 Transpose {
			get {
				var transpose = new Matrix3x3();
				for (int i = 0; i < Width; ++i) {
					for (int j = 0; j < Height; ++j) {
						transpose[i, j] = this[j, i];
					}
				}
				return transpose;
			}
		}

		/// <summary>
		///   Compute the inverse matrix.
		/// </summary>
		public Matrix3x3 Inverse {
			get {
				// Lazy shortcut: embed the 3x3 matrix in a 4x4 matrix with
				// last column and row set to 0, except the lower-right element
				// is 1.  Compute the inverse of that and take the upper-left
				// portion of the result.
				var matrix  = (Matrix4x4)this;
				var inverse = matrix.inverse;
				return new Matrix3x3(inverse);
			}
		}
#endregion  // Getters

#region  Multiplication
		/// <summary>
		///   Matrix-multiplication for a 3x3- and a 3x3 matrix.
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
		static Matrix3x3 Multiply(Matrix3x3 lhs, Matrix3x3 rhs) {
			var product = new Matrix3x3();
			for (int i = 0; i < Height; ++i) {
				for (int j = 0; j < Height; ++j) {
					var sum = 0f;
					for (int k = 0; k < Width; ++k) {
						sum += lhs[i, k] * rhs[k, j];
					}
					product[i, j] = sum;
				}
			}
			return product;
		}

		/// <summary>
		///   Matrix-multiplication for a 3x3- and a 3x4 matrix.
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
		static Matrix3x4 Multiply(Matrix3x3 lhs, Matrix3x4 rhs) {
			const int m = Matrix3x3.Height;  // Height of the product
			const int n = Matrix3x4.Width;   // Width of the product
			const int o = Matrix3x3.Width;   // Same as rhs.Width

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
		///   Matrix-multiplication for a 3x3-matrix and a 3-vector.
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
		public static Vector3 Multiply(Matrix3x3 lhs, Vector3 rhs) {
			const int m = Matrix3x3.Height;  // Height of the product
			const int n = Matrix3x3.Width;   // Height of the vector

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
		///   Matrix-multiplication with a 3x3 matrix.
		/// </summary>
		/// <param name="rhs">
		///   Right-hand side of the multiplication.
		/// </param>
		/// <returns>
		///   The matrix-product of <c>this</c> and
		///   <paramref name="rhs"><c>rhs</c></paramref>.
		/// </returns>
		public Matrix3x3 Multiply(Matrix3x3 rhs) {
			return Multiply(this, rhs);
		}

		/// <summary>
		///   Matrix-multiplication with a 3x4 matrix.
		/// </summary>
		/// <param name="rhs">
		///   Right-hand side of the multiplication.
		/// </param>
		/// <returns>
		///   The matrix-product of <c>this</c> and
		///   <paramref name="rhs"><c>rhs</c></paramref>.
		/// </returns>
		public Matrix3x4 Multiply(Matrix3x4 rhs) {
			return Multiply(this, rhs);
		}

		/// <summary>
		///   Matrix-multiplication with a 3-vector.
		/// </summary>
		/// <param name="rhs">
		///   Right-hand side of the multiplication.
		/// </param>
		/// <returns>
		///   The matrix-product of <c>this</c> and
		///   <paramref name="rhs"><c>rhs</c></paramref>.
		/// </returns>
		public Vector3 Multiply(Vector3 rhs) {
			return Multiply(this, rhs);
		}
#endregion  // Multiplication

#region  IEquatable<T>
		/// <summary>
		///   Compares whether current instance is equal to the specified
		///   <see cref="Matrix3x3"/>.
		/// </summary>
		/// <param name="other">
		///   The <see cref="Matrix3x3"/> to compare.
		/// </param>
		/// <returns>
		///   <c>true</c> if the instances are equal; <c>false</c> otherwise.
		/// </returns>
		public bool Equals(Matrix3x3 other) {
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
			if (obj is Matrix3x3) {
				equals = Equals((Matrix3x3)obj);
			}
			return equals;
        }

        /// <summary>
        ///   Gets the hash code of this <see cref="Matrix3x3"/>.
        /// </summary>
        /// <returns>
        ///   Hash code of this <see cref="Matrix3x3"/>.
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
		public static Matrix3x3 operator *(Matrix3x3 lhs, Matrix3x3 rhs) {
			return Multiply(lhs, rhs);
		}

		/// <summary>
		///   Shorthand for <c>Multiply(lhs, rhs)</c>.
		/// </summary>
		public static Matrix3x4 operator *(Matrix3x3 lhs, Matrix3x4 rhs) {
			return Multiply(lhs, rhs);
		}

		/// <summary>
		///   Shorthand for <c>Multiply(lhs, rhs)</c>.
		/// </summary>
		public static Vector3 operator *(Matrix3x3 lhs, Vector3 rhs) {
			return Multiply(lhs, rhs);
		}
#endregion  // Operators

#region  Casting
		/// <summary>
		///   Cast a 3x3 matrix to a 4x4 matrix.
		/// </summary>
		/// <remarks>
		///   <para>
		///     The 3x3 matrix is placed in the top-left part of the 4x4
		///     matrix, the new entries are set to 0, except the lower-right
		///     one is set to 1.
		///   </para>
		/// </remarks>
		public static implicit operator Matrix4x4(Matrix3x3 m33) {
			var m44 = new Matrix4x4();
			for (int r = 0; r < Height; ++r) {
				for (int c = 0; c < Width; ++c) {
					m44[r,c] = m33[r,c];
				}
			}
			m44[0,3] = m44[1,3] = m44[2,3] =
			m44[3,0] = m44[3,1] = m44[3,2] = 0f;
			m44[3,3] = 1f; 
			return m44;
		}
#endregion  // Casting
	}
}
