//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Simple slider functionality.
/// </summary>

[AddComponentMenu("NGUI/Interaction/Slider")]
public class UISlider : UIWidgetContainer
{
	public enum Direction
	{
		Horizontal,
		Vertical,
	}

	/// <summary>
	/// Current slider. This value is set prior to the callback function being triggered.
	/// </summary>

	static public UISlider current;

	/// <summary>
	/// Object used for the foreground.
	/// </summary>

	public Transform foreground;

	/// <summary>
	/// Object that acts as a thumb.
	/// </summary>

	public Transform thumb;

	/// <summary>
	/// Direction the slider will expand in.
	/// </summary>

	public Direction direction = Direction.Horizontal;

	/// <summary>
	/// Number of steps the slider should be divided into. For example 5 means possible values of 0, 0.25, 0.5, 0.75, and 1.0.
	/// </summary>

	public int numberOfSteps = 0;

	/// <summary>
	/// Callbacks triggered when the scroll bar's value changes.
	/// </summary>

	public List<EventDelegate> onChange = new List<EventDelegate>();

	// Used to be public prior to 1.87
	[HideInInspector][SerializeField] float rawValue = 1f;
	
	// Deprecated functionality, kept for backwards compatibility
	[HideInInspector][SerializeField] GameObject eventReceiver;
	[HideInInspector][SerializeField] string functionName = "OnSliderChange";

	BoxCollider mCol;
	Transform mTrans;
	Transform mFGTrans;
	UIWidget mFGWidget;
	UISprite mFGFilled;
	bool mInitDone = false;
	Vector2 mSize = Vector2.zero;
	Vector2 mCenter = Vector3.zero;

	/// <summary>
	/// Value of the slider.
	/// </summary>

	public float value
	{
		get
		{
			float val = rawValue;
			if (numberOfSteps > 1) val = Mathf.Round(val * (numberOfSteps - 1)) / (numberOfSteps - 1);
			return val;
		}
		set
		{
			Set(value, false);
		}
	}

	[System.Obsolete("Use 'value' instead")]
	public float sliderValue { get { return this.value; } set { this.value = value; } }

	/// <summary>
	/// Change the full size of the slider, in case you need to.
	/// </summary>

	public Vector2 fullSize { get { return mSize; } set { if (mSize != value) { mSize = value; ForceUpdate(); } } }

	/// <summary>
	/// Initialize the cached values.
	/// </summary>

	void Init ()
	{
		mInitDone = true;

		if (foreground != null)
		{
			mFGWidget = foreground.GetComponent<UIWidget>();
			mFGFilled = (mFGWidget != null) ? mFGWidget as UISprite : null;
			mFGTrans = foreground.transform;

			if (mSize == Vector2.zero)
			{
				UIWidget w = foreground.GetComponent<UIWidget>();
				mSize = (w != null) ? new Vector2(w.width, w.height) : (Vector2)foreground.localScale;
			}

			if (mCenter == Vector2.zero)
			{
				UIWidget w = foreground.GetComponent<UIWidget>();

				if (w != null)
				{
					Vector3[] wc = w.localCorners;
					mCenter = Vector3.Lerp(wc[0], wc[2], 0.5f);
				}
				else mCenter = foreground.localPosition + foreground.localScale * 0.5f;
			}
		}
		else if (mCol != null)
		{
			if (mSize == Vector2.zero) mSize = mCol.size;
			if (mCenter == Vector2.zero) mCenter = mCol.center;
		}
		else
		{
			Debug.LogWarning("UISlider expected to find a foreground object or a box collider to work with", this);
		}
	}

	/// <summary>
	/// Ensure that we have a background and a foreground object to work with.
	/// </summary>

	void Awake ()
	{
		mTrans = transform;
		mCol = collider as BoxCollider;
	}

	/// <summary>
	/// We want to receive drag events from the thumb.
	/// </summary>

	void Start ()
	{
		Init();

		// Remove legacy functionality
		if (EventDelegate.IsValid(onChange))
		{
			eventReceiver = null;
			functionName = null;
		}

		if (Application.isPlaying && thumb != null && thumb.collider != null)
		{
			UIEventListener listener = UIEventListener.Get(thumb.gameObject);
			listener.onPress += OnPressThumb;
			listener.onDrag += OnDragThumb;
		}
		Set(rawValue, true);
	}

	/// <summary>
	/// Update the slider's position on press.
	/// </summary>

	void OnPress (bool pressed) { if (enabled && pressed && UICamera.currentTouchID != -100) UpdateDrag(); }

	/// <summary>
	/// When dragged, figure out where the mouse is and calculate the updated value of the slider.
	/// </summary>

	void OnDrag (Vector2 delta) { if (enabled) UpdateDrag(); }

	/// <summary>
	/// Callback from the thumb.
	/// </summary>

	void OnPressThumb (GameObject go, bool pressed) { if (enabled && pressed) UpdateDrag(); }

