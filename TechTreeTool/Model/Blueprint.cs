using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace TechTree.Model
{
    /// <summary>
    /// The Blueprint is the basic unit of construction. It defines things which can be
    /// built, researched or achieved. Blueprints belong to a BlueprintGroup, and can require
    /// other Blueprint classes to be built first.
    /// </summary>
    public class Blueprint : BlueprintModelAsset
    {
        /// <summary>
        /// The unique ID of this blueprint. This is used to lookup a blueprint in your game source code.
        /// </summary>
        public string ID;
        /// <summary>
        /// The number of seconds needed to build this blueprint.
        /// </summary>
        public float constructTime;
        /// <summary>
        /// If true, this blueprint produces a factory unit. A factory unit is used to produce other units from it's list of blueprints.
        /// </summary>
        public bool isFactory = false;
        /// <summary>
        /// Your GameObject prefab for this blueprint, which can be used at runtime in your game.
        /// </summary>
        public GameObject gameObject;
        /// <summary>
        /// The sprite attached to this blueprint, used at runtime along with the gameObject.
        /// </summary>
        public Sprite sprite;
        /// <summary>
        /// The list of other blueprints that must be built before this blueprint can be built.
        /// </summary>
        public List<BlueprintPrerequisite> prerequisites = new List<BlueprintPrerequisite> ();
        /// <summary>
        /// A list of resources and costs required to build this blueprint.
        /// </summary>
        public List<ResourceCost> costs = new List<ResourceCost> ();
        /// <summary>
        /// /// A list of resources which the unit created by this blueprint will produce.
        /// </summary>
        public List<ResourceProductionRate> productionRates = new List<ResourceProductionRate> ();
        /// <summary>
        /// /// A list of resources which the unit created by this blueprint will consume while it is enabled.
        /// </summary>
        public List<ResourceConsumptionRate> consumptionRates = new List<ResourceConsumptionRate> ();
        /// <summary>
        /// If this is a factory blueprint, the blueprints that can be built are specified in this class.
        /// </summary>
        public Factory factory = null;
        /// <summary>
        /// Allow multiple builds of this blueprint, used for destroyable units.
        /// </summary>
        public bool allowMultiple = false;
        /// <summary>
        /// If available is set to true, this blueprint is automatically constructed into a unit when the game starts. This is used for the root level factory blueprints which will construct all other blueprints.
        /// </summary>
        public bool prebuilt = false;
        /// <summary>
        /// If true, only one of the child branches can be built. This forces the user to make strategic decisions which require advance planning.
        /// </summary>
        public bool mutex = false;
        /// <summary>
        /// The required level of the factory used to build this unit.
        /// </summary>
        public int requiredFactoryLevel = 0;
        /// <summary>
        /// EDITOR ONLY: This is the rect position of the blueprint in the editor.
        /// </summary>
        public Rect rect;
        /// <summary>
        /// The group the blueprint belongs to.
        /// </summary>
        public BlueprintGroup group;
        /// <summary>
        /// The unit is upgradeable.
        /// </summary>
        public bool isUpgradeable;
        /// <summary>
        /// The unit will inherit the factory that produced it.
        /// </summary>
        public bool inheritFactoryLevel = false;
        /// <summary>
        /// List of costs required to upgrade to the next level.
        /// </summary>
        public List<UpgradeLevel> upgradeLevels = new List<UpgradeLevel> ();

        public List<UnitStatValue> statValues = new List<UnitStatValue>();

#if UNITY_EDITOR
        public override void SetDirty ()
        {
            foreach(var c in costs) c.SetDirty();
            if(factory != null) factory.SetDirty();
            base.SetDirty ();
        }
#endif

        
    }

}
