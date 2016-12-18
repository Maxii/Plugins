using UnityEngine;
using System.Collections;
using System;

namespace Forge3D
{
// Weapon types
    public enum F3DFXType
    {
        Vulcan,
        SoloGun,
        Sniper,
        ShotGun,
        Seeker,
        RailGun,
        PlasmaGun,
        PlasmaBeam,
        PlasmaBeamHeavy,
        LightningGun,
        FlameRed,
        LaserImpulse
    }

    public class F3DFXController : MonoBehaviour
    {
        // Singleton instance
        public static F3DFXController instance;

        // GUI captions
        string[] fxTypeName =
        {
            "Vulcan", "Sologun", "Sniper", "Shotgun", "Seeker", "Railgun", "Plasmagun", "Plasma beam",
            "Heavy plasma beam", "Lightning gun", "Flamethrower", "Pulse laser"
        };

        // Current firing socket
        int curSocket=0;
        // Timer reference                
        int timerID = -1;

        [Header("Turret setup")] public Transform[] TurretSocket; // Sockets reference
        public ParticleSystem[] ShellParticles; // Bullet shells particle system

        public F3DFXType DefaultFXType; // Default starting weapon type

        [Header("Vulcan")] public Transform vulcanProjectile; // Projectile prefab
        public Transform vulcanMuzzle; // Muzzle flash prefab  
        public Transform vulcanImpact; // Impact prefab
        public float vulcanOffset;

        [Header("Solo gun")] public Transform soloGunProjectile;
        public Transform soloGunMuzzle;
        public Transform soloGunImpact;
        public float soloGunOffset;

        [Header("Sniper")] public Transform sniperBeam;
        public Transform sniperMuzzle;
        public Transform sniperImpact;
        public float sniperOffset;

        [Header("Shotgun")] public Transform shotGunProjectile;
        public Transform shotGunMuzzle;
        public Transform shotGunImpact;
        public float shotGunOffset;

        [Header("Seeker")] public Transform seekerProjectile;
        public Transform seekerMuzzle;
        public Transform seekerImpact;
        public float seekerOffset;

        [Header("Rail gun")] public Transform railgunBeam;
        public Transform railgunMuzzle;
        public Transform railgunImpact;
        public float railgunOffset;

        [Header("Plasma gun")] public Transform plasmagunProjectile;
        public Transform plasmagunMuzzle;
        public Transform plasmagunImpact;
        public float plasmaGunOffset;

        [Header("Plasma beam")] public Transform plasmaBeam;
        public float plasmaOffset;

        [Header("Plasma beam heavy")] public Transform plasmaBeamHeavy;
        public float plasmaBeamHeavyOffset;

        [Header("Lightning gun")] public Transform lightningGunBeam;
        public float lightingGunBeamOffset;

        [Header("Flame")] public Transform flameRed;
        public float flameOffset;

        [Header("Laser impulse")] public Transform laserImpulseProjectile;
        public Transform laserImpulseMuzzle;
        public Transform laserImpulseImpact;
        public float laserImpulseOffset;

        void Awake()
        {
            // Initialize singleton  
            instance = this;

        /*    // Initialize bullet shells particles
            for (int i = 0; i < ShellParticles.Length; i++)
            {
                var em = ShellParticles[i].emission;
                em.enabled = false;
                ShellParticles[i].gameObject.SetActive(true);
            }*/
        }

        // Display GUI
        void OnGUI()
        {
            GUIStyle caption = new GUIStyle(GUI.skin.label);
            caption.fontSize = 25;
            caption.fontStyle = FontStyle.Bold;
            caption.wordWrap = false;

            GUIStyle tooltip = new GUIStyle(GUI.skin.label);
            tooltip.fontSize = 11;
            tooltip.wordWrap = false;

            GUILayout.BeginArea(new Rect(Screen.width/2 - 150, Screen.height - 150, 300, 120));

            GUILayout.BeginVertical();

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(fxTypeName[(int) DefaultFXType], caption);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Press Left / Right arrow keys to switch", tooltip);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Previous", GUILayout.Width(90), GUILayout.Height(30)))
                PrevWeapon();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Next", GUILayout.Width(90), GUILayout.Height(30)))
                NextWeapon();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();

            GUILayout.EndVertical();

