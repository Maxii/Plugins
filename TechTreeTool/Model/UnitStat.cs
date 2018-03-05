using UnityEngine;
using System.Collections;

namespace TechTree.Model
{
    /// <summary>
    /// The UnitStat class is used to store runtime values of different things for units.
    /// </summary>
    public class UnitStat : BlueprintModelAsset
	{
        /// <summary>
        /// Unique ID of the stat.
        /// </summary>
        public string ID;
        /// <summary>
        /// The color used to identify the stat in the editor.
        /// </summary>
        public Color color;

#if UNITY_EDITOR
        public override void OnCreate ()
        {
            color = Color.HSVToRGB(Random.Range(0f,1f), Random.Range(0.9f,1f), Random.Range(0.9f,1f));
        }
#endif
	}
}
