//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;

/// <summary>
/// This script, when attached to a panel turns it into a scroll view.
/// You can then attach UIDragScrollView to colliders within to make it draggable.
/// </summary>

[ExecuteInEditMode]
[RequireComponent(typeof(UIPanel))]
[AddComponentMenu("NGUI/Interaction/Scroll View")]
public class UIScrollView : MonoBehaviour
{
	public enum Movement
	{
		Horizontal,
		Vertical,
		Unrestricted,
		Custom,
	}

	public enum DragEffect
	{
		None,
		Momentum,
		MomentumAndSpring,
	}

	public enum ShowCondition
	{
		Always,
		OnlyIfNeeded,
		WhenDragging,
	}

	public delegate void OnDragFinished ();

	/// <summary>
	/// Type of movement allowed by the scroll view.
	/// </summary>

	public Movement movement = Movement.Horizontal;

	/// <summary>
	/// Effect to apply when dragging.
	/// </summary>

	public DragEffect dragEffect = DragEffect.MomentumAndSpring;

	/// <summary>
	/// Whether the dragging will be restricted to be within the scroll view's bounds.
	/// </summary>

	public bool restrictWithinPanel = true;

	/// <summary>
	/// Whether dragging will be disabled if the contents fit.
	/// </summary>

	public bool disableDragIfFits = false;

	/// <summary>
	/// Whether the drag operation will be started smoothly, or if if it will be precise (but will have a noticeable "jump").
	/// </summary>

	public bool smoothDragStart = true;

	/// <summary>
	/// Whether to use iOS drag emulation, where the content only drags at half the speed of the touch/mouse movement when the content edge is within the clipping area.
	/// </summary>	
	
	public bool iOSDragEmulation = true;

	/// <summary>
	/// Effect the scroll wheel will have on the momentum.
	/// </summary>

	public float scrollWheelFactor = 0.25f;

	/// <summary>
	/// How much momentum gets applied when the press is released after dragging.
	/// </summary>

	public float momentumAmount = 35f;
	
	/// <summary>
	/// Horizontal scrollbar used for visualization.
	/// </summary>

	public UIScrollBar horizontalScrollBar;

	/// <summary>
	/// Vertical scrollbar used for visualization.
	/// </summary>

	public UIScrollBar verticalScrollBar;

	/// <summary>
	/// Condition that must be met for the scroll bars to become visible.
	/// </summary>

	public ShowCondition showScrollBars = ShowCondition.OnlyIfNeeded;

	/// <summary>
	/// Custom movement, if the 'movement' field is set to 'Custom'.
	/// </summary>

	public Vector2 customMovement = new Vector2(1f, 0f);

	/// <summary>
	/// Starting position of the clipped area. (0, 0) means top-left corner, (1, 1) means bottom-right.
	/// </summary>

	public Vector2 relativePositionOnReset = Vector2.zero;

	/// <summary>
	/// Event callback to trigger when the drag process finished. Can be used for additional effects, such as centering on some object.
	/// </summary>

	public OnDragFinished onDragFinished;

	// Deprecated functionality. Use 'movement' instead.
	[HideInInspector][SerializeField] Vector3 scale = new Vector3(1f, 0f, 0f);

	Transform mTrans;
	UIPanel mPanel;
	Plane mPlane;
	Vector3 mLastPos;
	bool mPressed = false;
	Vector3 mMomentum = Vector3.zero;
	float mScroll = 0f;
	Bounds mBounds;
	bool mCalculatedBounds = false;
	bool mShouldMove = false;
	bool mIgnoreCallbacks = false;
	int mDragID = -10;
	Vector2 mDragStartOffset = Vector2.zero;
	bool mDragStarted = false;

	/// <summary>
	/// Panel that's being dragged.
	/// </summary>

	public UIPanel panel { get { return mPanel; } }

	/// <summary>
	/// Calculate the bounds used by the widgets.
	/// </summary>

	public Bounds bounds
	{
		get
		{
			if (!mCalculatedBounds)
			{
				mCalculatedBounds = true;
				mBounds = NGUIMath.CalculateRelativeWidgetBounds(mTrans, mTrans);
			}
			return mBounds;
		}
	}

