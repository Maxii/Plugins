using UnityEngine;
using System.Collections.Generic;
using System;

namespace Forge3D
{
    [Serializable]
    public class F3DTurretConstructor : MonoBehaviour
    { 
        public GameObject[] Breeches;                 //breeches 
        public GameObject[] Barrels;                  //barrels
        public GameObject Base;
        public GameObject Swivel;
        public GameObject Mount;
        public GameObject Head;
        public int turretIndex = 0;
        public bool needUpdateListOfTemplates = false;  

        //Returns selected turret type
        public int GetSelectedType()
        {
            return turretIndex;
        }

        //Changes selected turret index to new value with new turret structure
        public void ChangeTurretIndex(int index, TurretStructure struc)
        {
            turretIndex = index;
            UpdateFullTurret(struc);
        }

        //Checks if name contains in array
        bool CheckForName(string[] names, string name)
        {
            for (int i = 0; i < names.Length; i++)
            {
                if (names[i] == name)
                {
                    return true;
                }
            }
            return false;
        }

        //Updates turret structure to another
        public void UpdateFullTurret(TurretStructure struc)
        {
            if (!Application.isPlaying)
            { 
                UpdateTurret(struc);
                if (struc.NeedLOD)
                {
                    InstallLods();
                }

                if (struc.HasTurretScript)
                {
                    InstallTurretController();
                }
                else
                {
                    var tmp = GetComponent<F3DTurret>();
                    if (tmp != null)
                    { 
                        tmp.destroyIt = true;
                    } 
                }
            }
        }

        //Unlinks turret from global constructor
        public void UnlinkGameObject()
        { 
            DestroyImmediate(this);
        }

        //Install LOD information
        void InstallLods()
        {
            if (this.transform == null || this.transform.childCount == 0)
                return;
            Transform baseTurrel = this.transform.GetChild(0);
            if (baseTurrel != null)
            {
                LODGroup oldLod = baseTurrel.gameObject.GetComponent<LODGroup>();
                if (oldLod == null)
                {
                    LODGroup lod = baseTurrel.gameObject.AddComponent<LODGroup>();
                    Renderer[] rends = baseTurrel.gameObject.GetComponentsInChildren<Renderer>();
                    List<List<Renderer>> renderers = new List<List<Renderer>>();
                    for (int i = 0; i < rends.Length; i++)
                    {
                        string curName = rends[i].gameObject.name;
                        if (curName[curName.Length - 1] == ')')
                        {
                            string cuting = "(Clone)";
                            curName = curName.Replace(cuting, "");
                        }
                        int lodGroup = -1;
                        if (int.TryParse(curName[curName.Length - 1].ToString(), out lodGroup))
                        {
                            if (renderers.Count <= lodGroup + 1)
                            {
                                for (int j = 0; j < lodGroup + 1 - renderers.Count; j++)
                                {
                                    renderers.Add(new List<Renderer>());
                                }
                            }
                            renderers[lodGroup].Add(rends[i]);
                        }
                    }

                    LOD[] lods = new LOD[renderers.Count];
                    for (int i = 0; i < lods.Length; i++)
                    {
                        lods[i].renderers = renderers[i].ToArray();
                        float x = 10f / lods.Length * (i + 1);
                        if (i < lods.Length * 0.5f)
                        {
                            lods[i].screenRelativeTransitionHeight = (-1.5f * x + 10f) / 10f + 0.02f; 
                        }
                        else
                        {
                            lods[i].screenRelativeTransitionHeight = (-0.5f * x + 5f) / 10f + 0.02f; 
                        } 
                    }

                    lod.SetLODs(lods);
                    lod.RecalculateBounds(); 
                }
            }
        }

