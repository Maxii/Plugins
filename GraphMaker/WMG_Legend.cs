using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WMG_Legend : WMG_GUI_Functions {

	public WMG_Graph_Manager theGraph;
	public bool hideLegend;
	public enum legendTypes {Bottom, Right};
	public legendTypes legendType;
	public WMG_Enums.labelTypes labelType;

	public bool showBackground;
	public bool oppositeSideLegend;
	public bool autoCenterLegend;
	public float offsetX;
	public float offsetY;

	public float legendEntryWidth;
	public float legendEntryHeight;

	public int numRowsOrColumns;
	public int numDecimals;

	public float legendEntryLinkSpacing;
	public float legendEntryFontSize = 0.75f;
	public float legendEntrySpacing;

	public float pieSwatchSize;

	public float backgroundPadding;

	public GameObject background;
	public GameObject entriesParent;
	public Object emptyPrefab;
	public List<WMG_Legend_Entry> legendEntries;

	// Original property values for use with dynamic resizing
	public float origLegendEntryWidth { get; private set; }
	public float origLegendEntryHeight { get; private set; }
	public float origLegendEntryLinkSpacing { get; private set; }
	public float origLegendEntryFontSize { get; private set; }
	public float origLegendEntrySpacing { get; private set; }
	public float origPieSwatchSize { get; private set; }
	public float origOffsetX { get; private set; }
	public float origOffsetY { get; private set; }

	// Cache
	private bool cachedHideLegend;
	private legendTypes cachedLegendType;
	private WMG_Enums.labelTypes cachedLabelType;
	private bool cachedShowBackground;
	private bool cachedOppositeSideLegend;
	private bool cachedAutoCenterLegend;
	private float cachedOffsetX;
	private float cachedOffsetY;
	private float cachedLegendEntryWidth;
	private float cachedLegendEntryHeight;
	private int cachedNumRowsOrColumns;
	private int cachedNumDecimals;
	private float cachedLegendEntryLinkSpacing;
	private float cachedLegendEntryFontSize;
	private float cachedLegendEntrySpacing;
	private float cachedPieSwatchSize;
	private float cachedBackgroundPadding;

	// Changed flag
	private bool legendChanged;
	private bool legendTypeChanged;

	public bool LegendChanged() {
		return legendChanged;
	}

	void Start() {
		checkCache(); // Set all cache variables to the current values
		setCacheFlags(true); // Set all cache change flags to true to update everything on start
		legendTypeChanged = false; // Except legend type, since this swaps offset variables
		setOriginalPropertyValues();
	}
	
	public void setOriginalPropertyValues() {
		origLegendEntryWidth = legendEntryWidth;
		origLegendEntryHeight = legendEntryHeight;
		origLegendEntryLinkSpacing = legendEntryLinkSpacing;
		origLegendEntryFontSize = legendEntryFontSize;
		origLegendEntrySpacing = legendEntrySpacing;
		origPieSwatchSize = pieSwatchSize;
		origOffsetX = offsetX;
		origOffsetY = offsetY;
	}

	public void checkCache() {
		theGraph.updateCacheAndFlag<bool>(ref cachedHideLegend, hideLegend, ref legendChanged);
		theGraph.updateCacheAndFlag<legendTypes>(ref cachedLegendType, legendType, ref legendTypeChanged);
		theGraph.updateCacheAndFlag<WMG_Enums.labelTypes>(ref cachedLabelType, labelType, ref legendChanged);
		theGraph.updateCacheAndFlag<bool>(ref cachedShowBackground, showBackground, ref legendChanged);
		theGraph.updateCacheAndFlag<bool>(ref cachedOppositeSideLegend, oppositeSideLegend, ref legendChanged);
		theGraph.updateCacheAndFlag<bool>(ref cachedAutoCenterLegend, autoCenterLegend, ref legendChanged);
		theGraph.updateCacheAndFlag<float>(ref cachedOffsetX, offsetX, ref legendChanged);
		theGraph.updateCacheAndFlag<float>(ref cachedOffsetY, offsetY, ref legendChanged);
		theGraph.updateCacheAndFlag<float>(ref cachedLegendEntryWidth, legendEntryWidth, ref legendChanged);
		theGraph.updateCacheAndFlag<float>(ref cachedLegendEntryHeight, legendEntryHeight, ref legendChanged);
		theGraph.updateCacheAndFlag<int>(ref cachedNumRowsOrColumns, numRowsOrColumns, ref legendChanged);
		theGraph.updateCacheAndFlag<int>(ref cachedNumDecimals, numDecimals, ref legendChanged);
		theGraph.updateCacheAndFlag<float>(ref cachedLegendEntryLinkSpacing, legendEntryLinkSpacing, ref legendChanged);
		theGraph.updateCacheAndFlag<float>(ref cachedLegendEntryFontSize, legendEntryFontSize, ref legendChanged);
		theGraph.updateCacheAndFlag<float>(ref cachedLegendEntrySpacing, legendEntrySpacing, ref legendChanged);
		theGraph.updateCacheAndFlag<float>(ref cachedPieSwatchSize, pieSwatchSize, ref legendChanged);
		theGraph.updateCacheAndFlag<float>(ref cachedBackgroundPadding, backgroundPadding, ref legendChanged);
	}

	public void setCacheFlags(bool val) {
		legendChanged = val;
		legendTypeChanged = val;
	}

	public void setLegendChanged() {
		legendChanged = true;
	}

	// Used for pie graphs
	public WMG_Legend_Entry createLegendEntry(Object prefab) {
		GameObject obj = Instantiate(prefab) as GameObject;
		theGraph.changeSpriteParent(obj, entriesParent);
		WMG_Legend_Entry entry = obj.GetComponent<WMG_Legend_Entry>();
		entry.legend = this;
		legendEntries.Add(entry);
		return entry;
	}

	// Used for other graphs
	public WMG_Legend_Entry createLegendEntry(Object prefab, WMG_Series series, int index) {
		GameObject obj = Instantiate(prefab) as GameObject;
		theGraph.changeSpriteParent(obj, entriesParent);
		WMG_Legend_Entry entry = obj.GetComponent<WMG_Legend_Entry>();
		entry.seriesRef = series;
		entry.legend = this;
		entry.nodeLeft = theGraph.CreateNode(emptyPrefab, obj);
		entry.nodeRight = theGraph.CreateNode(emptyPrefab, obj);
		if (theGraph.isDaikon()) {
			theGraph.changeSpritePivot(entry.nodeLeft, WMG_Graph_Manager.WMGpivotTypes.Center);
			theGraph.changeSpritePivot(entry.nodeRight, WMG_Graph_Manager.WMGpivotTypes.Center);
		}
		legendEntrySpacing = theGraph.getSpritePositionX(entry.label);
		legendEntries.Insert(index, entry);
		return entry;
	}

	public void deleteLegendEntry(int index) {
		DestroyImmediate(legendEntries[index].gameObject);
		legendEntries.RemoveAt(index);
	}

	public void updateLegend() {
		if (legendTypeChanged || legendChanged) {
			if (!hideLegend && showBackground && !theGraph.activeInHierarchy(background)) theGraph.SetActive(background,true);
			if ((hideLegend || !showBackground) && theGraph.activeInHierarchy(background)) theGraph.SetActive(background,false);
			if (!hideLegend && !theGraph.activeInHierarchy(entriesParent)) theGraph.SetActive(entriesParent,true);
			if (hideLegend && theGraph.activeInHierarchy(entriesParent)) theGraph.SetActive(entriesParent,false);
			if (hideLegend) return;

			// Swap parent offsets when changing the legend type
			if (legendTypeChanged) {
				theGraph.SwapVals<float>(ref offsetX, ref offsetY);
			}

			WMG_Axis_Graph axisGraph = theGraph.GetComponent<WMG_Axis_Graph>();
			WMG_Pie_Graph pieGraph = theGraph.GetComponent<WMG_Pie_Graph>();
			float graphY = 0;
			float graphX = 0;
			float maxPointSize = 0;
			if (axisGraph != null) {
				graphY = axisGraph.yAxisLength;
				graphX = axisGraph.xAxisLength;
				maxPointSize = axisGraph.getMaxPointSize();
			}
			if (pieGraph != null) {
				graphY = pieGraph.pieSize + pieGraph.explodeLength;
				graphX = graphY;
				maxPointSize = pieSwatchSize;
			}
			int numEntries = legendEntries.Count;
			for (int j = 0; j < legendEntries.Count; j++) {
				if (!activeInHierarchy(legendEntries[j].gameObject)) numEntries--;
			}
			int maxInRowOrColumn = Mathf.CeilToInt(1f * numEntries / numRowsOrColumns); // Max elements in a row for horizontal legends
			
			float oppositeSideOffset = 0;
			if (legendType == legendTypes.Bottom) {
				if (autoCenterLegend) {
					offsetX = (-maxInRowOrColumn * legendEntryWidth)/2f + legendEntryLinkSpacing + 5;
				}
				if (oppositeSideLegend) {
					oppositeSideOffset = 2*offsetY + graphY;
				}
				changeSpritePositionTo(this.gameObject, new Vector3(graphX / 2 + offsetX, -offsetY + oppositeSideOffset, 0));
			}
			else if (legendType == legendTypes.Right) {
				if (autoCenterLegend) {
					offsetY = (-(maxInRowOrColumn-1) * legendEntryHeight)/2f;
				}
				if (oppositeSideLegend) {
					oppositeSideOffset = -2*offsetX - graphX - legendEntryWidth + 2*legendEntryLinkSpacing;
				}
				changeSpritePositionTo(this.gameObject, new Vector3(graphX + offsetX + oppositeSideOffset, graphY / 2 - offsetY, 0));
			}
			if (pieGraph != null) {
				Vector2 offset = pieGraph.getPaddingOffset();
				changeSpritePositionRelativeToObjBy(this.gameObject, this.gameObject, new Vector3(-graphX/2f - offset.x, -graphY/2f - offset.y));
			}

			int numRows = maxInRowOrColumn;
			int numCols = numRowsOrColumns;

			if (legendType == legendTypes.Right) {
				theGraph.SwapVals<int>(ref numRows, ref numCols);
			}

			changeSpriteWidth(background, Mathf.RoundToInt(legendEntryWidth * numRows + 2*backgroundPadding + legendEntryLinkSpacing));
			changeSpriteHeight(background, Mathf.RoundToInt(legendEntryHeight * numCols + 2*backgroundPadding));
			changeSpritePositionTo(background, new Vector3(-backgroundPadding -legendEntryLinkSpacing -maxPointSize/2f, backgroundPadding + legendEntryHeight/2f));
			
			if (numRowsOrColumns < 1) numRowsOrColumns = 1; // Ensure not less than 1
			if (numRowsOrColumns > numEntries) numRowsOrColumns = numEntries; // Ensure cannot exceed number series 
			
			int extras = 0;
			if (numEntries > 0) {
				extras = numEntries % numRowsOrColumns; // When the number series does not divide evenly by the num rows setting, then this is the number of extras
			}
			int origExtras = extras; // Save the original extras, since we will need to decrement extras in the loop
			int cumulativeOffset = 0; // Used to offset the other dimension, for example, elements moved to a lower row (y), need to also move certain distance (x) left 
			int previousI = 0; // Used to determine when the i (row for horizontal) has changed from the previous i, which is used to increment the cumulative offset
			bool useSmaller = false; // Used to determine whether we need to subtract 1 from maxInRowOrColumn when calculating the cumulative offset 

			if (maxInRowOrColumn == 0) return; // Legend hidden / all entries deactivated

			// Calculate the position of the legend entry for each line series
			for (int j = 0; j < legendEntries.Count; j++) {
				WMG_Legend_Entry legendEntry = legendEntries[j];

				if (axisGraph != null) {
					theGraph.changeSpritePositionRelativeToObjBy(legendEntry.nodeLeft, legendEntry.swatchNode, new Vector3(-legendEntryLinkSpacing, 0, 0));
					theGraph.changeSpritePositionRelativeToObjBy(legendEntry.nodeRight, legendEntry.swatchNode, new Vector3(legendEntryLinkSpacing, 0, 0));
					
					WMG_Link theLine = legendEntry.line.GetComponent<WMG_Link>();
					theLine.Reposition();
				}
				else {
					changeSpriteWidth(legendEntry.swatchNode, Mathf.RoundToInt(pieSwatchSize));
					changeSpriteHeight(legendEntry.swatchNode, Mathf.RoundToInt(pieSwatchSize));
				}

				theGraph.changeSpritePositionToX(legendEntry.label, legendEntrySpacing);

				// Legend text
				if (axisGraph != null) {
					string theText = legendEntry.seriesRef.seriesName;
					
					if (labelType == WMG_Enums.labelTypes.None) {
						theText = "";
					}
					changeLabelText(legendEntry.label, theText);
				}
				legendEntry.label.transform.localScale = new Vector3(legendEntryFontSize, legendEntryFontSize, 1);

				// i is the row for horizontal legends, and the column for vertical
				int i = Mathf.FloorToInt(j / maxInRowOrColumn);
				if (origExtras > 0) {
					i = Mathf.FloorToInt((j + 1) / maxInRowOrColumn);
				}
				
				// If there were extras, but no longer any more extras, then need to subtract 1 from the maxInRowOrColumn, and recalculate i
				if (extras == 0 && origExtras > 0) {
					i = origExtras + Mathf.FloorToInt((j - origExtras * maxInRowOrColumn)/ (maxInRowOrColumn - 1));
					if ((j - origExtras * maxInRowOrColumn) > 0) useSmaller = true;
				}
				
				// When there are extras decrease i for the last element in the row
				if (extras > 0) {
					if ((j + 1) % maxInRowOrColumn == 0) {
						extras--;
						i--;
					}
				}
				
				// Increment cumulative offset when i changes, use offset to position other dimension correctly.
				if (previousI != i) {
					previousI = i;
					if (useSmaller) {
						cumulativeOffset += (maxInRowOrColumn - 1);
					}
					else {
						cumulativeOffset += maxInRowOrColumn;
					}
				}
				
				// Set the position based on the series index (j), i (row index for horizontal), and cumulative offset
				if (legendType == legendTypes.Bottom) {
					theGraph.changeSpritePositionTo(legendEntry.gameObject, new Vector3(j * legendEntryWidth - legendEntryWidth * cumulativeOffset, -i * legendEntryHeight, 0));
				}
				else if (legendType == legendTypes.Right) {
					theGraph.changeSpritePositionTo(legendEntry.gameObject, new Vector3(i * legendEntryWidth, -j * legendEntryHeight + legendEntryHeight * cumulativeOffset, 0));
				}
			}
		}
	}

}
