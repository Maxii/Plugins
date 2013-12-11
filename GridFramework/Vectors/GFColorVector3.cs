using UnityEngine;
using System.Collections;

/**
 * \brief A class that holds three colours as X-, Y- and Z-value.
 * 
 * This class groups three colours together, similar to how Vector3 groups three float numbers together.
 * Just like Vector3 you can read and assign values using x, y, or an indexer.
 */
[System.Serializable]
public class GFColorVector3{
	[SerializeField]
	private Color[] values = new Color[3] {new Color(1.0f, 0.0f, 0.0f, 0.5f), new Color(0.0f, 1.0f, 0.0f, 0.5f), new Color(0.0f, 0.0f, 1.0f, 0.5f)};
	/**
	 * \brief X component of the colour vector.
	 */
	public Color x {
		get {
			return values [0];
		}
		set{
			values [0] = value;
		}
	}
	/**
	 * \brief Y component of the colour vector.
	 */
	public Color y {
		get {
			return values [1];
		}
		set{
			values [1] = value;
		}
	}
	/**
	 * \brief Z component of the colour vector.
	 */
	public Color z {
		get {
			return values [2];
		}
		set{
			values [2] = value;
		}
	}

	/**
	 * @brief Access the X, Y or Z components using [0], [1], [2] respectively.
	 * @param	index	The index.
	 * 
	 * Access the x, y, z components using [0], [1], [2] respectively. Example:
	 * @code
	 * GFColorVector3 c = new GFColorVector3();
	 * c[1] = true; // the same as c.y = true
	 * @endcode
	 */
	public Color this[int index]{
		get{
			return values [index];
		}
		set{
			values [index] = value;
		}	
	}

	/**
	 * @brief Creates a new colour vector with given X, Y and Z components.
	 * @param	x,y,z	The value of each individual component.
	 */
	public GFColorVector3(Color x, Color y, Color z){ //taking individual colours
		values [0] = x;
		values [1] = y;
		values [2] = z;
	}

	/**
	 * @brief Creates a standard RGB GFBoolVector3.
	 * 
	 * Creates a new standard RGB <see cref="GFColorVector3"/> where all three colours have their alpha set to 0.5.
	 */
	public GFColorVector3(){ //default
		values [0] = new Color(1.0f, 0.0f, 0.0f, 0.5f);
		values [1] = new Color(0.0f, 1.0f, 0.0f, 0.5f);
		values [2] = new Color(0.0f, 0.0f, 1.0f, 0.5f);
	}
	/**
	 * @brief Creates a one-colour GFBoolVector3.
	 * @param	color	The colur for all ccomponents.
	 * 
	 * Creates a new <see cref="GFColorVector3"/> where all components are set to the same colour.
	 */
	public GFColorVector3(Color color){ //one colour for everything
		values [0] = color;
		values [1] = color;
		values [2] = color;
	}

	/// <summary>Shorthand writing for <see cref="GFColorVector3()"></summary>
	public static GFColorVector3 RGB {get{return new GFColorVector3();}} // standard RGB Colour Vector
	/// <summary>Shorthand writing for <c>GFColorVector3(Color(0,1,1,0.5), Color(1,0,1,0.5), Color(1,1,0,0.5))</c></summary>
	public static GFColorVector3 CMY {get{return new GFColorVector3(new Color(0, 1, 1, 0.5f), new Color(1, 0, 1, 0.5f), new Color(1, 1, 0, 0.5f));}}
	/// <summary>Shorthand writing for <c>GFColorVector3(Color(0,0,0,0.5), Color(0.5,0.5,0.5,0.5), Color(1,1,1,0.5))</c></summary>
	public static GFColorVector3 BGW {get{return new GFColorVector3(new Color(0, 0, 0, 0.5f), new Color(0.5f, 0.5f, 0.5f, 0.5f), new Color(1, 1, 1, 0.5f));}}
}