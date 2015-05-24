using UnityEngine;
using System.Collections;

public class F3DPulsewave : MonoBehaviour
{
    public float FadeOutDelay;      // Color fade delay in ms
    public float FadeOutTime;       // Color fade speed
    public float ScaleTime;         // Scaling speed
    public Vector3 ScaleSize;       // The size wave will be scaled to

    public bool DebugLoop;          // Constant looping flag mainly used in preview scene

    new Transform transform;        // Cached transform
    MeshRenderer meshRenderer;      // Cached mesh renderer

    int timerID = -1;               // Timer reference
    bool isFadeOut;                 // Fading flag
    bool isEnabled;                 // Enabled flag

    Color defaultColor;             // Default wave color
    Color color;                    // Current wave color

    int tintColorRef;               // Shader property reference

    void Awake()
    {
        // Cache components
        transform = GetComponent<Transform>();
        meshRenderer = GetComponent<MeshRenderer>();

        // Get shader property
        tintColorRef = Shader.PropertyToID("_TintColor");

        // Store default color
        defaultColor = meshRenderer.material.GetColor(tintColorRef);
    }

    void Start()
    {
        // Fire up manually
        if (DebugLoop)
            OnSpawned();
    }

    // OnSpawned called by pool manager 
    void OnSpawned()
    {
        // Set scale to zero
        transform.localScale = new Vector3(0f, 0f, 0f);

        // Set required flags and set delayed fade flag using timer 
        isEnabled = true;
        isFadeOut = false;
        timerID = F3DTime.time.AddTimer(FadeOutDelay, OnFadeOut);

        // Reset default color
        meshRenderer.material.SetColor(tintColorRef, defaultColor);
        color = defaultColor;
    }

    // OnDespawned called by pool manager 
    void OnDespawned()
    {
        // Remove timer
        if (timerID >= 0)
        {
            F3DTime.time.RemoveTimer(timerID);
            timerID = -1;
        }
    }

    // Toggle fading state
    void OnFadeOut()
    {        
        isFadeOut = true;
    }

    void Update ()
    {
        // Enabled state
        if (isEnabled)
        {
            // Scale the wave
            transform.localScale = Vector3.Lerp(transform.localScale, ScaleSize, Time.deltaTime * ScaleTime);

            // Check the fading state 
            if (isFadeOut)
            {
                // Lerp color and update the shader
                color = Color.Lerp(color, new Color(0, 0, 0, -0.1f), Time.deltaTime * FadeOutTime);
                meshRenderer.material.SetColor(tintColorRef, color);

                // Make sure alpha value is not overshooting 
                if (color.a <= 0f)
                {
                    // Disable the update loop 
                    isEnabled = false;

                    // Reset the sequence in case of the debug loop flag
                    if(DebugLoop)
                    {
                        OnDespawned();
                        OnSpawned();
                    }
                }
            }
        }
    }
}
