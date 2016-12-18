using UnityEngine;
using System.Collections;

namespace Forge3D
{
    public class F3DAudioController : MonoBehaviour
    {
        // Singleton instance 
        public static F3DAudioController instance;

        // Audio timers
        float timer_01, timer_02;
        public Transform audioSource;
        [Header("Vulcan")] public AudioClip[] vulcanHit; // Impact prefabs array  
        public AudioClip vulcanShot; // Shot prefab
        public float vulcanDelay; // Shot delay in ms
        public float vulcanHitDelay; // Hit delay in ms

        [Header("Solo gun")] public AudioClip[] soloGunHit;
        public AudioClip soloGunShot;
        public float soloGunDelay;
        public float soloGunHitDelay;

        [Header("Sniper")] public AudioClip[] sniperHit;
        public AudioClip sniperShot;
        public float sniperDelay;
        public float sniperHitDelay;

        [Header("Shot gun")] public AudioClip[] shotGunHit;
        public AudioClip shotGunShot;
        public float shotGunDelay;
        public float shotGunHitDelay;

        [Header("Seeker")] public AudioClip[] seekerHit;
        public AudioClip seekerShot;
        public float seekerDelay;
        public float seekerHitDelay;

        [Header("Rail gun")] public AudioClip[] railgunHit;
        public AudioClip railgunShot;
        public float railgunDelay;
        public float railgunHitDelay;

        [Header("Plasma gun")] public AudioClip[] plasmagunHit;
        public AudioClip plasmagunShot;
        public float plasmagunDelay;
        public float plasmagunHitDelay;

        [Header("Plasma beam")] public AudioClip plasmabeamOpen; // Open audio clip prefab
        public AudioClip plasmabeamLoop; // Loop audio clip prefab
        public AudioClip plasmabeamClose; // Close audio clip prefab

        [Header("Plasma beam heavy")] public AudioClip plasmabeamHeavyOpen;
        public AudioClip plasmabeamHeavyLoop;
        public AudioClip plasmabeamHeavyClose;

        [Header("Lightning gun")] public AudioClip lightningGunOpen;
        public AudioClip lightningGunLoop;
        public AudioClip lightningGunClose;

        [Header("Flame gun")] public AudioClip flameGunOpen;
        public AudioClip flameGunLoop;
        public AudioClip flameGunClose;

        [Header("Laser impulse")] public AudioClip[] laserImpulseHit;
        public AudioClip laserImpulseShot;
        public float laserImpulseDelay;
        public float laserImpulseHitDelay;

        void Awake()
        {
            // Initialize singleton
            instance = this;
        }

        void Update()
        {
            // Update timers
            timer_01 += Time.deltaTime;
            timer_02 += Time.deltaTime;
        }

        // Play vulcan shot audio at specific position
        public void VulcanShot(Vector3 pos)
        {
            // Audio source can only be played once for each vulcanDelay
            if (timer_01 >= vulcanDelay)
            {
                // Spawn audio source prefab from pool
                AudioSource aSrc =
                    F3DPoolManager.Pools["GeneratedPool"].SpawnAudio(audioSource, vulcanShot, pos, null)
                        .gameObject.GetComponent<AudioSource>();

                if (aSrc != null)
                {
                    // Modify audio source settings specific to it's type
                    aSrc.pitch = Random.Range(0.95f, 1f);
                    aSrc.volume = Random.Range(0.8f, 1f);
                    aSrc.minDistance = 5f;
                    aSrc.loop = false;
                    aSrc.Play();

                    // Reset delay timer
                    timer_01 = 0f;
                }
            }
        }

        // Play vulcan hit audio at specific position
        public void VulcanHit(Vector3 pos)
        {
            if (timer_02 >= vulcanHitDelay)
            {
                // Spawn random hit audio prefab from pool for specific weapon type
                AudioSource aSrc =
                    F3DPoolManager.Pools["GeneratedPool"].SpawnAudio(audioSource,
                        vulcanHit[Random.Range(0, vulcanHit.Length)], pos, null).gameObject.GetComponent<AudioSource>();
                if (aSrc != null)
                {
                    aSrc.pitch = Random.Range(0.95f, 1f);
                    aSrc.volume = Random.Range(0.6f, 1f);
                    aSrc.minDistance = 7f;
                    aSrc.loop = false;
                    aSrc.Play();

                    timer_02 = 0f;
                }
            }
        }

