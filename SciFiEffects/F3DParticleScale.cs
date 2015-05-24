using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class F3DParticleScale : MonoBehaviour
{
    [Range(0f, 20f)]
    public float ParticleScale = 1.0f;      // Particle scale
    public bool ScaleGameobject = true;     // Should the game be scaled as well

    float prevScale;                        // Previous scale

    void Start()
    { 
        // Store previous scale
        prevScale = ParticleScale;
    }    

    // Scale Shuriken particle system 
    void ScaleShurikenSystems(float scaleFactor)
    {
        #if UNITY_EDITOR
        ParticleSystem[] systems = GetComponentsInChildren<ParticleSystem>();

        foreach (ParticleSystem system in systems)
        {
            system.startSpeed *= scaleFactor;
            system.startSize *= scaleFactor;
            system.gravityModifier *= scaleFactor;

            SerializedObject so = new SerializedObject(system);

            so.FindProperty("VelocityModule.x.scalar").floatValue *= scaleFactor;
            so.FindProperty("VelocityModule.y.scalar").floatValue *= scaleFactor;
            so.FindProperty("VelocityModule.z.scalar").floatValue *= scaleFactor;
            so.FindProperty("ClampVelocityModule.magnitude.scalar").floatValue *= scaleFactor;
            so.FindProperty("ClampVelocityModule.x.scalar").floatValue *= scaleFactor;
            so.FindProperty("ClampVelocityModule.y.scalar").floatValue *= scaleFactor;
            so.FindProperty("ClampVelocityModule.z.scalar").floatValue *= scaleFactor;
            so.FindProperty("ForceModule.x.scalar").floatValue *= scaleFactor;
            so.FindProperty("ForceModule.y.scalar").floatValue *= scaleFactor;
            so.FindProperty("ForceModule.z.scalar").floatValue *= scaleFactor;
            so.FindProperty("ColorBySpeedModule.range").vector2Value *= scaleFactor;
            so.FindProperty("SizeBySpeedModule.range").vector2Value *= scaleFactor;
            so.FindProperty("RotationBySpeedModule.range").vector2Value *= scaleFactor;          

            so.ApplyModifiedProperties();
        }
        #endif
    }

    // Scale trail renderer
    void ScaleTrailRenderers(float scaleFactor)
    {
        TrailRenderer[] trails = GetComponentsInChildren<TrailRenderer>();

        foreach (TrailRenderer trail in trails)
        {
            trail.startWidth *= scaleFactor;
            trail.endWidth *= scaleFactor;
        }
    }

    void Update()
    {
        #if UNITY_EDITOR
        if (prevScale != ParticleScale && ParticleScale > 0)
        {
            if (ScaleGameobject)
                transform.localScale =
                new Vector3(ParticleScale, ParticleScale, ParticleScale);

            float scaleFactor = ParticleScale / prevScale;

            ScaleShurikenSystems(scaleFactor);
            ScaleTrailRenderers(scaleFactor);

            prevScale = ParticleScale;
        }
        #endif
    }
}