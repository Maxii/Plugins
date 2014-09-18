using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class FixWindowId : MonoBehaviour
{
	void Start()
	{
		SpaceDUIWindow thisWindow = this.GetComponent<SpaceDUIWindow>();
		if (thisWindow != null)
		{
			SpaceDUIWindow[] windows = (SpaceDUIWindow[])GameObject.FindObjectsOfType(typeof(SpaceDUIWindow));

			int LastId = 0;

			foreach (SpaceDUIWindow window in windows)
				if (window.WindowId > LastId)
					LastId = window.WindowId;

			thisWindow.WindowId = LastId + 1;
		}

		DestroyImmediate(this); // Remove this script
	}
}