        /// <summary>
        /// Installs controller of turret
        /// </summary>
        void InstallTurretController()
        { 
            F3DTurret turretController = this.gameObject.GetComponent<F3DTurret>();
            if (turretController == null)
            { 
                F3DTurret[] turretControllers = this.gameObject.GetComponentsInChildren<F3DTurret>();
                int i;
                for (i = 0; i < turretControllers.Length; i++)
                {
                    if (turretControllers[i] != null)
                    {
                        turretControllers[i].destroyIt = true;
                    } 
                }

                F3DTurret newScript = this.gameObject.AddComponent<F3DTurret>();
                newScript.smoothControlling = true;
                newScript.HeadingLimit.x = -60f;
                newScript.HeadingLimit.y = 60;
                newScript.ElevationLimit.x = -60f;
                newScript.ElevationLimit.y = 60f;
                newScript.ElevationTrackingSpeed = 30f;
                newScript.HeadingTrackingSpeed = 30f;

                string[] namesMount = new string[] { "MOUNT", "Mount" };
                newScript.Mount = FindGameObject(namesMount, this.gameObject);
                string[] namesSwivel = new string[] { "SWIVEL", "Swivel" };
                newScript.Swivel = FindGameObject(namesSwivel, this.gameObject);
            }
            else if (turretController.Swivel == null || turretController.Mount == null)
            {
                string[] namesMount = new string[] { "MOUNT", "Mount" };
                turretController.Mount = FindGameObject(namesMount, this.gameObject);
                string[] namesSwivel = new string[] { "SWIVEL", "Swivel" };
                turretController.Swivel = FindGameObject(namesSwivel, this.gameObject);
            }
        }

