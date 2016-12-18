using UnityEngine;
using System;

namespace Forge3D
{
    [Serializable]
    public class F3DPoolContainer
    {
        public string poolName = "GeneratedPool";
        public Transform[] templates = new Transform[0];
        public int[] poolLength = new int[0];
        public int[] poolLengthMax = new int[0];
        public string broadcastSpawnName = "";
        public string broadcastDespawnName = "";
        public bool delayedSpawnInInstall = true;
        public int objectsPerUpdate = 5;
        public bool needBroadcasting = true;
        public bool optimizeSpawn = true;
        public int targetFPS = 50;
        public bool pooling = true;
        public bool needSort = true;
        public bool needParenting = true;
        public bool needDebug = false; 
    }
}