        // Play solo gun shot audio at specific position
        public void SoloGunShot(Vector3 pos)
        {
            if (timer_01 >= soloGunDelay)
            {
                AudioSource aSrc =
                    F3DPoolManager.Pools["GeneratedPool"].SpawnAudio(audioSource, soloGunShot, pos, null)
                        .gameObject.GetComponent<AudioSource>();

                if (aSrc != null)
                {
                    aSrc.pitch = Random.Range(0.95f, 1f);
                    aSrc.volume = Random.Range(0.8f, 1f);
                    aSrc.minDistance = 30f;
                    aSrc.loop = false;

                    aSrc.Play();

                    timer_01 = 0f;
                }
            }
        }

        // Play solo gun hit audio at specific position
        public void SoloGunHit(Vector3 pos)
        {
            if (timer_02 >= soloGunHitDelay)
            {
                AudioSource aSrc =
                    F3DPoolManager.Pools["GeneratedPool"].SpawnAudio(audioSource,
                        soloGunHit[Random.Range(0, soloGunHit.Length)], pos, null)
                        .gameObject.GetComponent<AudioSource>();
                if (aSrc != null)
                {
                    aSrc.pitch = Random.Range(0.95f, 1f);
                    aSrc.volume = Random.Range(0.8f, 1f);
                    aSrc.minDistance = 50f;
                    aSrc.loop = false;
                    aSrc.Play();

                    timer_02 = 0f;
                }
            }
        }

        // Play sniper shot audio at specific position
        public void SniperShot(Vector3 pos)
        {
            if (timer_01 >= sniperDelay)
            {
                AudioSource aSrc =
                    F3DPoolManager.Pools["GeneratedPool"].SpawnAudio(audioSource, sniperShot, pos, null)
                        .gameObject.GetComponent<AudioSource>();

                if (aSrc != null)
                {
                    aSrc.pitch = Random.Range(0.9f, 1f);
                    aSrc.volume = Random.Range(0.8f, 1f);
                    aSrc.minDistance = 6f;
                    aSrc.loop = false;

                    aSrc.Play();

                    timer_01 = 0f;
                }
            }
        }

        // Play sniper hit audio at specific position
        public void SniperHit(Vector3 pos)
        {
            if (timer_02 >= sniperHitDelay)
            {
                AudioSource aSrc =
                    F3DPoolManager.Pools["GeneratedPool"].SpawnAudio(audioSource,
                        sniperHit[Random.Range(0, sniperHit.Length)], pos, null).gameObject.GetComponent<AudioSource>();
                if (aSrc != null)
                {
                    aSrc.pitch = Random.Range(0.9f, 1f);
                    aSrc.volume = Random.Range(0.8f, 1f);
                    aSrc.minDistance = 8f;
                    aSrc.loop = false;
                    aSrc.Play();

                    timer_02 = 0f;
                }
            }
        }

        // Play shotgun shot audio at specific position
        public void ShotGunShot(Vector3 pos)
        {
            if (timer_01 >= shotGunDelay)
            {
                AudioSource aSrc =
                    F3DPoolManager.Pools["GeneratedPool"].SpawnAudio(audioSource, shotGunShot, pos, null)
                        .gameObject.GetComponent<AudioSource>();

                if (aSrc != null)
                {
                    aSrc.pitch = Random.Range(0.9f, 1f);
                    aSrc.volume = Random.Range(0.8f, 1f);
                    aSrc.minDistance = 8f;
                    aSrc.loop = false;

                    aSrc.Play();

                    timer_01 = 0f;
                }
            }
        }

        // Play shotgun hit audio at specific position
        public void ShotGunHit(Vector3 pos)
        {
            if (timer_02 >= shotGunHitDelay)
            {
                AudioSource aSrc =
                    F3DPoolManager.Pools["GeneratedPool"].SpawnAudio(audioSource,
                        shotGunHit[Random.Range(0, shotGunHit.Length)], pos, null)
                        .gameObject.GetComponent<AudioSource>();
                if (aSrc != null)
                {
                    aSrc.pitch = Random.Range(0.9f, 1f);
                    aSrc.volume = Random.Range(0.8f, 1f);
                    aSrc.minDistance = 7f;
                    aSrc.loop = false;
                    aSrc.Play();

                    timer_02 = 0f;
                }
            }
        }

