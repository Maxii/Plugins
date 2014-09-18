using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;

[ExecuteInEditMode]
public class FixWidgetDepth : MonoBehaviour
{
	public bool appendNumber = true;

	void Start()
	{
		#if UNITY_EDITOR
		this.FixName();
		this.FixDepth();
		#endif
		DestroyImmediate(this); // Remove this script
	}
	
	private void FixName()
	{
		if (this.appendNumber)
			this.gameObject.name += " " + (this.transform.parent.childCount - 1).ToString();
	}
	
	private void FixDepth()
	{
		// Get the widgets
		List<UIWidget> widgets = this.GetWidgets();
		
		// Adjust the depth of the widgets to start from 0 and update them with the correct ones later
		for (int i = 0; i < widgets.Count; i++)
			widgets[i].depth = i;
		
		// Get the panel containing the widgets
		UIPanel rootPanel = NGUITools.FindInParents<UIPanel>(this.transform);
		
		// Make sure we have a root panel
		if (rootPanel == null)
			return;
		
		// Get the next depth for the panel
		int nextDepth = NGUITools.CalculateNextDepth(rootPanel.gameObject);
		
		// Adjust the depth of the widgets
		for (int i = 0; i < widgets.Count; i++)
			widgets[i].depth = nextDepth + i;
	}
	
	private List<UIWidget> GetWidgets()
	{
		List<UIWidget> widgets = new List<UIWidget>();
		
		// Get the widgets into the list
		this.GetTransformWidgets(this.transform, ref widgets);
		
		// Sort the list by depth
		widgets.Sort(SortByDepth);
		
		// Return the list
		return widgets;
	}
	
	private void GetTransformWidgets(Transform trans, ref List<UIWidget> widgets)
	{
		UIWidget w = trans.GetComponent<UIWidget>();
		
		// Check if we found a widget on that transform
		if (w != null)
			widgets.Add(w);
		
		foreach (Transform t in trans)
		{
			this.GetTransformWidgets(t, ref widgets);
		}
	}
	
	/// <summary>
	/// Function that sorts items by depth.
	/// </summary>
	
	public static int SortByDepth(UIWidget a, UIWidget b)
	{
		if (a != null && b != null)
			return a.depth.CompareTo(b.depth);
		
		return 0;
	}
}