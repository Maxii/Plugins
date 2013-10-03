using UnityEngine;
using System.Collections;

public static class GFTransformExtensions{
	
	/// <summary>Transforms point from local space to world space without taking scale into account.</summary>
	/// <param name="position">Position in world space.</param>
	/// <returns>Point in local space.</returns>
	/// 
	/// Works the same as Transform.TransformPoint but independent from the scale, i.e. transforms a point from local space to world space without taking the scale into account.
	/// This is not the same as Grid space.
	public static Vector3 GFTransformPointFixed(this Transform theTransform, Vector3 position){
		//return theTransform.localToWorldMatrix.MultiplyPoint3x4(position);
		return theTransform.TransformDirection(position) + theTransform.position;
	}
	
	/// <summary>Transforms point from world space to local space without taking scale into account.</summary>
	/// <param name="position">Position in local space.</param>
	/// <returns>Point in world space.</returns>
	/// 
	/// Works the same as Transform.InverseTransformPoint but independent from the scale, i.e. transforms a point from world space to local space without taking the scale into account.
	/// This is not the same as Grid space.
	public static Vector3 GFInverseTransformPointFixed(this Transform theTransform, Vector3 position){
		return theTransform.InverseTransformDirection(position - theTransform.position);
	}
}
