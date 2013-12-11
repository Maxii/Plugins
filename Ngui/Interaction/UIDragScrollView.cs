//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using System.Collections;

/// <summary>
/// Allows dragging of the specified scroll view by mouse or touch.
/// </summary>

[AddComponentMenu("NGUI/Interaction/Drag Scroll View")]
public class UIDragScrollView : MonoBehaviour
{
	/// <summary>
	/// Reference to the scroll view that will be dragged by the script.
	/// </summary>

	public UIScrollView scrollView;

	// Legacy functionality, kept for backwards compatibility. Use 'scrollView' instead.
	[HideInInspector][SerializeField] UIScrollView draggablePanel;

	Transform mTrans;
	UIScrollView mScroll;
	bool mAutoFind = false;

	/// <summary>
	/// Automatically find the scroll view if possible.
	/// </summary>

	void OnEnable ()
	{
		mTrans = transform;

		// Auto-upgrade
		if (scrollView == null && draggablePanel != null)
		{
			scrollView = draggablePanel;
			draggablePanel = null;
		}

		// If the scroll view is on a parent, don't try to remember it (as we want it to be dynamic in case of re-parenting)
		UIScrollView sv = NGUITools.FindInParents<UIScrollView>(mTrans);
		
		if (scrollView == null)
		{
			scrollView = sv;
			mAutoFind = true;
		}
		else if (scrollView == sv)
		{
			mAutoFind = true;
		}
	}

	/// <summary>
	/// Create a plane on which we will be performing the dragging.
	/// </summary>

	void OnPress (bool pressed)
	{
		if (scrollView && enabled && NGUITools.GetActive(gameObject))
		{
			scrollView.Press(pressed);
			if (!pressed && mAutoFind) scrollView = NGUITools.FindInParents<UIScrollView>(mTrans);
		}
	}

	/// <summary>
	/// Drag the object along the plane.
	/// </summary>

	void OnDrag (Vector2 delta)
	{
		if (scrollView && enabled && NGUITools.GetActive(gameObject))
			scrollView.Drag();
	}

	/// <summary>
	/// If the object should support the scroll wheel, do it.
	/// </summary>

	void OnScroll (float delta)
	{
		if (scrollView && enabled && NGUITools.GetActive(gameObject))
			scrollView.Scroll(delta);
	}
}
