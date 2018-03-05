using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using TechTree.Model;

namespace TechTree
{
    /// <summary>
    /// The BlueprintController wraps the Blueprint class. This is so you can have multiple instances of 
    /// a blueprint (eg multiple soldiers, turrets) and also share the one blueprint model among multiple
    /// players in a single game. This class is used to build units from blueprints.
    /// 
    /// There is one controller per blueprint, and it contains information that is shared between all the
    /// units it builds.
    /// </summary>
    public class BlueprintController
    {
        #region API
        /// <summary>
        /// All units built by this controller.
        /// </summary>
        public List<TechTreeUnit> units = new List<TechTreeUnit>();

        /// <summary>
        /// The blueprint being used by this class to create units.
        /// </summary>
        public Blueprint blueprint;

        /// <summary>
        /// How many units have been built from this bp.
        /// </summary>
        public int buildCount = 0;

        /// <summary>
        /// Gets a value indicating whether this blueprint has been built during the game session. The first time
        /// a unit is built with this controller, the value will become true. If enough Demolish calls are 
        /// made, this flag can become false.
        /// </summary>
        /// <value><c>true</c> if this instance has been built; otherwise, <c>false</c>.</value>
        public bool HasBeenBuilt {
            get {
                return buildCount > 0;
            }
        }

        /// <summary>
        /// Indicates whether this controller is able to be built, by checking that all prerequistes are met.
        /// Does not check if enough resources are available.
        /// </summary>
        /// <value><c>true</c> if this instance can be built; otherwise, <c>false</c>.</value>
        public bool CanBeBuilt {
            get {
                if(Excluded) return false;
                if ((IsBuilding || HasBeenBuilt) && !blueprint.allowMultiple)
                    return false;
                foreach (var b in blueprint.prerequisites) {
                    if (!controller.blueprints [b.blueprint.ID].HasBeenBuilt) {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is building.
        /// </summary>
        /// <value><c>true</c> if this instance is building; otherwise, <c>false</c>.</value>
        public bool IsBuilding {
            get;
            set;
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="TechTree.BlueprintController"/> is excluded. A blueprint is excluded when it depends on a Mutex blueprint, and was not chosen to be built.
        /// </summary>
        /// <value><c>true</c> if excluded; otherwise, <c>false</c>.</value>
        public bool Excluded { 
            get;
            private set;
        }

        /// <summary>
        /// If this blueprint is for a factory, returns all possible blueprint controllers that this factory can
        /// build. If this blueprint is not for a factory, an exception is raised.
        /// </summary>
        public IEnumerable<BlueprintController> GetFactoryBlueprints ()
        {
            if (!blueprint.isFactory)
                throw new Exception ("This is not a Factory: " + this.blueprint.ID);
            foreach (var b in blueprint.factory.blueprints) {
                yield return controller.blueprints [b.ID];
            }
        }

        /// <summary>
        /// Using a blueprint specified by ID, returns a build request. The build request will contain the
        /// gameobject and sprite specified in the model.
        /// </summary>
        /// <returns>The build request.</returns>
        public BlueprintBuildRequest CreateBuildRequest (TechTreeUnit factoryUnit, string ID)
        {
            if (!HasBeenBuilt) {
                return new BlueprintBuildRequest () { status = BuildStatus.FactoryNotBuilt };
            }
            var bpc = GetValidBPC (ID);
            if(factoryUnit.Level < bpc.blueprint.requiredFactoryLevel) {
                return new BlueprintBuildRequest () { status=BuildStatus.FactoryNotHighEnoughLevel };
            }
            foreach (var p in bpc.blueprint.prerequisites) {
                var pc = controller.blueprints [p.blueprint.ID];
                if (!pc.HasBeenBuilt) {
                    return new BlueprintBuildRequest () { status=BuildStatus.MissingPrerequisite, prerequisite=p };
                }
                var requiredLevel = p.level;
                if(requiredLevel > 0) {
                    var maxLevel = (from i in pc.units select i.Level).Max();
                    if(maxLevel < requiredLevel) {
                        return new BlueprintBuildRequest () { status=BuildStatus.PrerequisiteNotHighEnoughLevel, prerequisite=p };
                    }
                }
            }

            var req = new BlueprintBuildRequest () { blueprint = bpc.blueprint, percentComplete=0 };
            if(blueprint.constructTime <= 0) blueprint.constructTime = 0.01f;
            return req;
        }

        #endregion

        #region implementation
        BlueprintController GetValidBPC (string ID)
        {
            if (!blueprint.isFactory)
                throw new Exception ("This is not a Factory: " + this.blueprint.ID);
            BlueprintController bpc = null;
            foreach (var b in blueprint.factory.blueprints) {
                if (b.ID == ID)
                    bpc = controller.blueprints [ID];
            }
            if (bpc == null) {
                throw new Exception ("Blueprint not available in this Factory: " + ID);
            }
            return bpc;
        }

        public BlueprintController (BlueprintModelController controller, Blueprint blueprint)
        {
            this.controller = controller;
            this.blueprint = blueprint;
        }

        public void OnDependentHasBeenBuilt (BlueprintController bpc)
        {
            if(blueprint.mutex) {
                var children = (from i in controller.model.GetDependentBlueprints(blueprint) where i.ID != bpc.blueprint.ID select i);
                foreach(var c in children) {
                    controller.blueprints[c.ID].Excluded = true;
                }
            }
        }

        BlueprintModelController controller;
        #endregion

    }



}