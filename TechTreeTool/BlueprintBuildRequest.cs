using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using TechTree.Model;

namespace TechTree
{
    #region API
    /// <summary>
    /// The blueprint build request is used to determine the result of a Build method call, and is also used 
    /// in your game code to determine when the unit constructed from the blueprint is finished.
    /// </summary>
    public class BlueprintBuildRequest
    {

        /// <summary>
        /// The status of the build, make sure to check this for errors. If successful, you need to check 
        /// the Complete flag or percentComplete field to determine when the unit construction time has elapsed.
        /// </summary>
        public BuildStatus status = BuildStatus.Success;

        /// <summary>
        /// The blueprint used to build the unit.
        /// </summary>
        public Blueprint blueprint;

        /// <summary>
        /// If this build request has a failure state, this is the prerequisite that could not be satisfied.
        /// </summary>
        public BlueprintPrerequisite prerequisite = null;

        /// <summary>
        /// A value between 0 and 1 which is the progress towards completion of the unit.
        /// </summary>
        public float percentComplete;

        /// <summary>
        /// Gets a value indicating whether this build request is completed.
        /// </summary>
        public bool Complete {
            get {
                return percentComplete >= 1;
            }
        }

        public GameObject Prefab {
            get {
                return blueprint.gameObject;
            }
        }

        public Sprite Sprite {
            get {
                return blueprint.sprite;
            }
        }

        public void Cancel ()
        {
            this.cancel = true;
        }

        public bool cancel = false;
    }
    #endregion
}