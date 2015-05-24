using UnityEngine;
using System.Collections;

[RequireComponent(typeof(LineRenderer))]
public class F3DBeam : MonoBehaviour
{
    public LayerMask layerMask;

    public F3DFXType fxType;        // Weapon type
    public bool OneShot;            // Constant or single beam?
    
    public Texture[] BeamFrames;    // Animation frame sequence
    public float FrameStep;         // Animation time

    public float beamScale;         // Default beam scale to be kept over distance
    public float MaxBeamLength;     // Maximum beam length

    public bool AnimateUV;          // UV Animation
    public float UVTime;            // UV Animation speed
        
    public Transform rayImpact;     // Impact transform
    public Transform rayMuzzle;     // Muzzle flash transform

    LineRenderer lineRenderer;      // Line rendered component
    RaycastHit hitPoint;            // Raycast structure

    int frameNo;                    // Frame counter
    int FrameTimerID;               // Frame timer reference
    float beamLength;               // Current beam length
    float initialBeamOffset;        // Initial UV offset 

    void Awake()
    {
        // Get line renderer component
        lineRenderer = GetComponent<LineRenderer>();

        // Assign first frame texture
        if (!AnimateUV && BeamFrames.Length > 0)
            lineRenderer.material.mainTexture = BeamFrames[0];

        // Randomize uv offset
        initialBeamOffset = Random.Range(0f, 5f);
    }
    
    // OnSpawned called by pool manager 
    void OnSpawned()
    {
        // Do one time raycast in case of one shot flag
        if (OneShot)
            Raycast();

        // Start animation sequence if beam frames array has more than 2 elements
        if (BeamFrames.Length > 1)
            Animate();

        // Play audio depending on weapon type
        switch (fxType)
        {
            case F3DFXType.PlasmaBeam:
                // Spawn audio source prefab at specified position linked to parent of current transform
                F3DAudioController.instance.PlasmaBeamLoop(transform.position, transform.parent);
                break;

            case F3DFXType.PlasmaBeamHeavy:
                F3DAudioController.instance.PlasmaBeamHeavyLoop(transform.position, transform.parent);
                break;

            default:
                break;
        }         
    }

    // OnDespawned called by pool manager 
    void OnDespawned()
    {
        // Reset frame counter
        frameNo = 0;

        // Clear timer
        if (FrameTimerID != -1)
        {
            F3DTime.time.RemoveTimer(FrameTimerID);
            FrameTimerID = -1;
        }
        
        // Play audio
        switch (fxType)
        {
            case F3DFXType.PlasmaBeam:
                F3DAudioController.instance.PlasmaBeamClose(transform.position);
                break;

            case F3DFXType.PlasmaBeamHeavy:
                F3DAudioController.instance.PlasmaBeamHeavyClose(transform.position);
                break;

            default:
                break;
        }        
    }

    // Hit point calculation
    void Raycast()
    {
        // Prepare structure and create ray
        hitPoint = new RaycastHit();
        Ray ray = new Ray(transform.position, transform.forward);

        // Calculate default beam proportion multiplier based on default scale and maximum length
        float propMult = MaxBeamLength * (beamScale / 10f);

        // Raycast
        if (Physics.Raycast(ray, out hitPoint, MaxBeamLength, layerMask))
        {
            // Get current beam length and update line renderer accordingly
            beamLength = Vector3.Distance(transform.position, hitPoint.point);
            lineRenderer.SetPosition(1, new Vector3(0f, 0f, beamLength));

            // Calculate default beam proportion multiplier based on default scale and current length
            propMult = beamLength * (beamScale / 10f);

            // Spawn prefabs and apply force
            switch (fxType)
            {                
                case F3DFXType.Sniper:
                    
                    F3DFXController.instance.SniperImpact(hitPoint.point + hitPoint.normal * 0.2f);
                    ApplyForce(4f);
                    break;
                
                case F3DFXType.RailGun:
                    F3DFXController.instance.RailgunImpact(hitPoint.point + hitPoint.normal * 0.2f);
                    ApplyForce(7f);
                    break;
                
                case F3DFXType.PlasmaBeam:
                    ApplyForce(0.5f);
                    break;
                
                case F3DFXType.PlasmaBeamHeavy:
                    ApplyForce(2f);
                    break;

                default:
                    break;
            }

            // Adjust impact effect position
            if (rayImpact)
                rayImpact.position = hitPoint.point - transform.forward * 0.5f;
        }

        // Nothing was his
        else
        {
            // Set beam to maximum length
            beamLength = MaxBeamLength;
            lineRenderer.SetPosition(1, new Vector3(0f, 0f, beamLength));

            // Adjust impact effect position
            if (rayImpact)
                rayImpact.position = transform.position + transform.forward * beamLength;
        }

        // Adjust muzzle position
        if(rayMuzzle)
            rayMuzzle.position = transform.position + transform.forward * 0.1f;

        // Set beam scaling according to its length
        lineRenderer.material.SetTextureScale("_MainTex", new Vector2(propMult, 1f));
    }   

    // Advance texture frame
    void OnFrameStep()
    {
        // Set current texture frame based on frame counter
        lineRenderer.material.mainTexture = BeamFrames[frameNo];
        frameNo++;

        // Reset frame counter
        if (frameNo == BeamFrames.Length)
            frameNo = 0;
    }

    // Initialize frame animation
    void Animate()
    {
        if (BeamFrames.Length > 1)
        {
            // Set current frame
            frameNo = 0;
            lineRenderer.material.mainTexture = BeamFrames[frameNo];

            // Add timer 
            FrameTimerID = F3DTime.time.AddTimer(FrameStep, BeamFrames.Length - 1, OnFrameStep);

            frameNo = 1;
        }
    }

    // Apply force to last hit object
    void ApplyForce(float force)
    {
        if (hitPoint.rigidbody != null)
            hitPoint.rigidbody.AddForceAtPosition(transform.forward * force, hitPoint.point, ForceMode.VelocityChange);
    }
    
    void Update()
    {
        // Animate texture UV
        if (AnimateUV)        
            lineRenderer.material.SetTextureOffset("_MainTex", new Vector2(Time.time * UVTime + initialBeamOffset, 0f));

        // Raycast for laser beams
        if (!OneShot)        
            Raycast();       
    }
}
