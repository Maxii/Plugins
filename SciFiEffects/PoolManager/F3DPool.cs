using UnityEngine;
using System.Collections.Generic;

namespace Forge3D
{
    public class F3DPool : MonoBehaviour
    {
        [HideInInspector]
        public string poolName = "GeneratedPool";               // name of pool
        [HideInInspector]
        public Transform[] templates = new Transform[0];        // templates
        [HideInInspector]
        public Transform[] templatesParent = new Transform[0];  // parents of each type of template
        [HideInInspector]
        public int[] poolLength = new int[0];                   // base items count
        [HideInInspector]
        public int[] poolLengthCurrent = new int[0];            // Effect pool items current count 
        [HideInInspector]
        public int[] poolLengthMax = new int[0];                // max items count  
        [HideInInspector]
        public string broadcastSpawnName = "OnSpawned";         // name of onSpawned function   
        [HideInInspector]
        public string broadcastDespawnName = "OnDespawned";     // name of ondespawned function   
        [HideInInspector]
        public bool delayedSpawnInInstall = true;               // If we need to install all not in 1 update
        [HideInInspector]
        public int objectsPerUpdate = -1;                       // Current count of installing objects per frame
        [HideInInspector]
        public bool needBroadcasting = true;                    // if we need broadcasting |=> True
        [HideInInspector]
        public bool optimizeSpawn = true;                       // This is option for automated objectsPerUpdate calculation
        [HideInInspector]
        public int targetFPS = 50;                              // Target FPS for optimizeSpawn
        [HideInInspector]
        public bool pooling = true;                             // If need pooling?
        [HideInInspector]
        public bool needDebugging = false;                      // If need debugging
        [HideInInspector]
        public bool needSort = true;                            // if Need sort? It means, that after Despawn() it will be parented to it's template's parent
        [HideInInspector]
        public bool needParenting = true;                       // if need start parenting to parents

        private float normalValue = 0f;                                                                                 // Calculation of optimizeSpawn(not for editing)
        private Dictionary<Transform, List<Transform>> readyObjects = new Dictionary<Transform, List<Transform>>();     // Dict of ready for spawn items
        private Dictionary<Transform, List<Transform>> occupiedObjects = new Dictionary<Transform, List<Transform>>();  // Dict of ready for despawn items
        private bool installing = true;                                                                                 // if pool still installling = it will be true

        //return count of temlates
        public int GetTemplatesCount()
        {
            if (templates != null)
            {
                return templates.Length;
            }
            return -1;
        }

        //returns is template in this pool
        bool CheckForExistingTemplate(Transform obj)
        {
            int i;
            for (i = 0; i < templates.Length; i++)
            {
                if (obj == templates[i])
                {
                    return true;
                }
            }
            return false;
        }

        // Despawn - called for despawning some object, that was spawned
        public bool Despawn(Transform obj)
        {
            bool res = false;
            int i = 0, n = 0, j = 0;
            for (j = 0; j < occupiedObjects.Count; j++)
            {
                List<Transform> transforms = occupiedObjects[templates[j]];
                int transformsCount = transforms.Count;
                for (i = 0; i < transformsCount; i++)
                {
                    if (transforms[i] == obj)
                    {
                        res = true;
                        transforms.RemoveAt(i);
                        readyObjects[templates[j]].Add(obj);
                        obj.gameObject.SetActive(false); 
                        if (needSort)
                        {
                            if (needParenting)
                            {
                                obj.parent = templatesParent[n];
                            }
                        } 
                        if (needBroadcasting && broadcastDespawnName != "")
                            obj.BroadcastMessage(broadcastDespawnName, SendMessageOptions.DontRequireReceiver);
                        break;
                    }
                }
                n++;
            } 
            return res;
        }

        // If you have parent(template) of spawned object - use this variant for better perfomance!
        public bool Despawn(Transform obj, Transform template)
        {
            bool res = false;
            int i, j;
            for (i = 0; i < templates.Length; i++)
            {
                if (templates[i] == template)
                {
                    int listSize = occupiedObjects[template].Count;
                    for (j = 0; j < listSize; j++)
                    {
                        if (occupiedObjects[template][j] == obj)
                        {
                            res = true;
                            occupiedObjects[template].RemoveAt(j);
                            obj.gameObject.SetActive(false);

                            if (needSort)
                                obj.parent = templatesParent[i];

                            readyObjects[template].Add(obj);
                            break;
                        }
                    }
                    if (res)
                    {
                        break;
                    }
                }
            } 
            return res;
        }

