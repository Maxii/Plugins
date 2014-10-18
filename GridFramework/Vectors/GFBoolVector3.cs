using UnityEngine;

namespace GridFramework {
	namespace Vectors {
	/// <summary>A class that holds three booleans as X-, Y- and Z-value.</summary>
		/// This class groups three booleans together, similar to how Vector3 groups three float numbers together.
		/// Just like Vector3 you can read and assign values using x, y, or an indexer.
		[System.Serializable]
		public class BoolVector3 {
			[SerializeField]
			private bool[] values = new bool[3] {false, false, false};
			
			/// <summary>X component of the bool vector.</summary>
			public bool x {
				get {return values [0];}
				set {values [0] = value;}
			}
			/// <summary>Y component of the bool vector.</summary>
			public bool y {
				get {return values [1];}
				set {values [1] = value;}
			}
			/// <summary>Z component of the bool vector.</summary>
			public bool z {
				get {return values [2];}
				set {values [2] = value;}
			}
			
			/// <summary>Access the X, Y or Z components using [0], [1], [2] respectively.</summary>
			/// <param name="index">The index.</param>
			/// Access the x, y, z components using [0], [1], [2] respectively. Example:
			/// <code>
			/// BoolVector3 b = new BoolVector3();
			/// b[1] = true; // the same as b.y = true
			/// </code>
			public bool this[int index]{
				get {return values [index];}
				set {values [index] = value;}	
			}
			
			/// <summary>Creates a new bool vector with given X, Y and Z components.</summary>
			/// <param name="x">X value.</param>
			/// <param name="y">Y value.</param>
			/// <param name="z">Z value.</param>
			public BoolVector3(bool x, bool y, bool z){
				//values = new bool[3] { x, y, z };
				values [0] = x;
				values [1] = y;
				values [2] = z;
			}
			
			/// <summary>Creates an all-<c>false</c> BoolVector3.</summary>
			/// Creates an all-<c>false</c> BoolVector3.
			public BoolVector3(){
				values [0] = false;
				values [1] = false;
				values [2] = false;
			}
			
			/// <summary>Creates a new BoolVector3 set to a condition.</summary>
			/// <param name="condition">The value to be used for all components.</param>
			/// reates a new BoolVector3 set to <c>condition</c>.
			public BoolVector3(bool condition){
				values [0] = condition;
				values [1] = condition;
				values [2] = condition;
			}	
			
			/// <summary>Creates a new all-<c>false</c> BoolVector3.</summary>
			/// This is the same as calling <c>BoolVector3(false)</c>.
			public static BoolVector3 False {get{return new BoolVector3(false);}}
			
			/// <summary>Creates a new all-<c>true</c> BoolVector3.</summary>
			/// This is the same as calling <c>BoolVector3(true)</c>.
			public static BoolVector3 True {get{return new BoolVector3(true);}}
		}
	}
}
