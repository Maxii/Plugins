using UnityEngine;
using System.Collections;

// Contains GUI system dependent functions

public class WMG_Events : WMG_GUI_Functions {

	// Click events
	// Series Node
	public delegate void WMG_Click_H(WMG_Series aSeries, WMG_Node aNode);
	public event WMG_Click_H WMG_Click;
	
	public void addNodeClickEvent(GameObject go) {
		UIEventListener.Get(go).onClick += WMG_Click_2;
	}
	
	private void WMG_Click_2(GameObject go) {
		if (WMG_Click != null) {
			WMG_Series aSeries = go.transform.parent.parent.GetComponent<WMG_Series>();
			WMG_Click(aSeries, go.GetComponent<WMG_Node>());
		}
	}
	
	// Series Link
	public delegate void WMG_Link_Click_H(WMG_Series aSeries, WMG_Link aLink);
	public event WMG_Link_Click_H WMG_Link_Click;
	
	public void addLinkClickEvent(GameObject go) {
		UIEventListener.Get(go).onClick += WMG_Link_Click_2;
	}
	
	private void WMG_Link_Click_2(GameObject go) {
		if (WMG_Link_Click != null) {
			WMG_Series aSeries = go.transform.parent.parent.GetComponent<WMG_Series>();
			WMG_Link_Click(aSeries, go.GetComponent<WMG_Link>());
		}
	}
	
	// Series Legend Node
	public delegate void WMG_Click_Leg_H(WMG_Series aSeries, WMG_Node aNode);
	public event WMG_Click_Leg_H WMG_Click_Leg;
	
	public void addNodeClickEvent_Leg(GameObject go) {
		UIEventListener.Get(go).onClick += WMG_Click_Leg_2;
	}
	
	private void WMG_Click_Leg_2(GameObject go) {
		if (WMG_Click_Leg != null) {
			WMG_Series aSeries = go.transform.parent.GetComponent<WMG_Legend_Entry>().seriesRef;
			WMG_Click_Leg(aSeries, go.GetComponent<WMG_Node>());
		}
	}
	
	// Series Legend Link
	public delegate void WMG_Link_Click_Leg_H(WMG_Series aSeries, WMG_Link aLink);
	public event WMG_Link_Click_Leg_H WMG_Link_Click_Leg;
	
	public void addLinkClickEvent_Leg(GameObject go) {
		UIEventListener.Get(go).onClick += WMG_Link_Click_Leg_2;
	}
	
	private void WMG_Link_Click_Leg_2(GameObject go) {
		if (WMG_Link_Click_Leg != null) {
			WMG_Series aSeries = go.transform.parent.GetComponent<WMG_Legend_Entry>().seriesRef;
			WMG_Link_Click_Leg(aSeries, go.GetComponent<WMG_Link>());
		}
	}

	// Ring graph
	public delegate void WMG_Ring_Click_H(WMG_Ring ring);
	/// <summary>
	/// Occurs when a ring graph ring / band is clicked. 
	/// @code
	/// graph.WMG_Ring_Click += MyRingClick;
	/// void MyRingClick(WMG_Ring ring, UnityEngine.EventSystems.PointerEventData pointerEventData) {}
	/// @endcode
	/// </summary>
	public event WMG_Ring_Click_H WMG_Ring_Click;
	
	public void addRingClickEvent(GameObject go) {
		UIEventListener.Get(go).onClick += WMG_Ring_Click_2;
	}
	
	private void WMG_Ring_Click_2(GameObject go) {
		if (WMG_Ring_Click != null) {
			WMG_Ring ring = go.transform.parent.GetComponent<WMG_Ring>();
			WMG_Ring_Click(ring);
		}
	}

	// MouseEnter events
	// Series Node
	public delegate void WMG_MouseEnter_H(WMG_Series aSeries, WMG_Node aNode, bool state);
	public event WMG_MouseEnter_H WMG_MouseEnter;
	
	public void addNodeMouseEnterEvent(GameObject go) {
		UIEventListener.Get(go).onHover += WMG_MouseEnter_2;
	}
	
