//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Example script showing how to activate or deactivate a game object when OnActivate event is received.
/// OnActivate event is sent out by the UIToggle script.
/// </summary>

[ExecuteInEditMode]
[AddComponentMenu("NGUI/Interaction/Toggled Objects")]
public class UIToggledObjects : MonoBehaviour
{
	public List<GameObject> activate;
	public List<GameObject> deactivate;

	[HideInInspector][SerializeField] GameObject target;
	[HideInInspector][SerializeField] bool inverse = false;

	void Awake ()
	{
		// Legacy functionality -- auto-upgrade
		if (target != null)
		{
			if (activate.Count == 0 && deactivate.Count == 0)
			{
				if (inverse) deactivate.Add(target);
				else activate.Add(target);
			}
			else target = null;

#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(this);
#endif
		}

#if UNITY_EDITOR
		if (!Application.isPlaying) return;
#endif
		UIToggle toggle = GetComponent<UIToggle>();
		EventDelegate.Add(toggle.onChange, Toggle);
	}

	public void Toggle ()
	{
		bool val = UIToggle.current.value;

		if (enabled)
		{
			for (int i = 0; i < activate.Count; ++i)
				Set(activate[i], val);

			for (int i = 0; i < deactivate.Count; ++i)
				Set(deactivate[i], !val);
		}
	}

	void Set (GameObject go, bool state)
	{
		if (go != null)
		{
			NGUITools.SetActive(go, state);
			//UIPanel panel = NGUITools.FindInParents<UIPanel>(target);
			//if (panel != null) panel.Refresh();
		}
	}
}
