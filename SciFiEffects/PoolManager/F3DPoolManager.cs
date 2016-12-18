using UnityEngine; 
using System.Collections.Generic;
using System;

namespace Forge3D
{
    [Serializable]
    public class F3DPoolManager : MonoBehaviour
    { 
        public static F3DPoolManager instance;                                              // instance of current manager 
        public static Dictionary<string, F3DPool> Pools = new Dictionary<string, F3DPool>();

        public string databaseName = "";                                                    // Name of database. Need for editor 
        public int selectedItem = 0;                                                        // Editor's parameter 
        public bool[] haveToShowArr;                                                        // Editor's parameter               

        List<F3DPool> curPools = new List<F3DPool>();                                       // Our pools 

        void Awake()
        {
            InstallManager();
            instance = this;
        }

        //Retirning pool by it's name
        public F3DPool GetPool(string valueName)
        {
            int i;
            if (valueName != "" && curPools != null && curPools.Count > 0)
            {
                for (i = 0; i < curPools.Count; i++)
                {
                    if (curPools[i].poolName == valueName)
                    {
                        return curPools[i];
                    }
                }
            }
            return null;
        }

        //Installing of manager
        void InstallManager()
        {
            curPools.Clear();
            List<F3DPoolContainer> pools = new List<F3DPoolContainer>();
            Pools.Clear();
            Pools = new Dictionary<string, F3DPool>();
            F3DPoolManagerDB myManager = Resources.Load("F3DPoolManagerCache/" + databaseName) as F3DPoolManagerDB;
            if (myManager != null)
            {
                if (myManager.pools != null)
                {
                    foreach (F3DPoolContainer cont in myManager.pools)
                    {
                        pools.Add(cont);
                    }
                }
            } 

            //Transfering info from containers to our real pools and creating GO's for them
            int j;
            for (j = 0; j < pools.Count; j++)
            {
                GameObject newGO = new GameObject();
                newGO.transform.parent = this.gameObject.transform;
                newGO.name = pools[j].poolName;

                F3DPool newPool = newGO.AddComponent<F3DPool>();
                newPool.poolName = newGO.name;
                newPool.SetTemplates(pools[j].templates);
                newPool.SetLength(pools[j].poolLength);
                newPool.SetLengthMax(pools[j].poolLengthMax);
                newPool.needBroadcasting = pools[j].needBroadcasting;
                newPool.broadcastSpawnName = pools[j].broadcastSpawnName;
                newPool.broadcastDespawnName = pools[j].broadcastDespawnName;
                newPool.needSort = pools[j].needSort;
                newPool.delayedSpawnInInstall = pools[j].delayedSpawnInInstall;
                newPool.objectsPerUpdate = pools[j].objectsPerUpdate;
                newPool.optimizeSpawn = pools[j].optimizeSpawn;
                newPool.targetFPS = pools[j].targetFPS;
                newPool.needSort = pools[j].needSort;
                newPool.needParenting = pools[j].needParenting;
                newPool.needDebugging = pools[j].needDebug;
                newPool.pooling = true;
                newPool.Install();

                curPools.Add(newPool);
                Pools.Add(newPool.name, newPool);

            }
        }
         
        public int GetPoolsCount()
        {
            if (curPools != null)
                return curPools.Count;
            return -1;
        }
    }
}