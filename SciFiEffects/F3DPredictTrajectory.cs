using UnityEngine;
using System.Collections;

public class F3DPredictTrajectory : MonoBehaviour
{

	public static Vector3 Predict(Vector3 sPos, Vector3 tPos, Vector3 tLastPos, float pSpeed)
    {
        // Target velocity
        Vector3 tVel = (tPos - tLastPos) / Time.deltaTime;
        
        // Time to reach the target
        float flyTime = GetProjFlightTime(tPos - sPos, tVel, pSpeed);

        if (flyTime > 0)
            return tPos + flyTime * tVel;
        else
            return tPos;
    }

    static float GetProjFlightTime(Vector3 dist, Vector3 tVel, float pSpeed)
    {
        float a = Vector3.Dot(tVel, tVel) - pSpeed * pSpeed;
        float b = 2.0f * Vector3.Dot(tVel, dist);
        float c = Vector3.Dot(dist, dist);

        float det = b * b - 4 * a * c;

        if (det > 0)
            return 2 * c / (Mathf.Sqrt(det) - b);
        else
            return -1;
    }
}
