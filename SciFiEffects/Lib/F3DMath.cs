using UnityEngine;
using System.Collections;
using System;

namespace Forge3D
{
    public class F3DMath : MonoBehaviour
    {
        //Projects a vector onto a plane. The output is not normalized.
        public static Vector3 ProjectVectorOnPlane(Vector3 planeNormal, Vector3 vector)
        {
            return vector - (Vector3.Dot(vector, planeNormal)*planeNormal);
        }

        public static float SignedVectorAngle(Vector3 referenceVector, Vector3 otherVector, Vector3 normal)
        {
            Vector3 perpVector;
            float angle;

            //Use the geometry object normal and one of the input vectors to calculate the perpendicular vector
            perpVector = Vector3.Cross(normal, referenceVector);

            //Now calculate the dot product between the perpendicular vector (perpVector) and the other input vector
            angle = Vector3.Angle(referenceVector, otherVector);
            angle *= Mathf.Sign(Vector3.Dot(perpVector, otherVector));

            return angle;
        }
    }
}