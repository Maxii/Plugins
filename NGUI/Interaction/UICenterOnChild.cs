//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;

/// <summary>
/// Ever wanted to be able to auto-center on an object within a draggable panel?
/// Attach this script to the container that has the objects to center on as its children.
/// </summary>

[AddComponentMenu("NGUI/Interaction/Center Panel On Child")]
public class UICenterOnChild : MonoBehaviour
{
	/// <summary>
	/// The strength of the spring.
	/// </summary>

	public float springStrength = 8f;

	/// <summary>
	/// Callback to be triggered when the centering operation completes.
	/// </summary>

	public SpringPanel.OnFinished onFinished;

	UIScrollView mDrag;
	GameObject mCenteredObject;

	/// <summary>
	/// Game object that the draggable panel is currently centered on.
	/// </summary>

	public GameObject centeredObject { get { return mCenteredObject; } }

	void OnEnable () { Recenter(); }
	void OnDragFinished () { if (enabled) Recenter(); }

	/// <summary>
	/// Recenter the draggable list on the center-most child.
	/// </summary>

	public void Recenter ()
	{
		if (mDrag == null)
		{
			mDrag = NGUITools.FindInParents<UIScrollView>(gameObject);

			if (mDrag == null)
			{
				Debug.LogWarning(GetType() + " requires " + typeof(UIScrollView) + " on a parent object in order to work", this);
				enabled = false;
				return;
			}
			else
			{
				mDrag.onDragFinished = OnDragFinished;
				
				if (mDrag.horizontalScrollBar != null)
					mDrag.horizontalScrollBar.onDragFinished = OnDragFinished;

				if (mDrag.verticalScrollBar != null)
					mDrag.verticalScrollBar.onDragFinished = OnDragFinished;
			}
		}
		if (mDrag.panel == null) return;

		// Calculate the panel's center in world coordinates
		Vector3[] corners = mDrag.panel.worldCorners;
		Vector3 panelCenter = (corners[2] + corners[0]) * 0.5f;

		// Offset this value by the momentum
		Vector3 pickingPoint = panelCenter - mDrag.currentMomentum * (mDrag.momentumAmount * 0.1f);
		mDrag.currentMomentum = Vector3.zero;

		float min = float.MaxValue;
		Transform closest = null;
		Transform trans = transform;

		// Determine the closest child
		for (int i = 0, imax = trans.childCount; i < imax; ++i)
		{
			Transform t = trans.GetChild(i);
			float sqrDist = Vector3.SqrMagnitude(t.position - pickingPoint);

			if (sqrDist < min)
			{
				min = sqrDist;
				closest = t;
			}
		}
		CenterOn(closest, panelCenter);
	}

	/// <summary>
	/// Center the panel on the specified target.
	/// </summary>

	void CenterOn (Transform target, Vector3 panelCenter)
	{
		if (target != null && mDrag != null && mDrag.panel != null)
		{
			Transform panelTrans = mDrag.panel.cachedTransform;
			mCenteredObject = target.gameObject;

			// Figure out the difference between the chosen child and the panel's center in local coordinates
			Vector3 cp = panelTrans.InverseTransformPoint(target.position);
			Vector3 cc = panelTrans.InverseTransformPoint(panelCenter);
			Vector3 localOffset = cp - cc;

			// Offset shouldn't occur if blocked
			if (!mDrag.canMoveHorizontally) localOffset.x = 0f;
			if (!mDrag.canMoveVertically) localOffset.y = 0f;
			localOffset.z = 0f;

			// Spring the panel to this calculated position
			SpringPanel.Begin(mDrag.panel.cachedGameObject,
				panelTrans.localPosition - localOffset, springStrength).onFinished = onFinished;
		}
		else mCenteredObject = null;
	}

	/// <summary>
	/// Center the panel on the specified target.
	/// </summary>

	public void CenterOn (Transform target)
	{
		if (mDrag != null && mDrag.panel != null)
		{
			Vector3[] corners = mDrag.panel.worldCorners;
			Vector3 panelCenter = (corners[2] + corners[0]) * 0.5f;
			CenterOn(target, panelCenter);
		}
	}
}
