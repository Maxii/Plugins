using UnityEngine;

namespace GridFramework {
	namespace Vectors {
	/// <summary>A class that holds three colours as X-, Y- and Z-value.</summary>
		/// This class groups three colours together, similar to how Vector3 groups three float numbers together.
		/// Just like Vector3 you can read and assign values using x, y, or an indexer.
		[System.Serializable]
		public class ColorVector3{
			[SerializeField]
			private Color[] values = new Color[3] {new Color(1.0f, 0.0f, 0.0f, 0.5f), new Color(0.0f, 1.0f, 0.0f, 0.5f), new Color(0.0f, 0.0f, 1.0f, 0.5f)};
			
			/// <summary>X component of the colour vector.</summary>
			public Color x {
				get {return values [0];}
				set {values [0] = value;}
			}
			
			/// <summary> Y component of the colour vector.</summary>
			public Color y {
				get {return values [1];}
				set {values [1] = value;}
			}
			
			/// <summary> Z component of the colour vector.</summary>
			public Color z {
				get {return values [2];}
				set {values [2] = value;}
			}

			/// <summary>Access the X, Y or Z components using [0], [1], [2] respectively.</summary>
			/// <param	name="index">The index.</param>
			/// Access the x, y, z components using [0], [1], [2] respectively. Example:
			/// <code>
			/// ColorVector3 c = new ColorVector3();
			/// c[1] = true; // the same as c.y = true
			/// </code>
			public Color this[int index]{
				get {return values [index];}
				set {values [index] = value;}	
			}

			/// <summary>Creates a new colour vector with given X, Y and Z components.</summary>
			/// <param name="x">X-value of the new vector.</param>
			/// <param name="y">Y-value of the new vector.</param>
			/// <param name="z">Z-value of the new vector.</param>
			public ColorVector3(Color x, Color y, Color z){ //taking individual colours
				values[0] = x;
				values[1] = y;
				values[2] = z;
			}
			
			///<summary>Creates a standard RGB ColorVector3.</summary>
			/// Creates a new standard RGB <see cref="ColorVector3"/> where all three colours have their alpha set to 0.5.
			public ColorVector3(){ //default
				values [0] = new Color(1.0f, 0.0f, 0.0f, 0.5f);
				values [1] = new Color(0.0f, 1.0f, 0.0f, 0.5f);
				values [2] = new Color(0.0f, 0.0f, 1.0f, 0.5f);
			}
			
			/// <summary>Creates a one-colour ColorVector3.</summary>
			/// <param name="color">The colur for all ccomponents.</param>
			/// Creates a new <see cref="ColorVector3"/> where all components are set to the same colour.
			public ColorVector3(Color color){
				values [0] = color;
				values [1] = color;
				values [2] = color;
			}

			/// <summary>Shorthand writing for <c>ColorVector3()</c></summary>
			public static ColorVector3 RGB {get{return new ColorVector3();}} // standard RGB Colour Vector
			/// <summary>Shorthand writing for <c>ColorVector3(Color(0,1,1,0.5), Color(1,0,1,0.5), Color(1,1,0,0.5))</c></summary>
			public static ColorVector3 CMY {get{return new ColorVector3(new Color(0, 1, 1, 0.5f), new Color(1, 0, 1, 0.5f), new Color(1, 1, 0, 0.5f));}}
			/// <summary>Shorthand writing for <c>ColorVector3(Color(0,0,0,0.5), Color(0.5,0.5,0.5,0.5), Color(1,1,1,0.5))</c></summary>
			public static ColorVector3 BGW {get{return new ColorVector3(new Color(0, 0, 0, 0.5f), new Color(0.5f, 0.5f, 0.5f, 0.5f), new Color(1, 1, 1, 0.5f));}}
		}
	}
}
