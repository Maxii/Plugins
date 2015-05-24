using UnityEngine;
using System.Collections;

public class F3DFlameThrower : MonoBehaviour
{
    public Light pLight;            // Attached point light
    public ParticleSystem heat;     // Heat particles

    int lightState;                 // Point light state flag (fading in or out)
    bool despawn;                   // Despawn state flag

    ParticleSystem ps;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
    }

    // OnSpawned called by pool manager 
    void OnSpawned()
    {
        despawn = false;
        F3DAudioController.instance.FlameGunLoop(transform.position, transform);

        lightState = 1;
        pLight.intensity = 0f;
    }

    // OnDespawned called by pool manager 
    void OnDespawned()
    {
    }

    // Despawn game object
    void OnDespawn()
    {
        F3DPool.instance.Despawn(transform);
    }

    void Update()
    {
        // Despawn on mouse
        if (Input.GetMouseButtonUp(0))
        {
            if (!despawn)
            {
                // Set despawn flag and add despawn timer allowing particles fading
                despawn = true;
                F3DTime.time.AddTimer(1f, 1, OnDespawn);

                // Stop the particle systems
                ps.Stop();
                if (heat)
                    heat.Stop();

                // Play
                F3DAudioController.instance.FlameGunClose(transform.position);

                // Toggle light state
                pLight.intensity = 0.6f;
                lightState = -1;
            }
        }

        // Fade in point light
        if (lightState == 1)
        {
            pLight.intensity = Mathf.Lerp(pLight.intensity, 0.7f, Time.deltaTime * 10f);

            if (pLight.intensity >= 0.5f)
                lightState = 0;
        }
        // Fade out point light
        else if (lightState == -1)
        {
            pLight.intensity = Mathf.Lerp(pLight.intensity, -0.1f, Time.deltaTime * 10f);

            if (pLight.intensity <= 0f)
                lightState = 0;
        }
    }
}
