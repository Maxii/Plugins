using UnityEngine;
using System.Collections;

namespace MFnum {
	public enum B_FactionMethod { None, Tags, Layers }
}

[HelpURL("http://mobfarmgames.weebly.com/mf_b_classify.html")]
public class MF_B_Classify : MF_AbstractClassify {

	[Space(8f)]
	[Tooltip("How should this script determine my own faction? Only used with selection and visibility scripts.")]
	public MFnum.B_FactionMethod myFactionMethod;

	public override void Awake () {
		// where to find faction info
		if ( myFactionMethod == MFnum.B_FactionMethod.Tags && !transform.root.CompareTag("Untagged") ) {
			myFaction = (MFnum.FactionType) System.Enum.Parse( typeof(MFnum.FactionType), transform.root.tag );

		} else if ( myFactionMethod == MFnum.B_FactionMethod.Layers && transform.root.gameObject.layer != 0 ) {
			myFaction = (MFnum.FactionType) System.Enum.Parse( typeof(MFnum.FactionType), LayerMask.LayerToName( transform.root.gameObject.layer ) );	
		}

		base.Awake();
	}

}