            GUILayout.EndArea();
        }

        void Update()
        {
            // Switch weapon types using keyboard keys
            if (Input.GetKeyDown(KeyCode.RightArrow))
                NextWeapon();
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
                PrevWeapon();
        }

        // Switch to next weapon type
        void NextWeapon()
        {
            if ((int) DefaultFXType < Enum.GetNames(typeof (F3DFXType)).Length - 1)
            {
                DefaultFXType++;
            }
        }

        // Switch to previous weapon type
        void PrevWeapon()
        {
            if (DefaultFXType > 0)
            {
                DefaultFXType--;
            }
        }

        // Advance to next turret socket
        void AdvanceSocket()
        {
            curSocket++;
            if (curSocket >= TurretSocket.Length)
                curSocket = 0;
        }

        // Fire turret weapon
        public void Fire()
        {
            switch (DefaultFXType)
            {
                case F3DFXType.Vulcan:
                    // Fire vulcan at specified rate until canceled
                    timerID = F3DTime.time.AddTimer(0.05f, Vulcan);
                    // Invoke manually before the timer ticked to avoid initial delay
                    Vulcan();
                    break;

                case F3DFXType.SoloGun:
                    timerID = F3DTime.time.AddTimer(0.2f, SoloGun);
                    SoloGun();
                    break;

                case F3DFXType.Sniper:
                    timerID = F3DTime.time.AddTimer(0.3f, Sniper);
                    Sniper();
                    break;

                case F3DFXType.ShotGun:
                    timerID = F3DTime.time.AddTimer(0.3f, ShotGun);
                    ShotGun();
                    break;

                case F3DFXType.Seeker:
                    timerID = F3DTime.time.AddTimer(0.2f, Seeker);
                    Seeker();
                    break;

                case F3DFXType.RailGun:
                    timerID = F3DTime.time.AddTimer(0.2f, RailGun);
                    RailGun();
                    break;

                case F3DFXType.PlasmaGun:
                    timerID = F3DTime.time.AddTimer(0.2f, PlasmaGun);
                    PlasmaGun();
                    break;

                case F3DFXType.PlasmaBeam:
                    // Beams has no timer requirement
                    PlasmaBeam();
                    break;

                case F3DFXType.PlasmaBeamHeavy:
                    // Beams has no timer requirement
                    PlasmaBeamHeavy();
                    break;

                case F3DFXType.LightningGun:
                    // Beams has no timer requirement
                    LightningGun();
                    break;

                case F3DFXType.FlameRed:
                    // Flames has no timer requirement
                    FlameRed();
                    break;

                case F3DFXType.LaserImpulse:
                    timerID = F3DTime.time.AddTimer(0.15f, LaserImpulse);
                    LaserImpulse();
                    break;
            }
        }

        // Stop firing 
        public void Stop()
        {
            // Remove firing timer
            if (timerID != -1)
            {
                F3DTime.time.RemoveTimer(timerID);
                timerID = -1;
            }
            switch (DefaultFXType)
            {
                case F3DFXType.PlasmaBeam:
                    F3DAudioController.instance.PlasmaBeamClose(transform.position);
                    break;

                case F3DFXType.PlasmaBeamHeavy:
                    F3DAudioController.instance.PlasmaBeamHeavyClose(transform.position);
                    break;

                case F3DFXType.LightningGun:
                    F3DAudioController.instance.LightningGunClose(transform.position);
                    break;

                case F3DFXType.FlameRed:
                    F3DAudioController.instance.FlameGunClose(transform.position);
                    break;
            }
        }

        // Fire vulcan weapon
        void Vulcan()
        {
            // Get random rotation that offset spawned projectile
            Quaternion offset = Quaternion.Euler(UnityEngine.Random.onUnitSphere); 
            // Spawn muzzle flash and projectile with the rotation offset at current socket position
            F3DPoolManager.Pools["GeneratedPool"].Spawn(vulcanMuzzle, TurretSocket[curSocket].position,
                TurretSocket[curSocket].rotation, TurretSocket[curSocket]);
            GameObject newGO =
                F3DPoolManager.Pools["GeneratedPool"].Spawn(vulcanProjectile,
                    TurretSocket[curSocket].position + TurretSocket[curSocket].forward,
                    offset*TurretSocket[curSocket].rotation, null).gameObject;

            F3DProjectile proj = newGO.gameObject.GetComponent<F3DProjectile>();
            if (proj)
            {
                proj.SetOffset(vulcanOffset);
            }

            // Emit one bullet shell
            if(ShellParticles.Length>0)
                ShellParticles[curSocket].Emit(1);

            // Play shot sound effect
            F3DAudioController.instance.VulcanShot(TurretSocket[curSocket].position);

            // Advance to next turret socket
            AdvanceSocket();
        }

        // Spawn vulcan weapon impact
        public void VulcanImpact(Vector3 pos)
        {
            // Spawn impact prefab at specified position
            F3DPoolManager.Pools["GeneratedPool"].Spawn(vulcanImpact, pos, Quaternion.identity, null);
            // Play impact sound effect
            F3DAudioController.instance.VulcanHit(pos);
        }

        // Fire sologun weapon
        void SoloGun()
        {
            Quaternion offset = Quaternion.Euler(UnityEngine.Random.onUnitSphere);
            F3DPoolManager.Pools["GeneratedPool"].Spawn(soloGunMuzzle, TurretSocket[curSocket].position,
                TurretSocket[curSocket].rotation, TurretSocket[curSocket]);
            GameObject newGO =
                F3DPoolManager.Pools["GeneratedPool"].Spawn(soloGunProjectile,
                    TurretSocket[curSocket].position + TurretSocket[curSocket].forward,
                    offset*TurretSocket[curSocket].rotation, null).gameObject as GameObject;
            F3DProjectile proj = newGO.GetComponent<F3DProjectile>();
            if (proj)
            {
                proj.SetOffset(soloGunOffset);
            }
            F3DAudioController.instance.SoloGunShot(TurretSocket[curSocket].position);
            AdvanceSocket();
        }

        // Spawn sologun weapon impact
        public void SoloGunImpact(Vector3 pos)
        {
            F3DPoolManager.Pools["GeneratedPool"].Spawn(soloGunImpact, pos, Quaternion.identity, null);
            F3DAudioController.instance.SoloGunHit(pos);
        }

        // Fire sniper weapon
        void Sniper()
        {
            Quaternion offset = Quaternion.Euler(UnityEngine.Random.onUnitSphere);

            F3DPoolManager.Pools["GeneratedPool"].Spawn(sniperMuzzle, TurretSocket[curSocket].position,
                TurretSocket[curSocket].rotation, TurretSocket[curSocket]);
            GameObject newGO =
                F3DPoolManager.Pools["GeneratedPool"].Spawn(sniperBeam, TurretSocket[curSocket].position,
                    offset*TurretSocket[curSocket].rotation, null).gameObject as GameObject;
            F3DBeam beam = newGO.GetComponent<F3DBeam>();
            if (beam)
            {
                beam.SetOffset(sniperOffset);
            }
            F3DAudioController.instance.SniperShot(TurretSocket[curSocket].position);
            if (ShellParticles.Length > 0)
                ShellParticles[curSocket].Emit(1);
            AdvanceSocket();
        }

        // Spawn sniper weapon impact
        public void SniperImpact(Vector3 pos)
        {
            F3DPoolManager.Pools["GeneratedPool"].Spawn(sniperImpact, pos, Quaternion.identity, null);
            F3DAudioController.instance.SniperHit(pos);
        }

        // Fire shotgun weapon
        void ShotGun()
        {
            Quaternion offset = Quaternion.Euler(UnityEngine.Random.onUnitSphere);
            F3DPoolManager.Pools["GeneratedPool"].Spawn(shotGunMuzzle, TurretSocket[curSocket].position,
                TurretSocket[curSocket].rotation, TurretSocket[curSocket]);
            F3DPoolManager.Pools["GeneratedPool"].Spawn(shotGunProjectile, TurretSocket[curSocket].position,
                offset*TurretSocket[curSocket].rotation, null);
            F3DAudioController.instance.ShotGunShot(TurretSocket[curSocket].position);
            if (ShellParticles.Length > 0)
                ShellParticles[curSocket].Emit(1);
            AdvanceSocket();
        }

        // Fire seeker weapon
        void Seeker()
        {
            Quaternion offset = Quaternion.Euler(UnityEngine.Random.onUnitSphere);
            F3DPoolManager.Pools["GeneratedPool"].Spawn(seekerMuzzle, TurretSocket[curSocket].position,
                TurretSocket[curSocket].rotation, TurretSocket[curSocket]);
            GameObject newGO =
                F3DPoolManager.Pools["GeneratedPool"].Spawn(seekerProjectile, TurretSocket[curSocket].position,
                    offset*TurretSocket[curSocket].rotation, null).gameObject as GameObject;
            F3DProjectile proj = newGO.GetComponent<F3DProjectile>();
            if (proj)
            {
                proj.SetOffset(seekerOffset);
            }
            F3DAudioController.instance.SeekerShot(TurretSocket[curSocket].position);
            AdvanceSocket();
        }

        // Spawn seeker weapon impact
        public void SeekerImpact(Vector3 pos)
        {
            F3DPoolManager.Pools["GeneratedPool"].Spawn(seekerImpact, pos, Quaternion.identity, null);
            F3DAudioController.instance.SeekerHit(pos);
        }

        // Fire rail gun weapon
        void RailGun()
        {
            Quaternion offset = Quaternion.Euler(UnityEngine.Random.onUnitSphere);
            F3DPoolManager.Pools["GeneratedPool"].Spawn(railgunMuzzle, TurretSocket[curSocket].position,
                TurretSocket[curSocket].rotation, TurretSocket[curSocket]);
            GameObject newGO =
                F3DPoolManager.Pools["GeneratedPool"].Spawn(railgunBeam, TurretSocket[curSocket].position,
                    offset*TurretSocket[curSocket].rotation, null).gameObject as GameObject;
            F3DBeam beam = newGO.GetComponent<F3DBeam>();
            if (beam)
            {
                beam.SetOffset(railgunOffset);
            }
            F3DAudioController.instance.RailGunShot(TurretSocket[curSocket].position);
            if (ShellParticles.Length > 0)
                ShellParticles[curSocket].Emit(1);
            AdvanceSocket();
        }

        // Spawn rail gun weapon impact
        public void RailgunImpact(Vector3 pos)
        {
            F3DPoolManager.Pools["GeneratedPool"].Spawn(railgunImpact, pos, Quaternion.identity, null);
            F3DAudioController.instance.RailGunHit(pos);
        }

        // Fire plasma gun weapon
        void PlasmaGun()
        {
            Quaternion offset = Quaternion.Euler(UnityEngine.Random.onUnitSphere);
            F3DPoolManager.Pools["GeneratedPool"].Spawn(plasmagunMuzzle, TurretSocket[curSocket].position,
                TurretSocket[curSocket].rotation, TurretSocket[curSocket]);
            GameObject newGO =
                F3DPoolManager.Pools["GeneratedPool"].Spawn(plasmagunProjectile, TurretSocket[curSocket].position,
                    offset*TurretSocket[curSocket].rotation, null).gameObject as GameObject;
            F3DProjectile proj = newGO.GetComponent<F3DProjectile>();
            if (proj)
            {
                proj.SetOffset(plasmaOffset);
            }
            F3DAudioController.instance.PlasmaGunShot(TurretSocket[curSocket].position);
            AdvanceSocket();
        }

        // Spawn plasma gun weapon impact
        public void PlasmaGunImpact(Vector3 pos)
        {
            F3DPoolManager.Pools["GeneratedPool"].Spawn(plasmagunImpact, pos, Quaternion.identity, null);
            F3DAudioController.instance.PlasmaGunHit(pos);
        }

        // Fire plasma beam weapon
        void PlasmaBeam()
        {
            for (int i = 0; i < TurretSocket.Length; i++)
            {
                F3DPoolManager.Pools["GeneratedPool"].Spawn(plasmaBeam, TurretSocket[i].position, TurretSocket[i].rotation,
                    TurretSocket[i]);   
            } 
            F3DAudioController.instance.PlasmaBeamLoop(transform.position, transform.parent);
        }

        // Fire heavy beam weapon
        void PlasmaBeamHeavy()
        {
            for (int i = 0; i < TurretSocket.Length; i++)
            {
                F3DPoolManager.Pools["GeneratedPool"].Spawn(plasmaBeamHeavy, TurretSocket[i].position, TurretSocket[i].rotation,
                    TurretSocket[i]);
            }  
            F3DAudioController.instance.PlasmaBeamHeavyLoop(transform.position, transform.parent);
        }

        // Fire lightning gun weapon
        void LightningGun()
        {
            for (int i = 0; i < TurretSocket.Length; i++)
            {
                F3DPoolManager.Pools["GeneratedPool"].Spawn(lightningGunBeam, TurretSocket[i].position, TurretSocket[i].rotation,
                    TurretSocket[i]);
            }   
            F3DAudioController.instance.LightningGunLoop(transform.position, transform);
        }

        // Fire flames weapon
        void FlameRed()
        {
            for (int i = 0; i < TurretSocket.Length; i++)
            {
                F3DPoolManager.Pools["GeneratedPool"].Spawn(flameRed, TurretSocket[i].position, TurretSocket[i].rotation,
                    TurretSocket[i]);
            }   
            F3DAudioController.instance.FlameGunLoop(transform.position, transform);
        }

        // Fire laser pulse weapon
        void LaserImpulse()
        {
            Quaternion offset = Quaternion.Euler(UnityEngine.Random.onUnitSphere);
            F3DPoolManager.Pools["GeneratedPool"].Spawn(laserImpulseMuzzle, TurretSocket[curSocket].position,
                TurretSocket[curSocket].rotation, TurretSocket[curSocket]);
            GameObject newGO =
                F3DPoolManager.Pools["GeneratedPool"].Spawn(laserImpulseProjectile, TurretSocket[curSocket].position,
                    offset*TurretSocket[curSocket].rotation, null).gameObject as GameObject;
            F3DProjectile proj = newGO.GetComponent<F3DProjectile>();
            if (proj)
            {
                proj.SetOffset(laserImpulseOffset);
            }
            F3DAudioController.instance.LaserImpulseShot(TurretSocket[curSocket].position);

            AdvanceSocket();
        }

        // Spawn laser pulse weapon impact
        public void LaserImpulseImpact(Vector3 pos)
        {
            F3DPoolManager.Pools["GeneratedPool"].Spawn(laserImpulseImpact, pos, Quaternion.identity, null);
            F3DAudioController.instance.LaserImpulseHit(pos);
        }
    }
}