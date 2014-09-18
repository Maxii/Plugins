using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class FixPanelDepth : MonoBehaviour
{
	void Start()
	{
		#if UNITY_EDITOR
		UIPanel panel = this.GetComponent<UIPanel>();
		if (panel != null)
			panel.depth = UIPanel.nextUnusedDepth;
		#endif
		DestroyImmediate(this); // Remove this script
	}
}