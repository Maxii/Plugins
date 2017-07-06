using UnityEngine;
using System.Collections;

public class Split1Attribute : PropertyAttribute {
	public readonly bool shortField;
	public readonly int splitPercent;
	public readonly string tooltip;

	public Split1Attribute () {
		this.shortField = false;
		splitPercent = 50;
		this.tooltip = "";
	}
	public Split1Attribute ( int percent ) {
		this.shortField = false;
		splitPercent = percent;
		this.tooltip = "";
	}
	public Split1Attribute ( string tooltip ) {
		this.shortField = false;
		splitPercent = 50;
		this.tooltip = tooltip;
	}
	public Split1Attribute ( int percent, string tooltip ) {
		this.shortField = false;
		splitPercent = percent;
		this.tooltip = tooltip;
	}
	public Split1Attribute ( bool shortField ) {
		this.shortField = shortField;
		splitPercent = 50;
		this.tooltip = "";
	}
	public Split1Attribute ( bool shortField, int percent ) {
		this.shortField = shortField;
		splitPercent = percent;
		this.tooltip = "";
	}
	public Split1Attribute ( bool shortField, string tooltip ) {
		this.shortField = shortField;
		splitPercent = 50;
		this.tooltip = tooltip;
	}

	public Split1Attribute ( bool shortField, int percent, string tooltip ) {
		this.shortField = shortField;
		splitPercent = percent;
		this.tooltip = tooltip;
	}

}
