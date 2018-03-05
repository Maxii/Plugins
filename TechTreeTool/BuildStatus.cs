using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

namespace TechTree
{
    /// <summary>
    /// This enum is used to show whether a build request is successful, and if not why it failed.
    /// </summary>
    public enum BuildStatus
    {
        Success,
        NotEnoughResources,
        MissingPrerequisite,
        FactoryNotBuilt,
        Cancelled,
        FactoryNotHighEnoughLevel,
        PrerequisiteNotHighEnoughLevel
    }



}