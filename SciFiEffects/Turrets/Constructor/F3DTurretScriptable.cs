using UnityEngine;
using System.Collections.Generic;
using System;

namespace Forge3D
{
    [Serializable]
    public class F3DTurretScriptable : ScriptableObject
    {
        public List<TurretStructure> Turrets = new List<TurretStructure>();
        public List<GameObject> Bases = new List<GameObject>();
        public List<GameObject> Swivels = new List<GameObject>();
        public string SwivelPrefix = "*SOCKET_SWIVEL_";
        public List<GameObject> Heads = new List<GameObject>();
        public string HeadPrefix = "*SOCKET_HEAD_";
        public List<GameObject> Mounts = new List<GameObject>();
        public string MountPrefix = "*SOCKET_MOUNT_";
        public List<GameObject> Breeches = new List<GameObject>();
        public string BarrelPrefix = "*SOCKET_BARREL_";
        public List<GameObject> Barrels = new List<GameObject>();
        public int SelectedTurret = 0;
        public string WeaponSocket = "*SOCKET_WEAPON_"; 
    }

    [Serializable]
    public class TurretStructure
    {
        public bool NeedLOD = false;
        public string Name = "Turret";
        public GameObject Base;
        public GameObject Swivel;
        public string SwivelPrefix = "*SOCKET_SWIVEL_";
        public GameObject Head;
        public string HeadPrefix = "*SOCKET_HEAD_";
        public GameObject Mount;
        public string MountPrefix = "*SOCKET_MOUNT_";
       
        public GameObject Breech;
        public string BarrelPrefix = "*SOCKET_";
      
        public List<string> WeaponSlotsNames = new List<string>();
        public List<GameObject> WeaponSlots = new List<GameObject>();
        public List<GameObject> WeaponBreeches = new List<GameObject>();
        public List<string> WeaponBarrelSockets = new List<string>();
        public List<GameObject> WeaponBarrels = new List<GameObject>();
        public bool HasTurretScript;
    }
}