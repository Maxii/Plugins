using UnityEngine;
using System.Collections;

namespace TechTree.Model
{
    /// <summary>
    /// The Resource class is used to store details about your game 'resources'. For example Credits,
    /// Energy etc. These resources allow blueprints to costs a certain amount when being built.
    /// </summary>
    public class Resource : BlueprintModelAsset
	{
        /// <summary>
        /// Unique ID of the resource.
        /// </summary>
        public string ID;
        /// <summary>
        /// The color used to identify the resource in the editor.
        /// </summary>
        public Color color;
        /// <summary>
        /// The starting quantity of the resource.
        /// </summary>
		public float qty;
        /// <summary>
        /// A generic game object you can use to represent the resource.
        /// </summary>
        public GameObject gameObject;
        public bool autoReplenish = false;
        /// <summary>
        /// The rate, per second, that this resource will replenish at runtime.
        /// </summary>
        public float autoReplenishRate = 1;
        /// <summary>
        /// If true, this resource is capped at a certain value.
        /// </summary>
        public bool hasMaximumCapacity = false;
        /// <summary>
        /// The maximum capacity of this resource.
        /// </summary>
        public float maximumCapacity = 1;
        /// <summary>
        /// The maximum cost a blueprint can use from this resource.
        /// </summary>
        public float maxPossibleCost = 100;

#if UNITY_EDITOR
        public override void OnCreate ()
        {
            color = Color.HSVToRGB(Random.Range(0f,1f), Random.Range(0.5f,1f), Random.Range(0.5f,1f));
        }
#endif
	}
}
