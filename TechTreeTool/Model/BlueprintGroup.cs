using UnityEngine;
using System.Collections.Generic;

namespace TechTree.Model
{
	/// <summary>
	/// The Blueprint group categorises related blueprints together. For example you might have a group for a 
	/// Research Tree, a Group for building physical structures and a group for building mobile units.
	/// </summary>
    public class BlueprintGroup : BlueprintModelAsset
    {
        public string ID;
        public Rect rect;
        public bool visible = true;
        public Color color;
#if UNITY_EDITOR
        public override void SetDirty ()
        {
            base.SetDirty ();
        }

        public override void OnCreate ()
        {
            color = Color.HSVToRGB(Random.Range(0f,1f), Random.Range(0.5f,1f), Random.Range(0.5f,1f));
        }
#endif
    }

}
