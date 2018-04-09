using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using DG.Tweening;

[CustomEditor(typeof(WMG_Axis_Graph))]
public class WMG_Axis_Graph_E : WMG_E_Util
{
//	SerializedObject myTarget;
	WMG_Axis_Graph graph;

	enum eTabType
	{
		Core,
		Axes,
		Tooltip,
		Anim,
		Labels,
		Misc
	}

	private eTabType m_tabType = eTabType.Core;

	void OnEnable()
	{
//		myTarget = new SerializedObject(target);
		graph = (WMG_Axis_Graph)target;
	}

	public override void OnInspectorGUI()
	{
		// Update the serializedProperty - always do this in the beginning of OnInspectorGUI.
		serializedObject.Update();

		string[] toolBarButtonNames = System.Enum.GetNames(typeof(eTabType));
		
		m_tabType = (eTabType)GUILayout.Toolbar((int)m_tabType, toolBarButtonNames);

		switch (m_tabType)
		{
		case eTabType.Core: DrawCore(); break;
		case eTabType.Axes: DrawAxes(); break;
		case eTabType.Tooltip: DrawTooltip(); break;
		case eTabType.Anim: DrawAnim(); break;
		case eTabType.Labels: DrawLabels(); break;
		case eTabType.Misc: DrawMisc(); break;
		}								

		// In editor mode, update graphics based on graph width and height
		if (!Application.isPlaying) {
			UpdateSceneView();
		}

		if( GUI.changed ) {
			EditorUtility.SetDirty( graph );
		}
		
		// Apply changes to the serializedProperty - always do this in the end of OnInspectorGUI.
		serializedObject.ApplyModifiedProperties();
	}

	void UpdateSceneView() {
		Vector2 newSize = graph.getSpriteSize(graph.gameObject);
		graph.changeSpriteSize(graph.graphBackground, Mathf.RoundToInt(newSize.x), Mathf.RoundToInt(newSize.y));
		graph.changeSpritePositionToX(graph.graphBackground, -graph.paddingLeftRight.x);
		graph.changeSpritePositionToY(graph.graphBackground, -graph.paddingTopBottom.y);
		graph.UpdateBGandSeriesParentPositions(newSize.x, newSize.y);
		// Update axes lines
		int newX = Mathf.RoundToInt(newSize.x - graph.paddingLeftRight.x - graph.paddingLeftRight.y + graph.xAxisLinePadding);
		if (newX < 0) newX = 0;
		graph.changeSpriteWidth(graph.xAxisLine, newX);
		graph.changeSpritePositionToX(graph.xAxisLine, newX / 2f);
		int newY = Mathf.RoundToInt(newSize.y - graph.paddingTopBottom.x - graph.paddingTopBottom.y + graph.yAxisLinePadding);
		if (newY < 0) newY = 0;
		graph.changeSpriteHeight(graph.yAxisLine, newY);
		graph.changeSpritePositionToY(graph.yAxisLine, newY / 2f);
	}
	
