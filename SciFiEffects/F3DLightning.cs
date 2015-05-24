using UnityEngine;
using System.Collections;

[RequireComponent(typeof(LineRenderer))]
public class F3DLightning : MonoBehaviour
{
    public LayerMask layerMask;

    public Texture[] BeamFrames;    // Animation frame sequence
    public float FrameStep;         // Animation time
    public bool RandomizeFrames;    // Randomization of animation frames

    public int Points;              // How many points should be used to construct the beam

    public float MaxBeamLength;     // Maximum beam length
    public float beamScale;         // Default beam scale to be kept over distance

    public bool AnimateUV;          // UV Animation
    public float UVTime;            // UV Animation speed

    public bool Oscillate;          // Beam oscillation flag
    public float Amplitude;         // Beam amplitude
    public float OscillateTime;     // Beam oscillation rate

    public Transform rayImpact;     // Impact transform
    public Transform rayMuzzle;     // Muzzle flash transform

    LineRenderer lineRenderer;      // Line rendered component
    RaycastHit hitPoint;            // Raycast structure

    int frameNo;                    // Frame counter
    int FrameTimerID;               // Frame timer reference
    int OscillateTimerID;           // Beam oscillation timer reference

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
        // Start animation sequence if beam frames array has more than 2 elements
        if (BeamFrames.Length > 1)                 
            Animate();        

        // Start oscillation sequence
        if (Oscillate && Points > 0)
            OscillateTimerID = F3DTime.time.AddTimer(OscillateTime, OnOscillate);

        // Play audio
        if (F3DAudioController.instance)
            F3DAudioController.instance.LightningGunLoop(transform.position, transform);  
    }

    // OnDespawned called by pool manager 
    void OnDespawned()
    {
        // Reset frame counter
        frameNo = 0;

        // Clear frame animation timer
        if (FrameTimerID != -1)
        {
            F3DTime.time.RemoveTimer(FrameTimerID);
            FrameTimerID = -1;
        }

        // Clear oscillation timer
        if (OscillateTimerID != -1)
        {
            F3DTime.time.RemoveTimer(OscillateTimerID);
            OscillateTimerID = -1;
        }

        // Play audio
        if (F3DAudioController.instance)
            F3DAudioController.instance.LightningGunClose(transform.position);       
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
            // Get current beam length
            beamLength = Vector3.Distance(transform.position, hitPoint.point);

            // Update line renderer
            if (!Oscillate)
                lineRenderer.SetPosition(1, new Vector3(0f, 0f, beamLength));

            // Calculate default beam proportion multiplier based on default scale and current length
            propMult = beamLength * (beamScale / 10f);
                        
            // Apply hit force to rigidbody
            ApplyForce(0.1f);

            // Adjust impact effect position
            if (rayImpact)
                rayImpact.position = hitPoint.point - transform.forward * 0.5f;
        }

        // Nothing was his
        else
        {
            // Set beam to maximum length
            beamLength = MaxBeamLength;

            // Update beam length
            if (!Oscillate)
                lineRenderer.SetPosition(1, new Vector3(0f, 0f, beamLength));

            // Adjust impact effect position
            if (rayImpact)
                rayImpact.position = transform.position + transform.forward * beamLength;
        }

        // Adjust muzzle position
        if (rayMuzzle)
            rayMuzzle.position = transform.position + transform.forward * 0.1f;

        // Set beam scaling according to its length
        lineRenderer.material.SetTextureScale("_MainTex", new Vector2(propMult, 1f));
    }

    // Generate random noise numbers based on amplitude
    float GetRandomNoise()
    {
        return Random.Range(-Amplitude, Amplitude);
    }

    // Advance texture frame
    void OnFrameStep()
    {
        // Randomize frame counter
        if (RandomizeFrames)
            frameNo = Random.Range(0, BeamFrames.Length);

        // Set current texture frame based on frame counter
        lineRenderer.material.mainTexture = BeamFrames[frameNo];
        frameNo++;

        // Reset frame counter
        if (frameNo == BeamFrames.Length)
            frameNo = 0;
    }

    // Oscillate beam
    void OnOscillate()
    {   
        // Calculate number of points based on beam length and default number of points
        int points = (int)((beamLength / 10f) * Points);

        // Update line rendered segments in case number of points less than 2
        if (points < 2)
        {
            lineRenderer.SetVertexCount(2);
            lineRenderer.SetPosition(0, Vector3.zero);
            lineRenderer.SetPosition(1, new Vector3(0, 0, beamLength));
        }
        // Update line renderer segments
        else
        {
            // Update number of points for line renderer
            lineRenderer.SetVertexCount(points);
            // Set zero point manually
            lineRenderer.SetPosition(0, Vector3.zero);

            // Update each point with random noise based on amplitude
            for (int i = 1; i < points - 1; i++)
                lineRenderer.SetPosition(i, new Vector3(GetRandomNoise(), GetRandomNoise(), (beamLength / (points - 1)) * i));

            // Set last point manually 
            lineRenderer.SetPosition(points - 1, new Vector3(0f, 0f, beamLength));
        }
    }

    // Initialize frame animation
    void Animate()
    {
        // Set current frame
        frameNo = 0;
        lineRenderer.material.mainTexture = BeamFrames[frameNo];

        // Add timer 
        FrameTimerID = F3DTime.time.AddTimer(FrameStep, OnFrameStep);

        frameNo = 1;
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
       
        // Process raycasting 
        Raycast();
    }
}