        // Play seeker shot audio at specific position
        public void SeekerShot(Vector3 pos)
        {
            if (timer_01 >= seekerDelay)
            {
                AudioSource aSrc =
                    F3DPoolManager.Pools["GeneratedPool"].SpawnAudio(audioSource, seekerShot, pos, null)
                        .gameObject.GetComponent<AudioSource>();

                if (aSrc != null)
                {
                    aSrc.pitch = Random.Range(0.8f, 1f);
                    aSrc.volume = Random.Range(0.8f, 1f);
                    aSrc.minDistance = 8f;
                    aSrc.loop = false;

                    aSrc.Play();

                    timer_01 = 0f;
                }
            }
        }

        // Play seeker hit audio at specific position
        public void SeekerHit(Vector3 pos)
        {
            if (timer_02 >= seekerHitDelay)
            {
                AudioSource aSrc =
                    F3DPoolManager.Pools["GeneratedPool"].SpawnAudio(audioSource,
                        seekerHit[Random.Range(0, seekerHit.Length)], pos, null).gameObject.GetComponent<AudioSource>();
                if (aSrc != null)
                {
                    aSrc.pitch = Random.Range(0.8f, 1f);
                    aSrc.volume = Random.Range(0.8f, 1f);
                    aSrc.minDistance = 25f;
                    aSrc.loop = false;
                    aSrc.Play();

                    timer_02 = 0f;
                }
            }
        }

        // Play railgun shot audio at specific position
        public void RailGunShot(Vector3 pos)
        {
            if (timer_01 >= railgunDelay)
            {
                AudioSource aSrc =
                    F3DPoolManager.Pools["GeneratedPool"].SpawnAudio(audioSource, railgunShot, pos, null)
                        .gameObject.GetComponent<AudioSource>();

                if (aSrc != null)
                {
                    aSrc.pitch = Random.Range(0.8f, 1f);
                    aSrc.volume = Random.Range(0.8f, 1f);
                    aSrc.minDistance = 4f;
                    aSrc.loop = false;

                    aSrc.Play();

                    timer_01 = 0f;
                }
            }
        }

        // Play railgun hit audio at specific position
        public void RailGunHit(Vector3 pos)
        {
            if (timer_02 >= railgunHitDelay)
            {
                AudioSource aSrc =
                    F3DPoolManager.Pools["GeneratedPool"].SpawnAudio(audioSource,
                        railgunHit[Random.Range(0, railgunHit.Length)], pos, null)
                        .gameObject.GetComponent<AudioSource>();
                if (aSrc != null)
                {
                    aSrc.pitch = Random.Range(0.8f, 1f);
                    aSrc.volume = Random.Range(0.8f, 1f);
                    aSrc.minDistance = 20f;
                    aSrc.loop = false;
                    aSrc.Play();

                    timer_02 = 0f;
                }
            }
        }

        // Play plasma gun shot audio at specific position
        public void PlasmaGunShot(Vector3 pos)
        {
            if (timer_01 >= plasmagunDelay)
            {
                AudioSource aSrc =
                    F3DPoolManager.Pools["GeneratedPool"].SpawnAudio(audioSource, plasmagunShot, pos, null)
                        .gameObject.GetComponent<AudioSource>();

                if (aSrc != null)
                {
                    aSrc.pitch = Random.Range(0.8f, 1f);
                    aSrc.volume = Random.Range(0.8f, 1f);
                    aSrc.minDistance = 4f;
                    aSrc.loop = false;

                    aSrc.Play();

                    timer_01 = 0f;
                }
            }
        }

        // Play plasma gun hit audio at specific position
        public void PlasmaGunHit(Vector3 pos)
        {
            if (timer_02 >= plasmagunHitDelay)
            {
                AudioSource aSrc =
                    F3DPoolManager.Pools["GeneratedPool"].SpawnAudio(audioSource,
                        plasmagunHit[Random.Range(0, plasmagunHit.Length)], pos, null)
                        .gameObject.GetComponent<AudioSource>();

                if (aSrc != null)
                {
                    aSrc.pitch = Random.Range(0.8f, 1f);
                    aSrc.volume = Random.Range(0.8f, 1f);
                    aSrc.minDistance = 50f;
                    aSrc.loop = false;
                    aSrc.Play();

                    timer_02 = 0f;
                }
            }
        }

