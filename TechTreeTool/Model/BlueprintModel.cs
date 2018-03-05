using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace TechTree.Model
{
    /// <summary>
    /// The blueprint model is used to contain all data about the design of your blueprint trees.
    /// At runtime it is referenced by a controller which provides access to the blueprints and
    /// facilitates construction of units from the blueprints.
    /// </summary>
    public class BlueprintModel : BlueprintModelAsset
    {
         
        /// <summary>
        /// List of resources available to use when building a blueprint.
        /// </summary>
        public List<Resource> resources = new List<Resource>();
        /// <summary>
        /// The categories of blueprints.
        /// </summary>
        public List<BlueprintGroup> groups = new List<BlueprintGroup>();

        public List<Blueprint> blueprints = new List<Blueprint>();

        public List<UnitStat> stats = new List<UnitStat>();
    
        

#if UNITY_EDITOR
        /// <summary>
        /// EDITOR ONLY: Used to delete a resource and remove it from all blueprints.
        /// </summary>
        public void Delete (Resource resource)
        {
            foreach (var u in blueprints) {
                foreach (var c in u.costs.ToArray()) {
                    if (c.resource == resource) {
                        u.costs.Remove (c);
                    }
                }
            }
            resources.Remove (resource);
            BlueprintModelAsset.Remove (resource);
        }

        public void Delete (UnitStat stat)
        {
            foreach (var u in blueprints) {
                foreach (var c in u.statValues.ToArray()) {
                    if (c.stat == stat) {
                        u.statValues.Remove (c);
                    }
                }
            }
            stats.Remove (stat);
            BlueprintModelAsset.Remove (stat);
        }
        /// <summary>
        /// EDITOR ONLY: Used to delete a blueprint and remove it from all other blueprints.
        /// </summary>
        public void Delete (Blueprint unit)
        {

            foreach (var u in blueprints.ToArray()) {
                foreach (var p in u.prerequisites.ToArray()) {
                    if (p == unit) {
                        u.prerequisites.Remove (p);
                    }
                }
                if (u == unit) {
                    blueprints.Remove (u);
                }
            }

            BlueprintModelAsset.Remove (unit);
        }

        /// <summary>
        /// EDITOR ONLY: Used to remove a group and all it's blueprints from the model.
        /// </summary>
        public void Delete (BlueprintGroup g)
        {
            foreach (var u in blueprints.ToArray()) {
                if (u.group == g) {
                    Delete (u);
                }
            }
            groups.Remove (g);
            BlueprintModelAsset.Remove(g);
        }
#endif
        public Blueprint FindBlueprint (string bpID)
        {
            foreach(var b in blueprints) {
                if(b.ID == bpID) return b;
            }
            return null;
        }

        public IEnumerable<Blueprint> GetDependentBlueprints (Blueprint parent)
        {
            foreach (var b in blueprints) {
                var bpr = (from i in b.prerequisites select i.blueprint).ToList();
                if (bpr.Contains (parent))
                    yield return b;
            }
        }

#if UNITY_EDITOR
        public override void SetDirty ()
        {
            //SortBlueprints();
            foreach(var g in groups) g.SetDirty();
            foreach(var r in resources) r.SetDirty();
            foreach(var b in blueprints) b.SetDirty();
            base.SetDirty ();
        }


#endif


    }
}