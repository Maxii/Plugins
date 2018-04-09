using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class WMG_Series : MonoBehaviour {

	// Core parameters
	public WMG_Axis_Graph theGraph;
	public GameObject linkParent;
	public GameObject nodeParent;
	public int pointPrefab;
	public int linkPrefab;
	
	public List<Vector2> pointValues;
	public bool UseXDistBetweenToSpace;
	public bool AutoUpdateXDistBetween;
	public float xDistBetweenPoints;
	public float extraXSpace;
	public bool hidePoints;
	public bool hideLines;
	public bool connectFirstToLast;
	public string seriesName;
	
	// Display related
	public float lineScale;
	public float linePadding;
	public float pointWidthHeight;
	public Color lineColor;
	public Color pointColor;
	
	// Data labels
	public GameObject dataLabelsParent;
	public UnityEngine.Object dataLabelPrefab;
	public bool dataLabelsEnabled;
	public int dataLabelsNumDecimals;
	public float dataLabelsFontSize = 0.6f;
	public Vector2 dataLabelsOffset;
	
	// Area Shading
	public enum areaShadingTypes {None, Solid, Gradient};
	public areaShadingTypes areaShadingType;
	public Material areaShadingMatSolid;
	public Material areaShadingMatGradient;
	public GameObject areaShadingParent;
	public UnityEngine.Object areaShadingPrefab;
	public Color areaShadingColor;
	public float areaShadingAxisValue;
	
	// Legend info
	public UnityEngine.Object legendEntryPrefab;
	public WMG_Legend_Entry legendEntry;

	// Data sources
	public WMG_Data_Source realTimeDataSource;
	public WMG_Data_Source pointValuesDataSource;

	// Private vars
	private UnityEngine.Object nodePrefab;
	private List<GameObject> points = new List<GameObject>();
	private List<GameObject> lines = new List<GameObject>();
	private List<GameObject> areaShadingRects = new List<GameObject>();
	private List<GameObject> dataLabels = new List<GameObject>();
	private List<bool> barIsNegative = new List<bool>();

	// Original property values for use with dynamic resizing
	public float origPointWidthHeight { get; private set; }
	public float origLineScale { get; private set; }
	public float origDataLabelsFontSize { get; private set; }
	
	// Cache
	private WMG_Axis_Graph.graphTypes cachedSeriesType;
	private List<Vector2> cachedPointValues = new List<Vector2>();
	private bool cachedHidePoints;
	private bool cachedHideLines;
	private bool cachedConnectFirstToLast;
	private Color cachedLineColor;
	private Color cachedPointColor;
	private float cachedLineScale;
	private float cachedPointWidthHeight;
	private bool cachedUseXDistBetweenToSpace;
	private bool cachedAutoUpdateXDistBetween;
	private float cachedXDistBetweenPoints;
	private float cachedExtraXSpace;
	private int cachedPointPrefab;
	private int cachedLinkPrefab;
	private string cachedSeriesName;
	private float cachedLinePadding;
	private bool cachedDataLabelsEnabled;
	private int cachedDataLabelsNumDecimals;
	private float cachedDataLabelsFontSize;
	private Vector2 cachedDataLabelsOffset;
	private Color cachedAreaShadingColor;
	private float cachedAreaShadingAxisValue;
	private areaShadingTypes cachedAreaShadingType;
	
	// Changed Flags
	private bool pointValuesChanged;
	private bool hidePointsChanged;
	private bool hideLinesChanged;
	private bool lineColorChanged;
	private bool pointColorChanged;
	private bool lineScaleChanged;
	private bool pointWidthHeightChanged;
	private bool prefabChanged;
	private bool seriesNameChanged;
	private bool linePaddingChanged;
	private bool areaShadingTypeChanged;
	private bool areaShadingChanged;
	private bool dataLabelsChanged;
	
	// Real time update
	private bool realTimeRunning;
	private float realTimeLoopVar;
	private float realTimeOrigMax;
	
	// Automatic Animation variables and functions
	private bool animatingFromPreviousData;
	private List<Vector2> afterPositions = new List<Vector2>();
	private List<int> afterWidths = new List<int>();
	private List<int> afterHeights = new List<int>();
	
	public delegate void SeriesDataChangedHandler(WMG_Series aSeries);
	public event SeriesDataChangedHandler SeriesDataChanged;
	
	protected virtual void OnSeriesDataChanged() {
		SeriesDataChangedHandler handler = SeriesDataChanged;
		if (handler != null) {
			handler(this);
		}
	}

	public void UpdateFromDataSource() {
		if (pointValuesDataSource != null) {
			pointValues = pointValuesDataSource.getData<Vector2>();
			// sanitizeGroupData can change pointValues, call it here otherwise pointValuesChanged could be true every frame
			if (theGraph.useGroups) {
				sanitizeGroupData();
			}
		}
	}
	
	public List<Vector2> AfterPositions() {
		return afterPositions;
	}
	
	public List<int> AfterHeights() {
		return afterHeights;
	}
	
	public List<int> AfterWidths() {
		return afterWidths;
	}
	
	public bool AnimatingFromPreviousData() {
		return animatingFromPreviousData;
	}
	
	public void setAnimatingFromPreviousData() {
		// Automatic animations doesn't work for real time updating or stacked bar graphs
		if (realTimeRunning) return;
		if (theGraph.graphType == WMG_Axis_Graph.graphTypes.bar_stacked || theGraph.graphType == WMG_Axis_Graph.graphTypes.bar_stacked_percent) return;
		if (theGraph.autoAnimationsEnabled) {
			animatingFromPreviousData = true;
		}
	}

	void Start () {
		setOriginalPropertyValues();
	}

	public void createLegendEntry(int index) {
		legendEntry = theGraph.legend.createLegendEntry(legendEntryPrefab, this, index);
	}

	// Set initial property values for use with percentage based dynamic resizing 
	public void setOriginalPropertyValues() {
		origPointWidthHeight = pointWidthHeight;
		origLineScale = lineScale;
		origDataLabelsFontSize = dataLabelsFontSize;
	}
	
	public List<GameObject> getPoints() {
		return points;
	}
	
	public List<GameObject> getLines() {
		return lines;
	}
	
	public List<GameObject> getDataLabels() {
		return dataLabels;
	}
	
	public bool getBarIsNegative(int i) {
		return barIsNegative[i];
	}
	
	// Get Vector2 associated with a node in this series
	public Vector2 getNodeValue(WMG_Node aNode) {
		for (int i = 0; i < pointValues.Count; i++) { // TODO improve performance by mapping IDs to Vector2
			if (points[i].GetComponent<WMG_Node>() == aNode) return pointValues[i];
		}
		return Vector2.zero;
	}
	
	private void setAllChanged() {
		pointValuesChanged = true;
		hideLinesChanged = true;
		hidePointsChanged = true;
		lineColorChanged = true;
		pointColorChanged = true;
		lineScaleChanged = true;
		pointWidthHeightChanged = true;
		linePaddingChanged = true;
		areaShadingTypeChanged = true;
		areaShadingChanged = true;
		dataLabelsChanged = true;
		// Need to tell other series to point value changed if stacked bar or stacked bar percent graph
		if (theGraph.graphType == WMG_Axis_Graph.graphTypes.bar_stacked || theGraph.graphType == WMG_Axis_Graph.graphTypes.bar_stacked_percent) {
			for (int j = 0; j < theGraph.lineSeries.Count; j++) {
				if (!theGraph.activeInHierarchy(theGraph.lineSeries[j])) continue;
				WMG_Series theSeries = theGraph.lineSeries[j].GetComponent<WMG_Series>();
				if (theSeries.getPointValuesChanged()) continue;
				theSeries.setPointValuesChanged(true);
			}
		}
	}
	
	public void setPointValuesChanged(bool val) {
		pointValuesChanged = val;
	}
	
	public bool getPointValuesChanged() {
		return pointValuesChanged;
	}
	
	public void checkCache() {
		// Point Values
		if (cachedPointValues.Count != pointValues.Count) {
			cachedPointValues = new List<Vector2>(pointValues);
			setAllChanged();
		}
		else {
			// Vast majority of update loops will need to loop through all the point values and check cache, should be possible to improve performance here
			for (int i = 0; i < pointValues.Count; i++) {
				if (!Mathf.Approximately(pointValues[i].x, cachedPointValues[i].x) || !Mathf.Approximately(pointValues[i].y, cachedPointValues[i].y)) {
					cachedPointValues = new List<Vector2>(pointValues);
					setAllChanged();
					setAnimatingFromPreviousData();
					break;
				}
			}
		}
		// x dist between variables
		theGraph.updateCacheAndFlag<float>(ref cachedXDistBetweenPoints, xDistBetweenPoints, ref pointValuesChanged);
		theGraph.updateCacheAndFlag<bool>(ref cachedUseXDistBetweenToSpace, UseXDistBetweenToSpace, ref pointValuesChanged);
		theGraph.updateCacheAndFlag<bool>(ref cachedAutoUpdateXDistBetween, AutoUpdateXDistBetween, ref pointValuesChanged);
		theGraph.updateCacheAndFlag<float>(ref cachedExtraXSpace, extraXSpace, ref pointValuesChanged);
		// connect first to last
		if (cachedConnectFirstToLast != connectFirstToLast) {
			cachedConnectFirstToLast = connectFirstToLast;
			setAllChanged();
		}
		// Area shading
		theGraph.updateCacheAndFlag<areaShadingTypes>(ref cachedAreaShadingType, areaShadingType, ref areaShadingTypeChanged);
		theGraph.updateCacheAndFlag<Color>(ref cachedAreaShadingColor, areaShadingColor, ref areaShadingChanged);
		theGraph.updateCacheAndFlag<float>(ref cachedAreaShadingAxisValue, areaShadingAxisValue, ref areaShadingChanged);
		// Data labels
		theGraph.updateCacheAndFlag<bool>(ref cachedDataLabelsEnabled, dataLabelsEnabled, ref dataLabelsChanged);
		theGraph.updateCacheAndFlag<int>(ref cachedDataLabelsNumDecimals, dataLabelsNumDecimals, ref dataLabelsChanged);
		theGraph.updateCacheAndFlag<float>(ref cachedDataLabelsFontSize, dataLabelsFontSize, ref dataLabelsChanged);
		theGraph.updateCacheAndFlag<Vector2>(ref cachedDataLabelsOffset, dataLabelsOffset, ref dataLabelsChanged);
		// Others
		theGraph.updateCacheAndFlag<bool>(ref cachedHidePoints, hidePoints, ref hidePointsChanged);
		theGraph.updateCacheAndFlag<bool>(ref cachedHideLines, hideLines, ref hideLinesChanged);
		theGraph.updateCacheAndFlag<Color>(ref cachedLineColor, lineColor, ref lineColorChanged);
		theGraph.updateCacheAndFlag<Color>(ref cachedPointColor, pointColor, ref pointColorChanged);
		theGraph.updateCacheAndFlag<float>(ref cachedLineScale, lineScale, ref lineScaleChanged);
		theGraph.updateCacheAndFlag<float>(ref cachedPointWidthHeight, pointWidthHeight, ref pointWidthHeightChanged);
		
		theGraph.updateCacheAndFlag<int>(ref cachedPointPrefab, pointPrefab, ref prefabChanged);
		theGraph.updateCacheAndFlag<int>(ref cachedLinkPrefab, linkPrefab, ref prefabChanged);
		theGraph.updateCacheAndFlag<WMG_Axis_Graph.graphTypes>(ref cachedSeriesType, theGraph.graphType, ref prefabChanged);
		if (prefabChanged) {
			setAllChanged();
		}
		theGraph.updateCacheAndFlag<string>(ref cachedSeriesName, seriesName, ref seriesNameChanged);
		theGraph.updateCacheAndFlag<float>(ref cachedLinePadding, linePadding, ref linePaddingChanged);
	}
	
	public void setCacheFlags(bool val) {
		pointValuesChanged = val;
		hidePointsChanged = val;
		hideLinesChanged = val;
		lineColorChanged = val;
		pointColorChanged = val;
		lineScaleChanged = val;
		pointWidthHeightChanged = val;
		prefabChanged = val;
		seriesNameChanged = val;
		linePaddingChanged = val;
		areaShadingChanged = val;
		areaShadingTypeChanged = val;
		dataLabelsChanged = val;
	}

	public void UpdateHidePoints() {
		if (hidePointsChanged) {
			// Series points
			for (int i = 0; i < points.Count; i++) {
				theGraph.SetActive(points[i],!hidePoints);
			}
			// Legend point
			theGraph.SetActive(legendEntry.swatchNode, !hidePoints);
			StartCoroutine(SetDelayedAreaShadingChanged ());
		}
		// For null groups hide the appropriate points
		if (theGraph.useGroups && pointValuesChanged) {
			for (int i = 0; i < points.Count; i++) {
				theGraph.SetActive(points[i], pointValues[i].x > 0);
			}
			StartCoroutine(SetDelayedAreaShadingChanged ());
		}
	}
	
	public void UpdateHideLines() {
		if (hideLinesChanged) {
			// Series lines
			for (int i = 0; i < lines.Count; i++) {
				if (hideLines || theGraph.graphType != WMG_Axis_Graph.graphTypes.line) theGraph.SetActive(lines[i],false);
				else theGraph.SetActive(lines[i],true);
			}
			// Legend lines
			if (hideLines || theGraph.graphType != WMG_Axis_Graph.graphTypes.line) {
				theGraph.SetActive(legendEntry.line, false);
			}
			else {
				theGraph.SetActive(legendEntry.line, true);
			}
			StartCoroutine(SetDelayedAreaShadingChanged ());
		}
		// For null groups hide the appropriate lines
		if (theGraph.useGroups && pointValuesChanged && theGraph.graphType == WMG_Axis_Graph.graphTypes.line) {
			for (int i = 0; i < lines.Count; i++) {
				theGraph.SetActive(lines[i],true);
			}
			for (int i = 0; i < points.Count; i++) {
				if (pointValues[i].x < 0) {
					WMG_Node thePoint = points[i].GetComponent<WMG_Node>();
					for (int j = 0; j < thePoint.links.Count; j++) {
						theGraph.SetActive(thePoint.links[j], false);
					}
				}
			}
			StartCoroutine(SetDelayedAreaShadingChanged ());
		}
	}
	
	public void UpdateLineColor() {
		if (lineColorChanged) {
			// Series line colors
			for (int i = 0; i < lines.Count; i++) {
				WMG_Link theLine = lines[i].GetComponent<WMG_Link>();
				theGraph.changeSpriteColor(theLine.objectToColor, lineColor);
			}
			// Legend line colors
			WMG_Link legendLine = legendEntry.line.GetComponent<WMG_Link>();
			theGraph.changeSpriteColor(legendLine.objectToColor, lineColor);
		}
	}
	
	public void UpdatePointColor() {
		if (pointColorChanged) {
			// Series point colors
			for (int i = 0; i < points.Count; i++) {
				WMG_Node thePoint = points[i].GetComponent<WMG_Node>();
				theGraph.changeSpriteColor(thePoint.objectToColor, pointColor);
			}
			// Legend point color
			WMG_Node legendPoint = legendEntry.swatchNode.GetComponent<WMG_Node>();
			theGraph.changeSpriteColor(legendPoint.objectToColor, pointColor);
		}
	}
	
	public void UpdateLineScale() {
		if (lineScaleChanged) {
			// Series line widths
			for (int i = 0; i < lines.Count; i++) {
				WMG_Link theLine = lines[i].GetComponent<WMG_Link>();
				theLine.objectToScale.transform.localScale = new Vector3(lineScale, theLine.objectToScale.transform.localScale.y, theLine.objectToScale.transform.localScale.z);
			}
			// Legend line widths
			WMG_Link legendLine = legendEntry.line.GetComponent<WMG_Link>();
			legendLine.objectToScale.transform.localScale = new Vector3(lineScale, legendLine.objectToScale.transform.localScale.y, legendLine.objectToScale.transform.localScale.z);
		}
	}
	
	public void UpdatePointWidthHeight() {
		if (pointWidthHeightChanged) {
			// Series line point dimensions
			if (theGraph.graphType == WMG_Axis_Graph.graphTypes.line) {
				for (int i = 0; i < points.Count; i++) {
					WMG_Node thePoint = points[i].GetComponent<WMG_Node>();
					theGraph.changeSpriteHeight(thePoint.objectToColor, Mathf.RoundToInt(pointWidthHeight));
					theGraph.changeSpriteWidth(thePoint.objectToColor, Mathf.RoundToInt(pointWidthHeight));
				}
			}
			// Legend point / bar dimensions
			WMG_Node legendPoint = legendEntry.swatchNode.GetComponent<WMG_Node>();
			theGraph.changeSpriteHeight(legendPoint.objectToColor, Mathf.RoundToInt(pointWidthHeight));
			theGraph.changeSpriteWidth(legendPoint.objectToColor, Mathf.RoundToInt(pointWidthHeight));
			// Legend empty objects to get line to center correctly
			if (theGraph.isDaikon()) {
				theGraph.changeSpriteHeight(legendEntry.nodeLeft, Mathf.RoundToInt(pointWidthHeight));
				theGraph.changeSpriteWidth(legendEntry.nodeLeft, Mathf.RoundToInt(pointWidthHeight));
				theGraph.changeSpriteHeight(legendEntry.nodeRight, Mathf.RoundToInt(pointWidthHeight));
				theGraph.changeSpriteWidth(legendEntry.nodeRight, Mathf.RoundToInt(pointWidthHeight));
			}
		}
	}
	
	public void UpdatePrefabType() {
		if (prefabChanged) {
			// Update prefab variable used later in the creating sprites function
			if (theGraph.graphType == WMG_Axis_Graph.graphTypes.line) {
				nodePrefab = theGraph.pointPrefabs[pointPrefab];
			}
			else {
				nodePrefab = theGraph.barPrefab;
			}
			
			// Delete points and lines
			for (int i = points.Count - 1; i >= 0; i--) {
				if (points[i] != null) {
					WMG_Node thePoint = points[i].GetComponent<WMG_Node>();
					foreach (GameObject child in thePoint.links) {
						lines.Remove(child);
					}
					theGraph.DeleteNode(thePoint);
					points.RemoveAt(i);
				}
			}
			// Delete legend
			if (legendEntry.swatchNode != null) {
				theGraph.DeleteNode(legendEntry.swatchNode.GetComponent<WMG_Node>());
				theGraph.DeleteLink(legendEntry.line.GetComponent<WMG_Link>());
			}
		}
	}
	
	public void UpdateSeriesName() {
		if (seriesNameChanged) {
			theGraph.legend.setLegendChanged();
		}
	}
	
	public void UpdateLinePadding() {
		if (linePaddingChanged) {
			for (int i = 0; i < points.Count; i++) {
				points[i].GetComponent<WMG_Node>().radius = -1 * linePadding;
			}
			for (int i = 0; i < lines.Count; i++) {
				lines[i].GetComponent<WMG_Link>().Reposition();
			}
		}
	}

	public void RealTimeUpdate() {
		if (realTimeRunning) {
			DoRealTimeUpdate();
		}
	}

	public void CreateOrDeleteSpritesBasedOnPointValues() {
		if (pointValuesChanged) {
			if (theGraph.useGroups) {
				sanitizeGroupData();
			}
		}

		int pointValuesCount = pointValues.Count;

		if (pointValuesChanged) {
			createOrDeletePoints(pointValuesCount);
		}

		if (pointValuesChanged || dataLabelsChanged) {
			createOrDeleteLabels(pointValuesCount);
		}
		
		if (pointValuesChanged || areaShadingTypeChanged) {
			createOrDeleteAreaShading(pointValuesCount);
		}
	}

	void sanitizeGroupData() {
		// Groups are defined at the graph level in the groups variable.
		// If, for example, there are 5 groups defined, then the data in the series must comprise of 5 Vector2's
		// The x value in each Vector2 represents the group, and a negative x value represents a null group.
		// Null groups will not be graphed at all (for example line graph with broken line segments)
		// This function will automatically group together data and insert nulls as needed.
		// For example, for 3 groups, if you supply input of (2,3) (2,5), this will convert it to (-1,0) (2,8) (-3,0)

		// remove values that can't possibly represent groups
		for (int i = pointValues.Count - 1; i >= 0; i--) {
			int intVal = Mathf.RoundToInt(pointValues[i].x);
			if (intVal - pointValues[i].x != 0) {
				pointValues.RemoveAt(i); // Not an integer
				continue;
			}
			if (Mathf.Abs(intVal) > theGraph.groups.Count) {
				pointValues.RemoveAt(i); // Out of bounds
				continue;
			}
			if (intVal == 0) {
				pointValues.RemoveAt(i); // 0, because nulls are represented by negatives and there is no negative 0
				continue;
			}
		}

		// sort values, combine duplicates
		pointValues.Sort( (vec1,vec2)=>vec1.x.CompareTo(vec2.x));
		List<Vector2> newPoints = new List<Vector2>();
		bool newPoint = true;
		for (int i = 0; i < pointValues.Count; i++) {
			if (newPoint) {
				newPoints.Add(pointValues[i]);
				newPoint = false;
			}
			else {
				Vector2 prev = newPoints[newPoints.Count-1];
				newPoints[newPoints.Count-1] = new Vector2(prev.x, prev.y + pointValues[i].y);
			}

			if (i < pointValues.Count-1) {
				if (pointValues[i].x != pointValues[i+1].x) {
					newPoint = true;
				}
			}
		}

		// insert nulls
		if (newPoints.Count < theGraph.groups.Count) {
			int numNullsToAdd = theGraph.groups.Count - newPoints.Count;
			for (int i = 0; i < numNullsToAdd; i++) {
				newPoints.Insert(0, new Vector2(-1, 0));
			}
		}

		// this is rare, but there could be extras nulls (negatives), remove them until the counts are equal
		if (newPoints.Count > theGraph.groups.Count) {
			int numNullsToRemove = newPoints.Count - theGraph.groups.Count;
			for (int i = 0; i < numNullsToRemove; i++) {
				newPoints.RemoveAt(0);
			}
		}

		// at this point, we should have for example, if 5 groups, -1, -1, 1, 2, 5
		// now to easily determine which groups are null, need something like 1, 2, -3, -4, 5
		List<int> nullGroups = new List<int>();
		for (int i = 0; i < theGraph.groups.Count; i++) {
			nullGroups.Add(i+1);
		}
		for (int i = newPoints.Count - 1; i >= 0; i--) {
			if (newPoints[i].x > 0) nullGroups.Remove(Mathf.RoundToInt(newPoints[i].x));
		}
		for (int i = 0; i < nullGroups.Count; i++) {
			newPoints[i] = new Vector2(-1*nullGroups[i], 0);
		}

		// sort values, so that negatives treated same as positives
		newPoints.Sort( (vec1,vec2)=>Mathf.Abs(vec1.x).CompareTo(Mathf.Abs(vec2.x)));

		pointValues = newPoints;
	}

	void createOrDeletePoints(int pointValuesCount) {
		// Create points based on pointValues data
		for (int i = 0; i < pointValuesCount; i++) {
			if (points.Count <= i) {
				GameObject curObj = theGraph.CreateNode(nodePrefab, nodeParent);
				
				theGraph.addNodeClickEvent(curObj);
				theGraph.addNodeMouseEnterEvent(curObj);
				theGraph.addNodeMouseLeaveEvent(curObj);
				
				curObj.GetComponent<WMG_Node>().radius = -1 * linePadding;
				theGraph.SetActive(curObj,false);
				points.Add(curObj);
				barIsNegative.Add(false);
				if (i > 0) {
					WMG_Node fromNode = points[i-1].GetComponent<WMG_Node>();
					curObj = theGraph.CreateLink(fromNode, curObj, theGraph.linkPrefabs[linkPrefab], linkParent);
					
					theGraph.addLinkClickEvent(curObj);
					theGraph.addLinkMouseEnterEvent(curObj);
					theGraph.addLinkMouseLeaveEvent(curObj);
					
					theGraph.SetActive(curObj,false);
					lines.Add(curObj);
				}
			}
		}
		// If there are more points than pointValues data, delete the extras
		for (int i = points.Count - 1; i >= 0; i--) {
			if (points[i] != null && i >= pointValuesCount) {
				WMG_Node thePoint = points[i].GetComponent<WMG_Node>();
				foreach (GameObject child in thePoint.links) {
					lines.Remove(child);
				}
				theGraph.DeleteNode(thePoint);
				points.RemoveAt(i);
				barIsNegative.RemoveAt(i);
			}
			// Delete existing connect first to last
			if (i > 1 && i < pointValuesCount-1) {
				WMG_Node firstNode = points[0].GetComponent<WMG_Node>();
				WMG_Node toNode = points[i].GetComponent<WMG_Node>();
				WMG_Link delLink = theGraph.GetLink(firstNode,toNode);
				if (delLink != null) {
					lines.Remove(delLink.gameObject);
					theGraph.DeleteLink(delLink);
				}
			}
		}
		// Connect first to last
		if (points.Count > 2) {
			WMG_Node firstNode = points[0].GetComponent<WMG_Node>();
			WMG_Node toNode = points[points.Count-1].GetComponent<WMG_Node>();
			WMG_Link delLink = theGraph.GetLink(firstNode,toNode);
			if (connectFirstToLast && delLink == null) {
				GameObject curObj = theGraph.CreateLink(firstNode, toNode.gameObject, theGraph.linkPrefabs[linkPrefab], linkParent);
				
				theGraph.addLinkClickEvent(curObj);
				theGraph.addLinkMouseEnterEvent(curObj);
				theGraph.addLinkMouseLeaveEvent(curObj);
				
				theGraph.SetActive(curObj,false);
				lines.Add(curObj);
			}
			if (!connectFirstToLast && delLink != null) {
				lines.Remove(delLink.gameObject);
				theGraph.DeleteLink(delLink);
			}
		}
		// Create the legend if it doesn't exist
		if (legendEntry.swatchNode == null) {
			legendEntry.swatchNode = theGraph.CreateNode(nodePrefab, legendEntry.gameObject);
			
			theGraph.addNodeClickEvent_Leg(legendEntry.swatchNode);
			theGraph.addNodeMouseEnterEvent_Leg(legendEntry.swatchNode);
			theGraph.addNodeMouseLeaveEvent_Leg(legendEntry.swatchNode);
			
			WMG_Node cNode = legendEntry.swatchNode.GetComponent<WMG_Node>();
			theGraph.changeSpritePivot(cNode.objectToColor, WMG_Graph_Manager.WMGpivotTypes.Center);
			cNode.Reposition(0,0);
			
			legendEntry.line = theGraph.CreateLink(legendEntry.nodeRight.GetComponent<WMG_Node>(), legendEntry.nodeLeft, theGraph.linkPrefabs[linkPrefab], legendEntry.gameObject);
			
			theGraph.addLinkClickEvent_Leg(legendEntry.line);
			theGraph.addLinkMouseEnterEvent_Leg(legendEntry.line);
			theGraph.addLinkMouseLeaveEvent_Leg(legendEntry.line);
			
			theGraph.bringSpriteToFront(legendEntry.swatchNode);
		}
	}
	
	void createOrDeleteLabels(int pointValuesCount) {
		// Create / delete data labels
		if (dataLabelPrefab != null && dataLabelsParent != null) {
			if (dataLabelsEnabled) {
				for (int i = 0; i < pointValuesCount; i++) {
					if (dataLabels.Count <= i) {
						GameObject curObj = Instantiate(dataLabelPrefab) as GameObject;
						theGraph.changeSpriteParent(curObj, dataLabelsParent);
						curObj.transform.localScale = Vector3.one;
						dataLabels.Add(curObj);
						curObj.name = "Data_Label_" + dataLabels.Count;
						dataLabelsChanged = true;
					}
				}
			}
			int numLabels = pointValuesCount;
			if (!dataLabelsEnabled) {
				numLabels = 0;
			}
			else {
				// Data labels doesn't work for stacked or stacked percentage
				if (theGraph.graphType == WMG_Axis_Graph.graphTypes.bar_stacked || theGraph.graphType == WMG_Axis_Graph.graphTypes.bar_stacked_percent) {
					numLabels = 0;
					dataLabelsEnabled = false;
				}
			}
			// If there are more data labels than pointValues data, delete the extras
			for (int i = dataLabels.Count - 1; i >= 0; i--) {
				if (dataLabels[i] != null && i >= numLabels) {
					DestroyImmediate(dataLabels[i]);
					dataLabels.RemoveAt(i);
				}
			}
			StartCoroutine(SetDelayedAreaShadingChanged ()); // For some reason creating / deleting objects hides area shading
		}
	}

	void createOrDeleteAreaShading(int pointValuesCount) {
		if (areaShadingPrefab == null || areaShadingParent == null) return;
		// Create area shading rectangles based on pointValues data
		if (areaShadingType != areaShadingTypes.None) {
			for (int i = 0; i < pointValuesCount-1; i++) {
				if (areaShadingRects.Count <= i) {
					GameObject curObj = Instantiate(areaShadingPrefab) as GameObject;
					theGraph.changeSpriteParent(curObj, areaShadingParent);
					curObj.transform.localScale = Vector3.one;
					areaShadingRects.Add(curObj);
					curObj.name = "Area_Shading_" + areaShadingRects.Count;
					StartCoroutine(SetDelayedAreaShadingChanged ());
				}
			}
		}
		int numRects = pointValuesCount-1;
		if (areaShadingType == areaShadingTypes.None) {
			numRects = 0;
		}
		// If there are more shading rectangles than pointValues data, delete the extras
		for (int i = areaShadingRects.Count - 1; i >= 0; i--) {
			if (areaShadingRects[i] != null && i >= numRects) {
				DestroyImmediate(areaShadingRects[i]);
				areaShadingRects.RemoveAt(i);
				StartCoroutine(SetDelayedAreaShadingChanged ());
			}
		}
		Material matToUse = areaShadingMatSolid;
		if (areaShadingType == areaShadingTypes.Gradient) {
			matToUse = areaShadingMatGradient;
		}
		for (int i = 0; i < areaShadingRects.Count; i++) {
			theGraph.setTextureMaterial(areaShadingRects[i], matToUse);
			StartCoroutine(SetDelayedAreaShadingChanged ());
		}
	}
	
	IEnumerator SetDelayedAreaShadingChanged() {
		yield return new WaitForEndOfFrame();
		areaShadingChanged = true;
		yield return new WaitForEndOfFrame();
		areaShadingChanged = true;
	}
	
	public void UpdateSprites(List<GameObject> prevPoints) {
		if (pointValuesChanged) {
			updatePointSprites(prevPoints);
		}

		if (pointValuesChanged || dataLabelsChanged) {
			updateDataLabels();
		}

		if (pointValuesChanged || areaShadingChanged) {
			updateAreaShading();
		}
	}

	void updatePointSprites(List<GameObject> prevPoints) {
		float xAxisLength = theGraph.xAxisLength;
		float yAxisLength = theGraph.yAxisLength;
		float xAxisMax = theGraph.xAxisMaxValue;
		float yAxisMax = theGraph.yAxisMaxValue;
		float xAxisMin = theGraph.xAxisMinValue;
		float yAxisMin = theGraph.yAxisMinValue;
		
		if (theGraph.orientationType == WMG_Axis_Graph.orientationTypes.horizontal) {
			theGraph.SwapVals(ref xAxisLength, ref yAxisLength);
			theGraph.SwapVals(ref xAxisMax, ref yAxisMax);
			theGraph.SwapVals(ref xAxisMin, ref yAxisMin);
		}
		
		// Auto set xDistBetween based on the axis length and point count
		if (AutoUpdateXDistBetween) {
			xDistBetweenPoints = theGraph.getDistBetween(points.Count);
			
			cachedXDistBetweenPoints = xDistBetweenPoints;
		}
		
		// auto update space based on groups
		if (theGraph.useGroups) {
			if (theGraph.groupsCentered) extraXSpace = xDistBetweenPoints / 2;
			else extraXSpace = 0;
		}
		
		// Update point positions
		List<Vector2> newPositions = new List<Vector2>();
		List<int> newWidths = new List<int>();
		List<int> newHeights = new List<int>();
		
		for (int i = 0; i < points.Count; i++) {
			if (i >= pointValues.Count) break;
			
			float newX = 0;
			float newY = (pointValues[i].y - yAxisMin)/(yAxisMax - yAxisMin) * yAxisLength; // new y always based on the pointValues.y
			
			// If using xDistBetween then point positioning based on previous point point position
			if (!theGraph.useGroups && UseXDistBetweenToSpace) {
				if (i > 0) { // For points greater than 0, use the previous point position plus xDistBetween
					float prevPosX = newPositions[i-1].x;
					float barOffsetX = theGraph.getSpriteFactorX(points[i-1]) * newWidths[i-1] + theGraph.getSpriteOffsetX(points[i]);
					if (theGraph.orientationType == WMG_Axis_Graph.orientationTypes.horizontal) {
						prevPosX = newPositions[i-1].y;
						barOffsetX = theGraph.getSpriteFactorY(points[i-1]) * newHeights[i-1] - (theGraph.getSpriteFactorY(points[i]) - 1) * theGraph.barWidth;
					}
					newX = prevPosX + xDistBetweenPoints ;
					if (theGraph.graphType != WMG_Axis_Graph.graphTypes.line) {
						newX += barOffsetX;
					}
				}
				else { // For point 0, one of the positions is just 0
					newX = extraXSpace;
				}
			}
			else if (theGraph.useGroups) { // Using groups, x values represent integer index of group
				newX = extraXSpace + xDistBetweenPoints * (Mathf.Abs(pointValues[i].x) - 1);
			}
			else { // Not using xDistBetween or groups, so use the actual x values in the Vector2 list
				newX = (pointValues[i].x - xAxisMin)/(xAxisMax - xAxisMin) * xAxisLength;
			}
			
			if (theGraph.orientationType == WMG_Axis_Graph.orientationTypes.horizontal) {
				theGraph.SwapVals(ref newX, ref newY);
			}
			
			int newWidth = 0;
			int newHeight = 0;
			
			if (theGraph.graphType == WMG_Axis_Graph.graphTypes.line) {
				// Width and height of points for line graphs - needed because autospace functionality requires height and width of previous point
				// And previous point widths and heights will are not set in this loop because of automatic animations
				newWidth = Mathf.RoundToInt(pointWidthHeight);
				newHeight = Mathf.RoundToInt(pointWidthHeight);
			}
			else {
				// For bar graphs, need to update sprites width and height based on positions
				// For stacked percentage, need to set a y position based on the percentage of all series values combined
				if (theGraph.graphType == WMG_Axis_Graph.graphTypes.bar_stacked_percent && theGraph.TotalPointValues.Count > i) {
					if (theGraph.orientationType == WMG_Axis_Graph.orientationTypes.vertical) {
						newY = (pointValues[i].y - yAxisMin) / theGraph.TotalPointValues[i] * yAxisLength;
					}
					else {
						newX = (pointValues[i].y - yAxisMin) / theGraph.TotalPointValues[i] * yAxisLength;
					}
				}
				
				// Update sprite dimensions and increase position using previous point position
				// Previous points is null for side by side bar, but should not be empty for stacked and stacked percentage for series after the first series
				if (theGraph.orientationType == WMG_Axis_Graph.orientationTypes.vertical) {
					newWidth = Mathf.RoundToInt(theGraph.barWidth);
					newHeight = Mathf.RoundToInt(newY);
					// Adjust height based on barAxisValue
					int heightAdjust = 0;
					if (theGraph.graphType == WMG_Axis_Graph.graphTypes.bar_side) {
						heightAdjust = Mathf.RoundToInt((theGraph.barAxisValue - yAxisMin) / (yAxisMax - yAxisMin) * yAxisLength);
					}
					newHeight -= heightAdjust;
					newY -= (theGraph.getSpriteFactorY(points[i]) * newHeight) + (theGraph.getSpriteFactorY(points[i]) - 1) * -newHeight;
					barIsNegative[i] = false;
					if (newHeight < 0) {
						newHeight *= -1;
						newY -= newHeight;
						barIsNegative[i] = true;
					}
					if (prevPoints != null && i < prevPoints.Count) {
						newY += theGraph.getSpritePositionY(prevPoints[i]) + (theGraph.getSpriteFactorY(points[i]) - 1) * -theGraph.getSpriteHeight(prevPoints[i]);
					}
				}
				else {
					newWidth = Mathf.RoundToInt(newX);
					newHeight = Mathf.RoundToInt(theGraph.barWidth);
					// Adjust width based on barAxisValue
					int widthAdjust = 0;
					if (theGraph.graphType == WMG_Axis_Graph.graphTypes.bar_side) {
						widthAdjust = Mathf.RoundToInt((theGraph.barAxisValue - yAxisMin) / (yAxisMax - yAxisMin) * yAxisLength);
					}
					newWidth -= widthAdjust;
					newX = widthAdjust;
					newY -= theGraph.barWidth;
					barIsNegative[i] = false;
					if (newWidth < 0) {
						newWidth *= -1;
						newX -= newWidth;
						barIsNegative[i] = true;
					}
					if (prevPoints != null && i < prevPoints.Count) {
						newX += theGraph.getSpritePositionX(prevPoints[i]) + theGraph.getSpriteWidth(prevPoints[i]);
					}
				}
			}
			newWidths.Add(newWidth);
			newHeights.Add(newHeight);
			newPositions.Add(new Vector2(newX, newY));
		}
		
		if (animatingFromPreviousData) { // For animations, copy over the newly calculated values into lists to be used later in the animation code
			if (theGraph.graphType == WMG_Axis_Graph.graphTypes.line) {
				for (int i = 0; i < points.Count; i++) {
					if (i >= pointValues.Count) break;
					newPositions[i] = theGraph.getChangeSpritePositionTo(points[i], newPositions[i]);
				}
			}
			afterPositions = new List<Vector2>(newPositions);
			afterWidths = new List<int>(newWidths);
			afterHeights = new List<int>(newHeights);
			OnSeriesDataChanged();
			animatingFromPreviousData = false;
		}
		else { // Otherwise update the visuals now
			for (int i = 0; i < points.Count; i++) {
				if (i >= pointValues.Count) break;
				if (theGraph.graphType != WMG_Axis_Graph.graphTypes.line) {
					WMG_Node thePoint = points[i].GetComponent<WMG_Node>();
					theGraph.changeBarWidthHeight(thePoint.objectToColor, newWidths[i], newHeights[i]); 
				}
				theGraph.changeSpritePositionTo(points[i], new Vector3(newPositions[i].x, newPositions[i].y, 0));
			}
			// Reposition existing lines based on the new point positions
			for (int i = 0; i < lines.Count; i++) {
				WMG_Link theLine = lines[i].GetComponent<WMG_Link>();
				theLine.Reposition();
			}
		}
	}


	void updateDataLabels() {
		if (!dataLabelsEnabled) return;
		float numberToMult = Mathf.Pow(10f, dataLabelsNumDecimals);
		for (int i = 0; i < dataLabels.Count; i++) {
			Vector2 currentPointPosition = new Vector2(theGraph.getSpritePositionX(points[i]), theGraph.getSpritePositionY(points[i]));
			// Update font size
			dataLabels[i].transform.localScale = new Vector3(dataLabelsFontSize, dataLabelsFontSize, 1);
			// Update text based on y value and number decimals
			theGraph.changeLabelText(dataLabels[i], (Mathf.Round(pointValues[i].y * numberToMult) / numberToMult).ToString());
			
			if (theGraph.graphType == WMG_Axis_Graph.graphTypes.bar_side) {
				if (theGraph.orientationType == WMG_Axis_Graph.orientationTypes.vertical) {
					float newY = dataLabelsOffset.y + currentPointPosition.y + theGraph.getSpriteHeight(points[i]) - theGraph.getSpriteOffsetY(points[i]);
					if (barIsNegative[i]) {
						newY = -dataLabelsOffset.y - theGraph.getSpriteHeight(points[i]) + Mathf.RoundToInt((theGraph.barAxisValue - theGraph.yAxisMinValue) / (theGraph.yAxisMaxValue - theGraph.yAxisMinValue) * theGraph.yAxisLength);
					}
					theGraph.changeSpritePositionTo(dataLabels[i], new Vector3(
						dataLabelsOffset.x + currentPointPosition.x + theGraph.barWidth / 2, 
						newY, 
						0));
				}
				else {
					float newX = dataLabelsOffset.x + currentPointPosition.x + theGraph.getSpriteWidth(points[i]);
					if (barIsNegative[i]) {
						newX = -dataLabelsOffset.x - theGraph.getSpriteWidth(points[i]) + Mathf.RoundToInt((theGraph.barAxisValue - theGraph.xAxisMinValue) / (theGraph.xAxisMaxValue - theGraph.xAxisMinValue) * theGraph.xAxisLength);
					}
					theGraph.changeSpritePositionTo(dataLabels[i], new Vector3(
						newX, 
						dataLabelsOffset.y + currentPointPosition.y + theGraph.barWidth / 2 - theGraph.getSpriteOffsetY(points[i]), 
						0));
				}
			}
			else {
				theGraph.changeSpritePositionTo(dataLabels[i], new Vector3(
					dataLabelsOffset.x + currentPointPosition.x + theGraph.getSpriteOffsetX(points[i]), 
					dataLabelsOffset.y + currentPointPosition.y - theGraph.getSpriteOffsetY(points[i]), 
					0));
			}
		}
	}


	// Update the position, alpha clipping, and other properties of the area shading rectangles
	void updateAreaShading() {
		if (areaShadingType == areaShadingTypes.None) return;
		// Find the maximum area shading height so that we can corectly adjust each sprites transparency based on their height in comparison to the max height
		float maxVal = Mathf.NegativeInfinity;
		for (int i = 0; i < points.Count; i++) {
			if (i >= pointValues.Count) break;
			if (pointValues[i].y > maxVal) {
				maxVal = pointValues[i].y;
			}
		}
		for (int i = 0; i < points.Count - 1; i++) {
			if (i >= pointValues.Count) break;
			
			int rotation = 180;
			Vector2 currentPointPosition = new Vector2(theGraph.getSpritePositionX(points[i]), theGraph.getSpritePositionY(points[i]));
			Vector2 nextPointPosition = new Vector2(theGraph.getSpritePositionX(points[i+1]), theGraph.getSpritePositionY(points[i+1]));
			float axisMultiplier = theGraph.yAxisLength / (theGraph.yAxisMaxValue - theGraph.yAxisMinValue);
			float yPosOfAxisVal = (areaShadingAxisValue - theGraph.yAxisMinValue) * axisMultiplier;
			if (theGraph.orientationType == WMG_Axis_Graph.orientationTypes.horizontal) {
				rotation = 90;
				currentPointPosition = new Vector2(theGraph.getSpritePositionY(points[i]), theGraph.getSpritePositionX(points[i]));
				nextPointPosition = new Vector2(theGraph.getSpritePositionY(points[i+1]), theGraph.getSpritePositionX(points[i+1]));
				axisMultiplier = theGraph.xAxisLength / (theGraph.xAxisMaxValue - theGraph.xAxisMinValue);
				yPosOfAxisVal = (areaShadingAxisValue - theGraph.xAxisMinValue) * axisMultiplier;
			}
			
			areaShadingRects[i].transform.localEulerAngles = new Vector3(0, 0, rotation);
			float maxY = Mathf.Max(nextPointPosition.y, currentPointPosition.y);
			float minY = Mathf.Min(nextPointPosition.y, currentPointPosition.y);
			int newX = Mathf.RoundToInt(currentPointPosition.x);
			int newWidth = Mathf.RoundToInt(nextPointPosition.x - currentPointPosition.x);
			int newHeight = Mathf.RoundToInt(maxY - minY + 
			                                 ((Mathf.Min(pointValues[i+1].y, pointValues[i].y) - areaShadingAxisValue) * axisMultiplier ) );
			
			// If areaShading value goes above a line segment, decrease the width appropriately
			if (minY < yPosOfAxisVal) {
				float slope = (nextPointPosition.y - currentPointPosition.y) / (nextPointPosition.x - currentPointPosition.x);
				// Slope increasing
				if (nextPointPosition.y > currentPointPosition.y) {
					float deltaY = yPosOfAxisVal - minY;
					int deltaX = Mathf.RoundToInt(deltaY / slope);
					newWidth -= deltaX;
					// Increase position by delta
					newX += deltaX;
				}
				else {
					float deltaY = yPosOfAxisVal - minY;
					int deltaX = Mathf.RoundToInt(deltaY / slope * -1);
					newWidth -= deltaX;
				}
			}
			
			
			if (theGraph.orientationType == WMG_Axis_Graph.orientationTypes.horizontal) {
				theGraph.changeSpritePositionTo(areaShadingRects[i], new Vector3(maxY + theGraph.getSpriteOffsetX(points[i+1]),
				                                                                 newX + newWidth - theGraph.getSpriteOffsetY(points[i+1]), 0));
			}
			else {
				theGraph.changeSpritePositionTo(areaShadingRects[i], new Vector3(newX + theGraph.getSpriteOffsetX(points[i+1]),
				                                                                 maxY - theGraph.getSpriteOffsetY(points[i+1]), 0));
			}
			theGraph.changeSpriteSize(areaShadingRects[i], newWidth, newHeight);
			
			// Adjust previous sprite width based on previous width and position so that the sprites do not overlap due to position being a float and width being an integer
			if (i > 0) { // Don't need to adjust the width for the first rectangle
				if (theGraph.orientationType == WMG_Axis_Graph.orientationTypes.horizontal) {
					int previousSpriteWidthPlusPosition = Mathf.RoundToInt(theGraph.getSpritePositionY(areaShadingRects[i])) 
						- Mathf.RoundToInt(theGraph.getSpriteOffsetY(areaShadingRects[i])) - Mathf.RoundToInt(theGraph.getSpriteWidth(areaShadingRects[i]));
					int yPrevious = Mathf.RoundToInt(theGraph.getSpritePositionY(areaShadingRects[i-1])) - Mathf.RoundToInt(theGraph.getSpriteOffsetY(areaShadingRects[i-1]));
					// if y previous < y current - width current then increase current width by 1y 1
					if (previousSpriteWidthPlusPosition > yPrevious) {
						theGraph.changeSpriteWidth(areaShadingRects[i], Mathf.RoundToInt(theGraph.getSpriteWidth(areaShadingRects[i]) + 1));
					}
					// if y previous > y current - width current then decrease current width by 1y 1
					if (previousSpriteWidthPlusPosition < yPrevious) {
						theGraph.changeSpriteWidth(areaShadingRects[i], Mathf.RoundToInt(theGraph.getSpriteWidth(areaShadingRects[i]) - 1));
					}
				}
				else {
					int previousSpriteWidthPlusPosition = Mathf.RoundToInt(theGraph.getSpriteWidth(areaShadingRects[i-1])) + Mathf.RoundToInt(theGraph.getSpritePositionX(areaShadingRects[i-1]));
					// If greater then sprites would overlap, subtract width by 1
					if (previousSpriteWidthPlusPosition > Mathf.RoundToInt(theGraph.getSpritePositionX(areaShadingRects[i]))) {
						theGraph.changeSpriteWidth(areaShadingRects[i-1], Mathf.RoundToInt(theGraph.getSpriteWidth(areaShadingRects[i-1]) - 1));
					}
					// If lesser then sprites would have gap, increased width by 1
					if (previousSpriteWidthPlusPosition < Mathf.RoundToInt(theGraph.getSpritePositionX(areaShadingRects[i]))) {
						theGraph.changeSpriteWidth(areaShadingRects[i-1], Mathf.RoundToInt(theGraph.getSpriteWidth(areaShadingRects[i-1]) + 1));
					}
				}
			}
			
			// Set custom shader properties to do appropriate alpha clipping, gradient shading, color, etc.
			Material curMat = theGraph.getTextureMaterial(areaShadingRects[i]);
			if (curMat == null) continue;
			
			if (theGraph.orientationType == WMG_Axis_Graph.orientationTypes.horizontal) {
				curMat.SetFloat("_Slope", -(nextPointPosition.y - currentPointPosition.y) / newHeight);
			}
			else {
				curMat.SetFloat("_Slope", (nextPointPosition.y - currentPointPosition.y) / newHeight);
			}
			
			curMat.SetColor("_Color", areaShadingColor);
			curMat.SetFloat("_Transparency", 1 - areaShadingColor.a );
			// Set the gradient scale based on current sprite height in comparison to maximum sprite height
			curMat.SetFloat("_GradientScale", 
			                (Mathf.Max(pointValues[i+1].y, pointValues[i].y) - areaShadingAxisValue) / (maxVal - areaShadingAxisValue)
			                );
		}
	}
	
	public void StartRealTimeUpdate() {
		if (realTimeRunning) return;
		if (realTimeDataSource != null) {
			realTimeRunning = true;
			pointValues = new List<Vector2>();
			pointValues.Add(new Vector2(0, realTimeDataSource.getDatum<float>()));
			pointValuesChanged = true;
			realTimeLoopVar = 0;
			if (theGraph.orientationType == WMG_Axis_Graph.orientationTypes.vertical) {
				realTimeOrigMax = theGraph.xAxisMaxValue;
			}
			else {
				realTimeOrigMax = theGraph.yAxisMaxValue;
			}
		}
	}
	
	public void StopRealTimeUpdate() {
		realTimeRunning = false;
	}
	
	private void DoRealTimeUpdate() {
		/* This "Real Time" update is FPS dependent, so the time axis actually represents a number of frames.
		 * The waitForSeconds for coroutines does not actually wait for the specified number of seconds, and is also FPS dependent.
		 * An FPS independent solution only seems possible with fixedUpdate, which may be added later. */
		
		float waitTime = 0.0166f; // Each x-axis unit is 60 frames. This is 1 second at 60 fps.
		
		realTimeLoopVar += waitTime;
		
		float yval = realTimeDataSource.getDatum<float>();
		
		// Add new point or move the last existing point
		if (pointValues.Count > 1) {
			// For the third and additional points, calculate slopes and move previous point instead of creating a new point if slopes not significantly different
			float slope1 = (pointValues[pointValues.Count-1].y - pointValues[pointValues.Count-2].y) 
						/ (pointValues[pointValues.Count-1].x - pointValues[pointValues.Count-2].x);
			float slope2 = (yval - pointValues[pointValues.Count-2].y) / (realTimeLoopVar - pointValues[pointValues.Count-2].x);
			
			if (Mathf.Abs(slope1-slope2) <= Mathf.Abs(slope1)/1000f) { // Mathf.Approximately not always working, so defining significantly as 10^3 different
				// Slopes about the same, move the last point
				pointValues[pointValues.Count-1] = new Vector2(realTimeLoopVar,yval);
			}
			else {
				// Slopes significantly different, add a new point
				pointValues.Add(new Vector2(realTimeLoopVar, yval));
			}
		}
		else {
			// Just add the second point
			pointValues.Add(new Vector2(realTimeLoopVar, yval));
		}
		
		// If needed, change graph axis boundary and remove or move the first point to keep the series within the graph boundaries
		if (pointValues.Count > 1 && pointValues[pointValues.Count-1].x > realTimeOrigMax) {
			
			// For the last real time update series update the axis boundaries by the difference
			if (theGraph.orientationType == WMG_Axis_Graph.orientationTypes.vertical) {
				theGraph.xAxisMinValue = realTimeLoopVar - realTimeOrigMax;
				theGraph.xAxisMaxValue = realTimeLoopVar;
			}
			else {
				theGraph.yAxisMinValue = realTimeLoopVar - realTimeOrigMax;
				theGraph.yAxisMaxValue = realTimeLoopVar;
			}
			
			// First and second points used to see if the first point should be moved or deleted after incrementing the minimum axis value
			float x1 = pointValues[0].x;
			float x2 = pointValues[1].x;
			float y1 = pointValues[0].y;
			float y2 = pointValues[1].y;
			
			// Delete or move the very first point to keep the series in the graph boundary when the maximum is increased
			if (Mathf.Approximately(x1 + waitTime, x2)) pointValues.RemoveAt(0);
			else pointValues[0] = new Vector2(x1 + waitTime, y1 + (y2 - y1) / (x2 - x1) * waitTime);
		}
	}
	
	public void deleteAllNodesFromGraphManager() {
		// This should not be called manually, only an internal helper function for dynamically deleting series
		for (int i = points.Count - 1; i >= 0; i--) {
			theGraph.DeleteNode(points[i].GetComponent<WMG_Node>());
		}
		theGraph.DeleteNode(legendEntry.nodeLeft.GetComponent<WMG_Node>());
		theGraph.DeleteNode(legendEntry.nodeRight.GetComponent<WMG_Node>());
		theGraph.DeleteNode(legendEntry.swatchNode.GetComponent<WMG_Node>());
	}
}