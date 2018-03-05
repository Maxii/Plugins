using UnityEngine;
using System.Collections.Generic;

namespace TechTree.Model
{
    public enum FactoryQueueType
    {
        SingleQueue,
        ParallelQueue
    }

    /// <summary>
    /// The Factory class lists all possible blueprints that can be built by the parent unit.
    /// </summary>
    public class Factory : BlueprintModelAsset
    {
        /// <summary>
        /// The type of factory, a SingleQueue factory builds units one a time, whereas a 
        /// ParallelQueue builds units simultaneously on demand.
        /// </summary>
        public FactoryQueueType type;
        /// <summary>
        /// The list of blueprints which are built by this factory.
        /// </summary>
        public List<Blueprint> blueprints = new List<Blueprint> ();

        public void AddBlueprint (Blueprint b)
        {
            if (!blueprints.Contains (b))
                blueprints.Add (b);
        }
    }

}