	/// <summary>
	/// Whether the scroll view can move horizontally.
	/// </summary>

	public bool canMoveHorizontally
	{
		get
		{
			return movement == Movement.Horizontal ||
				movement == Movement.Unrestricted ||
				(movement == Movement.Custom && customMovement.x != 0f);
		}
	}

	/// <summary>
	/// Whether the scroll view can move vertically.
	/// </summary>

	public bool canMoveVertically
	{
		get
		{
			return movement == Movement.Vertical ||
				movement == Movement.Unrestricted ||
				(movement == Movement.Custom && customMovement.y != 0f);
		}
	}

	/// <summary>
	/// Whether the scroll view should be able to move horizontally (contents don't fit).
	/// </summary>

	public virtual bool shouldMoveHorizontally
	{
		get
		{
			float size = bounds.size.x;
			if (mPanel.clipping == UIDrawCall.Clipping.SoftClip) size += mPanel.clipSoftness.x * 2f;
			return size > mPanel.clipRange.z;
		}
	}

	/// <summary>
	/// Whether the scroll view should be able to move vertically (contents don't fit).
	/// </summary>

	public virtual bool shouldMoveVertically
	{
		get
		{
			float size = bounds.size.y;
			if (mPanel.clipping == UIDrawCall.Clipping.SoftClip) size += mPanel.clipSoftness.y * 2f;
			return size > mPanel.clipRange.w;
		}
	}

	/// <summary>
	/// Whether the contents of the scroll view should actually be draggable depends on whether they currently fit or not.
	/// </summary>

	protected virtual bool shouldMove
	{
		get
		{
			if (!disableDragIfFits) return true;

			if (mPanel == null) mPanel = GetComponent<UIPanel>();
			Vector4 clip = mPanel.clipRange;
			Bounds b = bounds;

			float hx = (clip.z == 0f) ? Screen.width  : clip.z * 0.5f;
			float hy = (clip.w == 0f) ? Screen.height : clip.w * 0.5f;

			if (canMoveHorizontally)
			{
				if (b.min.x < clip.x - hx) return true;
				if (b.max.x > clip.x + hx) return true;
			}

			if (canMoveVertically)
			{
				if (b.min.y < clip.y - hy) return true;
				if (b.max.y > clip.y + hy) return true;
			}
			return false;
		}
	}

	/// <summary>
	/// Current momentum, exposed just in case it's needed.
	/// </summary>

	public Vector3 currentMomentum { get { return mMomentum; } set { mMomentum = value; mShouldMove = true; } }

	/// <summary>
	/// Cache the transform and the panel.
	/// </summary>

	void Awake ()
	{
		mTrans = transform;
		mPanel = GetComponent<UIPanel>();

		if (mPanel.clipping == UIDrawCall.Clipping.None)
			mPanel.clipping = UIDrawCall.Clipping.ConstrainButDontClip;
		
		// Auto-upgrade
		if (movement != Movement.Custom && scale.sqrMagnitude > 0.001f)
		{
			if (scale.x == 1f && scale.y == 0f)
			{
				movement = Movement.Horizontal;
			}
			else if (scale.x == 0f && scale.y == 1f)
			{
				movement = Movement.Vertical;
			}
			else if (scale.x == 1f && scale.y == 1f)
			{
				movement = Movement.Unrestricted;
			}
			else
			{
				movement = Movement.Custom;
				customMovement.x = scale.x;
				customMovement.y = scale.y;
			}
			scale = Vector3.zero;
#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(this);
#endif
		}
		if (Application.isPlaying) mPanel.onChange += OnPanelChange;
	}

	void OnDestroy ()
	{
		if (Application.isPlaying && mPanel != null)
			mPanel.onChange -= OnPanelChange;
	}

	void OnPanelChange () { UpdateScrollbars(true); }

	/// <summary>
	/// Set the initial drag value and register the listener delegates.
	/// </summary>

