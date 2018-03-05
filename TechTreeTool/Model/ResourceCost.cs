using UnityEngine;
using System.Collections;

namespace TechTree.Model
{
    /// <summary>
    /// The Resource Cost class is used to store cost per resource information for a blueprint.
    /// </summary>
	public class ResourceCost : BlueprintModelAsset
	{
        /// <summary>
        /// The resource used for this cost.
        /// </summary>
		public Resource resource;
        /// <summary>
        /// The qty taken from the resource when the parent blueprint is built.
        /// </summary>
		public float qty;
	}
}
