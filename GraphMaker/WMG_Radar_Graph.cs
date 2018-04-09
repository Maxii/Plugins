using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WMG_Radar_Graph : MonoBehaviour {

	public WMG_Axis_Graph theGraph;

	public bool randomData; // sets random data for demonstration purposes, set this to false to use your own data

	public int numPoints; // e.g. 5 for pentagonal graphs
	public Vector2 offset;
	public float degreeOffset;
	public float radarMinVal;
	public float radarMaxVal;

	public int numGrids; // e.g. number of background grids
	public float gridLineWidth;
	public Color gridColor;

	public int numDataSeries;
	public float dataSeriesLineWidth;
	public List<Color> dataSeriesColors;

	public Color labelsColor;
	public float labelsOffset;
	public int fontSize;
	public List<string> labelStrings;
	public bool hideLabels;

	// change flags
	private bool gridChanged;
	private bool dataSeriesChanged;
	private bool labelsChanged;
	private bool graphChanged;

	// cache
	private int cachedNumGrids;
	private float cachedGridLineWidth;
	private Color cachedGridColor;
	private int cachedNumDataSeries;
	private float cachedDataSeriesLineWidth;
	private List<Color> cachedDataSeriesColors = new List<Color>();
	private Color cachedLabelsColor;
	private float cachedLabelsOffset;
	private int cachedFontSize;
	private List<string> cachedLabelStrings = new List<string>();
	private bool cachedHideLabels;
	private int cachedNumPoints;
	private Vector2 cachedOffset;
	private float cachedDegreeOffset;
	private float cachedRadarMinVal;
	private float cachedRadarMaxVal;

	public List<WMG_Series> grids;
	public List<WMG_Series> dataSeries;
	public WMG_Series radarLabels;

	private bool createdLabels;

	void Start() {
		checkCache(); // Set all cache variables to the current values
		setCacheFlags(true); // Set all cache change flags to true to update everything on start
	}

	void Update() {
		checkCache();
		refreshGraph();
		setCacheFlags(false);
	}

	void checkCache() {
		// data elements specific to the grids
		theGraph.updateCacheAndFlag<int>(ref cachedNumGrids, numGrids, ref gridChanged);
		theGraph.updateCacheAndFlag<float>(ref cachedGridLineWidth, gridLineWidth, ref gridChanged);
		theGraph.updateCacheAndFlag<Color>(ref cachedGridColor, gridColor, ref gridChanged);

		// data elements specific to the data series
		theGraph.updateCacheAndFlag<int>(ref cachedNumDataSeries, numDataSeries, ref dataSeriesChanged);
		theGraph.updateCacheAndFlag<float>(ref cachedDataSeriesLineWidth, dataSeriesLineWidth, ref dataSeriesChanged);
		theGraph.updateCacheAndFlagList<Color>(ref cachedDataSeriesColors, dataSeriesColors, ref dataSeriesChanged);

		// data elements specific to labels
		theGraph.updateCacheAndFlag<Color>(ref cachedLabelsColor, labelsColor, ref labelsChanged);
		theGraph.updateCacheAndFlag<float>(ref cachedLabelsOffset, labelsOffset, ref labelsChanged);
		theGraph.updateCacheAndFlag<int>(ref cachedFontSize, fontSize, ref labelsChanged);
		theGraph.updateCacheAndFlagList<string>(ref cachedLabelStrings, labelStrings, ref labelsChanged);
		theGraph.updateCacheAndFlag<bool>(ref cachedHideLabels, hideLabels, ref labelsChanged);

		// data elements that affect everything
		theGraph.updateCacheAndFlag<int>(ref cachedNumPoints, numPoints, ref graphChanged);
		theGraph.updateCacheAndFlag<Vector2>(ref cachedOffset, offset, ref graphChanged);
		theGraph.updateCacheAndFlag<float>(ref cachedDegreeOffset, degreeOffset, ref graphChanged);
		theGraph.updateCacheAndFlag<float>(ref cachedRadarMinVal, radarMinVal, ref graphChanged);
		theGraph.updateCacheAndFlag<float>(ref cachedRadarMaxVal, radarMaxVal, ref graphChanged);
	}

	void setCacheFlags(bool val) {
		gridChanged = val;
		dataSeriesChanged = val;
		labelsChanged = val;
		graphChanged = val;
	}

	void refreshGraph() {
		updateGrids();
		updateDataSeries();
		updateLabels();
	}

	void updateLabels() {
		if (labelsChanged || graphChanged) {
			if (!createdLabels) {
				WMG_Series labels = theGraph.addSeriesAt(numDataSeries+numGrids);
				labels.hideLines = true;
				createdLabels = true;
				labels.pointPrefab = 3;
				radarLabels = labels;
				StartCoroutine(invalidateCacheAfterAFrame());
			}

			for (int i = 0; i < numPoints; i++) {
				if (labelStrings.Count <= i) {
					labelStrings.Add("");
				}
			}
			for (int i = labelStrings.Count - 1; i >= 0; i--) {
				if (labelStrings[i] != null && i >= numPoints) {
					labelStrings.RemoveAt(i);
				}
			}

			radarLabels.hidePoints = hideLabels;
			radarLabels.pointValues = theGraph.GenCircular2(numPoints, offset.x, offset.y, labelsOffset + (radarMaxVal - radarMinVal), degreeOffset);
			List<GameObject> labelGOs = radarLabels.getPoints();
			for (int i = 0; i < labelGOs.Count; i++) {
				if (i >= numPoints) break;
				theGraph.changeLabelFontSize(labelGOs[i], fontSize);
				theGraph.changeLabelText(labelGOs[i], labelStrings[i]);
			}
			radarLabels.pointColor = labelsColor;
		}
	}

	void updateDataSeries() {
		if (dataSeriesChanged || graphChanged) {
			for (int i = 0; i < numDataSeries; i++) {
				if (dataSeries.Count <= i) {
					WMG_Series aSeries = theGraph.addSeriesAt(numGrids+i);
					aSeries.connectFirstToLast = true;
					aSeries.hidePoints = true;
					dataSeries.Add(aSeries);
				}
				if (dataSeriesColors.Count <= i) {
					dataSeriesColors.Add(new Color(Random.Range(0f,1f),Random.Range(0f,1f),Random.Range(0f,1f),1));
				}
			}
			for (int i = dataSeries.Count - 1; i >= 0; i--) {
				if (dataSeries[i] != null && i >= numDataSeries) {
					theGraph.deleteSeriesAt(numGrids+i);
					dataSeries.RemoveAt(i);
				}
			}
			for (int i = dataSeriesColors.Count - 1; i >= 0; i--) {
				if (i >= numDataSeries) {
					dataSeriesColors.RemoveAt(i);
				}
			}
			for (int i = 0; i < numDataSeries; i++) {
				WMG_Series aSeries = theGraph.lineSeries[i + numGrids].GetComponent<WMG_Series>();
				if (randomData) {
					aSeries.pointValues = theGraph.GenRadar(theGraph.GenRandomList(numPoints, radarMinVal, radarMaxVal), offset.x, offset.y, degreeOffset);
				}
				aSeries.lineScale = dataSeriesLineWidth;
				aSeries.linePadding = dataSeriesLineWidth;
				aSeries.lineColor = dataSeriesColors[i];
			}
		}
	}

	void updateGrids() {
		if (gridChanged || graphChanged) {
			for (int i = 0; i < numGrids; i++) {
				if (grids.Count <= i) {
					WMG_Series aGrid = theGraph.addSeriesAt(i);
					aGrid.connectFirstToLast = true;
					aGrid.hidePoints = true;
					grids.Add(aGrid);
				}
			}
			for (int i = grids.Count - 1; i >= 0; i--) {
				if (grids[i] != null && i >= numGrids) {
					theGraph.deleteSeriesAt(i);
					grids.RemoveAt(i);
				}
			}
			for (int i = 0; i < numGrids; i++) {
				WMG_Series aGrid = theGraph.lineSeries[i].GetComponent<WMG_Series>();
				aGrid.pointValues = theGraph.GenCircular2(numPoints, offset.x, offset.y, (i+1f) / numGrids * (radarMaxVal - radarMinVal), degreeOffset);
				aGrid.lineScale = gridLineWidth;
				aGrid.linePadding = gridLineWidth;
				aGrid.lineColor = gridColor;
			}
		}
	}

	IEnumerator invalidateCacheAfterAFrame() {
		yield return 0;
		setCacheFlags(true);
	}

}