	void Start ()
	{
		if (Application.isPlaying)
		{
			UpdateScrollbars(true);

			if (horizontalScrollBar != null)
			{
				EventDelegate.Add(horizontalScrollBar.onChange, OnHorizontalBar);
				horizontalScrollBar.alpha = ((showScrollBars == ShowCondition.Always) || shouldMoveHorizontally) ? 1f : 0f;
			}

			if (verticalScrollBar != null)
			{
				EventDelegate.Add(verticalScrollBar.onChange, OnVerticalBar);
				verticalScrollBar.alpha = ((showScrollBars == ShowCondition.Always) || shouldMoveVertically) ? 1f : 0f;
			}
		}
	}

	/// <summary>
	/// Restrict the scroll view's contents to be within the scroll view's bounds.
	/// </summary>

	public bool RestrictWithinBounds (bool instant) { return RestrictWithinBounds(instant, true, true); }

	/// <summary>
	/// Restrict the scroll view's contents to be within the scroll view's bounds.
	/// </summary>

	public bool RestrictWithinBounds (bool instant, bool horizontal, bool vertical)
	{
		Vector3 constraint = mPanel.CalculateConstrainOffset(bounds.min, bounds.max);

		if (!horizontal) constraint.x = 0f;
		if (!vertical) constraint.y = 0f;

		if (constraint.magnitude > 0.001f)
		{
			if (!instant && dragEffect == DragEffect.MomentumAndSpring)
			{
				// Spring back into place
				Vector3 pos = mTrans.localPosition + constraint;
				pos.x = Mathf.Round(pos.x);
				pos.y = Mathf.Round(pos.y);
				SpringPanel.Begin(mPanel.gameObject, pos, 13f);
			}
			else
			{
				// Jump back into place
				MoveRelative(constraint);
				mMomentum = Vector3.zero;
				mScroll = 0f;
			}
			return true;
		}
		return false;
	}

	/// <summary>
	/// Disable the spring movement.
	/// </summary>

	public void DisableSpring ()
	{
		SpringPanel sp = GetComponent<SpringPanel>();
		if (sp != null) sp.enabled = false;
	}

	/// <summary>
	/// Update the values of the associated scroll bars.
	/// </summary>

	public virtual void UpdateScrollbars (bool recalculateBounds)
	{
		if (mPanel == null) return;

		if (horizontalScrollBar != null || verticalScrollBar != null)
		{
			if (recalculateBounds)
			{
				mCalculatedBounds = false;
				mShouldMove = shouldMove;
			}

			Bounds b = bounds;
			Vector2 bmin = b.min;
			Vector2 bmax = b.max;

			if (horizontalScrollBar != null && bmax.x > bmin.x)
			{
				Vector4 clip = mPanel.clipRange;
				float extents = clip.z * 0.5f;

				if (mPanel.clipping == UIDrawCall.Clipping.SoftClip)
					extents -= mPanel.clipSoftness.x;

				float min = clip.x - extents - b.min.x;
				float max = b.max.x - extents - clip.x;

				float width = bmax.x - bmin.x;
				min = Mathf.Clamp01(min / width);
				max = Mathf.Clamp01(max / width);

				float sum = min + max;
				mIgnoreCallbacks = true;
				horizontalScrollBar.barSize = 1f - sum;
				horizontalScrollBar.value = (sum > 0.001f) ? min / sum : 0f;
				mIgnoreCallbacks = false;
			}

			if (verticalScrollBar != null && bmax.y > bmin.y)
			{
				Vector4 clip = mPanel.clipRange;
				float extents = clip.w * 0.5f;

				if (mPanel.clipping == UIDrawCall.Clipping.SoftClip)
					extents -= mPanel.clipSoftness.y;

				float min = clip.y - extents - bmin.y;
				float max = bmax.y - extents - clip.y;

				float height = bmax.y - bmin.y;
				min = Mathf.Clamp01(min / height);
				max = Mathf.Clamp01(max / height);
				float sum = min + max;

				mIgnoreCallbacks = true;
				verticalScrollBar.barSize = 1f - sum;
				verticalScrollBar.value = (sum > 0.001f) ? 1f - min / sum : 0f;
				mIgnoreCallbacks = false;
			}
		}
		else if (recalculateBounds)
		{
			mCalculatedBounds = false;
		}
	}

