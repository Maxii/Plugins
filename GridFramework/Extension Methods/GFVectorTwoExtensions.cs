using UnityEngine;
using System.Collections;

public static class GFVectorTwoExtensions{
	// returns the same vector, except the Y-component has been inverted, similar to complex conjugation
	public static Vector2 GFConjugate(this Vector2 theVector, bool reverse = false){
		if(!reverse){
			theVector.y = -theVector.y;
		} else{
			theVector.x = -theVector.x;
		}
		return theVector;
	}
}