        //Return template position in array
        public int GetTemplatePosition(Transform obj)
        {
            int pos = -1;
            int i;
            for (i = 0; i < templates.Length; i++)
            {
                if (obj == templates[i])
                {
                    pos = i;
                }
            }
            return pos;
        } 

        /// <summary>
        /// This function spawns audiosource somewhere with it's clip
        /// </summary>
        /// <param name="obj">Source template</param>
        /// <param name="clip">Clip that have to be played</param>
        /// <param name="pos">Place to spawn audio</param>
        /// <param name="par">Parent of spawned audiosource</param>
        /// <returns>Link to spawned audio source</returns>
        public Transform SpawnAudio(Transform obj, AudioClip clip, Vector3 pos, Transform par)
        {
            Transform tempTransform;
            int i;
            int curPos = GetTemplatePosition(obj);
            for (i = 0; i < readyObjects[templates[curPos]].Count; i++)
            {
                if (!readyObjects[templates[curPos]][i].gameObject.activeSelf)
                {
                    tempTransform = readyObjects[templates[curPos]][i];
                    readyObjects[templates[curPos]].RemoveAt(i);
                    occupiedObjects[templates[curPos]].Add(tempTransform);
                    tempTransform.position = pos;  
                    if (par == null)
                    {
                        tempTransform.SetParent(null);
                    }
                    else
                    {
                        tempTransform.SetParent(par);
                    }
                    tempTransform.gameObject.SetActive(true);

                    if (needBroadcasting && broadcastSpawnName != "")
                        tempTransform.BroadcastMessage(broadcastSpawnName, SendMessageOptions.DontRequireReceiver); 
                    AudioSource source = tempTransform.gameObject.GetComponent<AudioSource>();
                    if (source != null)
                    {
                        source.clip = clip;
                    }
                    return tempTransform;
                }
            }
            if (poolLengthCurrent[curPos] < poolLengthMax[curPos])
            {
                poolLengthCurrent[curPos]++;

                Transform newGO = Instantiate(templates[curPos], Vector3.zero, Quaternion.identity) as Transform;
                newGO.transform.parent = this.gameObject.transform;
                newGO.gameObject.SetActive(true);
                newGO.position = pos;  
                if (par == null)
                {
                    newGO.SetParent(null);
                }
                else
                {
                    newGO.SetParent(par);
                } 
                occupiedObjects[templates[curPos]].Add(newGO); 
                if (needBroadcasting && broadcastSpawnName != "")
                    newGO.BroadcastMessage(broadcastSpawnName, SendMessageOptions.DontRequireReceiver); 
                AudioSource source = newGO.gameObject.GetComponent<AudioSource>();
                if (source != null)
                {
                    source.clip = clip;
                }
                return newGO;
            } 
            return null;
        }

        //Return transfrom of spawned object
        public Transform Spawn(Transform obj, Transform par, Vector3 pos = new Vector3(), Quaternion rot = new Quaternion())
        {
            if (!CheckForExistingTemplate(obj))
            {
                if (needDebugging)
                    Debug.LogWarning(obj.name + " isn't in " + this.gameObject.name + "'s pool");
                return null;
            } 
            Transform tempTransform; 
            int i;
            int curPos = GetTemplatePosition(obj);
            for (i = 0; i < readyObjects[obj].Count; i++)
            {
                if (!readyObjects[obj][i].gameObject.activeSelf)
                {
                    tempTransform = readyObjects[obj][i];
                    readyObjects[obj].RemoveAt(i);
                    occupiedObjects[obj].Add(tempTransform);
                    tempTransform.position = pos;
                    tempTransform.rotation = rot; 
                    if (par == null)
                    {
                        tempTransform.SetParent(null);
                    }
                    else
                    {
                        tempTransform.SetParent(par);
                    }
                    tempTransform.gameObject.SetActive(true); 
                    if (needBroadcasting && broadcastSpawnName != "")
                        tempTransform.BroadcastMessage(broadcastSpawnName, SendMessageOptions.DontRequireReceiver); 
                    return tempTransform;
                }
            }
            if (poolLengthCurrent[curPos] < poolLengthMax[curPos])
            {
                poolLengthCurrent[curPos]++; 
                Transform newGO = Instantiate(templates[curPos], Vector3.zero, Quaternion.identity) as Transform;
                newGO.transform.parent = this.gameObject.transform;
                newGO.gameObject.SetActive(true);
                newGO.position = pos;
                newGO.rotation = rot; 
                if (par == null)
                {
                    newGO.SetParent(null);
                }
                else
                {
                    newGO.SetParent(par);
                } 
                occupiedObjects[obj].Add(newGO); 
                if (needBroadcasting && broadcastSpawnName != "")
                    newGO.BroadcastMessage(broadcastSpawnName, SendMessageOptions.DontRequireReceiver); 
                return newGO;
            }
            return null;
        }