	/// <summary>
	/// Changes the drag amount of the scroll view to the specified 0-1 range values.
	/// (0, 0) is the top-left corner, (1, 1) is the bottom-right.
	/// </summary>

	public virtual void SetDragAmount (float x, float y, bool updateScrollbars)
	{
		DisableSpring();

		Bounds b = bounds;
		if (b.min.x == b.max.x || b.min.y == b.max.y) return;
		
		Vector4 cr = mPanel.clipRange;
		cr.x = Mathf.Round(cr.x);
		cr.y = Mathf.Round(cr.y);
		cr.z = Mathf.Round(cr.z);
		cr.w = Mathf.Round(cr.w);

		float hx = cr.z * 0.5f;
		float hy = cr.w * 0.5f;
		float left = b.min.x + hx;
		float right = b.max.x - hx;
		float bottom = b.min.y + hy;
		float top = b.max.y - hy;

		if (mPanel.clipping == UIDrawCall.Clipping.SoftClip)
		{
			left -= mPanel.clipSoftness.x;
			right += mPanel.clipSoftness.x;
			bottom -= mPanel.clipSoftness.y;
			top += mPanel.clipSoftness.y;
		}

		// Calculate the offset based on the scroll value
		float ox = Mathf.Lerp(left, right, x);
		float oy = Mathf.Lerp(top, bottom, y);

		ox = Mathf.Round(ox);
		oy = Mathf.Round(oy);

		// Update the position
		if (!updateScrollbars)
		{
			Vector3 pos = mTrans.localPosition;
			if (canMoveHorizontally) pos.x += cr.x - ox;
			if (canMoveVertically) pos.y += cr.y - oy;
			mTrans.localPosition = pos;
		}

		// Update the clipping offset
		if (canMoveHorizontally) cr.x = ox;
		if (canMoveVertically) cr.y = oy;
		mPanel.clipRange = cr;

		// Update the scrollbars, reflecting this change
		if (updateScrollbars) UpdateScrollbars(false);
	}

	/// <summary>
	/// Reset the scroll view's position to the top-left corner.
	/// It's recommended to call this function before AND after you re-populate the scroll view's contents (ex: switching window tabs).
	/// Another option is to populate the scroll view's contents, reset its position, then call this function to reposition the clipping.
	/// </summary>

	[ContextMenu("Reset Clipping Position")]
	public void ResetPosition()
	{
		// Invalidate the bounds
		mCalculatedBounds = false;

		// First move the position back to where it would be if the scroll bars got reset to zero
		SetDragAmount(relativePositionOnReset.x, relativePositionOnReset.y, false);

		// Next move the clipping area back and update the scroll bars
		SetDragAmount(relativePositionOnReset.x, relativePositionOnReset.y, true);
	}

	/// <summary>
	/// Triggered by the horizontal scroll bar when it changes.
	/// </summary>

	void OnHorizontalBar ()
	{
		if (!mIgnoreCallbacks)
		{
			float x = (horizontalScrollBar != null) ? horizontalScrollBar.value : 0f;
			float y = (verticalScrollBar != null) ? verticalScrollBar.value : 0f;
			SetDragAmount(x, y, false);
		}
	}

	/// <summary>
	/// Triggered by the vertical scroll bar when it changes.
	/// </summary>

	void OnVerticalBar ()
	{
		if (!mIgnoreCallbacks)
		{
			float x = (horizontalScrollBar != null) ? horizontalScrollBar.value : 0f;
			float y = (verticalScrollBar != null) ? verticalScrollBar.value : 0f;
			SetDragAmount(x, y, false);
		}
	}

	/// <summary>
	/// Move the scroll view by the specified amount.
	/// </summary>

	public virtual void MoveRelative (Vector3 relative)
	{
		relative.x = Mathf.Round(relative.x);
		relative.y = Mathf.Round(relative.y);
		mTrans.localPosition += relative;
		Vector4 cr = mPanel.clipRange;
		cr.x -= relative.x;
		cr.y -= relative.y;
		mPanel.clipRange = cr;
		UpdateScrollbars(false);
	}

	/// <summary>
	/// Move the scroll view by the specified amount.
	/// </summary>

