using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

[CustomEditor(typeof(WMG_Pie_Graph))]
public class WMG_Pie_Graph_E : WMG_E_Util
{
//	SerializedObject myTarget;
	WMG_Pie_Graph graph;

	enum eTabType
	{
		Core,
		OtherSlice,
		Anim,
		Labels,
		Misc
	}

	private eTabType m_tabType = eTabType.Core;

	void OnEnable()
	{
//		myTarget = new SerializedObject(target);
		graph = (WMG_Pie_Graph)target;
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
		case eTabType.OtherSlice: DrawOtherSlice(); break;
		case eTabType.Anim: DrawAnim(); break;
		case eTabType.Labels: DrawLabels(); break;
		case eTabType.Misc: DrawMisc(); break;
		}								

		// Update graphics based on graph width and height
		UpdateSceneView();

		if( GUI.changed ) {
			EditorUtility.SetDirty( graph );
		}
		
		// Apply changes to the serializedProperty - always do this in the end of OnInspectorGUI.
		serializedObject.ApplyModifiedProperties();
	}

	void UpdateSceneView() {
		/*
		Vector2 newSize = graph.getSpriteSize(graph.gameObject);
		float minSize = newSize.x;
		if (newSize.y < minSize) minSize = newSize.y;
		graph.changeSpriteSize(graph.background, Mathf.RoundToInt(newSize.x), Mathf.RoundToInt(newSize.y));
		graph.changeSpriteSize(graph.backgroundCircle, Mathf.RoundToInt(minSize), Mathf.RoundToInt(minSize));
		*/
	}
	
	void DrawCore() {
		graph.autoRefresh = EditorGUILayout.Toggle ("Auto Refresh", graph.autoRefresh);
		graph.pieSize = EditorGUILayout.FloatField("Pie Size", graph.pieSize);
		graph.resizeType = (WMG_Pie_Graph.resizeTypes)EditorGUILayout.EnumPopup("Resize Type", graph.resizeType);
		#if !UNITY_2017_3_OR_NEWER 
		graph.resizeProperties = (WMG_Pie_Graph.ResizeProperties)EditorGUILayout.EnumMaskField("Resize Properties", graph.resizeProperties);
		#else
		graph.resizeProperties = (WMG_Pie_Graph.ResizeProperties)EditorGUILayout.EnumFlagsField("Resize Properties", graph.resizeProperties);
		#endif
		ArrayGUI("Values", "sliceValues");
		ArrayGUI("Labels", "sliceLabels");
		ArrayGUI("Colors", "sliceColors");
		graph.sortBy = (WMG_Pie_Graph.sortMethod)EditorGUILayout.EnumPopup("Sort By", graph.sortBy);
		graph.swapColorsDuringSort = EditorGUILayout.Toggle ("Swap Colors During Sort", graph.swapColorsDuringSort);
		graph.sliceLabelType = (WMG_Enums.labelTypes)EditorGUILayout.EnumPopup("Slice Label Type", graph.sliceLabelType);
		graph.explodeLength = EditorGUILayout.FloatField("Explode Length", graph.explodeLength);
		graph.explodeSymmetrical = EditorGUILayout.Toggle ("Explode Symmetrical", graph.explodeSymmetrical);
		graph.doughnutRadius = EditorGUILayout.FloatField("Doughnut Radius", graph.doughnutRadius);
	}

	void DrawOtherSlice() {
		graph.limitNumberSlices = EditorGUILayout.Toggle ("Limit Number Slices", graph.limitNumberSlices);
		graph.includeOthers = EditorGUILayout.Toggle ("Include Others", graph.includeOthers);
		graph.maxNumberSlices = EditorGUILayout.IntField("Max Number Slices", graph.maxNumberSlices);
		graph.includeOthersLabel = EditorGUILayout.TextField("Include Others Label", graph.includeOthersLabel);
		graph.includeOthersColor = EditorGUILayout.ColorField("Include Others Color", graph.includeOthersColor);
	}

	void DrawAnim() {
		graph.animationDuration = EditorGUILayout.FloatField("Animation Duration", graph.animationDuration);
		graph.sortAnimationDuration = EditorGUILayout.FloatField("Sort Animation Duration", graph.sortAnimationDuration);
	}

	void DrawLabels() {
		graph.sliceLabelExplodeLength = EditorGUILayout.FloatField("Slice Label Explode Length", graph.sliceLabelExplodeLength);
		graph.sliceLabelFontSize = EditorGUILayout.FloatField("Slice Label Font Size", graph.sliceLabelFontSize);
		graph.numberDecimalsInPercents = EditorGUILayout.IntField("Num Decimals In Percents", graph.numberDecimalsInPercents);
	}

	void DrawMisc() {
		graph.sliceValuesDataSource = (WMG_Data_Source)EditorGUILayout.ObjectField("Values Data Source", graph.sliceValuesDataSource, typeof(WMG_Data_Source), true);
		graph.sliceLabelsDataSource = (WMG_Data_Source)EditorGUILayout.ObjectField("Labels Data Source", graph.sliceLabelsDataSource, typeof(WMG_Data_Source), true);
		graph.sliceColorsDataSource = (WMG_Data_Source)EditorGUILayout.ObjectField("Colors Data Source", graph.sliceColorsDataSource, typeof(WMG_Data_Source), true);

		graph.leftRightPadding = EditorGUILayout.Vector2Field("Left Right Padding", graph.leftRightPadding);
		graph.topBotPadding = EditorGUILayout.Vector2Field("Top Bot Padding", graph.topBotPadding);
		graph.bgCircleOffset = EditorGUILayout.FloatField("Circle Background Offset", graph.bgCircleOffset);

		graph.background = (GameObject)EditorGUILayout.ObjectField("Background", graph.background, typeof(GameObject), true);
		graph.backgroundCircle = (GameObject)EditorGUILayout.ObjectField("Circle Background", graph.backgroundCircle, typeof(GameObject), true);
		graph.slicesParent = (GameObject)EditorGUILayout.ObjectField("Slices Parent", graph.slicesParent, typeof(GameObject), true);
		graph.legend = (WMG_Legend)EditorGUILayout.ObjectField("Legend", graph.legend, typeof(WMG_Legend), true);
		graph.legendEntryPrefab = EditorGUILayout.ObjectField("Legend Entry Prefab", graph.legendEntryPrefab, typeof(Object), false);
		graph.nodePrefab = EditorGUILayout.ObjectField("Slice Prefab", graph.nodePrefab, typeof(Object), false);
	}

}