	void DrawCore() {
		graph.autoRefresh = EditorGUILayout.Toggle ("Auto Refresh", graph.autoRefresh);
		graph.graphType = (WMG_Axis_Graph.graphTypes)EditorGUILayout.EnumPopup("Graph Type", graph.graphType);
		graph.orientationType = (WMG_Axis_Graph.orientationTypes)EditorGUILayout.EnumPopup("Orientation Type", graph.orientationType);
		graph.axesType = (WMG_Axis_Graph.axesTypes)EditorGUILayout.EnumPopup("Axes Type", graph.axesType);
		graph.resizeType = (WMG_Axis_Graph.resizeTypes)EditorGUILayout.EnumPopup("Resize Type", graph.resizeType);
		#if !UNITY_2017_3_OR_NEWER 
		graph.resizeProperties = (WMG_Axis_Graph.ResizeProperties)EditorGUILayout.EnumMaskField("Resize Properties", graph.resizeProperties);
		#else
		graph.resizeProperties = (WMG_Axis_Graph.ResizeProperties)EditorGUILayout.EnumFlagsField("Resize Properties", graph.resizeProperties);
		#endif
		graph.useGroups = EditorGUILayout.Toggle ("Use Groups", graph.useGroups);
		graph.groupsCentered = EditorGUILayout.Toggle ("Groups Centered", graph.groupsCentered);
		ArrayGUI("Groups", "groups");
		ArrayGUI("Series", "lineSeries");
		graph.paddingLeftRight = EditorGUILayout.Vector2Field("Padding Left Right", graph.paddingLeftRight);
		graph.paddingTopBottom = EditorGUILayout.Vector2Field("Padding Top Bottom", graph.paddingTopBottom);
		graph.theOrigin = EditorGUILayout.Vector2Field("The Origin", graph.theOrigin);
		graph.barWidth = EditorGUILayout.FloatField("Bar Width", graph.barWidth);
		graph.barAxisValue = EditorGUILayout.FloatField("Bar Axies Value", graph.barAxisValue);
		graph.autoUpdateOrigin = EditorGUILayout.Toggle ("Auto Update Origin", graph.autoUpdateOrigin);
		graph.autoUpdateBarWidth = EditorGUILayout.Toggle ("Auto Update Bar Width", graph.autoUpdateBarWidth);
		graph.autoUpdateBarAxisValue = EditorGUILayout.Toggle ("Auto Update Bar Axis Value", graph.autoUpdateBarAxisValue);
	}

	void DrawAxes() {
		graph.yAxisMinValue = EditorGUILayout.FloatField("Y Min Value", graph.yAxisMinValue);
		graph.yAxisMaxValue = EditorGUILayout.FloatField("Y Max Value", graph.yAxisMaxValue);
		graph.xAxisMinValue = EditorGUILayout.FloatField("X Min Value", graph.xAxisMinValue);
		graph.xAxisMaxValue = EditorGUILayout.FloatField("X Max Value", graph.xAxisMaxValue);
		graph.axisWidth = EditorGUILayout.IntField("Axis Width", graph.axisWidth);
		graph.yAxisNumTicks = EditorGUILayout.IntField("Y Num Ticks", graph.yAxisNumTicks);
		graph.xAxisNumTicks = EditorGUILayout.IntField("X Num Ticks", graph.xAxisNumTicks);
		ArrayGUI("Y Auto Grow", "yMinMaxAutoGrow", EditorListOption.ListLabel);
		ArrayGUI("Y Auto Shrink", "yMinMaxAutoShrink", EditorListOption.ListLabel);
		ArrayGUI("X Auto Grow", "xMinMaxAutoGrow", EditorListOption.ListLabel);
		ArrayGUI("X Auto Shrink", "xMinMaxAutoShrink", EditorListOption.ListLabel);
		graph.autoShrinkAtPercent = EditorGUILayout.FloatField("Auto Shrink At Percent", graph.autoShrinkAtPercent);
		graph.autoGrowAndShrinkByPercent = EditorGUILayout.FloatField("Auto Grow Shrink By Percent", graph.autoGrowAndShrinkByPercent);
		graph.yAxisLinePadding = EditorGUILayout.FloatField("Y Axis Line Padding", graph.yAxisLinePadding);
		graph.xAxisLinePadding = EditorGUILayout.FloatField("X Axis Line Padding", graph.xAxisLinePadding);
		graph.yAxisUseNonTickPercent = EditorGUILayout.Toggle ("Y Axis Not Tick Based", graph.yAxisUseNonTickPercent);
		graph.xAxisUseNonTickPercent = EditorGUILayout.Toggle ("X Axis Not Tick Based", graph.xAxisUseNonTickPercent);
		EditorGUILayout.Separator();
		EditorGUILayout.LabelField("Manual Axes Type Parameters", EditorStyles.boldLabel);
		graph.yAxisNonTickPercent = EditorGUILayout.FloatField("Y Axis Not Tick Percentage", graph.yAxisNonTickPercent);
		graph.xAxisNonTickPercent = EditorGUILayout.FloatField("X Axis Not Tick Percentage", graph.xAxisNonTickPercent);
		ArrayGUI("Y Axis Arrows", "yAxisArrows", EditorListOption.ListLabel);
		ArrayGUI("X Axis Arrows", "xAxisArrows", EditorListOption.ListLabel);
		graph.yAxisTicksRight = EditorGUILayout.Toggle ("Y Axis Ticks Right", graph.yAxisTicksRight);
		graph.xAxisTicksAbove = EditorGUILayout.Toggle ("X Axis Ticks Above", graph.xAxisTicksAbove);
		graph.yAxisXTick = EditorGUILayout.IntField("Y Axis X Tick", graph.yAxisXTick);
		graph.xAxisYTick = EditorGUILayout.IntField("X Axis Y Tick", graph.xAxisYTick);
		graph.hideYTick = EditorGUILayout.Toggle ("Hide Y Tick Label", graph.hideYTick);
		graph.hideXTick = EditorGUILayout.Toggle ("Hide X Tick Label", graph.hideXTick);
	}