	private void WMG_MouseEnter_2(GameObject go, bool state) {
		if (WMG_MouseEnter != null) {
			WMG_Series aSeries = go.transform.parent.parent.GetComponent<WMG_Series>();
			WMG_MouseEnter(aSeries, go.GetComponent<WMG_Node>(), state);
		}
	}
	
	// Series Link
	public delegate void WMG_Link_MouseEnter_H(WMG_Series aSeries, WMG_Link aLink, bool state);
	public event WMG_Link_MouseEnter_H WMG_Link_MouseEnter;
	
	public void addLinkMouseEnterEvent(GameObject go) {
		UIEventListener.Get(go).onHover += WMG_Link_MouseEnter_2;
	}
	
	private void WMG_Link_MouseEnter_2(GameObject go, bool state) {
		if (WMG_Link_MouseEnter != null) {
			WMG_Series aSeries = go.transform.parent.parent.GetComponent<WMG_Series>();
			WMG_Link_MouseEnter(aSeries, go.GetComponent<WMG_Link>(), state);
		}
	}
	
	// Series Legend Node
	public delegate void WMG_MouseEnter_Leg_H(WMG_Series aSeries, WMG_Node aNode, bool state);
	public event WMG_MouseEnter_Leg_H WMG_MouseEnter_Leg;
	
	public void addNodeMouseEnterEvent_Leg(GameObject go) {
		UIEventListener.Get(go).onHover += WMG_MouseEnter_Leg_2;
	}
	
	private void WMG_MouseEnter_Leg_2(GameObject go, bool state) {
		if (WMG_MouseEnter_Leg != null) {
			WMG_Series aSeries = go.transform.parent.GetComponent<WMG_Legend_Entry>().seriesRef;
			WMG_MouseEnter_Leg(aSeries, go.GetComponent<WMG_Node>(), state);
		}
	}
	
	// Series Legend Link
	public delegate void WMG_Link_MouseEnter_Leg_H(WMG_Series aSeries, WMG_Link aLink, bool state);
	public event WMG_Link_MouseEnter_Leg_H WMG_Link_MouseEnter_Leg;
	
	public void addLinkMouseEnterEvent_Leg(GameObject go) {
		UIEventListener.Get(go).onHover += WMG_Link_MouseEnter_Leg_2;
	}
	
	private void WMG_Link_MouseEnter_Leg_2(GameObject go, bool state) {
		if (WMG_Link_MouseEnter_Leg != null) {
			WMG_Series aSeries = go.transform.parent.GetComponent<WMG_Legend_Entry>().seriesRef;
			WMG_Link_MouseEnter_Leg(aSeries, go.GetComponent<WMG_Link>(), state);
		}
	}

	// Ring graph
	public delegate void WMG_Ring_MouseEnter_H(WMG_Ring ring, bool state);
	/// <summary>
	/// Occurs when a ring graph ring / band is hovered. 
	/// @code
	/// graph.WMG_Ring_MouseEnter += MyRingHover;
	/// void MyRingHover(WMG_Ring ring, bool state) {}
	/// @endcode
	/// </summary>
	public event WMG_Ring_MouseEnter_H WMG_Ring_MouseEnter;
	
	public void addRingMouseEnterEvent(GameObject go) {
		UIEventListener.Get(go).onHover += WMG_Ring_MouseEnter_2;
	}
	
	private void WMG_Ring_MouseEnter_2(GameObject go, bool state) {
		if (WMG_Ring_MouseEnter != null) {
			WMG_Ring ring = go.transform.parent.GetComponent<WMG_Ring>();
			WMG_Ring_MouseEnter(ring, state);
		}
	}

	// MouseLeave events
	// Series Node
	public void addNodeMouseLeaveEvent(GameObject go) {
		
	}
	
	// Series Link
	public void addLinkMouseLeaveEvent(GameObject go) {
		
	}
	
	// Series Legend Node
	public void addNodeMouseLeaveEvent_Leg(GameObject go) {
		
	}
	
	// Series Legend Link
	public void addLinkMouseLeaveEvent_Leg(GameObject go) {
		
	}
}