        // Play plasma beam shot and loop audio at specific position
        public void PlasmaBeamLoop(Vector3 pos, Transform loopParent)
        {
            AudioSource aOpen =
                F3DPoolManager.Pools["GeneratedPool"].SpawnAudio(audioSource, plasmabeamOpen, pos, null)
                    .gameObject.GetComponent<AudioSource>();
            AudioSource aLoop =
                F3DPoolManager.Pools["GeneratedPool"].SpawnAudio(audioSource, plasmabeamLoop, pos, loopParent)
                    .gameObject.GetComponent<AudioSource>();


            if (aOpen != null && aLoop != null)
            {
                aOpen.pitch = Random.Range(0.8f, 1f);
                aOpen.volume = Random.Range(0.8f, 1f);
                aOpen.minDistance = 50f;
                aOpen.loop = false;
                aOpen.Play();

                aLoop.pitch = Random.Range(0.95f, 1f);
                aLoop.volume = Random.Range(0.95f, 1f);
                aLoop.loop = true;
                aLoop.minDistance = 50f;
                aLoop.Play();
            }
        }

        // Play plasma beam closing audio at specific position
        public void PlasmaBeamClose(Vector3 pos)
        {
            AudioSource aClose =
                F3DPoolManager.Pools["GeneratedPool"].SpawnAudio(audioSource, plasmabeamClose, pos, null)
                    .gameObject.GetComponent<AudioSource>();

            if (aClose != null)
            {
                aClose.pitch = Random.Range(0.8f, 1f);
                aClose.volume = Random.Range(0.8f, 1f);
                aClose.minDistance = 50f;
                aClose.loop = false;
                aClose.Play();
            }
        }

        // Play heavy plasma beam shot and loop audio at specific position
        public void PlasmaBeamHeavyLoop(Vector3 pos, Transform loopParent)
        {
            AudioSource aOpen =
                F3DPoolManager.Pools["GeneratedPool"].SpawnAudio(audioSource, plasmabeamHeavyOpen, pos, null)
                    .gameObject.GetComponent<AudioSource>();
            AudioSource aLoop =
                F3DPoolManager.Pools["GeneratedPool"].SpawnAudio(audioSource, plasmabeamHeavyLoop, pos, loopParent)
                    .gameObject.GetComponent<AudioSource>();


            if (aOpen != null && aLoop != null)
            {
                aOpen.pitch = Random.Range(0.8f, 1f);
                aOpen.volume = Random.Range(0.8f, 1f);
                aOpen.minDistance = 50f;
                aOpen.loop = false;
                aOpen.Play();

                aLoop.pitch = Random.Range(0.95f, 1f);
                aLoop.volume = Random.Range(0.95f, 1f);
                aLoop.loop = true;
                aLoop.minDistance = 50f;

                aLoop.Play();
            }
        }

        // Play heavy plasma beam closing audio at specific position
        public void PlasmaBeamHeavyClose(Vector3 pos)
        {
            AudioSource aClose =
                F3DPoolManager.Pools["GeneratedPool"].SpawnAudio(audioSource, plasmabeamHeavyClose, pos, null)
                    .gameObject.GetComponent<AudioSource>();

            if (aClose != null)
            {
                aClose.pitch = Random.Range(0.8f, 1f);
                aClose.volume = Random.Range(0.8f, 1f);
                aClose.minDistance = 50f;
                aClose.loop = false;
                aClose.Play();
            }
        }

        // Play lightning gun shot and loop audio at specific position
        public void LightningGunLoop(Vector3 pos, Transform loopParent)
        {
            AudioSource aOpen =
                F3DPoolManager.Pools["GeneratedPool"].SpawnAudio(audioSource, lightningGunOpen, pos, null)
                    .gameObject.GetComponent<AudioSource>();
            AudioSource aLoop =
                F3DPoolManager.Pools["GeneratedPool"].SpawnAudio(audioSource, lightningGunLoop, pos, loopParent.parent)
                    .gameObject.GetComponent<AudioSource>();


            if (aOpen != null && aLoop != null)
            {
                aOpen.pitch = Random.Range(0.8f, 1f);
                aOpen.volume = Random.Range(0.8f, 1f);
                aOpen.minDistance = 50f;
                aOpen.loop = false;
                aOpen.Play();

                aLoop.pitch = Random.Range(0.95f, 1f);
                aLoop.volume = Random.Range(0.95f, 1f);
                aLoop.loop = true;
                aLoop.minDistance = 50f;

                aLoop.Play();
            }
        }