	void DrawTooltip() {
		graph.tooltipEnabled = EditorGUILayout.Toggle("Enabled", graph.tooltipEnabled);
		graph.tooltipOffset = EditorGUILayout.Vector2Field("Offset", graph.tooltipOffset);
		graph.tooltipNumberDecimals = EditorGUILayout.IntField("Number Decimals", graph.tooltipNumberDecimals);
		graph.tooltipDisplaySeriesName = EditorGUILayout.Toggle ("Display Series Name", graph.tooltipDisplaySeriesName);
	}

	void DrawAnim() {
		graph.tooltipAnimationsEnabled = EditorGUILayout.Toggle ("Tooltip Animations Enabled", graph.tooltipAnimationsEnabled);
		graph.tooltipAnimationsEasetype = (Ease)EditorGUILayout.EnumPopup("Tooltip Animations Easetype", graph.tooltipAnimationsEasetype);
		graph.tooltipAnimationsDuration = EditorGUILayout.FloatField("Tooltip Animations Duration", graph.tooltipAnimationsDuration);
		graph.autoAnimationsEnabled = EditorGUILayout.Toggle ("Auto Animations Enabled", graph.autoAnimationsEnabled);
		graph.autoAnimationsEasetype = (Ease)EditorGUILayout.EnumPopup("Auto Animations Easetype", graph.autoAnimationsEasetype);
		graph.autoAnimationsDuration = EditorGUILayout.FloatField("Auto Animations Duration", graph.autoAnimationsDuration);
	}

	void DrawLabels() {
		graph.yLabelType = (WMG_Axis_Graph.labelTypes)EditorGUILayout.EnumPopup("Y Label Type", graph.yLabelType);
		graph.xLabelType = (WMG_Axis_Graph.labelTypes)EditorGUILayout.EnumPopup("X Label Type", graph.xLabelType);
		ArrayGUI("Y Labels", "yAxisLabels");
		ArrayGUI("X Labels", "xAxisLabels");
		graph.yAxisLabelRotation = EditorGUILayout.FloatField("Y Label Rotation", graph.yAxisLabelRotation);
		graph.xAxisLabelRotation = EditorGUILayout.FloatField("X Label Rotation", graph.xAxisLabelRotation);
		graph.SetYLabelsUsingMaxMin = EditorGUILayout.Toggle ("Set Y Using Max Min", graph.SetYLabelsUsingMaxMin);
		graph.SetXLabelsUsingMaxMin = EditorGUILayout.Toggle ("Set X Using Max Min", graph.SetXLabelsUsingMaxMin);
		graph.yAxisLabelSize = EditorGUILayout.FloatField("Y Label Size", graph.yAxisLabelSize);
		graph.xAxisLabelSize = EditorGUILayout.FloatField("X Label Size", graph.xAxisLabelSize);
		graph.numDecimalsYAxisLabels = EditorGUILayout.IntField("Num Decimals Y Labels", graph.numDecimalsYAxisLabels);
		graph.numDecimalsXAxisLabels = EditorGUILayout.IntField("Num Decimals X Labels", graph.numDecimalsXAxisLabels);
		graph.hideXLabels = EditorGUILayout.Toggle ("Hide X Labels", graph.hideXLabels);
		graph.hideYLabels = EditorGUILayout.Toggle ("Hide Y Labels", graph.hideYLabels);
		graph.yAxisLabelSpacingX = EditorGUILayout.FloatField("Y Spacing X", graph.yAxisLabelSpacingX);
		graph.xAxisLabelSpacingY = EditorGUILayout.FloatField("X Spacing Y", graph.xAxisLabelSpacingY);
		EditorGUILayout.Separator();
		EditorGUILayout.LabelField("Manual Label Type Parameters", EditorStyles.boldLabel);
		graph.yAxisLabelSpacingY = EditorGUILayout.FloatField("Y Spacing Y", graph.yAxisLabelSpacingY);
		graph.xAxisLabelSpacingX = EditorGUILayout.FloatField("X Spacing X", graph.xAxisLabelSpacingX);
		graph.yAxisLabelDistBetween = EditorGUILayout.FloatField("Dist Between Y Labels", graph.yAxisLabelDistBetween);
		graph.xAxisLabelDistBetween = EditorGUILayout.FloatField("Dist Between X Labels", graph.xAxisLabelDistBetween);
	}

