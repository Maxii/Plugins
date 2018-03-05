using UnityEngine;
using System.Collections;

namespace TechTree.Model
{
    /// <summary>
    /// The UnitStat class is used to store runtime values of different things for units.
    /// </summary>
    public class UnitStatValue : BlueprintModelAsset
	{
        public UnitStat stat;
        public int startValue;
        public int maxValue;
        public bool regen = false;
        public float regenRate = 0;
        public bool notifyIfZero = false;
        public int level;
	}
}
