using UnityEngine;
using System.Collections;

public static class GFVectorThreeExtensions{
	//divides two vectors component-wise
	public static Vector3 GFReverseScale(this Vector3 theVector, Vector3 relativeVector){
		Vector3 resultVector = Vector3.zero;
		for (int i = 0; i <=2; i++){
			resultVector[i] = theVector[i]/relativeVector[i];
		}
		return resultVector;
	}
	
	//modulo of a vector and a scalar
	public static Vector3 GFModulo(this Vector3 theVector, float theScalar){
		theVector.x = theVector.x % theScalar;
		theVector.y = theVector.y % theScalar;
		theVector.z = theVector.z % theScalar;
		return theVector;
	}
	
	//modulo of two vectors component-wise
	public static Vector3 GFModulo3(this Vector3 theVector, Vector3 modVector){
		return new Vector3(theVector.x % modVector.x, theVector.y % modVector.y, theVector.z % modVector.z);
	}
	
	//return the same vector, except all components positive
	public static Vector3 GFAbs(this Vector3 theVector){
		theVector.x = Mathf.Abs(theVector.x);
		theVector.y = Mathf.Abs(theVector.y);
		theVector.z = Mathf.Abs(theVector.z);
		return theVector;
	}
	
	//returns a Vector3 of signs of each component
	public static Vector3 GFSign(this Vector3 theVector){
		theVector.x = Mathf.Sign(theVector.x);
		theVector.y = Mathf.Sign(theVector.y);
		theVector.z = Mathf.Sign(theVector.z);
		return theVector;
	}
}
