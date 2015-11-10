using UnityEngine;
using System.Collections;


public class F3DMissile : MonoBehaviour
{
    public enum MissileType
    {
        Unguided,
        Guided,
        Predictive
    }

    public MissileType missileType;
    public Transform target;
    public LayerMask layerMask;

    public float detonationDistance;

    public float lifeTime = 5f;                 // Missile life time
    public float despawnDelay;                  // Delay despawn in ms
    public float velocity = 300f;               // Missile velocity
    public float alignSpeed = 1f;
    public float RaycastAdvance = 2f;           // Raycast advance multiplier

    public bool DelayDespawn = false;           // Missile despawn flag

    public ParticleSystem[] delayedParticles;   // Array of delayed particles
    ParticleSystem[] particles;                 // Array of Missile particles

    new Transform transform;                    // Cached transform

    bool isHit = false;                         // Missile hit flag
    bool isFXSpawned = false;                   // Hit FX prefab spawned flag

    float timer = 0f;                           // Missile timer

    Vector3 targetLastPos;    
    Vector3 step;

    MeshRenderer meshRenderer;

    void Awake()
    {
        // Cache transform and get all particle systems attached
        transform = GetComponent<Transform>();
        particles = GetComponentsInChildren<ParticleSystem>();

        meshRenderer = GetComponent<MeshRenderer>();
    }

    // OnSpawned called by pool manager 
    public void OnSpawned()
    {       
        isHit = false;
        isFXSpawned = false;
        timer = 0f;        
        targetLastPos = Vector3.zero;
        step = Vector3.zero;
        meshRenderer.enabled = true;
    }

    // OnDespawned called by pool manager 
    public void OnDespawned()
    {          
    }

    // Stop attached particle systems emission and allow them to fade out before despawning
    void Delay()
    {       
        if(particles.Length > 0 && delayedParticles.Length > 0)
        {
            bool delayed;

            for (int i = 0; i < particles.Length; i++)
            {
                delayed = false;

                for (int y = 0; y < delayedParticles.Length; y++)                
                    if (particles[i] == delayedParticles[y])
                    {
                        delayed = true;
                        break;
                    }                

                particles[i].Stop(false);

                if (!delayed)
                    particles[i].Clear(false);                
            }
        }
    }

    // Despaw routine
    void OnMissileDestroy()
    {   
        if(F3DPool.instance != null)
            F3DPool.instance.Despawn(transform);
    }
    
    void OnHit()
    {
        meshRenderer.enabled = false;
        isHit = true;

        // Invoke delay routine if required
        if (DelayDespawn)
        {
            // Reset missile timer and let particles systems stop emitting and fade out correctly
            timer = 0f;
            Delay();
        }
    }

    void Update()
    {       
        // If something was hit
        if (isHit)
        {
            // Execute once
            if (!isFXSpawned)
            {
                // Put your calls to effect manager that spawns explosion on hit
                // .....

                F3DMissileLauncher.instance.SpawnExplosion(transform.position);

                isFXSpawned = true;
            }

            // Despawn current missile 
            if(!DelayDespawn || (DelayDespawn && (timer >= despawnDelay)))
                OnMissileDestroy();
        }
        // No collision occurred yet
        else
        {
            // Navigate
            if (target != null)
            {
                if (missileType == MissileType.Predictive)
                {
                    Vector3 hitPos = F3DPredictTrajectory.Predict(transform.position, target.position, targetLastPos, velocity);
                    targetLastPos = target.position;

                    transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(hitPos - transform.position), Time.deltaTime * alignSpeed);                
                }
                else if (missileType == MissileType.Guided)
                {
                    transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(target.position - transform.position), Time.deltaTime * alignSpeed);
                }
            }
            
            // Missile step per frame based on velocity and time
            step = transform.forward * Time.deltaTime * velocity;
                        
            if (target != null && missileType != MissileType.Unguided && Vector3.SqrMagnitude(transform.position - target.position) <= detonationDistance)
            {
                OnHit();
            }
            else if (missileType == MissileType.Unguided && Physics.Raycast(transform.position, transform.forward, step.magnitude * RaycastAdvance, layerMask))
            {
                OnHit();
            }
            // Nothing hit
            else
            {
                // Despawn missile at the end of life cycle
                if (timer >= lifeTime)
                {
                    // Do not detonate
                    isFXSpawned = true;
                    OnHit();
                }
            }

            // Advances missile forward
            transform.position += step;
        }

        // Updates missile timer
        timer += Time.deltaTime;
    }
}