	public void MoveAbsolute (Vector3 absolute)
	{
		Vector3 a = mTrans.InverseTransformPoint(absolute);
		Vector3 b = mTrans.InverseTransformPoint(Vector3.zero);
		MoveRelative(a - b);
	}

	/// <summary>
	/// Create a plane on which we will be performing the dragging.
	/// </summary>

	public void Press (bool pressed)
	{
		if (smoothDragStart && pressed)
		{
			mDragStarted = false;
			mDragStartOffset = Vector2.zero;
		}

		if (enabled && NGUITools.GetActive(gameObject))
		{
			if (!pressed && mDragID == UICamera.currentTouchID) mDragID = -10;

			mCalculatedBounds = false;
			mShouldMove = shouldMove;
			if (!mShouldMove) return;
			mPressed = pressed;

			if (pressed)
			{
				// Remove all momentum on press
				mMomentum = Vector3.zero;
				mScroll = 0f;

				// Disable the spring movement
				DisableSpring();

				// Remember the hit position
				mLastPos = UICamera.lastHit.point;

				// Create the plane to drag along
				mPlane = new Plane(mTrans.rotation * Vector3.back, mLastPos);

				// Ensure that we're working with whole numbers, keeping everything pixel-perfect
				Vector4 cr = mPanel.clipRange;
				cr.x = Mathf.Round(cr.x);
				cr.y = Mathf.Round(cr.y);
				cr.z = Mathf.Round(cr.z);
				cr.w = Mathf.Round(cr.w);
				mPanel.clipRange = cr;

				Vector3 v = mTrans.localPosition;
				v.x = Mathf.Round(v.x);
				v.y = Mathf.Round(v.y);
				mTrans.localPosition = v;
			}
			else
			{
				if (restrictWithinPanel && mPanel.clipping != UIDrawCall.Clipping.None && dragEffect == DragEffect.MomentumAndSpring)
					RestrictWithinBounds(false, canMoveHorizontally, canMoveVertically);

				if (!smoothDragStart || mDragStarted)
				{
					if (onDragFinished != null)
						onDragFinished();
				}
			}
		}
	}

	/// <summary>
	/// Drag the object along the plane.
	/// </summary>

	public void Drag ()
	{
		if (enabled && NGUITools.GetActive(gameObject) && mShouldMove)
		{
			if (mDragID == -10) mDragID = UICamera.currentTouchID;
			UICamera.currentTouch.clickNotification = UICamera.ClickNotification.BasedOnDelta;

			// Prevents the drag "jump". Contributed by 'mixd' from the Tasharen forums.
			if (smoothDragStart && !mDragStarted)
			{
				mDragStarted = true;
				mDragStartOffset = UICamera.currentTouch.totalDelta;
			}

			Ray ray = smoothDragStart ?
				UICamera.currentCamera.ScreenPointToRay(UICamera.currentTouch.pos - mDragStartOffset) :
				UICamera.currentCamera.ScreenPointToRay(UICamera.currentTouch.pos);

			float dist = 0f;

			if (mPlane.Raycast(ray, out dist))
			{
				Vector3 currentPos = ray.GetPoint(dist);
				Vector3 offset = currentPos - mLastPos;
				mLastPos = currentPos;

				if (offset.x != 0f || offset.y != 0f)
				{
					offset = mTrans.InverseTransformDirection(offset);

					if (movement == Movement.Horizontal)
					{
						offset.y = 0f;
						offset.z = 0f;
					}
					else if (movement == Movement.Vertical)
					{
						offset.x = 0f;
						offset.z = 0f;
					}
					else if (movement == Movement.Unrestricted)
					{
						offset.z = 0f;
					}
					else
					{
						offset.Scale((Vector3)customMovement);
					}
					offset = mTrans.TransformDirection(offset);
				}

				// Adjust the momentum
				mMomentum = Vector3.Lerp(mMomentum, mMomentum + offset * (0.01f * momentumAmount), 0.67f);

				// Move the scroll view
				if (!iOSDragEmulation)
				{
					MoveAbsolute(offset);	
				}
				else
				{
					Vector3 constraint = mPanel.CalculateConstrainOffset(bounds.min, bounds.max);

					if (constraint.magnitude > 0.001f)
					{
						MoveAbsolute(offset * 0.5f);
						mMomentum *= 0.5f;
					}
					else
					{
						MoveAbsolute(offset);
					}
				}

				// We want to constrain the UI to be within bounds
				if (restrictWithinPanel &&
					mPanel.clipping != UIDrawCall.Clipping.None &&
					dragEffect != DragEffect.MomentumAndSpring)
				{
					RestrictWithinBounds(true, canMoveHorizontally, canMoveVertically);
				}
			}
		}
	}

