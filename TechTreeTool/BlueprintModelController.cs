using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using TechTree.Model;

namespace TechTree
{
    /// <summary>
    /// The BlueprintModelController takes the blueprint model and uses it to track which units have been built
    /// at runtime, for a particular player.
    /// It provides access to factories, which is how blueprints can be built.
    /// </summary>
    public class BlueprintModelController : MonoBehaviour
    {
        #region API
        /// <summary>
        /// Controllers for the Blueprint groups in the model.
        /// </summary>
        public Dictionary<string, BlueprintGroupController> groups = new Dictionary<string, BlueprintGroupController> ();
        /// <summary>
        /// Controllers for the Blueprints in the model.
        /// </summary>
        public Dictionary<string, BlueprintController> blueprints = new Dictionary<string, BlueprintController> ();
        /// <summary>
        /// Controllers for the Resources in the model.
        /// </summary>
        public Dictionary<string, ResourceController> resources = new Dictionary<string, ResourceController> ();

        /// <summary>
        /// Get all possible factories from the model.
        /// </summary>
        public IEnumerable<BlueprintController> GetFactories ()
        {
            foreach (var b in blueprints.Values) {
                if (b.blueprint.isFactory)
                    yield return b;
            }
        }

        /// <summary>
        /// Gets a particular factory by ID from the model.
        /// </summary>
        public BlueprintController GetFactory (string ID)
        {
            var c = blueprints [ID];
            if (c.blueprint.isFactory)
                return c;
            return null;
        }
        #endregion

        #region implementation
        void Update ()
        {
            foreach (var r in resources.Values) {
                r.Update ();
            }
        }

        void Awake ()
        {
            if (model != null) {
                foreach (var r in model.resources) {
                    resources.Add (r.ID, new ResourceController (r));
                }

                foreach (var u in model.blueprints) {
                    blueprints.Add (u.ID, new BlueprintController (this, u));
                }

                foreach (var g in model.groups) {
                    groups.Add (g.ID, new BlueprintGroupController (g));
                }

                foreach (var bpc in blueprints.Values) {
                    if (bpc.blueprint.prebuilt) {
                        SpawnUnit (null, bpc);
                    }
                }
            }
        }

        public TechTreeUnit SpawnUnit (TechTreeFactory source, BlueprintController bpc)
        {
            GameObject unit;
            var prefab = bpc.blueprint.gameObject;
            if (prefab == null) {
                unit = new GameObject (bpc.blueprint.ID + " Unit");
            } else {
                unit = GameObject.Instantiate (prefab) as GameObject;
            }
            var ttu = unit.AddComponent<TechTreeUnit> ();
            bpc.units.Add(ttu);
            ttu.Init (this, bpc);
            ttu.Level = 0;
            if(source != null && bpc.blueprint.inheritFactoryLevel) {
                ttu.Level = source.unit.Level;
            }
            units.Add (ttu);
            unit.SendMessage ("OnSpawn", source, SendMessageOptions.DontRequireReceiver);
            return ttu;
        }
        #endregion

        public List<TechTreeUnit> units = new List<TechTreeUnit> ();
        public BlueprintModel model;
    }
   
}