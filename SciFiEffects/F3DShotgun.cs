using UnityEngine;
using System.Collections;

public class F3DShotgun : MonoBehaviour 
{
    #if UNITY_5_0   
    private ParticleCollisionEvent[] collisionEvents = new ParticleCollisionEvent[16];
    private ParticleSystem ps;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
    }

    // On particle collision
    void OnParticleCollision(GameObject other)
    {
        int safeLength = ParticlePhysicsExtensions.GetSafeCollisionEventSize(ps);

        if (collisionEvents.Length < safeLength)
            collisionEvents = new ParticleCollisionEvent[safeLength];

        int numCollisionEvents = ParticlePhysicsExtensions.GetCollisionEvents(ps, other, collisionEvents);

        // Play collision sound and apply force to the rigidbody was hit
        int i = 0;
        while (i < numCollisionEvents)
        {
            F3DAudioController.instance.ShotGunHit(collisionEvents[i].intersection);

            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb)
            {
                Vector3 pos = collisionEvents[i].intersection;
                Vector3 force = collisionEvents[i].velocity.normalized * 50f;

                rb.AddForceAtPosition(force, pos);
            }

            i++;
        }
    }    
    #else
    // Particle collision events
    private ParticleCollisionEvent[] collisionEvents = new ParticleCollisionEvent[16];

    // On particle collision
    void OnParticleCollision(GameObject other)
    {
        int safeLength = GetComponent<ParticleSystem>().GetSafeCollisionEventSize();

        if (collisionEvents.Length < safeLength)
            collisionEvents = new ParticleCollisionEvent[safeLength];

        int numCollisionEvents = GetComponent<ParticleSystem>().GetCollisionEvents(other, collisionEvents);
        
        // Play collision sound and apply force to the rigidbody was hit
        int i = 0;
        while (i < numCollisionEvents)
        {
            F3DAudioController.instance.ShotGunHit(collisionEvents[i].intersection);

            if (other.GetComponent<Rigidbody>())
            {
                Vector3 pos = collisionEvents[i].intersection;
                Vector3 force = collisionEvents[i].velocity.normalized * 50f;

                other.GetComponent<Rigidbody>().AddForceAtPosition(force, pos);
            }

            i++;
        }
    }  
    #endif
}
