using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace TechTree.Model
{
    /// <summary>
    /// The Upgrade Cost class is used to store cost per resource to upgrade a unit produced by a blueprint.
    /// </summary>
	public class UpgradeLevel : BlueprintModelAsset
	{
        public float constructTime = 0f;
        public GameObject gameObject;
        public List<ResourceCost> costs = new List<ResourceCost>();
        public List<ResourceConsumptionRate> consumptionRates = new List<ResourceConsumptionRate>();
        public List<ResourceProductionRate> productionRates = new List<ResourceProductionRate>();
	}
}