        // Play lightning gun closing audio at specific position
        public void LightningGunClose(Vector3 pos)
        {
            AudioSource aClose =
                F3DPoolManager.Pools["GeneratedPool"].SpawnAudio(audioSource, lightningGunClose, pos, null)
                    .gameObject.GetComponent<AudioSource>();

            if (aClose != null)
            {
                aClose.pitch = Random.Range(0.8f, 1f);
                aClose.volume = Random.Range(0.8f, 1f);
                aClose.minDistance = 50f;
                aClose.loop = false;
                aClose.Play();
            }
        }

        // Play flame shot and loop audio at specific position
        public void FlameGunLoop(Vector3 pos, Transform loopParent)
        {
            AudioSource aOpen =
                F3DPoolManager.Pools["GeneratedPool"].SpawnAudio(audioSource, flameGunOpen, pos, null)
                    .gameObject.GetComponent<AudioSource>();
            AudioSource aLoop =
                F3DPoolManager.Pools["GeneratedPool"].SpawnAudio(audioSource, flameGunLoop, pos, loopParent.parent)
                    .gameObject.GetComponent<AudioSource>();


            if (aOpen != null && aLoop != null)
            {
                aOpen.pitch = Random.Range(0.8f, 1f);
                aOpen.volume = Random.Range(0.8f, 1f);
                aOpen.minDistance = 50f;
                aOpen.loop = false;
                aOpen.Play();

                aLoop.pitch = Random.Range(0.95f, 1f);
                aLoop.volume = Random.Range(0.95f, 1f);
                aLoop.loop = true;
                aLoop.minDistance = 50f;

                aLoop.Play();
            }
        }

        // Play flame closing audio at specific position
        public void FlameGunClose(Vector3 pos)
        {
            AudioSource aClose =
                F3DPoolManager.Pools["GeneratedPool"].SpawnAudio(audioSource, flameGunClose, pos, null)
                    .gameObject.GetComponent<AudioSource>();

            if (aClose != null)
            {
                aClose.pitch = Random.Range(0.8f, 1f);
                aClose.volume = Random.Range(0.8f, 1f);
                aClose.minDistance = 50f;
                aClose.loop = false;
                aClose.Play();
            }
        }

        // Play laser pulse shot audio at specific position
        public void LaserImpulseShot(Vector3 pos)
        {
            if (timer_01 >= laserImpulseDelay)
            {
                AudioSource aSrc =
                    F3DPoolManager.Pools["GeneratedPool"].SpawnAudio(audioSource, laserImpulseShot, pos, null)
                        .gameObject.GetComponent<AudioSource>();

                if (aSrc != null)
                {
                    aSrc.pitch = Random.Range(0.9f, 1f);
                    aSrc.volume = Random.Range(0.8f, 1f);
                    aSrc.minDistance = 20f;
                    aSrc.loop = false;
                    aSrc.Play();

                    timer_01 = 0f;
                }
            }
        }

        // Play laser pulse hit audio at specific position
        public void LaserImpulseHit(Vector3 pos)
        {
            if (timer_02 >= laserImpulseHitDelay)
            {
                AudioSource aSrc =
                    F3DPoolManager.Pools["GeneratedPool"].SpawnAudio(audioSource,
                        laserImpulseHit[Random.Range(0, plasmagunHit.Length)], pos, null)
                        .gameObject.GetComponent<AudioSource>();

                if (aSrc != null)
                {
                    aSrc.pitch = Random.Range(0.8f, 1f);
                    aSrc.volume = Random.Range(0.8f, 1f);
                    aSrc.minDistance = 20f;
                    aSrc.loop = false;
                    aSrc.Play();

                    timer_02 = 0f;
                }
            }
        }
    }
}