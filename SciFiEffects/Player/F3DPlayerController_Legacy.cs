using UnityEngine;
using System.Collections;
namespace Forge3D
{
    public class F3DPlayerController : MonoBehaviour
    {

        public F3DTurret[] Turret;

        public bool DebugDrawTarget = true;
        private Vector3 targetPos;

        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            for (int i = 0; i < Turret.Length; i++)
            {
                if (Turret[i])
                {
                    // Simulating proper player input 
                    if (Input.GetMouseButtonDown(0))
                        Turret[i].PlayAnimation();
                    else if (Input.GetMouseButtonDown(1))
                        Turret[i].PlayAnimationLoop();
                    else if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1))
                        Turret[i].StopAnimation(); 
                    // Update the turret with the new target position
                    Turret[i].SetNewTarget(GetNewTargetPos());
                }
                else
                    break;
            }
        }

        // Constantly updates the ray against the scene geometry and background dummy collider.
        // Manually track the ray and to v3 position from scene geometry
        Vector3 GetNewTargetPos()
        {
            RaycastHit hitInfo;
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hitInfo))
            {
                targetPos = hitInfo.point;
                return targetPos;
            }
            return Vector3.zero;
        }

        // Debug draw target 
        void OnDrawGizmos()
        {
            if (DebugDrawTarget && targetPos != Vector3.zero)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(targetPos, 0.5f);
            }
        }
    }
}