	void DrawMisc() {
		ArrayGUI("Point Prefabs", "pointPrefabs");
		ArrayGUI("Link Prefabs", "linkPrefabs");
		graph.barPrefab = EditorGUILayout.ObjectField("Bar Prefab", graph.barPrefab, typeof(Object), false);
		graph.seriesPrefab = EditorGUILayout.ObjectField("Series Prefab", graph.seriesPrefab, typeof(Object), false);
		graph.legendPrefab = EditorGUILayout.ObjectField("Legend Prefab", graph.legendPrefab, typeof(Object), false);
		graph.hideXGrid = EditorGUILayout.Toggle ("Hide X Grid", graph.hideXGrid);
		graph.hideYGrid = EditorGUILayout.Toggle ("Hide Y Grid", graph.hideYGrid);
		graph.hideXTicks = EditorGUILayout.Toggle ("Hide X Ticks", graph.hideXTicks);
		graph.hideYTicks = EditorGUILayout.Toggle ("Hide Y Ticks", graph.hideYTicks);
		graph.tickSize = EditorGUILayout.Vector2Field("Tick Size", graph.tickSize);
		graph.graphTitleString = EditorGUILayout.TextField("Graph Title String", graph.graphTitleString);
		graph.yAxisTitleString = EditorGUILayout.TextField("Y Axis Title String", graph.yAxisTitleString);
		graph.xAxisTitleString = EditorGUILayout.TextField("X Axis Title String", graph.xAxisTitleString);
		graph.graphTitleOffset = EditorGUILayout.Vector2Field("Graph Title Offset", graph.graphTitleOffset);
		graph.yAxisTitleOffset = EditorGUILayout.Vector2Field("Y Axis Title Offset", graph.yAxisTitleOffset);
		graph.xAxisTitleOffset = EditorGUILayout.Vector2Field("X Axis Title Offset", graph.xAxisTitleOffset);
		graph.legend = (WMG_Legend)EditorGUILayout.ObjectField("Legend", graph.legend, typeof(WMG_Legend), true);
		graph.yAxisTicks = (GameObject)EditorGUILayout.ObjectField("Y Axis Ticks", graph.yAxisTicks, typeof(GameObject), true);
		graph.xAxisTicks = (GameObject)EditorGUILayout.ObjectField("X Axis Ticks", graph.xAxisTicks, typeof(GameObject), true);
		graph.yAxisLabelObjs = (GameObject)EditorGUILayout.ObjectField("Y Axis Labels", graph.yAxisLabelObjs, typeof(GameObject), true);
		graph.xAxisLabelObjs = (GameObject)EditorGUILayout.ObjectField("X Axis Labels", graph.xAxisLabelObjs, typeof(GameObject), true);

	}
	

}