	/// <summary>
	/// Callback from the thumb.
	/// </summary>

	void OnDragThumb (GameObject go, Vector2 delta) { if (enabled) UpdateDrag(); }

	/// <summary>
	/// Watch for key events and adjust the value accordingly.
	/// </summary>

	void OnKey (KeyCode key)
	{
		if (enabled)
		{
			float step = (numberOfSteps > 1f) ? 1f / (numberOfSteps - 1) : 0.125f;

			if (direction == Direction.Horizontal)
			{
				if (key == KeyCode.LeftArrow) Set(rawValue - step, false);
				else if (key == KeyCode.RightArrow) Set(rawValue + step, false);
			}
			else
			{
				if (key == KeyCode.DownArrow) Set(rawValue - step, false);
				else if (key == KeyCode.UpArrow) Set(rawValue + step, false);
			}
		}
	}

	/// <summary>
	/// Update the slider's position based on the mouse.
	/// </summary>

	void UpdateDrag ()
	{
		// Create a plane for the slider
		if (mCol == null || UICamera.currentCamera == null || UICamera.currentTouch == null) return;

		// Don't consider the slider for click events
		UICamera.currentTouch.clickNotification = UICamera.ClickNotification.None;

		// Create a ray and a plane
		Ray ray = UICamera.currentCamera.ScreenPointToRay(UICamera.currentTouch.pos);
		Plane plane = new Plane(mTrans.rotation * Vector3.back, mTrans.position);

		// If the ray doesn't hit the plane, do nothing
		float dist;
		if (!plane.Raycast(ray, out dist)) return;

		// Collider's bottom-left corner in local space
		Vector3 localOrigin = mTrans.localPosition + (Vector3)(mCenter - mSize * 0.5f);
		Vector3 localOffset = mTrans.localPosition - localOrigin;

		// Direction to the point on the plane in scaled local space
		Vector3 localCursor = mTrans.InverseTransformPoint(ray.GetPoint(dist));
		Vector3 dir = localCursor + localOffset;

		// Update the slider
		Set((direction == Direction.Horizontal) ? dir.x / mSize.x : dir.y / mSize.y, false);
	}

	/// <summary>
	/// Update the visible slider.
	/// </summary>

	void Set (float input, bool force)
	{
		if (!mInitDone) Init();

		// Clamp the input
		float val = Mathf.Clamp01(input);
		if (val < 0.001f) val = 0f;

		float prevStep = value;

		// Save the raw value
		rawValue = val;

#if UNITY_EDITOR
		if (!Application.isPlaying) return;
#endif
		// Take steps into account
		float stepValue = value;

		// If the stepped value doesn't match the last one, it's time to update
		if (force || prevStep != stepValue)
		{
			Vector3 scale = mSize;

			if (direction == Direction.Horizontal) scale.x *= stepValue;
			else scale.y *= stepValue;
			
			if (mFGFilled != null && mFGFilled.type == UISprite.Type.Filled)
			{
				mFGFilled.fillAmount = stepValue;
			}
			else if (mFGWidget != null)
			{
				if (stepValue > 0.001f)
				{
					mFGWidget.width = Mathf.RoundToInt(scale.x);
					mFGWidget.height = Mathf.RoundToInt(scale.y);
					mFGWidget.enabled = true;
				}
				else
				{
					mFGWidget.enabled = false;
				}
			}
			else if (foreground != null)
			{
				mFGTrans.localScale = scale;
			}

			if (thumb != null)
			{
				Vector3 pos = thumb.localPosition;

				if (mFGFilled != null && mFGFilled.type == UISprite.Type.Filled)
				{
					if (mFGFilled.fillDirection == UISprite.FillDirection.Horizontal)
					{
						pos.x = mFGFilled.invert ? mSize.x - scale.x : scale.x;
					}
					else if (mFGFilled.fillDirection == UISprite.FillDirection.Vertical)
					{
						pos.y = mFGFilled.invert ? mSize.y - scale.y : scale.y;
					}
					else
					{
						Debug.LogWarning("Slider thumb is only supported with Horizontal or Vertical fill direction", this);
					}
				}
				else if (direction == Direction.Horizontal)
				{
					pos.x = scale.x;
				}
				else
				{
					pos.y = scale.y;
				}
				thumb.localPosition = pos;
			}

			current = this;

			if (EventDelegate.IsValid(onChange))
			{
				EventDelegate.Execute(onChange);
			}
			else if (eventReceiver != null && !string.IsNullOrEmpty(functionName))
			{
				// Legacy functionality support (for backwards compatibility)
				eventReceiver.SendMessage(functionName, stepValue, SendMessageOptions.DontRequireReceiver);
			}
			current = null;
		}
	}

	/// <summary>
	/// Force-update the slider. Useful if you've changed the properties and want it to update visually.
	/// </summary>

	public void ForceUpdate () { Set(rawValue, true); }
}
