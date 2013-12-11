using UnityEngine;
using System.Collections;

/**
 * \brief A class that holds three booleans as X-, Y- and Z-value.
 * 
 * This class groups three booleans together, similar to how Vector3 groups three float numbers together.
 * Just like Vector3 you can read and assign values using x, y, or an indexer.
 */
[System.Serializable]
public class GFBoolVector3 {
	[SerializeField]
	private bool[] values = new bool[3] {false, false, false};
	/**
	 * \brief X component of the bool vector.
	 */
	public bool x {
		get {
			return values [0];
		}
		set{
			values [0] = value;
		}
	}
	/**
	 * \brief Y component of the bool vector.
	 */
	public bool y {
		get {
			return values [1];
		}
		set{
			values [1] = value;
		}
	}
	/**
	 * \brief Z component of the bool vector.
	 */
	public bool z {
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
	 * GFBoolVector3 b = new GFBoolVector3();
	 * b[1] = true; // the same as b.y = true
	 * @endcode
	 */
	public bool this[int index]{
		get{
			return values [index];
		}
		set{
			values [index] = value;
		}	
	}

	/**
	 * @brief Creates a new bool vector with given X, Y and Z components.
	 * @param	x,y,z	The value of each individual component.
	 */
	public GFBoolVector3(bool x, bool y, bool z){
		//values = new bool[3] { x, y, z };
		values [0] = x;
		values [1] = y;
		values [2] = z;
	}

	/**
	 * @brief Creates an all-<c>false</c> GFBoolVector3.
	 * 
	 * Creates an all-<c>false</c> GFBoolVector3.
	 */
	public GFBoolVector3(){
		values [0] = false;
		values [1] = false;
		values [2] = false;
	}

	/**
	 * @brief Creates a new GFBoolVector3 set to a condition.
	 * @param	condition	The value to be used for all components.
	 * 
	 * Creates a new GFBoolVector3 set to \c condition.
	 */
	public GFBoolVector3(bool condition){
		values [0] = condition;
		values [1] = condition;
		values [2] = condition;
	}	

	/**
	 * @brief Creates a new all-<c>false</c> GFBoolVector3.
	 * 
	 * This is the same as calling \c GFBoolVector3(false).
	 */
	public static GFBoolVector3 False {get{return new GFBoolVector3(false);}}

	/**
	 * @brief Creates a new all-<c>true</c> GFBoolVector3.
	 * 
	 * This is the same as calling \c GFBoolVector3(true).
	 */
	public static GFBoolVector3 True {get{return new GFBoolVector3(true);}}
}