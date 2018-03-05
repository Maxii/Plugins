using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using TechTree.Model;

namespace TechTree
{
    /// <summary>
    /// The Resource controller is used at runtime to control the resources for each player.
    /// </summary>
    public class ResourceController
    {
       
        public Resource resource;

        #region API
        /// <summary>
        /// Gets the qty of this resource that is available to be used in building units.
        /// </summary>
        public float qty { get; private set; }

        /// <summary>
        /// Consume some of the resource.
        /// </summary>
        public bool Take (float qty)
        {
            if (this.qty - qty < 0)
                return false;
            this.qty -= qty;
            return true;
        }

        /// <summary>
        /// Create an amount of the resource.
        /// </summary>
        public void Give (float qty)
        {
            this.qty += qty;
        }
        #endregion

        #region implementation
        public ResourceController (Resource resource)
        {
            this.resource = resource;
        }

        public void Update ()
        {
            if (resource.autoReplenish) {
                qty = qty + (resource.autoReplenishRate * Time.deltaTime);
            }
        }
        #endregion
    }

   

}