        //Return transfrom of spawned object
        public Transform Spawn(Transform obj, Vector3 pos , Quaternion rot , Transform par)
        {
            if (!CheckForExistingTemplate(obj))
            {
                if (needDebugging)
                    Debug.LogWarning(obj.name + " isn't in " + this.gameObject.name + "'s pool");
                return null;
            } 
            Transform tempTransform; 
            int i;
            int curPos = GetTemplatePosition(obj);

            for (i = 0; i < readyObjects[obj].Count; i++)
            {
                if (!readyObjects[obj][i].gameObject.activeSelf)
                {
                    tempTransform = readyObjects[obj][i];
                    readyObjects[obj].RemoveAt(i);
                    occupiedObjects[obj].Add(tempTransform);
                    tempTransform.position = pos;
                    tempTransform.rotation = rot;

                    if (par == null)
                    {
                        tempTransform.SetParent(null);
                    }
                    else
                    {
                        tempTransform.SetParent(par);
                    }

                    tempTransform.gameObject.SetActive(true);
                    if (needBroadcasting && broadcastSpawnName != "")
                        tempTransform.BroadcastMessage(broadcastSpawnName, SendMessageOptions.DontRequireReceiver); 
                    return tempTransform;
                }
            }

            if (poolLengthCurrent[curPos] < poolLengthMax[curPos])
            {
                poolLengthCurrent[curPos]++;
                Transform newGO = Instantiate(templates[curPos], Vector3.zero, Quaternion.identity) as Transform; 
                newGO.transform.parent = this.gameObject.transform;
                newGO.gameObject.SetActive(true);
                newGO.position = pos;
                newGO.rotation = rot; 
                if (par == null)
                {
                    newGO.SetParent(null);
                }
                else
                {
                    newGO.SetParent(par);
                }
                occupiedObjects[obj].Add(newGO);
                if (needBroadcasting && broadcastSpawnName != "")
                    newGO.BroadcastMessage(broadcastSpawnName, SendMessageOptions.DontRequireReceiver); 
                return newGO;
            }
            return null;
        }

        //Return transfrom of spawned object
        public Transform Spawn(Transform obj, Vector3 pos = new Vector3(), Quaternion rot = new Quaternion())
        {
            if (!CheckForExistingTemplate(obj))
            {
                if (needDebugging)
                    Debug.LogWarning(obj.name + " isn't in " + this.gameObject.name + "'s pool");
                return null;
            } 
            Transform tempTransform; 
            int i;
            int curPos = GetTemplatePosition(obj);
            for (i = 0; i < readyObjects[obj].Count; i++)
            {
                if (!readyObjects[obj][i].gameObject.activeSelf)
                {
                    tempTransform = readyObjects[obj][i];
                    readyObjects[obj].RemoveAt(i);
                    occupiedObjects[obj].Add(tempTransform);
                    tempTransform.position = pos;
                    tempTransform.rotation = rot;
                    tempTransform.SetParent(null); 
                    tempTransform.gameObject.SetActive(true);
                    if (needBroadcasting && broadcastSpawnName != "")
                        tempTransform.BroadcastMessage(broadcastSpawnName, SendMessageOptions.DontRequireReceiver); 
                    return tempTransform;
                }
            } 
            if (poolLengthCurrent[curPos] < poolLengthMax[curPos])
            {
                poolLengthCurrent[curPos]++;
                Transform newGO = Instantiate(templates[curPos], Vector3.zero, Quaternion.identity) as Transform;
                newGO.transform.parent = this.gameObject.transform;
                newGO.gameObject.SetActive(true);
                newGO.position = pos;
                newGO.rotation = rot;
                newGO.SetParent(null);
                occupiedObjects[obj].Add(newGO);
                if (needBroadcasting && broadcastSpawnName != "")
                    newGO.BroadcastMessage(broadcastSpawnName, SendMessageOptions.DontRequireReceiver); 
                return newGO;
            }
            return null;
        }

