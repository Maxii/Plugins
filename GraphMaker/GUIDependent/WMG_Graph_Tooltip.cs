using UnityEngine;
using System.Collections;

// Contains GUI system dependent functions

public class WMG_Graph_Tooltip : WMG_GUI_Functions {
	
	public WMG_Axis_Graph theGraph;
	
	void LateUpdate () {
		if (theGraph.tooltipEnabled) {
			if (isTooltipObjectNull()) return;
			if(getControlVisibility(theGraph.toolTipPanel)) {
				repositionTooltip();
			}
		}
	}
	
	public void subscribeToEvents(bool val) {
		if (val) {
			theGraph.WMG_MouseEnter += TooltipNodeMouseEnter;
			theGraph.WMG_MouseEnter_Leg += TooltipLegendNodeMouseEnter;
			theGraph.WMG_Link_MouseEnter_Leg += TooltipLegendLinkMouseEnter;
		}
		else {
			theGraph.WMG_MouseEnter -= TooltipNodeMouseEnter;
			theGraph.WMG_MouseEnter_Leg -= TooltipLegendNodeMouseEnter;
			theGraph.WMG_Link_MouseEnter_Leg -= TooltipLegendLinkMouseEnter;
		}
	}
	
	private bool isTooltipObjectNull() {
		if (theGraph.toolTipPanel == null) return true;
		if (theGraph.toolTipLabel == null) return true;
		return false;
	}
	
	private void repositionTooltip() {
		// This is called continuously during update if control is visible, and also once before shown visible so tooltip doesn't appear to jump positions
		// Convert position from "screen coordinates" to "gui coordinates"
		Vector3 position = UICamera.currentCamera.ViewportToWorldPoint(new Vector3(	Mathf.Clamp01(Input.mousePosition.x / Screen.width), 
																					Mathf.Clamp01(Input.mousePosition.y / Screen.height),0));
		// Without an offset, the tooltip's top left corner will be at the cursor position
		float offsetX = theGraph.tooltipOffset.x;
		float offsetY = theGraph.getSpriteHeight(theGraph.toolTipPanel) / 2 + theGraph.tooltipOffset.y;
		// Center the control on the mouse/touch
		theGraph.toolTipPanel.transform.position = position;
		theGraph.toolTipPanel.transform.localPosition = theGraph.toolTipPanel.transform.localPosition + new Vector3( offsetX, offsetY, 0);
	}
	
	private void TooltipNodeMouseEnter(WMG_Series aSeries, WMG_Node aNode, bool state) {
		if (isTooltipObjectNull()) return;
		if (state) {
			// Find out what point value data is for this node
			Vector2 nodeData = aSeries.getNodeValue(aNode);
			float numberToMult = Mathf.Pow(10f, theGraph.tooltipNumberDecimals);
			string nodeX = (Mathf.Round(nodeData.x*numberToMult)/numberToMult).ToString();
			string nodeY = (Mathf.Round(nodeData.y*numberToMult)/numberToMult).ToString();
			
			// Determine the tooltip to display and set the text
			string textToSet;
			if (theGraph.graphType != WMG_Axis_Graph.graphTypes.line) {
				textToSet = nodeY;
			}
			else {
				textToSet = "(" + nodeX + ", " + nodeY + ")";
			}
			if (theGraph.tooltipDisplaySeriesName) {
				textToSet = aSeries.seriesName + ": " + textToSet;
			}
			changeLabelText(theGraph.toolTipLabel, textToSet);
			
			// Resize this control to match the size of the contents
			changeSpriteWidth(theGraph.toolTipPanel, Mathf.RoundToInt(getSpriteWidth(theGraph.toolTipLabel)) + 24);
			
			// Ensure tooltip is in position before showing it so it doesn't appear to jump
			repositionTooltip();
			
			// Display the base panel
			showControl(theGraph.toolTipPanel);
			bringSpriteToFront(theGraph.toolTipPanel);
			
			Vector3 newVec = new Vector3(2,2,1);
			if (theGraph.graphType != WMG_Axis_Graph.graphTypes.line) {
				if (theGraph.orientationType == WMG_Axis_Graph.orientationTypes.vertical) {
					newVec = new Vector3(1,1.1f,1);
				}
				else {
					newVec = new Vector3(1.1f,1,1);
				}
			}
			
			performTooltipAnimation(aNode.transform, newVec);
		}
		else {
			hideControl(theGraph.toolTipPanel);
			sendSpriteToBack(theGraph.toolTipPanel);
			
			performTooltipAnimation(aNode.transform, new Vector3(1,1,1));
		}
	}
	
	private void TooltipLegendNodeMouseEnter(WMG_Series aSeries, WMG_Node aNode, bool state) {
		if (isTooltipObjectNull()) return;
		if (state) {
			// Set the text
			changeLabelText(theGraph.toolTipLabel, aSeries.seriesName);
			
			// Resize this control to match the size of the contents
			changeSpriteWidth(theGraph.toolTipPanel, Mathf.RoundToInt(getSpriteWidth(theGraph.toolTipLabel)) + 24);
			
			// Ensure tooltip is in position before showing it so it doesn't appear to jump
			repositionTooltip();
			
			// Display the base panel
			showControl(theGraph.toolTipPanel);
			bringSpriteToFront(theGraph.toolTipPanel);
			
			performTooltipAnimation(aNode.transform, new Vector3(2,2,1));
		}
		else {
			hideControl(theGraph.toolTipPanel);
			sendSpriteToBack(theGraph.toolTipPanel);
			
			performTooltipAnimation(aNode.transform, new Vector3(1,1,1));
		}
	}
	
	private void TooltipLegendLinkMouseEnter(WMG_Series aSeries, WMG_Link aLink, bool state) {
		if (isTooltipObjectNull()) return;
		if (!aSeries.hidePoints) return;
		if (state) {
			// Set the text
			changeLabelText(theGraph.toolTipLabel, aSeries.seriesName);
			
			// Resize this control to match the size of the contents
			changeSpriteWidth(theGraph.toolTipPanel, Mathf.RoundToInt(getSpriteWidth(theGraph.toolTipLabel)) + 24);
			
			// Ensure tooltip is in position before showing it so it doesn't appear to jump
			repositionTooltip();
			
			// Display the base panel
			showControl(theGraph.toolTipPanel);
			bringSpriteToFront(theGraph.toolTipPanel);
			
			performTooltipAnimation(aLink.transform, new Vector3(2,1.05f,1));
		}
		else {
			hideControl(theGraph.toolTipPanel);
			sendSpriteToBack(theGraph.toolTipPanel);
			
			performTooltipAnimation(aLink.transform, new Vector3(1,1,1));
		}
	}
	
	private void performTooltipAnimation (Transform trans, Vector3 newScale) {
		if (theGraph.tooltipAnimationsEnabled) {
			WMG_Anim.animScale(trans.gameObject, theGraph.tooltipAnimationsDuration, theGraph.tooltipAnimationsEasetype, newScale, 0);
		}
	}
}
