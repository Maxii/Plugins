using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[HelpURL("http://mobfarmgames.weebly.com/mf_b_engine.html")]
public class MF_B_Engine : MF_AbstractComponent {

	float _throttle;
	public float throttle {
		get { return _throttle; }
		set { 
			if ( value != _throttle ) {
				_throttle = value; 
				if ( throttle == 0 ) {
					monitor = 0f;
				} else {
					monitor = ( throttle * (monitorMax - monitorMin) ) + monitorMin;
				}
				SendCheckUnit();
			}
		}
	}
	[Split1("Monitor will be mapped to the range of Monitor Min to Monitor Max. Monitor is the current throttle value that may be read by MF_FxController.")]
	public float monitorMin;
	[Split2("Monitor will be mapped to the range of Monitor Min to Monitor Max. Monitor is the current throttle value that may be read by MF_FxController.")]
	public float monitorMax;

	protected override void OnEnable () {
		throttle = 0f;
		base.OnEnable();
	}

}