        //Updating templates array
        public void SetTemplates(Transform[] newArray)
        {
            templates = newArray;
        }

        //updating poolLength array
        public void SetLength(int[] newPoolLenght)
        {
            poolLength = newPoolLenght;
        }

        //updating poolLengthMax
        public void SetLengthMax(int[] newPoolLengthMax)
        {
            poolLengthMax = newPoolLengthMax;
        }

        //Automatic spawning function
        void CalculateObjectsPerUpdate()
        { 
            if (objectsPerUpdate != 0)
            {
                if (objectsPerUpdate == -1)
                {
                    objectsPerUpdate = 1;
                }
                else
                {
                    objectsPerUpdate = (int)(objectsPerUpdate * (normalValue / Time.deltaTime));
                    if (objectsPerUpdate == 0)
                    {
                        objectsPerUpdate = 1;
                    }
                }
            }
        }

        //Delayed spawn function. Instantiates objects not in one frame. 
        void DelayedSpawnInInstall()
        {
            if (optimizeSpawn)
            {
                CalculateObjectsPerUpdate();
            }
            int leftObjectsToInstall = objectsPerUpdate;
            int i, j;
            for (i = 0; i < templates.Length; i++)
            {
                for (j = 0; j < poolLength[i]; j++)
                {
                    if (poolLengthCurrent[i] < poolLength[i])
                    {
                        InstantiateItem(templates[i], templatesParent[i], i);
                        leftObjectsToInstall--;
                        if (leftObjectsToInstall == 0)
                        {
                            break;
                        }
                    }
                }
                if (leftObjectsToInstall == 0)
                {
                    break;
                }
            }
            if (leftObjectsToInstall != 0)
            {
                installing = false;
            } 
        }

        //Instantiates all objects
        void NonDelayedSpawnInInstall()
        { 
            for (int i = 0; i < templates.Length; i++)
            {
                for (int j = 0; j < poolLength[i]; j++)
                {
                    if (poolLengthCurrent[i] < poolLength[i])
                    {
                        InstantiateItem(templates[i], templatesParent[i], i);
                    }
                }
            }
        }

        void Update()
        {
            if (installing)
            {
                if (pooling == false)
                {
                    installing = false;
                }
                else
                {
                    //Delayed spawn in install
                    if (delayedSpawnInInstall)
                    {
                        DelayedSpawnInInstall();
                    }
                    else
                    {
                        NonDelayedSpawnInInstall();
                        installing = false;
                    }
                }
            }
        }

        // Instantiating item
        Transform InstantiateItem(Transform temp, Transform par, int templatePosition)
        {
            Transform newGO = Instantiate(temp, Vector3.zero, Quaternion.identity) as Transform;
            newGO.transform.SetParent(par);
            newGO.gameObject.SetActive(false);
            readyObjects[temp].Add(newGO);
            poolLengthCurrent[templatePosition]++;
            return newGO;
        }

        //Installing basic arrays for working with this pool
        public void Install()
        {
            poolLengthCurrent = new int[poolLength.Length];
            templatesParent = new Transform[templates.Length];
            int i;
            for (i = 0; i < templates.Length; i++)
            {
                if (templates[i] != null)
                {
                    if (needParenting)
                    {
                        GameObject newParent = new GameObject(templates[i].name);
                        templatesParent[i] = newParent.transform;
                        newParent.transform.SetParent(this.transform);
                        List<Transform> tempList = new List<Transform>();
                        if (tempList != null)
                        { 
                            readyObjects.Add(templates[i], tempList);
                        }
                        occupiedObjects.Add(templates[i], new List<Transform>());
                    }
                    else
                    { 
                        List<Transform> tempList = new List<Transform>();
                        if (tempList != null)
                        { 
                            readyObjects.Add(templates[i], tempList);
                        }
                        occupiedObjects.Add(templates[i], new List<Transform>());
                    }
                }
            }
            if (targetFPS == 0)
            {
                targetFPS = 60;
            }
            normalValue = 1f / targetFPS;
            installing = true;
        }
    }
}