        /// <summary>
        /// This function used to find child by part of it's name, separated by "_"
        /// </summary>
        /// <param name="names">Array of available name part</param>
        /// <param name="parent">Parent</param>
        /// <returns></returns>
        GameObject FindGameObject(string[] names, GameObject parent)
        {
            int i;
            Transform[] childrens = parent.GetComponentsInChildren<Transform>();
            foreach (Transform child in childrens)
            {
                if (child != null)
                {
                    string[] nameParts = child.name.Split('_');
                    for (i = 0; i < nameParts.Length; i++)
                    {
                        if (CheckForName(names, nameParts[i]))
                        {
                            return child.gameObject;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// This function used to update turret's structure
        /// </summary>
        /// <param name="curStructure"></param>
        public void UpdateTurret(TurretStructure curStructure)
        {
            if (curStructure == null)
                return;
            UpdateBase(curStructure.Base);
            UpdateSwivel(curStructure.Swivel, curStructure.SwivelPrefix);
            UpdateHead(curStructure.Head, curStructure.HeadPrefix);
            UpdateMount(curStructure.Mount, curStructure.MountPrefix);
            UpdateBreeches(curStructure.WeaponBreeches, curStructure.WeaponSlotsNames, curStructure.WeaponBarrels, curStructure.WeaponBarrelSockets);

        }

        /// <summary>
        /// Function updates breeches
        /// </summary>
        /// <param name="breeches">List of breech prefabs</param>
        /// <param name="socketNames">List of breech names</param>
        void UpdateBreeches(List<GameObject> breeches, List<string> socketNames, List<GameObject> barrels, List<string> barrelSocketNames)
        {
            int i = 0;
            if (Breeches != null)
            { 
                for (i = 0; i < Breeches.Length; i++)
                {
                    DestroyImmediate(Breeches[i]);
                }
            }
            List<GameObject> generatedBreeches = new List<GameObject>();
            List<GameObject> tempBreeches = breeches;
            List<string> tempSockets = socketNames;
            List<GameObject> generatedBarrels = new List<GameObject>();
            if (Base != null)
            {
                Transform[] childrens = Base.GetComponentsInChildren<Transform>();
                foreach (Transform child in childrens)
                {
                    for (i = 0; i < tempSockets.Count; i++)
                    {
                        if (child.name == tempSockets[i])
                        {
                            if (tempBreeches[i] != null)
                            {
                                GameObject newGO = Instantiate(tempBreeches[i], Vector3.zero, Quaternion.identity) as GameObject;
                                newGO.transform.parent = child;
                                newGO.transform.position = child.position;
                                newGO.transform.rotation = child.rotation;
                                newGO.transform.localScale = new Vector3(1f, 1f, 1f);
                                generatedBarrels.Add(UpdateBarrel(barrels[i], newGO, barrelSocketNames[i]));
                                generatedBreeches.Add(newGO); 
                            }
                            else
                            {
                                generatedBarrels.Add(UpdateBarrel(barrels[i], child.gameObject, barrelSocketNames[i]));
                                generatedBreeches.Add(null);
                            }
                        }
                    }
                }
                Breeches = generatedBreeches.ToArray();
            }
        }

        // Installs new barrel to parent
        GameObject UpdateBarrel(GameObject barrelGO, GameObject parentGO, string socket)
        {
            if (parentGO == null || barrelGO == null)
                return null;
            Transform[] transforms = parentGO.GetComponentsInChildren<Transform>();

            foreach (Transform child in transforms)
            {
                if (child.name == socket)
                {
                    GameObject newGO = Instantiate(barrelGO, Vector3.zero, Quaternion.identity) as GameObject;
                    newGO.transform.parent = child;
                    newGO.transform.position = child.position;
                    newGO.transform.rotation = child.rotation;
                    newGO.transform.localScale = new Vector3(1f, 1f, 1f);

                    return newGO;
                }
            }

            GameObject newGOParented = Instantiate(barrelGO, Vector3.zero, Quaternion.identity) as GameObject;
            newGOParented.transform.parent = parentGO.transform;
            newGOParented.transform.position = parentGO.transform.position;
            newGOParented.transform.rotation = parentGO.transform.rotation;
            newGOParented.transform.localScale = new Vector3(1f, 1f, 1f);

            return newGOParented;
        }

        /// <summary>
        /// This function deletes all current childrens and creates new HEAD
        /// </summary>
        /// <param name="newBase">Head prefab</param>
        /// <param name="prefix">Head socket name</param>
        void UpdateHead(GameObject headGO, string prefix)
        {
            if (Head != null)
            {
                if (Head.transform.childCount > 0)
                {
                    Transform[] childs = Head.GetComponentsInChildren<Transform>();
                    foreach (Transform child in childs)
                    {
                        if (child != null && child != Head.transform)
                        {
                            if (child.gameObject != this.gameObject)
                            {
                                DestroyImmediate(child.gameObject);
                            }
                        }
                    }
                }
                DestroyImmediate(Head);
            }

            if (headGO != null)
            {
                if (Base != null)
                {
                    Transform[] transforms = Base.GetComponentsInChildren<Transform>();
                    foreach (Transform child in transforms)
                    {
                        if (child.name == prefix)
                        {
                            GameObject newGO = Instantiate(headGO, Vector3.zero, Quaternion.identity) as GameObject;
                            newGO.transform.parent = child;
                            newGO.transform.position = child.position;
                            newGO.transform.rotation = child.rotation;
                            newGO.transform.localScale = new Vector3(1f, 1f, 1f);
                            Head = newGO;
                            break;
                        }
                    }
                } 
                else
                {
                    GameObject newGO = Instantiate(headGO, Vector3.zero, Quaternion.identity) as GameObject;
                    newGO.transform.parent = this.gameObject.transform;
                    newGO.transform.position = this.gameObject.transform.position;
                    newGO.transform.rotation = this.gameObject.transform.rotation;
                    newGO.transform.localScale = new Vector3(1f, 1f, 1f);
                    Head = newGO;
                }
            }
        }

        /// <summary>
        /// This function deletes all current childrens and creates new MOUNT
        /// </summary>
        /// <param name="newBase">Mount prefab</param>
        /// <param name="prefix">Mount socket name</param>
        void UpdateMount(GameObject mountGO, string prefix)
        {
            if (Mount != null)
            {
                if (Mount.transform.childCount > 0)
                {
                    Transform[] childs = Mount.GetComponentsInChildren<Transform>();
                    foreach (Transform child in childs)
                    {
                        if (child != null && child != Mount.transform)
                        {
                            if (child.gameObject != this.gameObject)
                            {
                                DestroyImmediate(child.gameObject);
                            }
                        }
                    }
                }
                DestroyImmediate(Mount);
            }

            if (mountGO != null)
            {
                if (Base != null)
                {
                    Transform[] transforms = Base.GetComponentsInChildren<Transform>();
                    foreach (Transform child in transforms)
                    {
                        if (child.name == prefix)
                        {
                            GameObject newGO = Instantiate(mountGO, Vector3.zero, Quaternion.identity) as GameObject;
                            newGO.transform.parent = child;
                            newGO.transform.position = child.position;
                            newGO.transform.rotation = child.rotation;
                            newGO.transform.localScale = new Vector3(1f, 1f, 1f);
                            Mount = newGO;
                            if (Base == null)
                            {
                                Base = Mount;
                                Swivel = Mount;
                            }
                            break;
                        }
                    }
                }
                else
                {
                    GameObject newGO = Instantiate(mountGO, Vector3.zero, Quaternion.identity) as GameObject;
                    newGO.transform.parent = this.gameObject.transform;
                    newGO.transform.position = this.gameObject.transform.position;
                    newGO.transform.rotation = this.gameObject.transform.rotation;
                    newGO.transform.localScale = new Vector3(1f, 1f, 1f);
                    Mount = newGO;
                    Base = Mount;
                    Swivel = Mount;
                }
            }
        }

        /// <summary>
        /// This function deletes all current childrens and creates new SWIVEL
        /// </summary>
        /// <param name="newBase">Swivel prefab</param>
        /// <param name="prefix">Swivel socket name</param>
        void UpdateSwivel(GameObject swivelGO, string prefix)
        {
            if (Swivel != null)
            {
                if (Swivel.transform.childCount > 0)
                {
                    Transform[] childs = Swivel.GetComponentsInChildren<Transform>();
                    foreach (Transform child in childs)
                    {
                        if (child != null && child != Swivel.transform)
                        {
                            if (child.gameObject != this.gameObject)
                            {
                                DestroyImmediate(child.gameObject);
                            }
                        }
                    }
                }
                DestroyImmediate(Swivel);
            }

            if (swivelGO != null)
            {
                if (Base != null)
                {
                    Transform[] transforms = Base.GetComponentsInChildren<Transform>();
                    foreach (Transform child in transforms)
                    {
                        if (child.name == prefix)
                        {
                            GameObject newGO = Instantiate(swivelGO, Vector3.zero, Quaternion.identity) as GameObject;
                            newGO.transform.parent = child;
                            newGO.transform.position = child.position;
                            newGO.transform.rotation = child.rotation;
                            newGO.transform.localScale = new Vector3(1f, 1f, 1f);
                            Swivel = newGO;
                            if (Base == null)
                            {
                                Base = Swivel;
                            }
                            break;
                        }
                    }
                }
                else
                {
                    GameObject newGO = Instantiate(swivelGO, Vector3.zero, Quaternion.identity) as GameObject;
                    newGO.transform.parent = this.gameObject.transform;
                    newGO.transform.position = this.gameObject.transform.position;
                    newGO.transform.rotation = this.gameObject.transform.rotation;
                    newGO.transform.localScale = new Vector3(1f, 1f, 1f);
                    Swivel = newGO;
                    Base = Swivel;
                }
            }
        }

        /// <summary>
        /// This function deletes all current childrens and creates new BASE
        /// </summary>
        /// <param name="newBase">Base prefab</param>
        void UpdateBase(GameObject baseGO)
        {
            if (this.gameObject.transform.childCount > 0)
            {
                Transform[] childs = this.GetComponentsInChildren<Transform>();
                foreach (Transform child in childs)
                {
                    if (child != null)
                    {
                        if (child.gameObject != this.gameObject)
                        {
                            DestroyImmediate(child.gameObject);
                        }
                    }
                }
            }
            if (baseGO != null)
            {
                GameObject newGO = Instantiate(baseGO, Vector3.zero, Quaternion.identity) as GameObject;
                newGO.transform.parent = this.gameObject.transform;
                newGO.transform.position = this.gameObject.transform.position;
                newGO.transform.rotation = this.gameObject.transform.rotation;
                newGO.transform.localScale = new Vector3(1f, 1f, 1f);
                Base = newGO;
            }
            else
            {
                Base = null;
            }
        }  
    }
}