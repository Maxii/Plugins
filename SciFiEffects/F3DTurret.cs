using UnityEngine;
using System.Collections;

public class F3DTurret : MonoBehaviour
{
    public Transform hub;           // Turret hub 
    public Transform barrel;        // Turret barrel

    RaycastHit hitInfo;             // Raycast structure
    bool isFiring;                  // Is turret currently in firing state
    
    float hubAngle, barrelAngle;    // Current hub and barrel angles

    // Project vector on plane
    Vector3 ProjectVectorOnPlane(Vector3 planeNormal, Vector3 vector)
    {
        return vector - (Vector3.Dot(vector, planeNormal) * planeNormal);
    }

    // Get signed vector angle
    float SignedVectorAngle(Vector3 referenceVector, Vector3 otherVector, Vector3 normal)
    {
        Vector3 perpVector;
        float angle;
       
        perpVector = Vector3.Cross(normal, referenceVector);
        angle = Vector3.Angle(referenceVector, otherVector);
        angle *= Mathf.Sign(Vector3.Dot(perpVector, otherVector));

        return angle;
    }

    // Turret tracking
    void Track()
    {
        if(hub != null && barrel != null)
        {
            // Construct a ray pointing from screen mouse position into world space
            Ray cameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);

            // Raycast
            if (Physics.Raycast(cameraRay, out hitInfo, 500f))
            {
                // Calculate heading vector and rotation quaternion
                Vector3 headingVector = ProjectVectorOnPlane(hub.up, hitInfo.point - hub.position);
                Quaternion newHubRotation = Quaternion.LookRotation(headingVector);

                // Check current heading angle
                hubAngle = SignedVectorAngle(transform.forward, headingVector, Vector3.up);
                                
                // Limit heading angle if required
                if (hubAngle <= -60)
                    newHubRotation = Quaternion.LookRotation(Quaternion.Euler(0, -60, 0) * transform.forward);
                else if (hubAngle >= 60)
                    newHubRotation = Quaternion.LookRotation(Quaternion.Euler(0, 60, 0) * transform.forward);

                // Apply heading rotation
                hub.rotation = Quaternion.Slerp(hub.rotation, newHubRotation, Time.deltaTime * 5f);

                // Calculate elevation vector and rotation quaternion
                Vector3 elevationVector = ProjectVectorOnPlane(hub.right, hitInfo.point - barrel.position);
                Quaternion newBarrelRotation = Quaternion.LookRotation(elevationVector);

                // Check current elevation angle
                barrelAngle = SignedVectorAngle(hub.forward, elevationVector, hub.right);
              
                // Limit elevation angle if required
                if (barrelAngle < -30)
                    newBarrelRotation = Quaternion.LookRotation(Quaternion.AngleAxis(-30f, hub.right) * hub.forward);   
                else if (barrelAngle > 15)
                    newBarrelRotation = Quaternion.LookRotation(Quaternion.AngleAxis(15f, hub.right) * hub.forward);  

                // Apply elevation rotation
                barrel.rotation = Quaternion.Slerp(barrel.rotation, newBarrelRotation, Time.deltaTime * 5f);
            }
        }
    }

    void Update()
    {
        // Track turret
        Track();

        // Fire turret
        if (!isFiring && Input.GetKeyDown(KeyCode.Mouse0))
        {
            isFiring = true;
            F3DFXController.instance.Fire();
        }

        // Stop firing
        if (isFiring && Input.GetKeyUp(KeyCode.Mouse0))
        {
            isFiring = false;
            F3DFXController.instance.Stop();
        }
    }
}