	/// <summary>
	/// If the object should support the scroll wheel, do it.
	/// </summary>

	public void Scroll (float delta)
	{
		if (enabled && NGUITools.GetActive(gameObject) && scrollWheelFactor != 0f)
		{
			DisableSpring();
			mShouldMove = shouldMove;
			if (Mathf.Sign(mScroll) != Mathf.Sign(delta)) mScroll = 0f;
			mScroll += delta * scrollWheelFactor;
		}
	}

	/// <summary>
	/// Apply the dragging momentum.
	/// </summary>

	void LateUpdate ()
	{
		if (!Application.isPlaying) return;
		float delta = RealTime.deltaTime;

		// Fade the scroll bars if needed
		if (showScrollBars != ShowCondition.Always)
		{
			bool vertical = false;
			bool horizontal = false;

			if (showScrollBars != ShowCondition.WhenDragging || mDragID != -10 || mMomentum.magnitude > 0.01f)
			{
				vertical = shouldMoveVertically;
				horizontal = shouldMoveHorizontally;
			}

			if (verticalScrollBar)
			{
				float alpha = verticalScrollBar.alpha;
				alpha += vertical ? delta * 6f : -delta * 3f;
				alpha = Mathf.Clamp01(alpha);
				if (verticalScrollBar.alpha != alpha) verticalScrollBar.alpha = alpha;
			}

			if (horizontalScrollBar)
			{
				float alpha = horizontalScrollBar.alpha;
				alpha += horizontal ? delta * 6f : -delta * 3f;
				alpha = Mathf.Clamp01(alpha);
				if (horizontalScrollBar.alpha != alpha) horizontalScrollBar.alpha = alpha;
			}
		}

		// Apply momentum
		if (mShouldMove && !mPressed)
		{
			if (movement == Movement.Horizontal || movement == Movement.Unrestricted)
			{
				mMomentum.x -= mScroll * 0.05f;
			}
			else if (movement == Movement.Vertical)
			{
				mMomentum.y -= mScroll * 0.05f;
			}
			else
			{
				mMomentum -= (Vector3)(customMovement * (mScroll * 0.05f));
			}

			if (mMomentum.magnitude > 0.0001f)
			{
				mScroll = NGUIMath.SpringLerp(mScroll, 0f, 20f, delta);

				// Move the scroll view
				Vector3 offset = NGUIMath.SpringDampen(ref mMomentum, 9f, delta);
				MoveAbsolute(offset);

				// Restrict the contents to be within the scroll view's bounds
				if (restrictWithinPanel && mPanel.clipping != UIDrawCall.Clipping.None)
					RestrictWithinBounds(false, canMoveHorizontally, canMoveVertically);
				
				if (mMomentum.magnitude < 0.0001f && onDragFinished != null) 
					onDragFinished();
				
				return;
			}
			else
			{
				mScroll = 0f;
				mMomentum = Vector3.zero;
			}
		}
		else mScroll = 0f;

		// Dampen the momentum
		NGUIMath.SpringDampen(ref mMomentum, 9f, delta);
	}

#if UNITY_EDITOR

	/// <summary>
	/// Draw a visible orange outline of the bounds.
	/// </summary>

	void OnDrawGizmos ()
	{
		if (mPanel != null)
		{
			Bounds b = bounds;
			Gizmos.matrix = transform.localToWorldMatrix;
			Gizmos.color = new Color(1f, 0.4f, 0f);
			Gizmos.DrawWireCube(new Vector3(b.center.x, b.center.y, b.min.z), new Vector3(b.size.x, b.size.y, 0f));
		}
	}
#endif
}
