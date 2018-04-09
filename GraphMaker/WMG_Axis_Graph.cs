using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class WMG_Axis_Graph : WMG_Graph_Manager {

	public bool autoRefresh = true; // Set to false, and then manually call refreshGraph for performance boost

	public enum graphTypes {line, bar_side, bar_stacked, bar_stacked_percent};
	public graphTypes graphType;
	public enum orientationTypes {vertical, horizontal};
	public orientationTypes orientationType;
	public enum axesTypes {MANUAL, CENTER, AUTO_ORIGIN, AUTO_ORIGIN_X, AUTO_ORIGIN_Y, I, II, III, IV, I_II, III_IV, II_III, I_IV};
	public axesTypes axesType;
	public enum resizeTypes {none, pixel_padding, percentage_padding}
	public resizeTypes resizeType;
	public enum labelTypes {ticks, ticks_center, groups, manual};
	public labelTypes xLabelType;
	public labelTypes yLabelType;

	[System.Flags]
	public enum ResizeProperties {
		PointSizeBarWidth 	= 1 << 0,
		SeriesLineWidth 	= 1 << 1,
		AxesWidth	 		= 1 << 2,
		FontSizeAxesLabels 	= 1 << 3,
		FontSizeTitle 		= 1 << 4,
		FontSizeAxesTitles 	= 1 << 5,
		FontSizeDataLabels 	= 1 << 6,
		FontSizeLegends	 	= 1 << 7,
		LegendEntryLine		= 1 << 8,
		LegendEntrySpacing	= 1 << 9
	}

	[WMG_EnumFlagAttribute]
	public ResizeProperties resizeProperties;

	public bool useGroups;
	public bool groupsCentered;
	public List<string> groups;

	public bool tooltipEnabled;
	public Vector2 tooltipOffset = new Vector2(10,10);
	public int tooltipNumberDecimals = 2;
	public bool tooltipDisplaySeriesName;
	public bool tooltipAnimationsEnabled;
	public Ease tooltipAnimationsEasetype = Ease.OutElastic;
	public float tooltipAnimationsDuration = 0.5f;
	public bool autoAnimationsEnabled;
	public Ease autoAnimationsEasetype = Ease.OutQuad;
	public float autoAnimationsDuration = 1;
	public List<GameObject> lineSeries;
	
	public List<Object> pointPrefabs;
	public List<Object> linkPrefabs;
	public Object barPrefab;
	public Object seriesPrefab;
	public Object legendPrefab;
	
	public float barAxisValue;
	public bool autoUpdateBarAxisValue;
	public Vector2 theOrigin;
	public bool autoUpdateOrigin;
	
	public float yAxisMaxValue;
	public float yAxisMinValue;
	public int yAxisNumTicks;
	
	public float xAxisMaxValue;
	public float xAxisMinValue;
	public int xAxisNumTicks;

	public float xAxisLength { 
		get {
			return getSpriteWidth(this.gameObject) - paddingLeftRight.x - paddingLeftRight.y;
		}
	}
	
	public float yAxisLength { 
		get {
			return getSpriteHeight(this.gameObject) - paddingTopBottom.x - paddingTopBottom.y;
		}
	}
	
	public bool[] yMinMaxAutoGrow = new bool[2];
	public bool[] yMinMaxAutoShrink = new bool[2];
	public bool[] xMinMaxAutoGrow = new bool[2];
	public bool[] xMinMaxAutoShrink = new bool[2];
	public float autoShrinkAtPercent = 0.6f;
	public float autoGrowAndShrinkByPercent = 0.2f;
	
	public float barWidth;
	public bool autoUpdateBarWidth;
	public int axisWidth;
	
	public bool hideXTicks;
	public bool hideYTicks;
	public bool hideXGrid;
	public bool hideYGrid;
	public bool hideXLabels;
	public bool hideYLabels;
	public List<string> yAxisLabels;
	public List<string> xAxisLabels;
	
	public bool SetYLabelsUsingMaxMin;
	public int numDecimalsYAxisLabels;
	public bool SetXLabelsUsingMaxMin;
	public int numDecimalsXAxisLabels;

	public float yAxisLabelSpacingY;
	public float yAxisLabelSpacingX;
	public float xAxisLabelSpacingY;
	public float xAxisLabelSpacingX;

	public float yAxisLabelRotation;
	public float xAxisLabelRotation;
	public float yAxisLabelDistBetween;
	public float xAxisLabelDistBetween;
	
	public float yAxisLabelSize;
	public float xAxisLabelSize;
	
	public bool yAxisTicksRight;
	public bool xAxisTicksAbove;
	
	public bool[] xAxisArrows = new bool[2];
	public float xAxisLinePadding;
	public int xAxisYTick;
	public float xAxisNonTickPercent;
	public bool xAxisUseNonTickPercent;
	public bool hideYTick;
	
	public bool[] yAxisArrows = new bool[2];
	public float yAxisLinePadding;
	public int yAxisXTick;
	public float yAxisNonTickPercent;
	public bool yAxisUseNonTickPercent;
	public bool hideXTick;

	public Vector2 tickSize = new Vector2(2,5);
	
	public Vector2 paddingLeftRight;
	public Vector2 paddingTopBottom;
	public string graphTitleString;
	public Vector2 graphTitleOffset;
	public string yAxisTitleString;
	public Vector2 yAxisTitleOffset;
	public string xAxisTitleString;
	public Vector2 xAxisTitleOffset;
	
	public WMG_Legend legend;
	public GameObject graphTitle;
	public GameObject yAxisTitle;
	public GameObject xAxisTitle;
	public GameObject graphBackground;
	public GameObject horizontalGridLines;
	public GameObject yAxisTicks;
	public GameObject verticalGridLines;
	public GameObject xAxisTicks;
	public GameObject yAxisLine;
	public GameObject xAxisLine;
	public GameObject xAxisArrowR;
	public GameObject xAxisArrowL;
	public GameObject yAxisArrowU;
	public GameObject yAxisArrowD;
	public GameObject yAxis;
	public GameObject xAxis;
	public GameObject seriesParent;
	public GameObject toolTipPanel;
	public GameObject toolTipLabel;
	public GameObject yAxisLabelObjs;
	public GameObject xAxisLabelObjs;

	// Private variables
	private List<float> totalPointValues = new List<float>();
	private int maxSeriesPointCount;
	private float yGridLineLength;
	private float xGridLineLength;
	private float xAxisLinePaddingTot;
	private float yAxisLinePaddingTot;
	private float xAxisPercentagePosition;
	private float yAxisPercentagePosition;

	// Original property values for use with dynamic resizing
	private float origWidth;
	private float origHeight;
	private float origYAxisLength;
	private float origXAxisLength;
	private float origBarWidth;
	private float origAxisWidth;
	private float origYAxisLabelSize;
	private float origXAxisLabelSize;
	private float origTitleFontSize;
	private float origXTitleFontSize;
	private float origYTitleFontSize;
	private Vector2 origPaddingLeftRight;
	private Vector2 origPaddingTopBottom;

	// Cache
	private orientationTypes cachedOrientationType;
	private axesTypes cachedAxesType;
	private graphTypes cachedGraphType;
	private resizeTypes cachedResizeType;
	private labelTypes cachedXLabelType;
	private labelTypes cachedYLabelType;
	private ResizeProperties cachedResizeProperties;
	private float cachedContainerWidth;
	private float cachedContainerHeight;
	private bool cachedUseGroups;
	private bool cachedGroupsCentered;
	private List<string> cachedGroups = new List<string>(); 
	private bool cachedTooltipEnabled;
	private bool cachedAutoAnimationsEnabled;
	private int cachedLineSeriesCount;
	private float cachedBarAxisValue;
	private bool cachedAutoUpdateBarAxisValue;
	private Vector2 cachedTheOrigin;
	private bool cachedAutoUpdateOrigin;
	private float cachedYAxisMaxValue;
	private float cachedYAxisMinValue;
	private int cachedYAxisNumTicks;
	private float cachedXAxisMaxValue;
	private float cachedXAxisMinValue;
	private int cachedXAxisNumTicks;
	private bool[] cachedYMinMaxAutoGrow = new bool[2];
	private bool[] cachedYMinMaxAutoShrink = new bool[2];
	private bool[] cachedXMinMaxAutoGrow = new bool[2];
	private bool[] cachedXMinMaxAutoShrink = new bool[2];
	private float cachedAutoShrinkAtPercent;
	private float cachedAutoGrowAndShrinkByPercent;
	private float cachedBarWidth;
	private bool cachedAutoUpdateBarWidth;
	private int cachedAxisWidth;
	
	private bool cachedHideXTicks;
	private bool cachedHideYTicks;
	private bool cachedHideXGrid;
	private bool cachedHideYGrid;
	private bool cachedHideXLabels;
	private bool cachedHideYLabels;
	private bool cachedSetYLabelsUsingMaxMin;
	private int cachedNumDecimalsYAxisLabels;
	private bool cachedSetXLabelsUsingMaxMin;
	private int cachedNumDecimalsXAxisLabels;
	private float cachedYAxisLabelSize;
	private float cachedXAxisLabelSize;
	private bool[] cachedXAxisArrows = new bool[2];
	private bool[] cachedYAxisArrows = new bool[2];
	private float cachedXAxisLinePadding;
	private float cachedYAxisLinePadding;
	private bool cachedHideYTick;
	private bool cachedHideXTick;
	private Vector2 cachedTickSize;
	private List<string> cachedYAxisLabels = new List<string>();
	private List<string> cachedXAxisLabels = new List<string>();

	private float cachedYAxisLabelSpacingY;
	private float cachedYAxisLabelSpacingX;
	private float cachedXAxisLabelSpacingY;
	private float cachedXAxisLabelSpacingX;

	private float cachedYAxisLabelRotation;
	private float cachedXAxisLabelRotation;
	private float cachedYAxisLabelDistBetween;
	private float cachedXAxisLabelDistBetween;

	private bool cachedYAxisTicksRight;
	private bool cachedXAxisTicksAbove;
	private int cachedXAxisYTick;
	private int cachedYAxisXTick;
	private float cachedXAxisNonTickPercent;
	private float cachedYAxisNonTickPercent;
	private bool cachedXAxisUseNonTickPercent;
	private bool cachedYAxisUseNonTickPercent;
	
	private Vector2 cachedPaddingLeftRight;
	private Vector2 cachedPaddingTopBottom;
	
	private string cachedGraphTitleString;
	private string cachedYAxisTitleString;
	private string cachedXAxisTitleString;
	private Vector2 cachedGraphTitleOffset;
	private Vector2 cachedYAxisTitleOffset;
	private Vector2 cachedXAxisTitleOffset;
	
	private int cachedNumYAxisLabels;
	private int cachedNumXAxisLabels;
	
	// Changed Flags
	private bool aSeriesPointsChanged;
	private bool orientationTypeChanged;
	private bool axesTypeChanged;
	private bool graphTypeChanged;
	private bool resizeChanged;
	private bool xLabelTypeChanged;
	private bool yLabelTypeChanged;
	private bool groupsChanged;
	private bool tooltipEnabledChanged;
	private bool autoAnimationsEnabledChanged;
	private bool lineSeriesCountChanged;
	private bool barAxisValueChanged;
	private bool theOriginChanged;
	private bool yAxisMaxValueChanged;
	private bool yAxisMinValueChanged;
	private bool yAxisLengthChanged;
	private bool yAxisNumTicksChanged;
	private bool xAxisMaxValueChanged;
	private bool xAxisMinValueChanged;
	private bool xAxisLengthChanged;
	private bool xAxisNumTicksChanged;
	private bool autoGrowShrinkChanged;
	private bool barWidthChanged;
	private bool axisWidthChanged;
	
	private bool hideXTicksChanged;
	private bool hideYTicksChanged;
	private bool hideXGridChanged;
	private bool hideYGridChanged;
	private bool hideXLabelsChanged;
	private bool hideYLabelsChanged;
	private bool SetYLabelsUsingMaxMinChanged;
	private bool numDecimalsYAxisLabelsChanged;
	private bool SetXLabelsUsingMaxMinChanged;
	private bool numDecimalsXAxisLabelsChanged;
	private bool yAxisLabelSizeChanged;
	private bool xAxisLabelSizeChanged;
	private bool xAxisArrowsChanged;
	private bool yAxisArrowsChanged;
	private bool xAxisLinePaddingChanged;
	private bool yAxisLinePaddingChanged;
	private bool hideYTickChanged;
	private bool hideXTickChanged;
	private bool tickSizeChanged;
	private bool yAxisLabelsChanged;
	private bool xAxisLabelsChanged;

	private bool yAxisLabelSpacingYChanged;
	private bool yAxisLabelSpacingXChanged;
	private bool xAxisLabelSpacingYChanged;
	private bool xAxisLabelSpacingXChanged;

	private bool yAxisLabelRotationChanged;
	private bool xAxisLabelRotationChanged;
	private bool yAxisLabelDistBetweenChanged;
	private bool xAxisLabelDistBetweenChanged;

	private bool yAxisTicksRightChanged;
	private bool xAxisTicksAboveChanged;
	private bool xAxisYTickChanged;
	private bool yAxisXTickChanged;
	private bool xAxisNonTickPercentChanged;
	private bool yAxisNonTickPercentChanged;
	private bool xAxisUseNonTickPercentChanged;
	private bool yAxisUseNonTickPercentChanged;

	private bool paddingChanged;
	
	private bool graphTitleChanged;
	private bool yAxisTitleChanged;
	private bool xAxisTitleChanged;
	
	private bool numYAxisLabelsChanged;
	private bool numXAxisLabelsChanged;

	// Other private variables
	private WMG_Graph_Tooltip theTooltip;
	private WMG_Graph_Auto_Anim autoAnim;
	
	void Start() {
		// Create legend
		GameObject legendObj = GameObject.Instantiate(legendPrefab) as GameObject;
		changeSpriteParent(legendObj, graphBackground.transform.parent.gameObject);
		legend = legendObj.GetComponent<WMG_Legend>();
		legend.theGraph = this;
		for (int j = 0; j < lineSeries.Count; j++) {
			if (!activeInHierarchy(lineSeries[j])) continue;
			WMG_Series theSeries = lineSeries[j].GetComponent<WMG_Series>();
			theSeries.createLegendEntry(j);
		}
		checkCache(); // Set all cache variables to the current values
		setCacheFlags(true); // Set all cache change flags to true to update everything on start
		orientationTypeChanged = false; // Except orientation, since this swaps the orientation
		theTooltip = this.gameObject.AddComponent<WMG_Graph_Tooltip>(); // Add tooltip script
		theTooltip.hideFlags = HideFlags.HideInInspector; // Don't show tooltip script
		theTooltip.theGraph = this; // Set tooltip graph
		autoAnim = this.gameObject.AddComponent<WMG_Graph_Auto_Anim>(); // Add automatic animations script
		autoAnim.hideFlags = HideFlags.HideInInspector; // Don't show automatic animations script
		autoAnim.theGraph = this; // Set automatic animations graph
		setOriginalPropertyValues();
	}

	// Set initial property values for use with percentage based dynamic resizing 
	public void setOriginalPropertyValues() {
		origWidth = getSpriteWidth(this.gameObject);
		origHeight = getSpriteHeight(this.gameObject);
		origYAxisLength = yAxisLength;
		origXAxisLength = xAxisLength;
		origBarWidth = barWidth;
		origAxisWidth = axisWidth;
		origYAxisLabelSize = yAxisLabelSize;
		origXAxisLabelSize = xAxisLabelSize;
		origTitleFontSize = 1;
		origXTitleFontSize = 1;
		origYTitleFontSize = 1;
		origPaddingLeftRight = paddingLeftRight;
		origPaddingTopBottom = paddingTopBottom;
	}
	
	void LateUpdate () {
		if (autoRefresh) {
			Refresh();
		}
	}

	public void Refresh() {
		checkCache(); // Check current vs cached values and set changed flags so only some things are updated upon refresh
		refreshNonSeries(); // Refresh axes, grids, background, etc.
		refreshSeries(); // Refresh series data plotting
		legend.updateLegend(); // Refresh the legend entries
		setCacheFlags(false); // Set all cache change flags to false
	}

	public void refreshNonSeries() {
		// Swap values based on horizontal vs vertical
		UpdateOrientation();

		// Update from container
		UpdateFromContainer();

		// Update total point values used in stacked charts, and max series point count
		UpdateTotals();

		// Update bar width
		UpdateBarWidth();
		
		// Auto update Axes Min Max values based on grow and shrink booleans
		UpdateAxesMinMaxValues();
		
		// Update axes quadrant and related boolean variables such as which arrows appear
		UpdateAxesType();
		
		// Update visuals of axes, grids, and ticks
		UpdateAxesGridsAndTicks();
		
		// Update position and text of axes labels which might be based off max / min values or percentages for stacked percentage bar
		UpdateAxesLabels();
		
		// Update Line Series Parents
		UpdateSeriesParentPositions();
		
		// Update background sprite
		UpdateBackground();
		
		// Update Titles
		UpdateTitles();
		
		// Update tooltip
		UpdateTooltip();
		
		// Update automatic animations events
		UpdateAutoAnimEvents();
	}
	
	void refreshSeries() {
		
		if (yAxisMaxValue <= yAxisMinValue || xAxisMaxValue <= xAxisMinValue) return;
		
		for (int j = 0; j < lineSeries.Count; j++) {
			if (!activeInHierarchy(lineSeries[j])) continue;
			WMG_Series theSeries = lineSeries[j].GetComponent<WMG_Series>();
			
			List<GameObject> prevPoints = null;
			if (j > 0 && (graphType == graphTypes.bar_stacked || graphType == graphTypes.bar_stacked_percent) && activeInHierarchy(lineSeries[j-1])) {
				WMG_Series prevSeries = lineSeries[j-1].GetComponent<WMG_Series>();
				prevPoints = prevSeries.getPoints();
			}
			
			theSeries.UpdatePrefabType();
			theSeries.UpdateFromDataSource();
			theSeries.RealTimeUpdate();
			theSeries.CreateOrDeleteSpritesBasedOnPointValues();
			theSeries.UpdateLineColor();
			theSeries.UpdatePointColor();
			theSeries.UpdateLineScale();
			theSeries.UpdatePointWidthHeight();
			theSeries.UpdateHideLines();
			theSeries.UpdateHidePoints();
			theSeries.UpdateSeriesName();
			theSeries.UpdateLinePadding();
			theSeries.UpdateSprites( prevPoints);
		}
	}
	
	void checkCache() {
		updateCacheAndFlag<orientationTypes>(ref cachedOrientationType, orientationType, ref orientationTypeChanged);
		updateCacheAndFlag<axesTypes>(ref cachedAxesType, axesType, ref axesTypeChanged);
		updateCacheAndFlag<graphTypes>(ref cachedGraphType, graphType, ref graphTypeChanged);

		updateCacheAndFlag<resizeTypes>(ref cachedResizeType, resizeType, ref resizeChanged);
		updateCacheAndFlag<ResizeProperties>(ref cachedResizeProperties, resizeProperties, ref resizeChanged);
		updateCacheAndFlag<float>(ref cachedContainerWidth, getSpriteWidth(this.gameObject), ref xAxisLengthChanged);
		updateCacheAndFlag<float>(ref cachedContainerHeight, getSpriteHeight(this.gameObject), ref yAxisLengthChanged);
		if (xAxisLengthChanged || yAxisLengthChanged) resizeChanged = true;

		updateCacheAndFlag<Vector2>(ref cachedPaddingLeftRight, paddingLeftRight, ref xAxisLengthChanged);
		updateCacheAndFlag<Vector2>(ref cachedPaddingTopBottom, paddingTopBottom, ref yAxisLengthChanged);
		if (xAxisLengthChanged || yAxisLengthChanged) paddingChanged = true;

		updateCacheAndFlag<labelTypes>(ref cachedXLabelType, xLabelType, ref xLabelTypeChanged);
		updateCacheAndFlag<labelTypes>(ref cachedYLabelType, yLabelType, ref yLabelTypeChanged);

		updateCacheAndFlag<bool>(ref cachedUseGroups, useGroups, ref groupsChanged);
		updateCacheAndFlag<bool>(ref cachedGroupsCentered, groupsCentered, ref groupsChanged);
		updateCacheAndFlagList<string>(ref cachedGroups, groups, ref groupsChanged);

		updateCacheAndFlag<bool>(ref cachedTooltipEnabled, tooltipEnabled, ref tooltipEnabledChanged);
		updateCacheAndFlag<bool>(ref cachedAutoAnimationsEnabled, autoAnimationsEnabled, ref autoAnimationsEnabledChanged);
		updateCacheAndFlag<int>(ref cachedLineSeriesCount, lineSeries.Count, ref lineSeriesCountChanged);
		updateCacheAndFlag<float>(ref cachedBarAxisValue, barAxisValue, ref barAxisValueChanged);
		updateCacheAndFlag<bool>(ref cachedAutoUpdateBarAxisValue, autoUpdateBarAxisValue, ref barAxisValueChanged);
		updateCacheAndFlag<Vector2>(ref cachedTheOrigin, theOrigin, ref theOriginChanged);
		updateCacheAndFlag<bool>(ref cachedAutoUpdateOrigin, autoUpdateOrigin, ref theOriginChanged);
		updateCacheAndFlag<float>(ref cachedYAxisMaxValue, yAxisMaxValue, ref yAxisMaxValueChanged);
		updateCacheAndFlag<float>(ref cachedYAxisMinValue, yAxisMinValue, ref yAxisMinValueChanged);
		updateCacheAndFlag<int>(ref cachedYAxisNumTicks, yAxisNumTicks, ref yAxisNumTicksChanged);
		updateCacheAndFlag<float>(ref cachedXAxisMaxValue, xAxisMaxValue, ref xAxisMaxValueChanged);
		updateCacheAndFlag<float>(ref cachedXAxisMinValue, xAxisMinValue, ref xAxisMinValueChanged);
		updateCacheAndFlag<int>(ref cachedXAxisNumTicks, xAxisNumTicks, ref xAxisNumTicksChanged);
		
		updateCacheAndFlag<bool>(ref cachedYMinMaxAutoGrow[0], yMinMaxAutoGrow[0], ref autoGrowShrinkChanged);
		updateCacheAndFlag<bool>(ref cachedYMinMaxAutoGrow[1], yMinMaxAutoGrow[1], ref autoGrowShrinkChanged);
		updateCacheAndFlag<bool>(ref cachedYMinMaxAutoShrink[0], yMinMaxAutoShrink[0], ref autoGrowShrinkChanged);
		updateCacheAndFlag<bool>(ref cachedYMinMaxAutoShrink[1], yMinMaxAutoShrink[1], ref autoGrowShrinkChanged);
		updateCacheAndFlag<bool>(ref cachedXMinMaxAutoGrow[0], xMinMaxAutoGrow[0], ref autoGrowShrinkChanged);
		updateCacheAndFlag<bool>(ref cachedXMinMaxAutoGrow[1], xMinMaxAutoGrow[1], ref autoGrowShrinkChanged);
		updateCacheAndFlag<bool>(ref cachedXMinMaxAutoShrink[0], xMinMaxAutoShrink[0], ref autoGrowShrinkChanged);
		updateCacheAndFlag<bool>(ref cachedXMinMaxAutoShrink[1], xMinMaxAutoShrink[1], ref autoGrowShrinkChanged);
		updateCacheAndFlag<float>(ref cachedAutoShrinkAtPercent, autoShrinkAtPercent, ref autoGrowShrinkChanged);
		updateCacheAndFlag<float>(ref cachedAutoGrowAndShrinkByPercent, autoGrowAndShrinkByPercent, ref autoGrowShrinkChanged);
		
		updateCacheAndFlag<float>(ref cachedBarWidth, barWidth, ref barWidthChanged);
		updateCacheAndFlag<bool>(ref cachedAutoUpdateBarWidth, autoUpdateBarWidth, ref barWidthChanged);
		updateCacheAndFlag<int>(ref cachedAxisWidth, axisWidth, ref axisWidthChanged);
		
		updateCacheAndFlag<bool>(ref cachedHideXTicks, hideXTicks, ref hideXTicksChanged);
		updateCacheAndFlag<bool>(ref cachedHideYTicks, hideYTicks, ref hideYTicksChanged);
		updateCacheAndFlag<bool>(ref cachedHideXGrid, hideXGrid, ref hideXGridChanged);
		updateCacheAndFlag<bool>(ref cachedHideYGrid, hideYGrid, ref hideYGridChanged);
		updateCacheAndFlag<bool>(ref cachedHideXLabels, hideXLabels, ref hideXLabelsChanged);
		updateCacheAndFlag<bool>(ref cachedHideYLabels, hideYLabels, ref hideYLabelsChanged);
		updateCacheAndFlag<bool>(ref cachedSetYLabelsUsingMaxMin, SetYLabelsUsingMaxMin, ref SetYLabelsUsingMaxMinChanged);
		updateCacheAndFlag<int>(ref cachedNumDecimalsYAxisLabels, numDecimalsYAxisLabels, ref numDecimalsYAxisLabelsChanged);
		updateCacheAndFlag<bool>(ref cachedSetXLabelsUsingMaxMin, SetXLabelsUsingMaxMin, ref SetXLabelsUsingMaxMinChanged);
		updateCacheAndFlag<int>(ref cachedNumDecimalsXAxisLabels, numDecimalsXAxisLabels, ref numDecimalsXAxisLabelsChanged);
		updateCacheAndFlag<float>(ref cachedYAxisLabelSize, yAxisLabelSize, ref yAxisLabelSizeChanged);
		updateCacheAndFlag<float>(ref cachedXAxisLabelSize, xAxisLabelSize, ref xAxisLabelSizeChanged);
		updateCacheAndFlag<bool>(ref cachedXAxisArrows[0], xAxisArrows[0], ref xAxisArrowsChanged);
		updateCacheAndFlag<bool>(ref cachedXAxisArrows[1], xAxisArrows[1], ref xAxisArrowsChanged);
		updateCacheAndFlag<bool>(ref cachedYAxisArrows[0], yAxisArrows[0], ref yAxisArrowsChanged);
		updateCacheAndFlag<bool>(ref cachedYAxisArrows[1], yAxisArrows[1], ref yAxisArrowsChanged);
		updateCacheAndFlag<float>(ref cachedXAxisLinePadding, xAxisLinePadding, ref xAxisLinePaddingChanged);
		updateCacheAndFlag<float>(ref cachedYAxisLinePadding, yAxisLinePadding, ref yAxisLinePaddingChanged);
		updateCacheAndFlag<bool>(ref cachedHideYTick, hideYTick, ref hideYTickChanged);
		updateCacheAndFlag<bool>(ref cachedHideXTick, hideXTick, ref hideXTickChanged);
		updateCacheAndFlag<Vector2>(ref cachedTickSize, tickSize, ref tickSizeChanged);
		updateCacheAndFlagList<string>(ref cachedYAxisLabels, yAxisLabels, ref yAxisLabelsChanged);
		updateCacheAndFlagList<string>(ref cachedXAxisLabels, xAxisLabels, ref xAxisLabelsChanged);

		updateCacheAndFlag<float>(ref cachedYAxisLabelSpacingY, yAxisLabelSpacingY, ref yAxisLabelSpacingYChanged);
		updateCacheAndFlag<float>(ref cachedYAxisLabelSpacingX, yAxisLabelSpacingX, ref yAxisLabelSpacingXChanged);
		updateCacheAndFlag<float>(ref cachedXAxisLabelSpacingY, xAxisLabelSpacingY, ref xAxisLabelSpacingYChanged);
		updateCacheAndFlag<float>(ref cachedXAxisLabelSpacingX, xAxisLabelSpacingX, ref xAxisLabelSpacingXChanged);

		updateCacheAndFlag<float>(ref cachedYAxisLabelRotation, yAxisLabelRotation, ref yAxisLabelRotationChanged);
		updateCacheAndFlag<float>(ref cachedXAxisLabelRotation, xAxisLabelRotation, ref xAxisLabelRotationChanged);
		updateCacheAndFlag<float>(ref cachedYAxisLabelDistBetween, yAxisLabelDistBetween, ref yAxisLabelDistBetweenChanged);
		updateCacheAndFlag<float>(ref cachedXAxisLabelDistBetween, xAxisLabelDistBetween, ref xAxisLabelDistBetweenChanged);

		updateCacheAndFlag<bool>(ref cachedYAxisTicksRight, yAxisTicksRight, ref yAxisTicksRightChanged);
		updateCacheAndFlag<bool>(ref cachedXAxisTicksAbove, xAxisTicksAbove, ref xAxisTicksAboveChanged);
		updateCacheAndFlag<int>(ref cachedXAxisYTick, xAxisYTick, ref xAxisYTickChanged);
		updateCacheAndFlag<int>(ref cachedYAxisXTick, yAxisXTick, ref yAxisXTickChanged);
		updateCacheAndFlag<float>(ref cachedXAxisNonTickPercent, xAxisNonTickPercent, ref xAxisNonTickPercentChanged);
		updateCacheAndFlag<float>(ref cachedYAxisNonTickPercent, yAxisNonTickPercent, ref yAxisNonTickPercentChanged);
		updateCacheAndFlag<bool>(ref cachedXAxisUseNonTickPercent, xAxisUseNonTickPercent, ref xAxisUseNonTickPercentChanged);
		updateCacheAndFlag<bool>(ref cachedYAxisUseNonTickPercent, yAxisUseNonTickPercent, ref yAxisUseNonTickPercentChanged);
		
		// Titles
		updateCacheAndFlag<string>(ref cachedGraphTitleString, graphTitleString, ref graphTitleChanged);
		updateCacheAndFlag<string>(ref cachedYAxisTitleString, yAxisTitleString, ref yAxisTitleChanged);
		updateCacheAndFlag<string>(ref cachedXAxisTitleString, xAxisTitleString, ref xAxisTitleChanged);
		updateCacheAndFlag<Vector2>(ref cachedGraphTitleOffset, graphTitleOffset, ref graphTitleChanged);
		updateCacheAndFlag<Vector2>(ref cachedYAxisTitleOffset, yAxisTitleOffset, ref yAxisTitleChanged);
		updateCacheAndFlag<Vector2>(ref cachedXAxisTitleOffset, xAxisTitleOffset, ref xAxisTitleChanged);
		
		updateCacheAndFlag<int>(ref cachedNumYAxisLabels, yAxisTicks.GetComponent<WMG_Grid>().gridNumNodesY, ref numYAxisLabelsChanged);
		updateCacheAndFlag<int>(ref cachedNumXAxisLabels, xAxisTicks.GetComponent<WMG_Grid>().gridNumNodesX, ref numXAxisLabelsChanged);

		// Legend
		legend.checkCache();
		if (xAxisLengthChanged || yAxisLengthChanged) {
			legend.setLegendChanged();
		}

		// Series
		for (int j = 0; j < lineSeries.Count; j++) {
			if (!activeInHierarchy(lineSeries[j])) continue;
			WMG_Series theSeries = lineSeries[j].GetComponent<WMG_Series>();
			
			theSeries.checkCache();
			
			if (yAxisMaxValueChanged || yAxisMinValueChanged || yAxisLengthChanged ||
				xAxisMaxValueChanged || xAxisMinValueChanged || xAxisLengthChanged || barWidthChanged || barAxisValueChanged ||
			    groupsChanged) {
				theSeries.setPointValuesChanged(true);
			}
			
			if (theSeries.getPointValuesChanged()) aSeriesPointsChanged = true;
		}
	}
	
	void setCacheFlags(bool val) {
		aSeriesPointsChanged = val;
		orientationTypeChanged = val;
		axesTypeChanged = val;
		graphTypeChanged = val;
		resizeChanged = val;
		xLabelTypeChanged = val;
		yLabelTypeChanged = val;
		groupsChanged = val;
		tooltipEnabledChanged = val;
		autoAnimationsEnabledChanged = val;
		lineSeriesCountChanged = val;
		barAxisValueChanged = val;
		theOriginChanged = val;
		yAxisMaxValueChanged = val;
		yAxisMinValueChanged = val;
		yAxisLengthChanged = val;
		yAxisNumTicksChanged = val;
		xAxisMaxValueChanged = val;
		xAxisMinValueChanged = val;
		xAxisLengthChanged = val;
		xAxisNumTicksChanged = val;
		autoGrowShrinkChanged = val;
		barWidthChanged = val;
		axisWidthChanged = val;
		
		hideXTicksChanged = val;
		hideYTicksChanged = val;
		hideXGridChanged = val;
		hideYGridChanged = val;
		hideXLabelsChanged = val;
		hideYLabelsChanged = val;
		SetYLabelsUsingMaxMinChanged = val;
		numDecimalsYAxisLabelsChanged = val;
		SetXLabelsUsingMaxMinChanged = val;
		numDecimalsXAxisLabelsChanged = val;
		yAxisLabelSizeChanged = val;
		xAxisLabelSizeChanged = val;
		xAxisArrowsChanged = val;
		yAxisArrowsChanged = val;
		xAxisLinePaddingChanged = val;
		yAxisLinePaddingChanged = val;
		hideYTickChanged = val;
		hideXTickChanged = val;
		tickSizeChanged = val;
		yAxisLabelsChanged = val;
		xAxisLabelsChanged = val;
		yAxisLabelSpacingYChanged = val;
		yAxisLabelSpacingXChanged = val;
		xAxisLabelSpacingYChanged = val;
		xAxisLabelSpacingXChanged = val;

		yAxisLabelRotationChanged = val;
		xAxisLabelRotationChanged = val;
		yAxisLabelDistBetweenChanged = val;
		xAxisLabelDistBetweenChanged = val;
		
		yAxisTicksRightChanged = val;
		xAxisTicksAboveChanged = val;
		xAxisYTickChanged = val;
		yAxisXTickChanged = val;
		xAxisNonTickPercentChanged = val;
		yAxisNonTickPercentChanged = val;
		xAxisUseNonTickPercentChanged = val;
		yAxisUseNonTickPercentChanged = val;

		paddingChanged = val;
		
		graphTitleChanged = val;
		yAxisTitleChanged = val;
		xAxisTitleChanged = val;
		
		numYAxisLabelsChanged = val;
		numXAxisLabelsChanged = val;

		legend.setCacheFlags(val);
		
		for (int j = 0; j < lineSeries.Count; j++) {
			if (!activeInHierarchy(lineSeries[j])) continue;
			WMG_Series theSeries = lineSeries[j].GetComponent<WMG_Series>();
			
			theSeries.setCacheFlags(val);
		}
	}
	
	void UpdateOrientation() {
		if (orientationTypeChanged) {

			SwapVals<labelTypes>(ref xLabelType, ref yLabelType);
			SwapVals<float>(ref xAxisMaxValue, ref yAxisMaxValue);
			SwapVals<float>(ref xAxisMinValue, ref yAxisMinValue);
			SwapVals<int>(ref xAxisNumTicks, ref yAxisNumTicks);
			SwapVals<int>(ref numDecimalsXAxisLabels, ref numDecimalsYAxisLabels);
			SwapVals<bool>(ref xMinMaxAutoGrow[0], ref yMinMaxAutoGrow[0]);
			SwapVals<bool>(ref xMinMaxAutoGrow[1], ref yMinMaxAutoGrow[1]);
			SwapVals<bool>(ref xMinMaxAutoShrink[0], ref yMinMaxAutoShrink[0]);
			SwapVals<bool>(ref xMinMaxAutoShrink[1], ref yMinMaxAutoShrink[1]);
			SwapVals<bool>(ref SetXLabelsUsingMaxMin, ref SetYLabelsUsingMaxMin);
			SwapVals<float>(ref yAxisLabelSpacingY, ref xAxisLabelSpacingX);
			SwapVals<string>(ref yAxisTitleString, ref xAxisTitleString);
			SwapValsList<string>(ref xAxisLabels, ref yAxisLabels);

			for (int j = 0; j < lineSeries.Count; j++) {
				if (!activeInHierarchy(lineSeries[j])) continue;
				WMG_Series theSeries = lineSeries[j].GetComponent<WMG_Series>();
				theSeries.dataLabelsOffset = new Vector2(theSeries.dataLabelsOffset.y, theSeries.dataLabelsOffset.x);
			}
			
			yAxisTicks.GetComponent<WMG_Grid>().gridNumNodesY = yAxisNumTicks;
			yAxisTicks.GetComponent<WMG_Grid>().Refresh();
			xAxisTicks.GetComponent<WMG_Grid>().gridNumNodesX = xAxisNumTicks;
			xAxisTicks.GetComponent<WMG_Grid>().Refresh();
			
			checkCache();
			
			for (int j = 0; j < lineSeries.Count; j++) {
				if (!activeInHierarchy(lineSeries[j])) continue;
				WMG_Series theSeries = lineSeries[j].GetComponent<WMG_Series>();
				theSeries.setAnimatingFromPreviousData(); // If automatic animations set, then set flag to animate for each series
				
			}
		}
	}
	
	void UpdateAxesType() {
		if (axesType == axesTypes.MANUAL) {
			// Don't do anything with the axes position related variables
		}
		else if (axesType == axesTypes.AUTO_ORIGIN || axesType == axesTypes.AUTO_ORIGIN_X || axesType == axesTypes.AUTO_ORIGIN_Y) {
			// Automatically position axes relative to the origin
			updateAxesRelativeToOrigin();
		}
		else {
			// Automatically position origin relative to the axes
			updateOriginRelativeToAxes();
			// These are the static axes types (axes dont change based on the min and max values)
			if (axesTypeChanged || yAxisNumTicksChanged || xAxisNumTicksChanged || yAxisUseNonTickPercentChanged || xAxisUseNonTickPercentChanged) {
				if (axesType == axesTypes.I || axesType == axesTypes.II || axesType == axesTypes.III || axesType == axesTypes.IV) {
					// These axes types should always position based on the edge
					if (axesType == axesTypes.I) {
						setAxesQuadrant1();
					}
					else if (axesType == axesTypes.II) {
						setAxesQuadrant2();
					}
					else if (axesType == axesTypes.III) {
						setAxesQuadrant3();
					}
					else if (axesType == axesTypes.IV) {
						setAxesQuadrant4();
					}
				}
				else {
					// These axes types may not necessarily have an axis on the edge
					// Set the x / y axisUseNonTickPercent to true to not constrain the axes to a tick
					if (axesType == axesTypes.CENTER) {
						setAxesQuadrant1_2_3_4();
					}
					else if (axesType == axesTypes.I_II) {
						setAxesQuadrant1_2();
					}
					else if (axesType == axesTypes.III_IV) {
						setAxesQuadrant3_4();
					}
					else if (axesType == axesTypes.II_III) {
						setAxesQuadrant2_3();
					}
					else if (axesType == axesTypes.I_IV) {
						setAxesQuadrant1_4();
					}
					// Ensure tick is not hidden if percent is being used and num ticks is even
					if (xAxisUseNonTickPercent && yAxisNumTicks % 2 == 0) {
						hideYTick = false;
					}
					if (yAxisUseNonTickPercent && xAxisNumTicks % 2 == 0) {
						hideXTick = false;
					}
				}
			}
		}
	}

	void updateOriginRelativeToAxes() {
		if (axesTypeChanged || theOriginChanged || yAxisUseNonTickPercentChanged || xAxisUseNonTickPercentChanged ||
		    xAxisMinValueChanged || xAxisMaxValueChanged || yAxisMinValueChanged || yAxisMaxValueChanged || barAxisValueChanged) {
			if (autoUpdateOrigin) {
				if (axesType == axesTypes.I) {
					theOrigin = new Vector2(xAxisMinValue, yAxisMinValue);
				}
				else if (axesType == axesTypes.II) {
					theOrigin = new Vector2(xAxisMaxValue, yAxisMinValue);
				}
				else if (axesType == axesTypes.III) {
					theOrigin = new Vector2(xAxisMaxValue, yAxisMaxValue);
				}
				else if (axesType == axesTypes.IV) {
					theOrigin = new Vector2(xAxisMinValue, yAxisMaxValue);
				}
				else if (axesType == axesTypes.CENTER) {
					theOrigin = new Vector2((xAxisMaxValue + xAxisMinValue) / 2, (yAxisMaxValue + yAxisMinValue) / 2);
				}
				else if (axesType == axesTypes.I_II) {
					theOrigin = new Vector2((xAxisMaxValue + xAxisMinValue) / 2, yAxisMinValue);
				}
				else if (axesType == axesTypes.III_IV) {
					theOrigin = new Vector2((xAxisMaxValue + xAxisMinValue) / 2, yAxisMaxValue);
				}
				else if (axesType == axesTypes.II_III) {
					theOrigin = new Vector2(xAxisMaxValue, (yAxisMaxValue + yAxisMinValue) / 2);
				}
				else if (axesType == axesTypes.I_IV) {
					theOrigin = new Vector2(xAxisMinValue, (yAxisMaxValue + yAxisMinValue) / 2);
				}
			}
			if (autoUpdateBarAxisValue) {
				if (orientationType == orientationTypes.vertical) {
					barAxisValue = theOrigin.y;
				}
				else {
					barAxisValue = theOrigin.x;
				}
			}
		}
	}
	
	void updateAxesRelativeToOrigin() {
		if (axesTypeChanged || theOriginChanged || yAxisUseNonTickPercentChanged || xAxisUseNonTickPercentChanged ||
		    xAxisMinValueChanged || xAxisMaxValueChanged || xAxisNumTicksChanged || xAxisYTickChanged ||
		    yAxisMinValueChanged || yAxisMaxValueChanged || yAxisNumTicksChanged || yAxisXTickChanged) {
			// Y axis
			if (axesType == axesTypes.AUTO_ORIGIN || axesType == axesTypes.AUTO_ORIGIN_Y) {
				if (xAxisMinValue >= theOrigin.x) {
					yAxisXTick = 0;
					yAxisNonTickPercent = 0;
					// On left side, don't hide tick and show right arrow
					hideYTick = false;
					yAxisTicksRight = false;
					xAxisArrows[0] = true;
	       			xAxisArrows[1] = false;
				}
				else if (xAxisMaxValue <= theOrigin.x) {
					yAxisXTick = xAxisNumTicks - 1;
					yAxisNonTickPercent = 1;
					// On right side, don't hide tick and show left arrow
					hideYTick = false;
					yAxisTicksRight = true;
					xAxisArrows[0] = false;
	       			xAxisArrows[1] = true;
				}
				else {
					yAxisXTick = Mathf.RoundToInt((theOrigin.x - xAxisMinValue) / (xAxisMaxValue - xAxisMinValue) * (xAxisNumTicks - 1));
					yAxisNonTickPercent = (theOrigin.x - xAxisMinValue) / (xAxisMaxValue - xAxisMinValue);
					// Somewhere in between, show both arrows
					hideYTick = true;
					yAxisTicksRight = false;
					xAxisArrows[0] = true;
	       			xAxisArrows[1] = true;
				}
			}
			
			// X axis
			if (axesType == axesTypes.AUTO_ORIGIN || axesType == axesTypes.AUTO_ORIGIN_X) {
				if (yAxisMinValue >= theOrigin.y) {
					xAxisYTick = 0;
					xAxisNonTickPercent = 0;
					// On the bottom, don't hide tick and show top arrow
					hideXTick = false;
					xAxisTicksAbove = false;
					yAxisArrows[0] = true;
	       			yAxisArrows[1] = false;
				}
				else if (yAxisMaxValue <= theOrigin.y) {
					xAxisYTick = yAxisNumTicks - 1;
					xAxisNonTickPercent = 1;
					// On the top, don't hide tick and show bottom arrow
					hideXTick = false;
					xAxisTicksAbove = true;
					yAxisArrows[0] = false;
			        yAxisArrows[1] = true;
				}
				else {
					xAxisYTick = Mathf.RoundToInt((theOrigin.y - yAxisMinValue) / (yAxisMaxValue - yAxisMinValue) * (yAxisNumTicks - 1));
					xAxisNonTickPercent = (theOrigin.y - yAxisMinValue) / (yAxisMaxValue - yAxisMinValue);
					// Somewhere in between, show both arrows
					hideXTick = true;
					xAxisTicksAbove = false;
					yAxisArrows[0] = true;
			        yAxisArrows[1] = true;
				}
			}
			if (autoUpdateBarAxisValue) {
				if (orientationType == orientationTypes.vertical) {
					barAxisValue = theOrigin.y;
				}
				else {
					barAxisValue = theOrigin.x;
				}
			}
		}
	}
	
	void UpdateAxesMinMaxValues() {
		if (autoGrowShrinkChanged || orientationTypeChanged || graphTypeChanged || aSeriesPointsChanged ||
			yAxisMaxValueChanged || yAxisMinValueChanged || xAxisMaxValueChanged || xAxisMinValueChanged ||
		    lineSeriesCountChanged) {
			
			if (!xMinMaxAutoGrow[0] && !xMinMaxAutoGrow[1] && !xMinMaxAutoShrink[0] && !xMinMaxAutoShrink[1] &&
				!yMinMaxAutoGrow[0] && !yMinMaxAutoGrow[1] && !yMinMaxAutoShrink[0] && !yMinMaxAutoShrink[1]) return;
			float minX = Mathf.Infinity;
			float maxX = Mathf.NegativeInfinity;
			float minY = Mathf.Infinity;
			float maxY = Mathf.NegativeInfinity;
			for (int j = 0; j < lineSeries.Count; j++) {
				if (!activeInHierarchy(lineSeries[j])) continue;
				WMG_Series theSeries = lineSeries[j].GetComponent<WMG_Series>();
				// Find the current max and min point value data
				if (orientationType == orientationTypes.vertical) {
					for (int i = 0; i < theSeries.pointValues.Count; i++) {
						if (theSeries.pointValues[i].x < minX) minX = theSeries.pointValues[i].x;
						if (theSeries.pointValues[i].y < minY) minY = theSeries.pointValues[i].y;
						if (theSeries.pointValues[i].x > maxX) maxX = theSeries.pointValues[i].x;
						if (theSeries.pointValues[i].y > maxY) maxY = theSeries.pointValues[i].y;
						if (graphType == graphTypes.bar_stacked) {
							if (totalPointValues[i] + yAxisMinValue > maxY) maxY = totalPointValues[i] + yAxisMinValue;
						}
					}
				}
				else {
					for (int i = 0; i < theSeries.pointValues.Count; i++) {
						if (theSeries.pointValues[i].y < minX) minX = theSeries.pointValues[i].y;
						if (theSeries.pointValues[i].x < minY) minY = theSeries.pointValues[i].x;
						if (theSeries.pointValues[i].y > maxX) maxX = theSeries.pointValues[i].y;
						if (theSeries.pointValues[i].x > maxY) maxY = theSeries.pointValues[i].x;
						if (graphType == graphTypes.bar_stacked) {
							if (totalPointValues[i] + xAxisMinValue > maxX) maxX = totalPointValues[i] + xAxisMinValue;
						}
					}
				}
			}
			// If point data outside axis max / min then grow, if the point data significantly (percentage of total axis length variable) less than axis min / max then srhink 
			// y-axis
			if (yMinMaxAutoGrow[0] || yMinMaxAutoGrow[1] || yMinMaxAutoShrink[0] || yMinMaxAutoShrink[1]) {
				if (minY == maxY || minY == Mathf.Infinity || maxY == Mathf.NegativeInfinity) return;
				float origMax = yAxisMaxValue;
				float origMin = yAxisMinValue;
				// grow - max
				if (yMinMaxAutoGrow[1] && maxY > origMax) {
					AutoSetAxisMinMax(true, maxY, minY, true, true, origMin, origMax);
				}
				// grow - min
				if (yMinMaxAutoGrow[0] && minY < origMin) {
					AutoSetAxisMinMax(true, minY, maxY, false, true, origMin, origMax);
				}
				// shrink - max
				if (yMinMaxAutoShrink[1] && autoShrinkAtPercent > (maxY - origMin) / (origMax - origMin) ) {
					AutoSetAxisMinMax(true, maxY, minY, true, false, origMin, origMax);
				}
				// shrink - min
				if (yMinMaxAutoShrink[0] && autoShrinkAtPercent > (origMax - minY) / (origMax - origMin) ) {
					AutoSetAxisMinMax(true, minY, maxY, false, false, origMin, origMax);
				}
			}
			// x-axis
			if (xMinMaxAutoGrow[0] || xMinMaxAutoGrow[1] || xMinMaxAutoShrink[0] || xMinMaxAutoShrink[1]) {
				if (minX == maxX || minX == Mathf.Infinity || maxX == Mathf.NegativeInfinity) return;
				float origMax = xAxisMaxValue;
				float origMin = xAxisMinValue;
				// grow - max
				if (xMinMaxAutoGrow[1] && maxX > origMax) {
					AutoSetAxisMinMax(false, maxX, minX, true, true, origMin, origMax);
				}
				// grow - min
				if (xMinMaxAutoGrow[0] && minX < origMin) {
					AutoSetAxisMinMax(false, minX, maxX, false, true, origMin, origMax);
				}
				// shrink - max
				if (xMinMaxAutoShrink[1] && autoShrinkAtPercent > (maxX - origMin) / (origMax - origMin) ) {
					AutoSetAxisMinMax(false, maxX, minX, true, false, origMin, origMax);
				}
				// shrink - min
				if (xMinMaxAutoShrink[0] && autoShrinkAtPercent > (origMax - minX) / (origMax - origMin) ) {
					AutoSetAxisMinMax(false, minX, maxX, false, false, origMin, origMax);
				}
			}
		}
	}

	void UpdateAxesGridsAndTicks() {
		if (yAxisNumTicksChanged || xAxisNumTicksChanged || yAxisLengthChanged || xAxisLengthChanged || tickSizeChanged ||
		    yAxisTicksRightChanged || xAxisTicksAboveChanged || axisWidthChanged || xAxisYTickChanged || yAxisXTickChanged || 
		    xAxisUseNonTickPercentChanged || yAxisUseNonTickPercentChanged || xAxisNonTickPercentChanged || yAxisNonTickPercentChanged ||
		    yAxisLinePaddingChanged || xAxisLinePaddingChanged || yAxisArrowsChanged || xAxisArrowsChanged || numYAxisLabelsChanged || numXAxisLabelsChanged ||
		    hideYTicksChanged || hideXTicksChanged || hideYGridChanged || hideXGridChanged) {

			// Calculate variables used in axis and grid positions
			UpdateAxisAndGridVariables();

			// Hide grids
			SetActive(verticalGridLines, !hideXGrid);
			SetActive(horizontalGridLines, !hideYGrid);

			if (!hideXGrid) {
				// Update vertical grid lines
				WMG_Grid vGridLines = verticalGridLines.GetComponent<WMG_Grid>();
				vGridLines.gridNumNodesX = xAxisNumTicks;
				vGridLines.gridLinkLengthX = xGridLineLength;
				vGridLines.gridLinkLengthY = yAxisLength;
				vGridLines.Refresh();
			}

			if (!hideYGrid) {
				// Update horizontal grid lines
				WMG_Grid hGridLines = horizontalGridLines.GetComponent<WMG_Grid>();
				hGridLines.gridNumNodesY = yAxisNumTicks;
				hGridLines.gridLinkLengthY = yGridLineLength;
				hGridLines.gridLinkLengthX = xAxisLength;
				hGridLines.Refresh();
			}

			// Hide ticks
			SetActive(yAxisTicks, !hideYTicks);
			SetActive(xAxisTicks, !hideXTicks);

			if (!hideYTicks) {
				// Update y-axis ticks
				WMG_Grid yTicks = yAxisTicks.GetComponent<WMG_Grid>();
				yTicks.gridNumNodesY = yAxisNumTicks;
				yTicks.gridLinkLengthY = yGridLineLength;
				yTicks.Refresh();

				if (!yAxisTicksRight) {
					changeSpritePositionToX(yAxisTicks, yAxisPercentagePosition * xAxisLength - axisWidth / 2 - tickSize.y / 2 );
				}
				else {
					changeSpritePositionToX(yAxisTicks, yAxisPercentagePosition * xAxisLength + axisWidth / 2 + tickSize.y / 2);
				}

				foreach (WMG_Node node in getYAxisTicks()) {
					changeSpriteSize(node.objectToScale, Mathf.RoundToInt(tickSize.y), Mathf.RoundToInt(tickSize.x));
				}
			}

			if (!hideXTicks) {
				// Update x-axis ticks
				WMG_Grid xTicks = xAxisTicks.GetComponent<WMG_Grid>();
				xTicks.gridNumNodesX = xAxisNumTicks;
				xTicks.gridLinkLengthX = xGridLineLength;
				xTicks.Refresh();

				if (!xAxisTicksAbove) {
					changeSpritePositionToY(xAxisTicks, xAxisPercentagePosition * yAxisLength - axisWidth / 2 - tickSize.y / 2);
				}
				else {
					changeSpritePositionToY(xAxisTicks, xAxisPercentagePosition * yAxisLength + axisWidth / 2 + tickSize.y / 2);
				}

				// Update size of ticks
				foreach (WMG_Node node in getXAxisTicks()) {
					changeSpriteSize(node.objectToScale, Mathf.RoundToInt(tickSize.x), Mathf.RoundToInt(tickSize.y));
				}
			}


			// update axis visuals
			SetAxisVisuals(	true, yAxisLine, ref yAxisLinePaddingTot, yAxisLinePadding, yAxisArrows, yAxisLength, yAxisArrowU, yAxisArrowD);
			SetAxisVisuals(	false, xAxisLine, ref xAxisLinePaddingTot, xAxisLinePadding, xAxisArrows, xAxisLength, xAxisArrowR, xAxisArrowL);
		}
	}

	void UpdateAxisAndGridVariables() {
		// Ensure num ticks don't go below 1, update gridLineLength
		if (yAxisNumTicks <= 1) {
			yAxisNumTicks = 1;
			yGridLineLength = 0;
		}
		else {
			yGridLineLength = yAxisLength / (yAxisNumTicks-1);
		}
		if (xAxisNumTicks <= 1) {
			xAxisNumTicks = 1;
			xGridLineLength = 0;
		}
		else {
			xGridLineLength = xAxisLength / (xAxisNumTicks-1);
		}
		
		// update yAxisPercentagePosition
		if (yAxisUseNonTickPercent) { // position axis based on the percentage specified
			yAxisPercentagePosition = yAxisNonTickPercent;
		}
		else { // position axis based on the number of ticks and the specified tick
			if (xAxisNumTicks == 1) yAxisPercentagePosition = 1;
			else yAxisPercentagePosition = yAxisXTick / (xAxisNumTicks - 1f);
		}
		
		// update xAxisPercentagePosition
		if (xAxisUseNonTickPercent) { // position axis based on the percentage specified
			xAxisPercentagePosition = xAxisNonTickPercent;
		}
		else { // position axis based on the number of ticks and the specified tick
			if (yAxisNumTicks == 1) xAxisPercentagePosition = 1;
			else xAxisPercentagePosition = xAxisYTick / (yAxisNumTicks - 1f);
		}
	}

	void SetAxisVisuals(bool isY, GameObject AxisLine, ref float AxisLinePaddingTot, float AxisLinePadding, 
	                    bool[] AxisArrows, float axisLength,GameObject topRightArrow, GameObject bottomLeftArrow) {
		
		AxisLinePaddingTot = 2 * AxisLinePadding;
		float axisRepos = 0;
		if (!AxisArrows[0]) AxisLinePaddingTot -= AxisLinePadding;
		else axisRepos += AxisLinePadding / 2f;
		if (!AxisArrows[1]) AxisLinePaddingTot -= AxisLinePadding;
		else axisRepos -= AxisLinePadding / 2f;
		
		if (isY) {
			changeSpriteSize(AxisLine, axisWidth, Mathf.RoundToInt(axisLength + AxisLinePaddingTot));
			
			changeSpritePositionTo(yAxisLine, new Vector3(0, axisRepos + axisLength/2, 0));

			changeSpritePositionToX(yAxis, yAxisPercentagePosition * xAxisLength);
		}
		else {
			changeSpriteSize(AxisLine, Mathf.RoundToInt(axisLength + AxisLinePaddingTot), axisWidth);

			changeSpritePositionTo(xAxisLine, new Vector3(axisRepos + axisLength/2, 0, 0));

			changeSpritePositionToY(xAxis, xAxisPercentagePosition * yAxisLength);
		}
		
		// Update Arrows
		SetActiveAnchoredSprite(topRightArrow,AxisArrows[0]);
		SetActiveAnchoredSprite(bottomLeftArrow,AxisArrows[1]);
	}

	
	void UpdateAxesLabels() {
		// y-axis
		if (orientationTypeChanged || graphTypeChanged || yAxisTicksRightChanged || yAxisLabelSpacingYChanged || yAxisLabelSpacingXChanged || barWidthChanged ||
			yAxisNumTicksChanged || hideYTickChanged || xAxisYTickChanged || hideYLabelsChanged || yAxisLabelSizeChanged || numYAxisLabelsChanged ||
			SetYLabelsUsingMaxMinChanged || yAxisMaxValueChanged || yAxisMinValueChanged || numDecimalsYAxisLabelsChanged || yAxisLabelsChanged ||
		    yAxisLengthChanged || yAxisLabelRotationChanged || yLabelTypeChanged || groupsChanged || yAxisLabelDistBetweenChanged ||
		    yAxisNonTickPercentChanged || yAxisUseNonTickPercentChanged)
		{
			SetAxisLabels(	true, yAxisLabelObjs, ref yAxisLabels, yAxisNumTicks, hideYTick, xAxisYTick, hideYLabels, yAxisLabelSize, 
			              SetYLabelsUsingMaxMin, yAxisMaxValue, yAxisMinValue, numDecimalsYAxisLabels, 
			              ref yAxisLabelSpacingY, yAxisLength, yLabelType);
		}
		// x-axis
		if (orientationTypeChanged || graphTypeChanged || xAxisTicksAboveChanged || xAxisLabelSpacingYChanged || xAxisLabelSpacingXChanged || barWidthChanged ||
			xAxisNumTicksChanged || hideXTickChanged || yAxisXTickChanged || hideXLabelsChanged || xAxisLabelSizeChanged || numXAxisLabelsChanged ||
			SetXLabelsUsingMaxMinChanged || xAxisMaxValueChanged || xAxisMinValueChanged || numDecimalsXAxisLabelsChanged || xAxisLabelsChanged ||
		    xAxisLengthChanged || xAxisLabelRotationChanged || xLabelTypeChanged || groupsChanged || xAxisLabelDistBetweenChanged || 
		    xAxisNonTickPercentChanged || xAxisUseNonTickPercentChanged)
		{
			SetAxisLabels(	false, xAxisLabelObjs, ref xAxisLabels, xAxisNumTicks, hideXTick, yAxisXTick, hideXLabels, xAxisLabelSize, 
			              SetXLabelsUsingMaxMin, xAxisMaxValue, xAxisMinValue, numDecimalsXAxisLabels,
			              ref xAxisLabelSpacingX, xAxisLength, xLabelType);
		}
	}
	
	void SetAxisLabels(	bool isY, GameObject AxisLabelObjs, ref List<string> AxisLabels, int numTicks, bool hideTick, 
	                   int axisTick, bool hideLabels, float labelSize, bool setUsingMaxMin, float axisMax, float axisMin, int numDecimals,
	                   ref float AxisLabelSpacing, float AxisLength, labelTypes LabelType) {
		// Calculate the number of labels we have
		int numLabels = 0;
		if (isY) {
			if (LabelType == labelTypes.ticks) numLabels = numTicks;
			else if (LabelType == labelTypes.ticks_center) numLabels = numTicks - 1;
			else if (LabelType == labelTypes.groups) numLabels = groups.Count;
			else numLabels = AxisLabels.Count;
		}
		else {
			if (LabelType == labelTypes.ticks) numLabels = numTicks;
			else if (LabelType == labelTypes.ticks_center) numLabels = numTicks - 1;
			else if (LabelType == labelTypes.groups) numLabels = groups.Count;
			else numLabels = AxisLabels.Count;
		}

		// Update spacing between labels
		float distBetween = getDistBetween(groups.Count);
		if (isY) {
			if (LabelType == labelTypes.ticks) yAxisLabelDistBetween = AxisLength / (numLabels - 1);
			else if (LabelType == labelTypes.ticks_center) yAxisLabelDistBetween = AxisLength / numLabels;
			else if (LabelType == labelTypes.groups) yAxisLabelDistBetween = distBetween;
		}
		else {
			if (LabelType == labelTypes.ticks) xAxisLabelDistBetween = AxisLength / (numLabels - 1);
			else if (LabelType == labelTypes.ticks_center) xAxisLabelDistBetween = AxisLength / numLabels;
			else if (LabelType == labelTypes.groups) xAxisLabelDistBetween = distBetween;
		}

		// Actually create or delete the labels and apply the spacing
		WMG_Grid axisLabelGrid = AxisLabelObjs.GetComponent<WMG_Grid>();
		if (isY) {
			axisLabelGrid.gridNumNodesY = numLabels;
			axisLabelGrid.gridLinkLengthY = yAxisLabelDistBetween;
			axisLabelGrid.Refresh();
		}
		else {
			axisLabelGrid.gridNumNodesX = numLabels;
			axisLabelGrid.gridLinkLengthX = xAxisLabelDistBetween;
			axisLabelGrid.Refresh();
		}

		// Create or delete strings based on number of labels
		for (int i = 0; i < numLabels; i++) {
			if (AxisLabels.Count <= i) {
				AxisLabels.Add("");
			}
		}
		for (int i = AxisLabels.Count - 1; i >= 0; i--) {
			if (i >= numLabels) {
				AxisLabels.RemoveAt(i);
			}
		}

		// Update xSpacingx and ySpacingy
		if (LabelType == labelTypes.ticks) AxisLabelSpacing = 0;
		else if (LabelType == labelTypes.ticks_center) {
			if (numTicks == 1) AxisLabelSpacing = 0;
			else AxisLabelSpacing = AxisLength / (numTicks - 1) / 2;
		}
		else if (LabelType == labelTypes.groups) {
			if (groupsCentered) {
				AxisLabelSpacing = distBetween / 2;
				if (graphType == graphTypes.bar_side) {
					AxisLabelSpacing += lineSeries.Count * barWidth / 2;
				}
				else if (graphType == graphTypes.bar_stacked) {
					AxisLabelSpacing += barWidth / 2;
				}
				else if (graphType == graphTypes.bar_stacked_percent) {
					AxisLabelSpacing += barWidth / 2;
				}
				if (isY) AxisLabelSpacing += 2; // todo
			}
			else AxisLabelSpacing = 0;
		}

		// Position the label parent objects
		if (isY) {
			if (!yAxisTicksRight) {
				changeSpritePositionToX(AxisLabelObjs, yAxisPercentagePosition * xAxisLength - tickSize.y / 2 - axisWidth / 2);
			}
			else {
				changeSpritePositionToX(AxisLabelObjs, yAxisPercentagePosition * xAxisLength + axisWidth / 2);
			}
		}
		else {
			if (!xAxisTicksAbove) {
				changeSpritePositionToY(AxisLabelObjs, xAxisPercentagePosition * yAxisLength - tickSize.y / 2 - axisWidth / 2);
			}
			else {
				changeSpritePositionToY(AxisLabelObjs, xAxisPercentagePosition * yAxisLength + axisWidth / 2);
			}
		}

		// Get the label objects, change their position, and set their text
		List<WMG_Node> LabelNodes = null;
		if (isY) LabelNodes = getYAxisLabels();
		else LabelNodes = getXAxisLabels();
		
		if (LabelNodes == null) return;

		for (int i = 0; i < AxisLabels.Count; i++) {
			if (i >= LabelNodes.Count) break;

			// Hide labels
			SetActive(LabelNodes[i].gameObject,!hideLabels);
			// Hide label that is the same as the axis
			if (LabelType == labelTypes.ticks) {
				if (hideTick && i == axisTick) SetActive(LabelNodes[axisTick].gameObject,false);
			}

			// Position the labels
			if (isY) {
				LabelNodes[i].objectToLabel.transform.localEulerAngles = new Vector3(0, 0, yAxisLabelRotation);
				if (!yAxisTicksRight) {
					changeSpritePivot(LabelNodes[i].objectToLabel, WMG_Graph_Manager.WMGpivotTypes.Right);
					changeSpritePositionTo(LabelNodes[i].objectToLabel, new Vector3(-yAxisLabelSpacingX, yAxisLabelSpacingY, 0));
				}
				else {
					changeSpritePivot(LabelNodes[i].objectToLabel, WMG_Graph_Manager.WMGpivotTypes.Left);
					changeSpritePositionTo(LabelNodes[i].objectToLabel, new Vector3(yAxisLabelSpacingX, yAxisLabelSpacingY, 0));
				}
			}
			else {
				LabelNodes[i].objectToLabel.transform.localEulerAngles = new Vector3(0, 0, xAxisLabelRotation);
				if (!xAxisTicksAbove) {
					changeSpritePivot(LabelNodes[i].objectToLabel, WMG_Graph_Manager.WMGpivotTypes.Center);
					changeSpritePositionTo(LabelNodes[i].objectToLabel, new Vector3(xAxisLabelSpacingX, -xAxisLabelSpacingY, 0));
				}
				else {
					changeSpritePivot(LabelNodes[i].objectToLabel, WMG_Graph_Manager.WMGpivotTypes.Center);
					changeSpritePositionTo(LabelNodes[i].objectToLabel, new Vector3(xAxisLabelSpacingX, xAxisLabelSpacingY, 0));
				}
			}

			// Scale the labels
			LabelNodes[i].objectToLabel.transform.localScale = new Vector3(labelSize, labelSize, 1);

			// Set the labels
			if (setUsingMaxMin) {
				float num = axisMin + i * (axisMax - axisMin) / (LabelNodes.Count-1);
				if (i == 0) num = axisMin;
				
				if (graphType == graphTypes.bar_stacked_percent && ((isY && orientationType == orientationTypes.vertical) 
																	|| (!isY && orientationType == orientationTypes.horizontal))) {
					num = i / (LabelNodes.Count-1f) * 100f;
				}
				float numberToMult = Mathf.Pow(10f, numDecimals);
				
				AxisLabels[i] = (Mathf.Round(num*numberToMult)/numberToMult).ToString();
				if (graphType == graphTypes.bar_stacked_percent && ((isY && orientationType == orientationTypes.vertical) 
																	|| (!isY && orientationType == orientationTypes.horizontal))) {
					AxisLabels[i] += "%";
				}
			}
			if (LabelType == labelTypes.groups) {
				AxisLabels[i] = groups[i];
			}

			changeLabelText(LabelNodes[i].objectToLabel, AxisLabels[i]);
		}
	}
	
	void UpdateSeriesParentPositions () {
		if (graphTypeChanged || lineSeriesCountChanged || orientationTypeChanged || axisWidthChanged || barWidthChanged || 
		    xAxisNonTickPercentChanged || yAxisNonTickPercentChanged) {

			for (int j = 0; j < lineSeries.Count; j++) {
				if (!activeInHierarchy(lineSeries[j])) continue;
				Vector2 axisWidthOffset = getAxesOffsetFactor();
				axisWidthOffset = new Vector2(-axisWidth/2 * axisWidthOffset.x, -axisWidth/2 * axisWidthOffset.y);

				if (graphType != graphTypes.line) {
					if (orientationType == orientationTypes.vertical) {
						changeSpritePositionTo(lineSeries[j], new Vector3(axisWidthOffset.x, axisWidthOffset.y, 0));
					}
					else {
						changeSpritePositionTo(lineSeries[j], new Vector3(axisWidthOffset.x, axisWidthOffset.y + barWidth, 0));
					}
				}
				else {
					changeSpritePositionTo(lineSeries[j], new Vector3(0, 0, 0));
				}
				
				// Update spacing between series
				if (graphType == graphTypes.bar_side) {
					if (j > 0) {
						if (orientationType == orientationTypes.vertical) {
							changeSpritePositionRelativeToObjByX(lineSeries[j], lineSeries[j-1], barWidth);
						}
						else {
							changeSpritePositionRelativeToObjByY(lineSeries[j], lineSeries[j-1], barWidth);
						}
					}
				}
				else {
					if (j > 0) {
						if (orientationType == orientationTypes.vertical) {
							changeSpritePositionRelativeToObjByX(lineSeries[j], lineSeries[0], 0);
						}
						else {
							changeSpritePositionRelativeToObjByY(lineSeries[j], lineSeries[0], 0);
						}
					}
				}
			}
		}
	}
	
	void UpdateBackground() {
		if (paddingChanged || xAxisLengthChanged || yAxisLengthChanged) {
			UpdateBG();
		}
	}

	public void UpdateBG() {
		changeSpriteSize(graphBackground, Mathf.RoundToInt(getSpriteWidth(this.gameObject)), Mathf.RoundToInt(getSpriteHeight(this.gameObject)));
		changeSpritePositionToX(graphBackground, -paddingLeftRight.x);
		changeSpritePositionToY(graphBackground, -paddingTopBottom.y);
		UpdateBGandSeriesParentPositions(cachedContainerWidth, cachedContainerHeight);
	}

	public void UpdateBGandSeriesParentPositions (float x, float y) {
		Vector3 newChildPos = new Vector3(-x/2 + getSpriteFactorX(this.gameObject)*x + paddingLeftRight.x, 
		                                  -y/2 - getSpriteFactorY(this.gameObject)*y + paddingTopBottom.y);
		changeSpritePositionTo(graphBackground.transform.parent.gameObject, newChildPos);
		changeSpritePositionTo(seriesParent, newChildPos);
	}

	void UpdateFromContainer() {
		if (resizeChanged) {
			if (resizeType != resizeTypes.none) {
				// Adjust background padding for percentage padding, otherwise keep padding in pixels
				if (resizeType == resizeTypes.percentage_padding) {
					paddingLeftRight = new Vector2 (origPaddingLeftRight.x * cachedContainerWidth / origWidth, origPaddingLeftRight.y * cachedContainerWidth / origWidth);
					paddingTopBottom = new Vector2 (origPaddingTopBottom.x * cachedContainerHeight / origHeight, origPaddingTopBottom.y * cachedContainerHeight / origHeight);
				}

				// Adjust children position to match the container position / size
				UpdateBG();

				// Calculate the percentage factor, orientation independent factor, and smaller factor for use with resizing additional properties
				Vector2 percentFactor = new Vector2(xAxisLength / origXAxisLength, yAxisLength / origYAxisLength);
				Vector2 orientationIndependentPF = percentFactor; 
				if (orientationType == orientationTypes.horizontal) {
					orientationIndependentPF = new Vector2(percentFactor.y, percentFactor.x);
				}
				float smallerFactor = percentFactor.x;
				if (percentFactor.y < smallerFactor) smallerFactor = percentFactor.y;

				// Resize additional properties based on the resize properties flags enum
				if ((resizeProperties & ResizeProperties.PointSizeBarWidth) == ResizeProperties.PointSizeBarWidth) {
					barWidth = orientationIndependentPF.x * origBarWidth;
				}
				if ((resizeProperties & ResizeProperties.AxesWidth) == ResizeProperties.AxesWidth) {
					axisWidth = Mathf.RoundToInt(smallerFactor * origAxisWidth);
				}
				if ((resizeProperties & ResizeProperties.FontSizeAxesLabels) == ResizeProperties.FontSizeAxesLabels) {
					xAxisLabelSize = percentFactor.x * origXAxisLabelSize;
					yAxisLabelSize = percentFactor.y * origYAxisLabelSize;
				}
				if ((resizeProperties & ResizeProperties.FontSizeTitle) == ResizeProperties.FontSizeTitle) {
					graphTitle.transform.localScale = new Vector3(smallerFactor * origTitleFontSize, smallerFactor * origTitleFontSize);
				}
				if ((resizeProperties & ResizeProperties.FontSizeAxesTitles) == ResizeProperties.FontSizeAxesTitles) {
					xAxisTitle.transform.localScale = new Vector3(percentFactor.x * origXTitleFontSize, percentFactor.x * origXTitleFontSize);
					yAxisTitle.transform.localScale = new Vector3(percentFactor.y * origYTitleFontSize, percentFactor.y * origYTitleFontSize);
				}
				if ((resizeProperties & ResizeProperties.LegendEntrySpacing) == ResizeProperties.LegendEntrySpacing) {
					legend.legendEntryWidth = smallerFactor * legend.origLegendEntryWidth;
					legend.legendEntryHeight = smallerFactor * legend.origLegendEntryHeight;
					legend.legendEntrySpacing = smallerFactor * legend.origLegendEntrySpacing;
				}
				if ((resizeProperties & ResizeProperties.FontSizeLegends) == ResizeProperties.FontSizeLegends) {
					legend.legendEntryFontSize = smallerFactor * legend.origLegendEntryFontSize;
				}
				if ((resizeProperties & ResizeProperties.LegendEntryLine) == ResizeProperties.LegendEntryLine) {
					legend.legendEntryLinkSpacing = smallerFactor * legend.origLegendEntryLinkSpacing;
				}
				// Properties that affect series
				if ((resizeProperties & ResizeProperties.PointSizeBarWidth) == ResizeProperties.PointSizeBarWidth ||
				    (resizeProperties & ResizeProperties.SeriesLineWidth) == ResizeProperties.SeriesLineWidth ||
				    (resizeProperties & ResizeProperties.FontSizeDataLabels) == ResizeProperties.FontSizeDataLabels) {
					for (int i = 0; i < lineSeries.Count; i++) {
						if (!activeInHierarchy(lineSeries[i])) continue;
						WMG_Series theSeries = lineSeries[i].GetComponent<WMG_Series>();

						if ((resizeProperties & ResizeProperties.PointSizeBarWidth) == ResizeProperties.PointSizeBarWidth) {
							theSeries.pointWidthHeight = smallerFactor * theSeries.origPointWidthHeight;
						}
						if ((resizeProperties & ResizeProperties.SeriesLineWidth) == ResizeProperties.SeriesLineWidth) {
							theSeries.lineScale = smallerFactor * theSeries.origLineScale;
						}
						if ((resizeProperties & ResizeProperties.FontSizeDataLabels) == ResizeProperties.FontSizeDataLabels) {
							theSeries.dataLabelsFontSize = smallerFactor * theSeries.origDataLabelsFontSize;
						}
					}
				}
			}
		}
	}

	void UpdateTotals() {
		if (aSeriesPointsChanged) {
			// Find max number points
			int maxNumValues = 0;
			for (int j = 0; j < lineSeries.Count; j++) {
				if (!activeInHierarchy(lineSeries[j])) continue;
				WMG_Series theSeries = lineSeries[j].GetComponent<WMG_Series>();
				if (maxNumValues < theSeries.pointValues.Count) maxNumValues = theSeries.pointValues.Count;
			}
			// Update max series point count
			maxSeriesPointCount = maxNumValues;
			// Update total values
			for (int i = 0; i < maxNumValues; i++) {
				if (totalPointValues.Count <= i) {
					totalPointValues.Add(0);
				}
				totalPointValues[i] = 0;
				for (int j = 0; j < lineSeries.Count; j++) {
					if (!activeInHierarchy(lineSeries[j])) continue;
					WMG_Series theSeries = lineSeries[j].GetComponent<WMG_Series>();
					if (theSeries.pointValues.Count > i) {
						if (orientationType == WMG_Axis_Graph.orientationTypes.vertical) {
							totalPointValues[i] += (theSeries.pointValues[i].y - yAxisMinValue);
						}
						else {
							totalPointValues[i] += (theSeries.pointValues[i].y - xAxisMinValue);
						}
					}
				}
			}

		}
	}

	void UpdateBarWidth() {
		if (aSeriesPointsChanged || graphTypeChanged || orientationTypeChanged) {
			if (autoUpdateBarWidth) { // Don't do anything if not auto updating bar width
				// If orientation changed, change bar width based on ratio of axis lengths
				if (orientationTypeChanged) {
					if (orientationType == orientationTypes.horizontal) {
						barWidth *= yAxisLength / xAxisLength;
					}
					else {
						barWidth *= xAxisLength / yAxisLength;
					}
				}
				// Ensure the bar width is not greater than a value that would cause the bars to overlap
				float axisLength = xAxisLength;
				float newBarWidth = 0;
				if (orientationType == orientationTypes.horizontal) {
					axisLength = yAxisLength;
				}
				if (graphType == graphTypes.bar_side) {
					newBarWidth = (axisLength - maxSeriesPointCount) / (maxSeriesPointCount * lineSeries.Count);
				}
				else if (graphType == graphTypes.bar_stacked || graphType == graphTypes.bar_stacked_percent) {
					newBarWidth = (axisLength - maxSeriesPointCount) / (maxSeriesPointCount);
				}
				else {
					newBarWidth = barWidth;
				}
				if (barWidth > newBarWidth) barWidth = newBarWidth;

				// A bit hacky, but otherwise barwidth changed is true, which sets points changed to true which can mess up animations
				checkCache();
			}
		}
	}

	void UpdateTitles() {
		if (graphTitleChanged || xAxisLengthChanged || yAxisLengthChanged) {
			if (graphTitle != null) {
				changeLabelText(graphTitle, graphTitleString);
				changeSpritePositionTo(graphTitle, new Vector3(xAxisLength / 2 + graphTitleOffset.x, yAxisLength + graphTitleOffset.y));
			}
		}
		if (yAxisTitleChanged || yAxisLengthChanged) {
			if (yAxisTitle != null) {
				changeLabelText(yAxisTitle, yAxisTitleString);
				changeSpritePositionTo(yAxisTitle, new Vector3(yAxisTitleOffset.x, yAxisLength / 2 + yAxisTitleOffset.y));
			}
		}
		if (xAxisTitleChanged || xAxisLengthChanged) {
			if (xAxisTitle != null) {
				changeLabelText(xAxisTitle, xAxisTitleString);
				changeSpritePositionTo(xAxisTitle, new Vector3(xAxisTitleOffset.x + xAxisLength / 2, xAxisTitleOffset.y));
			}
		}
	}
	
	void UpdateTooltip() {
		// Add or remove tooltip events
		if (tooltipEnabledChanged) {
			theTooltip.subscribeToEvents(tooltipEnabled);
		}
	}
	
	void UpdateAutoAnimEvents() {
		// Add or remove automatic animation events
		if (autoAnimationsEnabledChanged) {
			autoAnim.subscribeToEvents(autoAnimationsEnabled);
		}
	}
	
	// Helper function for update min max, ensures the new values have sensible level of precision
	void AutoSetAxisMinMax(bool isY, float val, float val2, bool max, bool grow, float aMin, float aMax) {
		int numTicks = 0;
		if (isY) numTicks = yAxisNumTicks-1;
		else numTicks = xAxisNumTicks-1;
		
		float changeAmt = 1 + autoGrowAndShrinkByPercent;
		
		// Find tentative new max / min value
		float temp = 0;
		if (max) {
			if (grow) temp = changeAmt * (val - aMin) / (numTicks);
			else temp = changeAmt * (val - val2) / (numTicks);
		}
		else {
			if (grow) temp = changeAmt * (aMax - val) / (numTicks);
			else temp = changeAmt * (val2 - val) / (numTicks);
		}
		
		if (temp == 0 || aMax <= aMin) return;
		
		// Determine level of precision of tentative new value
		float temp2 = temp;
		int pow = 0;
		
		if (Mathf.Abs(temp2) > 1) {
			while (Mathf.Abs(temp2) > 10) {
				pow++;
				temp2 /= 10f;
			}
		}
		else {
			while (Mathf.Abs(temp2) < 0.1f) {
				pow--;
				temp2 *= 10f;
			}
		}
		
		// Update tentative to sensible level of precision
		float temp3 = Mathf.Pow( 10f, pow-1);
		temp2 = temp - (temp % temp3) + temp3;
		
		float newVal = 0;
		if (max) {
			if (grow) newVal = (numTicks) * temp2 + aMin;
			else newVal = (numTicks) * temp2 + val2;
		}
		else {
			if (grow) newVal = aMax - (numTicks) * temp2;
			else newVal = val2 - (numTicks) * temp2;
		}
		
		// Set the min / max value to the newly calculated value
		if (max) {
			if (isY) yAxisMaxValue = newVal;
			else xAxisMaxValue = newVal;
		}
		else {
			if (isY) yAxisMinValue = newVal;
			else xAxisMinValue = newVal;
		}
	}
	
	public List<float> TotalPointValues {
		get { return totalPointValues; }
	}

	public float getDistBetween(int pointsCount) {
		float theAxisLength = xAxisLength;
		if (orientationType == WMG_Axis_Graph.orientationTypes.horizontal) theAxisLength = yAxisLength;

		float xDistBetweenPoints = 0;
		if ((pointsCount - 1) == 0) {
			xDistBetweenPoints = 0;
		}
		else {
			int numPoints = (pointsCount - 1);
			if (groupsCentered) numPoints += 1;
			
			xDistBetweenPoints = theAxisLength / numPoints;
			if (graphType == WMG_Axis_Graph.graphTypes.bar_side) {
				xDistBetweenPoints -= (lineSeries.Count * barWidth) / numPoints;
			}
			else if (graphType == WMG_Axis_Graph.graphTypes.bar_stacked) {
				xDistBetweenPoints -= barWidth / numPoints;
			}
			else if (graphType == WMG_Axis_Graph.graphTypes.bar_stacked_percent) {
				xDistBetweenPoints -= barWidth / numPoints;
			}
		}
		return xDistBetweenPoints;
	}

	public List<WMG_Node> getXAxisTicks() {
		WMG_Grid xTicks = xAxisTicks.GetComponent<WMG_Grid>();
		return xTicks.getRow(0);
	}

	public List<WMG_Node> getXAxisLabels() {
		WMG_Grid xLabels = xAxisLabelObjs.GetComponent<WMG_Grid>();
		return xLabels.getRow(0);
	}
	
	public List<WMG_Node> getYAxisTicks() {
		WMG_Grid yTicks = yAxisTicks.GetComponent<WMG_Grid>();
		return yTicks.getColumn(0);
	}

	public List<WMG_Node> getYAxisLabels() {
		WMG_Grid yLabels = yAxisLabelObjs.GetComponent<WMG_Grid>();
		return yLabels.getColumn(0);
	}
	
	public void changeAllLinePivots(WMGpivotTypes newPivot) {
		for (int j = 0; j < lineSeries.Count; j++) {
			if (!activeInHierarchy(lineSeries[j])) continue;
			WMG_Series aSeries = lineSeries[j].GetComponent<WMG_Series>();
			List<GameObject> lines = aSeries.getLines();
			for (int i = 0; i < lines.Count; i++) {
				changeSpritePivot(lines[i], newPivot);
				WMG_Link aLink = lines[i].GetComponent<WMG_Link>();
				aLink.Reposition();
			}
		}
	}
	
	public List<Vector3> getSeriesScaleVectors(bool useLineWidthForX, float x, float y) {
		List<Vector3> results = new List<Vector3>();
		for (int j = 0; j < lineSeries.Count; j++) {
			if (!activeInHierarchy(lineSeries[j])) continue;
			WMG_Series aSeries = lineSeries[j].GetComponent<WMG_Series>();
			if (useLineWidthForX) {
				results.Add(new Vector3(aSeries.lineScale, y, 1));
			}
			else {
				results.Add(new Vector3(x, y, 1));
			}
		}
		return results;
	}

	public float getMaxPointSize() {
		if (graphType != graphTypes.line) {
			return barWidth;
		}
		else {
			float size = 0;
			for (int j = 0; j < lineSeries.Count; j++) {
				if (!activeInHierarchy(lineSeries[j])) continue;
				WMG_Series aSeries = lineSeries[j].GetComponent<WMG_Series>();
				if (aSeries.pointWidthHeight > size) size = aSeries.pointWidthHeight;
			}
			return size;
		}
	}

	public int getMaxNumPoints() {
		return maxSeriesPointCount;
	}
	
	public void setAxesQuadrant1() {
		xAxisArrows[0] = true;
		xAxisArrows[1] = false;
		yAxisArrows[0] = true;
		yAxisArrows[1] = false;
		hideYTick = false;
		hideXTick = false;
		xAxisYTick = 0;
		yAxisXTick = 0;
		xAxisNonTickPercent = 0;
		yAxisNonTickPercent = 0;
		yAxisTicksRight = false;
		xAxisTicksAbove = false;
	}
	
	public void setAxesQuadrant2() {
		xAxisArrows[0] = false;
		xAxisArrows[1] = true;
		yAxisArrows[0] = true;
		yAxisArrows[1] = false;
		hideYTick = false;
		hideXTick = false;
		xAxisYTick = 0;
		yAxisXTick = xAxisNumTicks - 1;
		xAxisNonTickPercent = 0;
		yAxisNonTickPercent = 1;
		yAxisTicksRight = true;
		xAxisTicksAbove = false;
	}
	
	public void setAxesQuadrant3() {
		xAxisArrows[0] = false;
		xAxisArrows[1] = true;
		yAxisArrows[0] = false;
		yAxisArrows[1] = true;
		hideYTick = false;
		hideXTick = false;
		xAxisYTick = yAxisNumTicks - 1;
		yAxisXTick = xAxisNumTicks - 1;
		xAxisNonTickPercent = 1;
		yAxisNonTickPercent = 1;
		yAxisTicksRight = true;
		xAxisTicksAbove = true;
	}
	
	public void setAxesQuadrant4() {
		xAxisArrows[0] = true;
		xAxisArrows[1] = false;
		yAxisArrows[0] = false;
		yAxisArrows[1] = true;
		hideYTick = false;
		hideXTick = false;
		xAxisYTick = yAxisNumTicks - 1;
		yAxisXTick = 0;
		xAxisNonTickPercent = 1;
		yAxisNonTickPercent = 0;
		yAxisTicksRight = false;
		xAxisTicksAbove = true;
	}
	
	public void setAxesQuadrant1_2_3_4() {
		xAxisArrows[0] = true;
		xAxisArrows[1] = true;
		yAxisArrows[0] = true;
		yAxisArrows[1] = true;
		hideYTick = true;
		hideXTick = true;
		xAxisYTick = yAxisNumTicks / 2;
		yAxisXTick = xAxisNumTicks / 2;
		xAxisNonTickPercent = 0.5f;
		yAxisNonTickPercent = 0.5f;
		yAxisTicksRight = false;
		xAxisTicksAbove = false;
	}
	
	public void setAxesQuadrant1_2() {
		xAxisArrows[0] = true;
		xAxisArrows[1] = true;
		yAxisArrows[0] = true;
		yAxisArrows[1] = false;
		hideYTick = true;
		hideXTick = false;
		xAxisYTick = 0;
		yAxisXTick = xAxisNumTicks / 2;
		xAxisNonTickPercent = 0;
		yAxisNonTickPercent = 0.5f;
		yAxisTicksRight = false;
		xAxisTicksAbove = false;
	}
	
	public void setAxesQuadrant3_4() {
		xAxisArrows[0] = true;
		xAxisArrows[1] = true;
		yAxisArrows[0] = false;
		yAxisArrows[1] = true;
		hideYTick = true;
		hideXTick = false;
		xAxisYTick = yAxisNumTicks - 1;
		yAxisXTick = xAxisNumTicks / 2;
		xAxisNonTickPercent = 1;
		yAxisNonTickPercent = 0.5f;
		yAxisTicksRight = false;
		xAxisTicksAbove = true;
	}
	
	public void setAxesQuadrant2_3() {
		xAxisArrows[0] = false;
		xAxisArrows[1] = true;
		yAxisArrows[0] = true;
		yAxisArrows[1] = true;
		hideYTick = false;
		hideXTick = true;
		xAxisYTick = yAxisNumTicks / 2;
		yAxisXTick = xAxisNumTicks - 1;
		xAxisNonTickPercent = 0.5f;
		yAxisNonTickPercent = 1;
		yAxisTicksRight = true;
		xAxisTicksAbove = false;
	}
	
	public void setAxesQuadrant1_4() {
		xAxisArrows[0] = true;
		xAxisArrows[1] = false;
		yAxisArrows[0] = true;
		yAxisArrows[1] = true;
		hideYTick = false;
		hideXTick = true;
		xAxisYTick = yAxisNumTicks / 2;
		yAxisXTick = 0;
		xAxisNonTickPercent = 0.5f;
		yAxisNonTickPercent = 0;
		yAxisTicksRight = false;
		xAxisTicksAbove = false;
	}

	Vector2 getAxesOffsetFactor() {
		if (axesType == axesTypes.I) {
			return new Vector2(-1,-1);
		}
		else if (axesType == axesTypes.II) {
			return new Vector2(1,-1);
		}
		else if (axesType == axesTypes.III) {
			return new Vector2(1,1);
		}
		else if (axesType == axesTypes.IV) {
			return new Vector2(-1,1);
		}
		else if (axesType == axesTypes.CENTER) {
			return new Vector2(0,0);
		}
		else if (axesType == axesTypes.I_II) {
			return new Vector2(0,-1);
		}
		else if (axesType == axesTypes.III_IV) {
			return new Vector2(0,1);
		}
		else if (axesType == axesTypes.II_III) {
			return new Vector2(1,0);
		}
		else if (axesType == axesTypes.I_IV) {
			return new Vector2(-1,0);
		}
		else if (axesType == axesTypes.AUTO_ORIGIN || axesType == axesTypes.AUTO_ORIGIN_X || axesType == axesTypes.AUTO_ORIGIN_Y) {
			float x = 0;
			float y = 0;
			if (axesType == axesTypes.AUTO_ORIGIN || axesType == axesTypes.AUTO_ORIGIN_Y) {
				if (xAxisMinValue >= theOrigin.x) {
					y = -1;
				}
				else if (xAxisMaxValue <= theOrigin.x) {
					y = 1;
				}
			}
			if (axesType == axesTypes.AUTO_ORIGIN || axesType == axesTypes.AUTO_ORIGIN_X) {
				if (yAxisMinValue >= theOrigin.y) {
					x = -1;
				}
				else if (yAxisMaxValue <= theOrigin.y) {
					x = 1;
				}
			}
			return new Vector2(x, y);
		}
		return new Vector2(0,0);
	}
	
	// Animate all the points in all the series simultaneously
	public void animScaleAllAtOnce(bool isPoint, float duration, float delay, Ease anEaseType, List<Vector3> before, List<Vector3> after) {
		for (int j = 0; j < lineSeries.Count; j++) {
			if (!activeInHierarchy(lineSeries[j])) continue;
			WMG_Series aSeries = lineSeries[j].GetComponent<WMG_Series>();
			List<GameObject> objects;
			if (isPoint) objects = aSeries.getPoints();
			else objects = aSeries.getLines();
			for (int i = 0; i < objects.Count; i++) {
				objects[i].transform.localScale = before[j];
				WMG_Anim.animScale(objects[i], duration, anEaseType, after[j], delay);
			}
		}
	}
	
	// Animate all the points in a single series simultaneously, and then proceed to the next series
	public void animScaleBySeries(bool isPoint, float duration, float delay, Ease anEaseType, List<Vector3> before, List<Vector3> after) {
		Sequence sequence = DOTween.Sequence();
		float individualDuration = duration / lineSeries.Count;
		float individualDelay = delay / lineSeries.Count;
		for (int j = 0; j < lineSeries.Count; j++) {
			if (!activeInHierarchy(lineSeries[j])) continue;
			WMG_Series aSeries = lineSeries[j].GetComponent<WMG_Series>();
			List<GameObject> objects;
			if (isPoint) objects = aSeries.getPoints();
			else objects = aSeries.getLines();
			float insertTimeLoc = j * individualDuration + (j+1) * individualDelay;
			for (int i = 0; i < objects.Count; i++) {
				objects[i].transform.localScale = before[j];
				WMG_Anim.animScaleSeqInsert(ref sequence, insertTimeLoc, objects[i], individualDuration, anEaseType, after[j], individualDelay);
			}
		}
	    sequence.Play();
	}
	
	// Animate the points across multiple series simultaneously, and then proceed to the next points.
	public void animScaleOneByOne(bool isPoint, float duration, float delay, Ease anEaseType, List<Vector3> before, List<Vector3> after, int loopDir) {
		for (int j = 0; j < lineSeries.Count; j++) {
			if (!activeInHierarchy(lineSeries[j])) continue;
			Sequence sequence = DOTween.Sequence();
			WMG_Series aSeries = lineSeries[j].GetComponent<WMG_Series>();
			List<GameObject> objects;
			if (isPoint) objects = aSeries.getPoints();
			else objects = aSeries.getLines();
			float individualDuration = duration / objects.Count;
			float individualDelay = delay / objects.Count;
			if (loopDir == 0) {
				// Loop from left to right
				for (int i = 0; i < objects.Count; i++) {
					objects[i].transform.localScale = before[j];
					WMG_Anim.animScaleSeqAppend(ref sequence, objects[i], individualDuration, anEaseType, after[j], individualDelay);
				}
			}
			else if (loopDir == 1) {
				// Loop from right to left
				for (int i = objects.Count-1; i >= 0; i--) {
					objects[i].transform.localScale = before[j];
					WMG_Anim.animScaleSeqAppend(ref sequence, objects[i], individualDuration, anEaseType, after[j], individualDelay);
				}
			}
			else if (loopDir == 2) {
				// Loop from the center point to the edges, alternating sides.
				int max = objects.Count - 1;
				int i = max / 2;
				int dir = -1;
				int difVal = 0;
				bool reachedMin = false;
				bool reachedMax = false;
				while (true) {
					
					if (reachedMin && reachedMax) break;
					
					if (i >= 0 && i <= max) {
						objects[i].transform.localScale = before[j];
						WMG_Anim.animScaleSeqAppend(ref sequence, objects[i], individualDuration, anEaseType, after[j], individualDelay);
					}
					
					difVal++;
					dir *= -1;
					i = i + (dir * difVal);
					
					if (i < 0) reachedMin = true;
					if (i > max) reachedMax = true;
					
				}
			}
	        sequence.Play();
		}
	}
	
	public WMG_Series addSeries() {
		return addSeriesAt(lineSeries.Count);
	}
	
	public void deleteSeries() {
		deleteSeriesAt(lineSeries.Count-1);
	}

	public WMG_Series addSeriesAt(int index) {
		GameObject curObj = Instantiate(seriesPrefab) as GameObject;
		curObj.name = "Series" + (index+1);
		changeSpriteParent(curObj, seriesParent);
		curObj.transform.localScale = Vector3.one;
		WMG_Series theSeries = curObj.GetComponent<WMG_Series>();
		if (autoAnimationsEnabled) autoAnim.addSeriesForAutoAnim(theSeries);
		theSeries.theGraph = this;
		lineSeries.Insert(index, curObj);
		theSeries.createLegendEntry(index);
		theSeries.checkCache();
		theSeries.setCacheFlags(true);
		legend.setLegendChanged();
		refreshSeries();
		barWidthChanged = true;
		return curObj.GetComponent<WMG_Series>();
	}

	public void deleteSeriesAt(int index) {
		GameObject seriesToDelete = lineSeries[index];
		WMG_Series theSeries = seriesToDelete.GetComponent<WMG_Series>();
		legend.setLegendChanged();
		legend.updateLegend();
		lineSeries.Remove(seriesToDelete);
		theSeries.deleteAllNodesFromGraphManager();
		legend.deleteLegendEntry(index);
		Destroy(seriesToDelete);
		barWidthChanged = true;
	}
}
