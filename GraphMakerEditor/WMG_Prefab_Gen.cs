using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class PrefabGenerators : MonoBehaviour {
	
	static GameObject theCanvas;
	static GameObject baseAxis;

	static bool setup() {
		theCanvas = GameObject.Find("Canvas");
		if (theCanvas == null) return false;
		baseAxis = AssetDatabase.LoadAssetAtPath("Assets/Graph_Maker/Prefabs/Graphs/LineGraph.prefab", typeof(GameObject)) as GameObject;
		if (baseAxis == null) return false;
		return true;
	}

	[MenuItem ("Assets/Graph Maker/Create Bar and Scatter Graphs")]
	static void CreateBarAndScatterGraphs () {
		if (!setup()) return;
		createBarGraph();
		createScatterPlot();
	}

	[MenuItem ("Assets/Graph Maker/Create Area and Stacked Shading Graphs")]
	static void CreateAreaAndStackedGraphs () {
		if (!setup()) return;
		createAreaGraph();
		createStackedGraph();
	}

	[MenuItem ("Assets/Graph Maker/Create Radar Graph")]
	static void CreateRadarGraph () {
		if (!setup()) return;
		createRadarGraph();
	}

	static void createRadarGraph() {
		GameObject graphGO = GameObject.Instantiate(baseAxis) as GameObject;
		WMG_Axis_Graph graph = graphGO.GetComponent<WMG_Axis_Graph>();
		graph.changeSpriteParent(graphGO, theCanvas);
		graphGO.name = "RadarGraph";
		graph.changeSpriteSize(graphGO, 405, 280);
		graph.changeSpritePositionTo(graphGO, new Vector3(0, 0, 0));
		graph.paddingTopBottom = new Vector2 (graph.paddingTopBottom.x, 60);
		Object newLegend = AssetDatabase.LoadAssetAtPath("Assets/Graph_Maker/Prefabs/Misc/Legend-None.prefab", typeof(GameObject));
		if (newLegend != null) {
			graph.legendPrefab = newLegend;
		}

		graph.changeSpriteColor(graph.graphBackground, Color.black);
		graph.SetActive(graph.xAxis, false);
		graph.SetActive(graph.yAxis, false);
		graph.axesType = WMG_Axis_Graph.axesTypes.CENTER;
		DestroyImmediate(graph.lineSeries[1]);
		graph.lineSeries.RemoveAt(1);
		DestroyImmediate(graph.lineSeries[0]);
		graph.lineSeries.RemoveAt(0);

		graph.yAxisMinValue = -100;
		graph.yAxisMaxValue = 100;
		graph.xAxisMinValue = -100;
		graph.xAxisMaxValue = 100;
		graph.yAxisNumTicks = 5;
		graph.autoAnimationsEnabled = false;
		graph.hideXLabels = true;
		graph.hideYLabels = true;
		graph.hideXTicks = true;
		graph.hideYTicks = true;

		WMG_Radar_Graph radar = graphGO.AddComponent<WMG_Radar_Graph>();
		radar.theGraph = graph;
		radar.randomData = true;
		radar.numPoints = 5;
		radar.offset = new Vector2(-3,-20);
		radar.degreeOffset = 90;
		radar.radarMaxVal = 100;
		radar.numGrids = 7;
		radar.gridLineWidth = 0.5f;
		radar.gridColor = new Color32(125, 125, 125, 255);
		radar.numDataSeries = 1;
		radar.dataSeriesLineWidth = 1;
		List<Color> radarColors = new List<Color>();
		radarColors.Add(new Color32(0,255,180,255));
		radarColors.Add(new Color32(210,0,255,255));
		radarColors.Add(new Color32(160,210,65,255));
		radar.dataSeriesColors = radarColors;
		radar.labelsColor = Color.white;
		radar.labelsOffset = 26;
		radar.fontSize = 14;
		List<string> labelStrings = new List<string>();
		labelStrings.Add("Strength");
		labelStrings.Add("Speed");
		labelStrings.Add("Agility");
		labelStrings.Add("Magic");
		labelStrings.Add("Defense");
		radar.labelStrings = labelStrings;

		graph.pointPrefabs.Add(AssetDatabase.LoadAssetAtPath("Assets/Graph_Maker/Prefabs/Nodes/TextNode.prefab", typeof(GameObject)));
		UnityEditorInternal.ComponentUtility.MoveComponentUp(radar);

		graph.hideYGrid = true;
		graph.hideXGrid = true;
	}

	static void createAreaGraph() {
		GameObject graphGO = GameObject.Instantiate(baseAxis) as GameObject;
		WMG_Axis_Graph graph = graphGO.GetComponent<WMG_Axis_Graph>();
		graph.changeSpriteParent(graphGO, theCanvas);
		graphGO.name = "AreaShadingGraph";
		graph.changeSpriteSize(graphGO, 525, 325);
		graph.changeSpritePositionTo(graphGO, new Vector3(-190.2f, 180.2f, 0));
		graph.paddingTopBottom = new Vector2 (graph.paddingTopBottom.x, 60);
		graph.changeSpriteColor(graph.graphBackground, Color.black);
		Object newLegend = AssetDatabase.LoadAssetAtPath("Assets/Graph_Maker/Prefabs/Misc/Legend-None.prefab", typeof(GameObject));
		if (newLegend != null) {
			graph.legendPrefab = newLegend;
		}
		DestroyImmediate(graph.lineSeries[1]);
		graph.lineSeries.RemoveAt(1);
		graph.yAxisMinValue = -5;
		graph.yAxisNumTicks = 6;
		graph.autoAnimationsEnabled = false;

		WMG_Series series = graph.lineSeries[0].GetComponent<WMG_Series>();
		series.areaShadingType = WMG_Series.areaShadingTypes.Gradient;
		series.areaShadingColor = new Color32(0, 20, 150, 255);
		series.areaShadingAxisValue = -2;

		graph.hideYGrid = true;
		graph.hideXGrid = true;
	}

	static void createStackedGraph() {
		GameObject graphGO = GameObject.Instantiate(baseAxis) as GameObject;
		WMG_Axis_Graph graph = graphGO.GetComponent<WMG_Axis_Graph>();
		graph.changeSpriteParent(graphGO, theCanvas);
		graphGO.name = "StackedLineGraph";
		graph.changeSpriteSize(graphGO, 525, 325);
		graph.changeSpritePositionTo(graphGO, new Vector3(210.2f, -155.2f, 0));
		graph.paddingTopBottom = new Vector2 (graph.paddingTopBottom.x, 60);
		graph.changeSpriteColor(graph.graphBackground, Color.black);
		Object newLegend = AssetDatabase.LoadAssetAtPath("Assets/Graph_Maker/Prefabs/Misc/Legend-None.prefab", typeof(GameObject));
		if (newLegend != null) {
			graph.legendPrefab = newLegend;
		}
		graph.yAxisMinValue = -5;
		graph.yAxisNumTicks = 6;
		graph.autoAnimationsEnabled = false;
		
		WMG_Series series = graph.lineSeries[0].GetComponent<WMG_Series>();
		series.areaShadingType = WMG_Series.areaShadingTypes.Solid;
		series.areaShadingColor = new Color32(0, 20, 150, 255);
		series.areaShadingAxisValue = -4.75f;
		List<Vector2> s1Data = new List<Vector2>();
		s1Data.Add(new Vector2(0, 0.5f));
		s1Data.Add(new Vector2(0, 1));
		s1Data.Add(new Vector2(0, 1.5f));
		s1Data.Add(new Vector2(0, 3));
		s1Data.Add(new Vector2(0, 4));
		s1Data.Add(new Vector2(0, 6));
		s1Data.Add(new Vector2(0, 9));
		s1Data.Add(new Vector2(0, 14));
		s1Data.Add(new Vector2(0, 15));
		s1Data.Add(new Vector2(0, 17));
		s1Data.Add(new Vector2(0, 19));
		s1Data.Add(new Vector2(0, 20));
		series.pointValues = s1Data;
		series.extraXSpace = 2;

		WMG_Series series2 = graph.lineSeries[1].GetComponent<WMG_Series>();
		series2.areaShadingType = WMG_Series.areaShadingTypes.Solid;
		series2.areaShadingColor = new Color32(0, 125, 15, 255);
		series2.areaShadingAxisValue = -4.75f;
		List<Vector2> s2Data = new List<Vector2>();
		s2Data.Add(new Vector2(0, -3));
		s2Data.Add(new Vector2(0, -2));
		s2Data.Add(new Vector2(0, -3));
		s2Data.Add(new Vector2(0, -2));
		s2Data.Add(new Vector2(0, 0));
		s2Data.Add(new Vector2(0, 1));
		s2Data.Add(new Vector2(0, 2));
		s2Data.Add(new Vector2(0, 4));
		s2Data.Add(new Vector2(0, 8));
		s2Data.Add(new Vector2(0, 6));
		s2Data.Add(new Vector2(0, 7));
		s2Data.Add(new Vector2(0, 4));
		series2.pointValues = s2Data;
		series2.extraXSpace = 2;
		series2.pointColor = new Color32(255, 120, 0, 255);

		graph.hideYGrid = true;
		graph.hideXGrid = true;
	}

	static void createBarGraph() {
		GameObject graphGO = GameObject.Instantiate(baseAxis) as GameObject;
		WMG_Axis_Graph graph = graphGO.GetComponent<WMG_Axis_Graph>();
		graph.changeSpriteParent(graphGO, theCanvas);
		graphGO.name = "BarGraph";
		graph.graphType = WMG_Axis_Graph.graphTypes.bar_side;
		graph.changeSpriteSize(graphGO, 405, 280);
		graph.changeSpritePositionTo(graphGO, new Vector3(-250, 180, 0));
		graph.paddingTopBottom = new Vector2 (graph.paddingTopBottom.x, 60);
		Object newLegend = AssetDatabase.LoadAssetAtPath("Assets/Graph_Maker/Prefabs/Misc/Legend-None.prefab", typeof(GameObject));
		if (newLegend != null) {
			graph.legendPrefab = newLegend;
		}

		graph.hideYGrid = true;
		graph.hideXGrid = true;
	}

	static void createScatterPlot() {
		GameObject graphGO = GameObject.Instantiate(baseAxis) as GameObject;
		WMG_Axis_Graph graph = graphGO.GetComponent<WMG_Axis_Graph>();
		graph.changeSpriteParent(graphGO, theCanvas);
		graphGO.name = "ScatterPlot";
		graph.changeSpriteSize(graphGO, 405, 280);
		graph.changeSpritePositionTo(graphGO, new Vector3(250, 180, 0));
		graph.paddingTopBottom = new Vector2 (graph.paddingTopBottom.x, 60);
		Object newLegend = AssetDatabase.LoadAssetAtPath("Assets/Graph_Maker/Prefabs/Misc/Legend-None.prefab", typeof(GameObject));
		if (newLegend != null) {
			graph.legendPrefab = newLegend;
		}
		graph.SetXLabelsUsingMaxMin = true;
		graph.xLabelType = WMG_Axis_Graph.labelTypes.ticks;

		WMG_Series series1 = graph.lineSeries[0].GetComponent<WMG_Series>();
		if (series1 == null) return;
		series1.AutoUpdateXDistBetween = false;
		series1.UseXDistBetweenToSpace = false;
		series1.hideLines = true;
		series1.pointWidthHeight = 5;
		List<Vector2> s1Data = new List<Vector2>();
		s1Data.Add(new Vector2(1, 19));
		s1Data.Add(new Vector2(3, 20));
		s1Data.Add(new Vector2(3, 16));
		s1Data.Add(new Vector2(5, 18));
		s1Data.Add(new Vector2(6, 13));
		s1Data.Add(new Vector2(7, 12));
		s1Data.Add(new Vector2(8, 14));
		s1Data.Add(new Vector2(13, 8));
		s1Data.Add(new Vector2(16, 7));
		s1Data.Add(new Vector2(18, 6));
		s1Data.Add(new Vector2(21, 5.6f));
		s1Data.Add(new Vector2(24, 5));
		s1Data.Add(new Vector2(27, 4.5f));
		s1Data.Add(new Vector2(38, 3.5f));
		s1Data.Add(new Vector2(45, 3));
		s1Data.Add(new Vector2(55, 2.5f));
		s1Data.Add(new Vector2(65, 2));
		s1Data.Add(new Vector2(75, 2.3f));
		s1Data.Add(new Vector2(80, 2));
		s1Data.Add(new Vector2(85, 1.6f));
		s1Data.Add(new Vector2(88, 1));
		s1Data.Add(new Vector2(91, 1.5f));
		s1Data.Add(new Vector2(93, 2));
		s1Data.Add(new Vector2(95, 1.3f));
		s1Data.Add(new Vector2(99, 1));
		series1.pointValues = s1Data;
		series1.pointColor = new Color32(65, 255, 0, 255);


		WMG_Series series2 = graph.lineSeries[1].GetComponent<WMG_Series>();
		if (series2 == null) return;
		series2.AutoUpdateXDistBetween = false;
		series2.UseXDistBetweenToSpace = false;
		series2.hidePoints = true;
		series2.lineScale = 1;
		List<Vector2> s2Data = new List<Vector2>();
		s2Data.Add(new Vector2(2, 19));
		s2Data.Add(new Vector2(12, 7));
		s2Data.Add(new Vector2(45, 2.5f));
		s2Data.Add(new Vector2(95, 1.7f));
		series2.pointValues = s2Data;
		series2.pointPrefab = 0;
		series2.linkPrefab = 1;
		series2.lineColor = new Color32(0, 190, 255, 145);
		series2.pointColor = new Color32(0, 190, 255, 255);

		graph.hideYGrid = true;
		graph.hideXGrid = true;
	}

	static void createPrefab(GameObject obj, string prefabPath) {
		// Create / overwrite prefab
		Object prefab = AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject));
		
		if (prefab != null) {
			PrefabUtility.ReplacePrefab(obj, prefab, ReplacePrefabOptions.ReplaceNameBased);
		}
		else {
			prefab = PrefabUtility.CreateEmptyPrefab(prefabPath);
			PrefabUtility.ReplacePrefab(obj, prefab, ReplacePrefabOptions.ReplaceNameBased);
		}
	}

}

