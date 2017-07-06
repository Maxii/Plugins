using UnityEngine;
using System.Collections;

public class Split2Attribute : PropertyAttribute {
	public readonly bool shortField;
	public readonly int splitPercent;
	public readonly string tooltip;
	public readonly float height;

	float defaultHeight = 18f;

	public Split2Attribute () {
		this.shortField = false;
		splitPercent = 50;
		this.tooltip = "";
		height = defaultHeight;
	}
	public Split2Attribute ( int percent ) {
		this.shortField = false;
		splitPercent = percent;
		this.tooltip = "";
		height = defaultHeight;
	}
	public Split2Attribute ( string tooltip ) {
		this.shortField = false;
		splitPercent = 50;
		this.tooltip = tooltip;
		height = defaultHeight;
	}
	public Split2Attribute ( int percent, string tooltip ) {
		this.shortField = false;
		splitPercent = percent;
		this.tooltip = tooltip;
		height = defaultHeight;
	}
	public Split2Attribute ( bool shortField ) {
		this.shortField = shortField;
		splitPercent = 50;
		this.tooltip = "";
		height = defaultHeight;
	}
	public Split2Attribute ( bool shortField, int percent ) {
		this.shortField = shortField;
		splitPercent = percent;
		this.tooltip = "";
		height = defaultHeight;
	}
	public Split2Attribute ( bool shortField, int percent, string tooltip ) {
		this.shortField = shortField;
		splitPercent = percent;
		this.tooltip = tooltip;
		height = defaultHeight;
	}
	public Split2Attribute ( bool shortField, string tooltip ) {
		this.shortField = shortField;
		splitPercent = 50;
		this.tooltip = tooltip;
		height = defaultHeight;
	}

	public Split2Attribute ( float myHeight ) {
		this.shortField = false;
		splitPercent = 50;
		this.tooltip = "";
		height = myHeight;
	}
	public Split2Attribute ( int percent, float myHeight ) {
		this.shortField = false;
		splitPercent = percent;
		this.tooltip = "";
		height = myHeight;
	}
	public Split2Attribute ( string tooltip, float myHeight ) {
		this.shortField = false;
		splitPercent = 50;
		this.tooltip = tooltip;
		height = myHeight;
	}
	public Split2Attribute ( int percent, string tooltip, float myHeight ) {
		this.shortField = false;
		splitPercent = percent;
		this.tooltip = tooltip;
		height = myHeight;
	}
	public Split2Attribute ( bool shortField, float myHeight ) {
		this.shortField = shortField;
		splitPercent = 50;
		this.tooltip = "";
		height = myHeight;
	}
	public Split2Attribute ( bool shortField, string tooltip, float myHeight ) {
		this.shortField = shortField;
		splitPercent = 50;
		this.tooltip = tooltip;
		height = myHeight;
	}
	public Split2Attribute ( bool shortField, int percent, float myHeight ) {
		this.shortField = shortField;
		splitPercent = percent;
		this.tooltip = "";
		height = myHeight;
	}
	public Split2Attribute ( bool shortField, int percent, string tooltip, float myHeight ) {
		this.shortField = shortField;
		splitPercent = percent;
		this.tooltip = tooltip;
		height = myHeight;
	}

}
