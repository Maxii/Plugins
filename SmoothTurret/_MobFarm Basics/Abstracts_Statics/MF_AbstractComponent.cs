using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[HelpURL("http://mobfarmgames.weebly.com/mf_abstractcomponent.html")]
public abstract class MF_AbstractComponent : MonoBehaviour {

	[Tooltip("The strength value of this component. Means different things for different components.")]
	public float strength;

	[HideInInspector] public float monitor;
	[HideInInspector] public List<MF_FxController> fxScript;

	protected virtual void OnEnable () {
		monitor = 0f;
		SendCheckUnit();
	}

	protected void SendCheckUnit () {
		if ( fxScript.Count > 0 ) {
			for ( int i=0; i < fxScript.Count; i++ ) {
				if ( fxScript[i] != null ) { fxScript[i].CheckUnit(); }
			}
		}
	}

}
