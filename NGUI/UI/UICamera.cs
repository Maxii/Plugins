//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// This script should be attached to each camera that's used to draw the objects with
/// UI components on them. This may mean only one camera (main camera or your UI camera),
/// or multiple cameras if you happen to have multiple viewports. Failing to attach this
/// script simply means that objects drawn by this camera won't receive UI notifications:
/// 
/// * OnHover (isOver) is sent when the mouse hovers over a collider or moves away.
/// * OnPress (isDown) is sent when a mouse button gets pressed on the collider.
/// * OnSelect (selected) is sent when a mouse button is released on the same object as it was pressed on.
/// * OnClick () is sent with the same conditions as OnSelect, with the added check to see if the mouse has not moved much.
///   UICamera.currentTouchID tells you which button was clicked.
/// * OnDoubleClick () is sent when the click happens twice within a fourth of a second.
///   UICamera.currentTouchID tells you which button was clicked.
/// 
/// * OnDragStart () is sent to a game object under the touch just before the OnDrag() notifications begin.
/// * OnDrag (delta) is sent to an object that's being dragged.
/// * OnDragOver (draggedObject) is sent to a game object when another object is dragged over its area.
/// * OnDragOut (draggedObject) is sent to a game object when another object is dragged out of its area.
/// * OnDragEnd () is sent to a dragged object when the drag event finishes.
/// 
/// * OnInput (text) is sent when typing (after selecting a collider by clicking on it).
/// * OnTooltip (show) is sent when the mouse hovers over a collider for some time without moving.
/// * OnScroll (float delta) is sent out when the mouse scroll wheel is moved.
/// * OnKey (KeyCode key) is sent when keyboard or controller input is used.
/// </summary>

[ExecuteInEditMode]
[AddComponentMenu("NGUI/UI/NGUI Event System (UICamera)")]
[RequireComponent(typeof(Camera))]
public class UICamera : MonoBehaviour
{
	/// <summary>
	/// Whether the touch event will be sending out the OnClick notification at the end.
	/// </summary>

	public enum ClickNotification
	{
		None,
		Always,
		BasedOnDelta,
	}

	/// <summary>
	/// Ambiguous mouse, touch, or controller event.
	/// </summary>

	public class MouseOrTouch
	{
		public Vector2 pos;				// Current position of the mouse or touch event
		public Vector2 lastPos;			// Previous position of the mouse or touch event
		public Vector2 delta;			// Delta since last update
		public Vector2 totalDelta;		// Delta since the event started being tracked

		public Camera pressedCam;		// Camera that the OnPress(true) was fired with

		public GameObject last;			// Last object under the touch or mouse
		public GameObject current;		// Current game object under the touch or mouse
		public GameObject pressed;		// Last game object to receive OnPress
		public GameObject dragged;		// Game object that's being dragged

		public float clickTime = 0f;	// The last time a click event was sent out

		public ClickNotification clickNotification = ClickNotification.Always;
		public bool touchBegan = true;
		public bool pressStarted = false;
		public bool dragStarted = false;
	}

	/// <summary>
	/// Camera type controls how raycasts are handled by the UICamera.
	/// </summary>

	public enum EventType
	{
		World,	// Perform a Physics.Raycast and sort by distance to the point that was hit.
		UI,		// Perform a Physics.Raycast and sort by widget depth.
	}

	class Highlighted
	{
		public GameObject go;
		public int counter = 0;
	}

	/// <summary>
	/// List of all active cameras in the scene.
	/// </summary>
 
	static public List<UICamera> list = new List<UICamera>();

	public delegate void OnScreenResize ();

	/// <summary>
	/// Delegate triggered when the screen size changes for any reason.
	/// Subscribe to it if you don't want to compare Screen.width and Screen.height each frame.
	/// </summary>

	static public OnScreenResize onScreenResize;

	/// <summary>
	/// Event type -- use "UI" for your user interfaces, and "World" for your game camera.
	/// This setting changes how raycasts are handled. Raycasts have to be more complicated for UI cameras.
	/// </summary>

	public EventType eventType = EventType.UI;

	/// <summary>
	/// Which layers will receive events.
	/// </summary>

	public LayerMask eventReceiverMask = -1;

	/// <summary>
	/// If 'true', currently hovered object will be shown in the top left corner.
	/// </summary>

	public bool debug = false;

	/// <summary>
	/// Whether the mouse input is used.
	/// </summary>

	public bool useMouse = true;

	/// <summary>
	/// Whether the touch-based input is used.
	/// </summary>

	public bool useTouch = true;

	/// <summary>
	/// Whether multi-touch is allowed.
	/// </summary>

	public bool allowMultiTouch = true;

	/// <summary>
	/// Whether the keyboard events will be processed.
	/// </summary>

	public bool useKeyboard = true;

	/// <summary>
	/// Whether the joystick and controller events will be processed.
	/// </summary>

	public bool useController = true;

	[System.Obsolete("Use new OnDragStart / OnDragOver / OnDragOut / OnDragEnd events instead")]
	public bool stickyPress { get { return true; } }

	/// <summary>
	/// Whether the tooltip will disappear as soon as the mouse moves (false) or only if the mouse moves outside of the widget's area (true).
	/// </summary>

	public bool stickyTooltip = true;

	/// <summary>
	/// How long of a delay to expect before showing the tooltip.
	/// </summary>

	public float tooltipDelay = 1f;

	/// <summary>
	/// How much the mouse has to be moved after pressing a button before it starts to send out drag events.
	/// </summary>

	public float mouseDragThreshold = 4f;

	/// <summary>
	/// How far the mouse is allowed to move in pixels before it's no longer considered for click events, if the click notification is based on delta.
	/// </summary>

	public float mouseClickThreshold = 10f;

	/// <summary>
	/// How much the mouse has to be moved after pressing a button before it starts to send out drag events.
	/// </summary>

	public float touchDragThreshold = 40f;

	/// <summary>
	/// How far the touch is allowed to move in pixels before it's no longer considered for click events, if the click notification is based on delta.
	/// </summary>

	public float touchClickThreshold = 40f;

	/// <summary>
	/// Raycast range distance. By default it's as far as the camera can see.
	/// </summary>

	public float rangeDistance = -1f;

	/// <summary>
	/// Name of the axis used for scrolling.
	/// </summary>

	public string scrollAxisName = "Mouse ScrollWheel";

	/// <summary>
	/// Name of the axis used to send up and down key events.
	/// </summary>

	public string verticalAxisName = "Vertical";

	/// <summary>
	/// Name of the axis used to send left and right key events.
	/// </summary>

	public string horizontalAxisName = "Horizontal";

	/// <summary>
	/// Various keys used by the camera.
	/// </summary>

	public KeyCode submitKey0 = KeyCode.Return;
	public KeyCode submitKey1 = KeyCode.JoystickButton0;
	public KeyCode cancelKey0 = KeyCode.Escape;
	public KeyCode cancelKey1 = KeyCode.JoystickButton1;

	public delegate void OnCustomInput ();

	/// <summary>
	/// Custom input processing logic, if desired. For example: WP7 touches.
	/// Use UICamera.current to get the current camera.
	/// </summary>

	static public OnCustomInput onCustomInput;

	/// <summary>
	/// Whether tooltips will be shown or not.
	/// </summary>

	static public bool showTooltips = true;

	/// <summary>
	/// Position of the last touch (or mouse) event.
	/// </summary>

	static public Vector2 lastTouchPosition = Vector2.zero;

	/// <summary>
	/// Last raycast hit prior to sending out the event. This is useful if you want detailed information
	/// about what was actually hit in your OnClick, OnHover, and other event functions.
	/// </summary>

	static public RaycastHit lastHit;

	/// <summary>
	/// UICamera that sent out the event.
	/// </summary>

	static public UICamera current = null;

	/// <summary>
	/// Last camera active prior to sending out the event. This will always be the camera that actually sent out the event.
	/// </summary>

	static public Camera currentCamera = null;

	/// <summary>
	/// ID of the touch or mouse operation prior to sending out the event. Mouse ID is '-1' for left, '-2' for right mouse button, '-3' for middle.
	/// </summary>

	static public int currentTouchID = -1;

	/// <summary>
	/// Key that triggered the event, if any.
	/// </summary>

	static public KeyCode currentKey = KeyCode.None;

	/// <summary>
	/// Current touch, set before any event function gets called.
	/// </summary>

	static public MouseOrTouch currentTouch = null;

	/// <summary>
	/// Whether an input field currently has focus.
	/// </summary>

	static public bool inputHasFocus = false;

	/// <summary>
	/// If set, this game object will receive all events regardless of whether they were handled or not.
	/// </summary>

	static public GameObject genericEventHandler;

	/// <summary>
	/// If events don't get handled, they will be forwarded to this game object.
	/// </summary>

	static public GameObject fallThrough;

	// List of currently highlighted items
	static List<Highlighted> mHighlighted = new List<Highlighted>();

	// Selected widget (for input)
	static GameObject mCurrentSelection = null;
	static GameObject mNextSelection = null;

	// Mouse events
	static MouseOrTouch[] mMouse = new MouseOrTouch[] { new MouseOrTouch(), new MouseOrTouch(), new MouseOrTouch() };

	// The last object to receive OnHover
	static GameObject mHover;

	// Joystick/controller/keyboard event
	static MouseOrTouch mController = new MouseOrTouch();

	// Used to ensure that joystick-based controls don't trigger that often
	static float mNextEvent = 0f;

	// List of currently active touches
	static Dictionary<int, MouseOrTouch> mTouches = new Dictionary<int, MouseOrTouch>();

	// Used to detect screen dimension changes
	static int mWidth = 0;
	static int mHeight = 0;

	// Tooltip widget (mouse only)
	GameObject mTooltip = null;

	// Mouse input is turned off on iOS
	Camera mCam = null;
	LayerMask mLayerMask;
	float mTooltipTime = 0f;

	/// <summary>
	/// Helper function that determines if this script should be handling the events.
	/// </summary>

	bool handlesEvents { get { return eventHandler == this; } }

	/// <summary>
	/// Caching is always preferable for performance.
	/// </summary>

	public Camera cachedCamera { get { if (mCam == null) mCam = camera; return mCam; } }

	/// <summary>
	/// Set to 'true' just before OnDrag-related events are sent. No longer needed, but kept for backwards compatibility.
	/// </summary>

	static public bool isDragging = false;

	/// <summary>
	/// The object hit by the last Raycast that was the result of a mouse or touch event.
	/// </summary>

	static public GameObject hoveredObject;

	/// <summary>
	/// Option to manually set the selected game object.
	/// </summary>

	static public GameObject selectedObject
	{
		get
		{
			return mCurrentSelection;
		}
		set
		{
			if (mNextSelection != null)
			{
				mNextSelection = value;
			}
			else if (mCurrentSelection != value)
			{
				if (mCurrentSelection != null)
				{
					UICamera uicam = FindCameraForLayer(mCurrentSelection.layer);

					if (uicam != null)
					{
						current = uicam;
						currentCamera = uicam.mCam;
						Notify(mCurrentSelection, "OnSelect", false);
						if (uicam.useController || uicam.useKeyboard) Highlight(mCurrentSelection, false);
						current = null;
					}
				}

				mCurrentSelection = null;
				mNextSelection = value;

				if (mNextSelection != null)
				{
					UICamera uicam = FindCameraForLayer(mNextSelection.layer);
					if (uicam != null) uicam.StartCoroutine(uicam.ChangeSelection(mNextSelection));
				}
			}
		}
	}

	/// <summary>
	/// Selection change is delayed on purpose. This way selection changes during event processing won't cause
	/// the newly selected widget to continue processing when it is it's turn. Example: pressing 'tab' on one
	/// button selects the next button, and then it also processes its 'tab' in turn, selecting the next one.
	/// </summary>

	System.Collections.IEnumerator ChangeSelection (GameObject go)
	{
		yield return new WaitForEndOfFrame();
		mCurrentSelection = go;
		mNextSelection = null;

		if (mCurrentSelection != null)
		{
			current = this;
			currentCamera = mCam;
			if (useController || useKeyboard) Highlight(mCurrentSelection, true);
			Notify(mCurrentSelection, "OnSelect", true);
			current = null;
		}
	}

	/// <summary>
	/// Number of active touches from all sources.
	/// </summary>

	static public int touchCount
	{
		get
		{
			int count = 0;

			foreach (KeyValuePair<int, MouseOrTouch> touch in mTouches)
				if (touch.Value.pressed != null)
					++count;

			for (int i = 0; i < mMouse.Length; ++i)
				if (mMouse[i].pressed != null)
					++count;

			if (mController.pressed != null)
				++count;

			return count;
		}
	}

	/// <summary>
	/// Number of active drag events from all sources.
	/// </summary>

	static public int dragCount
	{
		get
		{
			int count = 0;

			foreach (KeyValuePair<int, MouseOrTouch> touch in mTouches)
				if (touch.Value.dragged != null)
					++count;

			for (int i = 0; i < mMouse.Length; ++i)
				if (mMouse[i].dragged != null)
					++count;

			if (mController.dragged != null)
				++count;

			return count;
		}
	}

	/// <summary>
	/// Clear the list on application quit (also when Play mode is exited)
	/// </summary>

	void OnApplicationQuit () { mHighlighted.Clear(); }

	/// <summary>
	/// Convenience function that returns the main HUD camera.
	/// </summary>

	static public Camera mainCamera
	{
		get
		{
			UICamera mouse = eventHandler;
			return (mouse != null) ? mouse.cachedCamera : null;
		}
	}

	/// <summary>
	/// Event handler for all types of events.
	/// </summary>

	static public UICamera eventHandler
	{
		get
		{
			for (int i = 0; i < list.Count; ++i)
			{
				// Invalid or inactive entry -- keep going
				UICamera cam = list[i];
				if (cam == null || !cam.enabled || !NGUITools.GetActive(cam.gameObject)) continue;
				return cam;
			}
			return null;
		}
	}

	/// <summary>
	/// Static comparison function used for sorting.
	/// </summary>

	static int CompareFunc (UICamera a, UICamera b)
	{
		if (a.cachedCamera.depth < b.cachedCamera.depth) return 1;
		if (a.cachedCamera.depth > b.cachedCamera.depth) return -1;
		return 0;
	}

	struct DepthEntry
	{
		public int depth;
		public RaycastHit hit;
	}

	static DepthEntry mHit = new DepthEntry();
	static BetterList<DepthEntry> mHits = new BetterList<DepthEntry>();
	static RaycastHit mEmpty = new RaycastHit();

	/// <summary>
	/// Returns the object under the specified position.
	/// </summary>

	static public bool Raycast (Vector3 inPos, out RaycastHit hit)
	{
		for (int i = 0; i < list.Count; ++i)
		{
			UICamera cam = list[i];
			
			// Skip inactive scripts
			if (!cam.enabled || !NGUITools.GetActive(cam.gameObject)) continue;

			// Convert to view space
			currentCamera = cam.cachedCamera;
			Vector3 pos = currentCamera.ScreenToViewportPoint(inPos);
			if (float.IsNaN(pos.x) || float.IsNaN(pos.y)) continue;

			// If it's outside the camera's viewport, do nothing
			if (pos.x < 0f || pos.x > 1f || pos.y < 0f || pos.y > 1f) continue;

			// Cast a ray into the screen
			Ray ray = currentCamera.ScreenPointToRay(inPos);

			// Raycast into the screen
			int mask = currentCamera.cullingMask & (int)cam.eventReceiverMask;
			float dist = (cam.rangeDistance > 0f) ? cam.rangeDistance : currentCamera.farClipPlane - currentCamera.nearClipPlane;

			if (cam.eventType == EventType.World)
			{
				if (Physics.Raycast(ray, out hit, dist, mask))
				{
					hoveredObject = hit.collider.gameObject;
					return true;
				}
				continue;
			}
			else if (cam.eventType == EventType.UI)
			{
				RaycastHit[] hits = Physics.RaycastAll(ray, dist, mask);

				if (hits.Length > 1)
				{
					for (int b = 0; b < hits.Length; ++b)
					{
						GameObject go = hits[b].collider.gameObject;
						mHit.depth = NGUITools.CalculateRaycastDepth(go);

						if (mHit.depth != int.MaxValue)
						{
							mHit.hit = hits[b];
							mHits.Add(mHit);
						}
					}

					mHits.Sort(delegate(DepthEntry r1, DepthEntry r2) { return r2.depth.CompareTo(r1.depth); });

					for (int b = 0; b < mHits.size; ++b)
					{
#if UNITY_FLASH
						if (IsVisible(mHits.buffer[b]))
#else
						if (IsVisible(ref mHits.buffer[b]))
#endif
						{
							hit = mHits[b].hit;
							hoveredObject = hit.collider.gameObject;
							mHits.Clear();
							return true;
						}
					}
					mHits.Clear();
				}
				else if (hits.Length == 1 && IsVisible(ref hits[0]))
				{
					hit = hits[0];
					hoveredObject = hit.collider.gameObject;
					return true;
				}
				continue;
			}
		}
		hit = mEmpty;
		return false;
	}

	/// <summary>
	/// Helper function to check if the specified hit is visible by the panel.
	/// </summary>

	static bool IsVisible (ref RaycastHit hit)
	{
		UIPanel panel = NGUITools.FindInParents<UIPanel>(hit.collider.gameObject);

		if (panel == null || panel.IsVisible(hit.point))
		{
			return true;
		}
		return false;
	}

	/// <summary>
	/// Helper function to check if the specified hit is visible by the panel.
	/// </summary>

#if UNITY_FLASH
	static bool IsVisible (DepthEntry de)
#else
	static bool IsVisible (ref DepthEntry de)
#endif
	{
		UIPanel panel = NGUITools.FindInParents<UIPanel>(de.hit.collider.gameObject);
		return (panel == null || panel.IsVisible(de.hit.point));
	}

	/// <summary>
	/// Find the camera responsible for handling events on objects of the specified layer.
	/// </summary>

	static public UICamera FindCameraForLayer (int layer)
	{
		int layerMask = 1 << layer;

		for (int i = 0; i < list.Count; ++i)
		{
			UICamera cam = list[i];
			Camera uc = cam.cachedCamera;
			if ((uc != null) && (uc.cullingMask & layerMask) != 0) return cam;
		}
		return null;
	}

	/// <summary>
	/// Using the keyboard will result in 1 or -1, depending on whether up or down keys have been pressed.
	/// </summary>

	static int GetDirection (KeyCode up, KeyCode down)
	{
		if (Input.GetKeyDown(up)) return 1;
		if (Input.GetKeyDown(down)) return -1;
		return 0;
	}

	/// <summary>
	/// Using the keyboard will result in 1 or -1, depending on whether up or down keys have been pressed.
	/// </summary>

	static int GetDirection (KeyCode up0, KeyCode up1, KeyCode down0, KeyCode down1)
	{
		if (Input.GetKeyDown(up0) || Input.GetKeyDown(up1)) return 1;
		if (Input.GetKeyDown(down0) || Input.GetKeyDown(down1)) return -1;
		return 0;
	}

	/// <summary>
	/// Using the joystick to move the UI results in 1 or -1 if the threshold has been passed, mimicking up/down keys.
	/// </summary>

	static int GetDirection (string axis)
	{
		float time = RealTime.time;

		if (mNextEvent < time)
		{
			float val = Input.GetAxis(axis);

			if (val > 0.75f)
			{
				mNextEvent = time + 0.25f;
				return 1;
			}

			if (val < -0.75f)
			{
				mNextEvent = time + 0.25f;
				return -1;
			}
		}
		return 0;
	}

	/// <summary>
	/// Returns whether the widget should be currently highlighted as far as the UICamera knows.
	/// </summary>

	static public bool IsHighlighted (GameObject go)
	{
		for (int i = mHighlighted.Count; i > 0; )
		{
			Highlighted hl = mHighlighted[--i];
			if (hl.go == go) return true;
		}
		return false;
	}

	/// <summary>
	/// Apply or remove highlighted (hovered) state from the specified object.
	/// </summary>

	static void Highlight (GameObject go, bool highlighted)
	{
		if (go != null)
		{
			for (int i = mHighlighted.Count; i > 0; )
			{
				Highlighted hl = mHighlighted[--i];

				if (hl == null || hl.go == null)
				{
					mHighlighted.RemoveAt(i);
				}
				else if (hl.go == go)
				{
					if (highlighted)
					{
						++hl.counter;
					}
					else if (--hl.counter < 1)
					{
						mHighlighted.Remove(hl);
						Notify(go, "OnHover", false);
					}
					return;
				}
			}

			if (highlighted)
			{
				Highlighted hl = new Highlighted();
				hl.go = go;
				hl.counter = 1;
				mHighlighted.Add(hl);
				Notify(go, "OnHover", true);
			}
		}
	}

	/// <summary>
	/// Generic notification function. Used in place of SendMessage to shorten the code and allow for more than one receiver.
	/// </summary>

	static public void Notify (GameObject go, string funcName, object obj)
	{
		if (go != null)
		{
			go.SendMessage(funcName, obj, SendMessageOptions.DontRequireReceiver);

			if (genericEventHandler != null && genericEventHandler != go)
			{
				genericEventHandler.SendMessage(funcName, obj, SendMessageOptions.DontRequireReceiver);
			}
		}
	}

	/// <summary>
	/// Get the details of the specified mouse button.
	/// </summary>

	static public MouseOrTouch GetMouse (int button) { return mMouse[button]; }

	/// <summary>
	/// Get or create a touch event.
	/// </summary>

	static public MouseOrTouch GetTouch (int id)
	{
		MouseOrTouch touch = null;

		if (id < 0) return GetMouse(-id - 1);

		if (!mTouches.TryGetValue(id, out touch))
		{
			touch = new MouseOrTouch();
			touch.touchBegan = true;
			mTouches.Add(id, touch);
		}
		return touch;
	}

	/// <summary>
	/// Remove a touch event from the list.
	/// </summary>

	static public void RemoveTouch (int id) { mTouches.Remove(id); }

	/// <summary>
	/// Add this camera to the list.
	/// </summary>

	void Awake ()
	{
		mWidth = Screen.width;
		mHeight = Screen.height;

		if (Application.platform == RuntimePlatform.Android ||
			Application.platform == RuntimePlatform.IPhonePlayer
#if !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1
			|| Application.platform == RuntimePlatform.WP8Player
#if UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3
			|| Application.platform == RuntimePlatform.BB10Player
#else
			|| Application.platform == RuntimePlatform.BlackBerryPlayer
#endif
#endif
			)
		{
			useMouse = false;
			useTouch = true;

			if (Application.platform == RuntimePlatform.IPhonePlayer)
			{
				useKeyboard = false;
				useController = false;
			}
		}
		else if (Application.platform == RuntimePlatform.PS3 ||
				 Application.platform == RuntimePlatform.XBOX360)
		{
			useMouse = false;
			useTouch = false;
			useKeyboard = false;
			useController = true;
		}

		// Save the starting mouse position
		mMouse[0].pos.x = Input.mousePosition.x;
		mMouse[0].pos.y = Input.mousePosition.y;

		for (int i = 1; i < 3; ++i)
		{
			mMouse[i].pos = mMouse[0].pos;
			mMouse[i].lastPos = mMouse[0].pos;
		}
		lastTouchPosition = mMouse[0].pos;
	}

	/// <summary>
	/// Sort the list when enabled.
	/// </summary>

	void OnEnable () { list.Add(this); list.Sort(CompareFunc); }

	/// <summary>
	/// Remove this camera from the list.
	/// </summary>

	void OnDisable () { list.Remove(this); }

#if !UNITY_3_5 && !UNITY_4_0
	/// <summary>
	/// We don't want the camera to send out any kind of mouse events.
	/// </summary>
	
	void Start ()
	{
		cachedCamera.eventMask = 0;
		cachedCamera.transparencySortMode = TransparencySortMode.Orthographic;
		if (debug) NGUIDebug.debugRaycast = true;
	}
#endif

#if UNITY_EDITOR
	void OnValidate () { NGUIDebug.debugRaycast = debug; }
#endif

	/// <summary>
	/// Update the object under the mouse if we're not using touch-based input.
	/// </summary>

	void FixedUpdate ()
	{
		if (useMouse && Application.isPlaying && handlesEvents)
		{
			if (!Raycast(Input.mousePosition, out lastHit)) hoveredObject = fallThrough;
			if (hoveredObject == null) hoveredObject = genericEventHandler;
			for (int i = 0; i < 3; ++i) mMouse[i].current = hoveredObject;
		}
	}

	/// <summary>
	/// Check the input and send out appropriate events.
	/// </summary>

	void Update ()
	{
		// Only the first UI layer should be processing events
		if (!Application.isPlaying || !handlesEvents) return;

		current = this;

		int w = Screen.width;
		int h = Screen.height;

		if (w != mWidth || h != mHeight)
		{
			mWidth = w;
			mHeight = h;
			if (onScreenResize != null)
				onScreenResize();
		}

		if (useTouch)
		{
			// Process touch events first
			ProcessTouches ();

			// If we want to process mouse events, only do so if there are no active touch events,
			// otherwise there is going to be event duplication as Unity treats touch events as mouse events.
			if (useMouse && Input.touchCount == 0)
				ProcessMouse();
		}
		else if (useMouse) ProcessMouse();

		// Custom input processing
		if (onCustomInput != null) onCustomInput();

		// Clear the selection on the cancel key, but only if mouse input is allowed
		if (useMouse && mCurrentSelection != null)
		{
			if (cancelKey0 != KeyCode.None && Input.GetKeyDown(cancelKey0))
			{
				currentKey = cancelKey0;
				selectedObject = null;
			}
			else if (cancelKey1 != KeyCode.None && Input.GetKeyDown(cancelKey1))
			{
				currentKey = cancelKey1;
				selectedObject = null;
			}
		}

		// Forward the input to the selected object
		if (mCurrentSelection != null)
		{
			string input = Input.inputString;

			// Adding support for some macs only having the "Delete" key instead of "Backspace"
			if (useKeyboard && Input.GetKeyDown(KeyCode.Delete)) input += "\b";

			if (input.Length > 0)
			{
				if (!stickyTooltip && mTooltip != null) ShowTooltip(false);
				Notify(mCurrentSelection, "OnInput", input);
			}
		}
		else inputHasFocus = false;

		// Update the keyboard and joystick events
		if (mCurrentSelection != null) ProcessOthers();

		// If it's time to show a tooltip, inform the object we're hovering over
		if (useMouse && mHover != null)
		{
			float scroll = Input.GetAxis(scrollAxisName);
			if (scroll != 0f) Notify(mHover, "OnScroll", scroll);

			if (showTooltips && mTooltipTime != 0f && (mTooltipTime < RealTime.time ||
				Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
			{
				mTooltip = mHover;
				ShowTooltip(true);
			}
		}
		current = null;
	}

	/// <summary>
	/// Update mouse input.
	/// </summary>

	public void ProcessMouse ()
	{
		lastTouchPosition = Input.mousePosition;
		bool highlightChanged = (mMouse[0].last != mMouse[0].current);

		// Update the position and delta
		mMouse[0].delta = lastTouchPosition - mMouse[0].pos;
		mMouse[0].pos = lastTouchPosition;
		bool posChanged = mMouse[0].delta.sqrMagnitude > 0.001f;

		// Propagate the updates to the other mouse buttons
		for (int i = 1; i < 3; ++i)
		{
			mMouse[i].pos = mMouse[0].pos;
			mMouse[i].delta = mMouse[0].delta;
		}

		// Is any button currently pressed?
		bool isPressed = false;

		for (int i = 0; i < 3; ++i)
		{
			if (Input.GetMouseButton(i))
			{
				isPressed = true;
				break;
			}
		}

		if (isPressed)
		{
			// A button was pressed -- cancel the tooltip
			mTooltipTime = 0f;
		}
		else if (useMouse && posChanged && (!stickyTooltip || highlightChanged))
		{
			if (mTooltipTime != 0f)
			{
				// Delay the tooltip
				mTooltipTime = RealTime.time + tooltipDelay;
			}
			else if (mTooltip != null)
			{
				// Hide the tooltip
				ShowTooltip(false);
			}
		}

		// The button was released over a different object -- remove the highlight from the previous
		if (useMouse && !isPressed && mHover != null && highlightChanged)
		{
			if (mTooltip != null) ShowTooltip(false);
			Highlight(mHover, false);
			mHover = null;
		}

		// Process all 3 mouse buttons as individual touches
		if (useMouse)
		{
			for (int i = 0; i < 3; ++i)
			{
				bool pressed = Input.GetMouseButtonDown(i);
				bool unpressed = Input.GetMouseButtonUp(i);
	
				currentTouch = mMouse[i];
				currentTouchID = -1 - i;
				currentKey = KeyCode.Mouse0 + i;
	
				// We don't want to update the last camera while there is a touch happening
				if (pressed) currentTouch.pressedCam = currentCamera;
				else if (currentTouch.pressed != null) currentCamera = currentTouch.pressedCam;
	
				// Process the mouse events
				ProcessTouch(pressed, unpressed);
				currentKey = KeyCode.None;
			}
			currentTouch = null;
		}

		// If nothing is pressed and there is an object under the touch, highlight it
		if (useMouse && !isPressed && highlightChanged)
		{
			mTooltipTime = RealTime.time + tooltipDelay;
			mHover = mMouse[0].current;
			Highlight(mHover, true);
		}

		// Update the last value
		mMouse[0].last = mMouse[0].current;
		for (int i = 1; i < 3; ++i) mMouse[i].last = mMouse[0].last;
	}

	/// <summary>
	/// Update touch-based events.
	/// </summary>

	public void ProcessTouches ()
	{
		for (int i = 0; i < Input.touchCount; ++i)
		{
			Touch input = Input.GetTouch(i);

			currentTouchID = allowMultiTouch ? input.fingerId : 1;
			currentTouch = GetTouch(currentTouchID);

			bool pressed = (input.phase == TouchPhase.Began) || currentTouch.touchBegan;
			bool unpressed = (input.phase == TouchPhase.Canceled) || (input.phase == TouchPhase.Ended);
			currentTouch.touchBegan = false;

			// Although input.deltaPosition can be used, calculating it manually is safer (just in case)
			currentTouch.delta = pressed ? Vector2.zero : input.position - currentTouch.pos;
			currentTouch.pos = input.position;

			// Raycast into the screen
			if (!Raycast(currentTouch.pos, out lastHit)) hoveredObject = fallThrough;
			if (hoveredObject == null) hoveredObject = genericEventHandler;
			currentTouch.last = currentTouch.current;
			currentTouch.current = hoveredObject;
			lastTouchPosition = currentTouch.pos;

			// We don't want to update the last camera while there is a touch happening
			if (pressed) currentTouch.pressedCam = currentCamera;
			else if (currentTouch.pressed != null) currentCamera = currentTouch.pressedCam;

			// Double-tap support
			if (input.tapCount > 1) currentTouch.clickTime = RealTime.time;

			// Process the events from this touch
			ProcessTouch(pressed, unpressed);

			// If the touch has ended, remove it from the list
			if (unpressed) RemoveTouch(currentTouchID);
			currentTouch.last = null;
			currentTouch = null;

			// Don't consider other touches
			if (!allowMultiTouch) break;
		}
	}

	/// <summary>
	/// Process keyboard and joystick events.
	/// </summary>

	public void ProcessOthers ()
	{
		currentTouchID = -100;
		currentTouch = mController;

		// If this is an input field, ignore WASD and Space key presses
		inputHasFocus = (mCurrentSelection != null && mCurrentSelection.GetComponent<UIInput>() != null);

		bool submitKeyDown = false;
		bool submitKeyUp = false;

		if (submitKey0 != KeyCode.None && Input.GetKeyDown(submitKey0))
		{
			currentKey = submitKey0;
			submitKeyDown = true;
		}

		if (submitKey1 != KeyCode.None && Input.GetKeyDown(submitKey1))
		{
			currentKey = submitKey1;
			submitKeyDown = true;
		}

		if (submitKey0 != KeyCode.None && Input.GetKeyUp(submitKey0))
		{
			currentKey = submitKey0;
			submitKeyUp = true;
		}

		if (submitKey1 != KeyCode.None && Input.GetKeyUp(submitKey1))
		{
			currentKey = submitKey1;
			submitKeyUp = true;
		}

		if (submitKeyDown || submitKeyUp)
		{
			currentTouch.last = currentTouch.current;
			currentTouch.current = mCurrentSelection;
			ProcessTouch(submitKeyDown, submitKeyUp);
			currentTouch.last = null;
		}

		int vertical = 0;
		int horizontal = 0;

		if (useKeyboard)
		{
			if (inputHasFocus)
			{
				vertical += GetDirection(KeyCode.UpArrow, KeyCode.DownArrow);
				horizontal += GetDirection(KeyCode.RightArrow, KeyCode.LeftArrow);
			}
			else
			{
				vertical += GetDirection(KeyCode.W, KeyCode.UpArrow, KeyCode.S, KeyCode.DownArrow);
				horizontal += GetDirection(KeyCode.D, KeyCode.RightArrow, KeyCode.A, KeyCode.LeftArrow);
			}
		}

		if (useController)
		{
			if (!string.IsNullOrEmpty(verticalAxisName)) vertical += GetDirection(verticalAxisName);
			if (!string.IsNullOrEmpty(horizontalAxisName)) horizontal += GetDirection(horizontalAxisName);
		}

		// Send out key notifications
		if (vertical != 0) Notify(mCurrentSelection, "OnKey", vertical > 0 ? KeyCode.UpArrow : KeyCode.DownArrow);
		if (horizontal != 0) Notify(mCurrentSelection, "OnKey", horizontal > 0 ? KeyCode.RightArrow : KeyCode.LeftArrow);
		
		if (useKeyboard && Input.GetKeyDown(KeyCode.Tab))
		{
			currentKey = KeyCode.Tab;
			Notify(mCurrentSelection, "OnKey", KeyCode.Tab);
		}

		// Send out the cancel key notification
		if (cancelKey0 != KeyCode.None && Input.GetKeyDown(cancelKey0))
		{
			currentKey = cancelKey0;
			Notify(mCurrentSelection, "OnKey", KeyCode.Escape);
		}

		if (cancelKey1 != KeyCode.None && Input.GetKeyDown(cancelKey1))
		{
			currentKey = cancelKey1;
			Notify(mCurrentSelection, "OnKey", KeyCode.Escape);
		}

		currentTouch = null;
		currentKey = KeyCode.None;
	}

	/// <summary>
	/// Process the events of the specified touch.
	/// </summary>

	public void ProcessTouch (bool pressed, bool unpressed)
	{
		// Whether we're using the mouse
		bool isMouse = (currentTouch == mMouse[0] || currentTouch == mMouse[1] || currentTouch == mMouse[2]);
		float drag   = isMouse ? mouseDragThreshold : touchDragThreshold;
		float click  = isMouse ? mouseClickThreshold : touchClickThreshold;

		// Send out the press message
		if (pressed)
		{
			if (mTooltip != null) ShowTooltip(false);

			currentTouch.pressStarted = true;
			Notify(currentTouch.pressed, "OnPress", false);
			currentTouch.pressed = currentTouch.current;
			currentTouch.dragged = currentTouch.current;
			currentTouch.clickNotification = ClickNotification.BasedOnDelta;
			currentTouch.totalDelta = Vector2.zero;
			currentTouch.dragStarted = false;
			Notify(currentTouch.pressed, "OnPress", true);

			// Clear the selection
			if (currentTouch.pressed != mCurrentSelection)
			{
				if (mTooltip != null) ShowTooltip(false);
				selectedObject = null;
			}
		}
		else if (currentTouch.pressed != null && (currentTouch.delta.magnitude != 0f || currentTouch.current != currentTouch.last))
		{
			// Keep track of the total movement
			currentTouch.totalDelta += currentTouch.delta;
			float mag = currentTouch.totalDelta.magnitude;
			bool justStarted = false;

			// If the drag event has not yet started, see if we've dragged the touch far enough to start it
			if (!currentTouch.dragStarted && drag < mag)
			{
				justStarted = true;
				currentTouch.dragStarted = true;
				currentTouch.delta = currentTouch.totalDelta;
			}

			// If we're dragging the touch, send out drag events
			if (currentTouch.dragStarted)
			{
				if (mTooltip != null) ShowTooltip(false);

				isDragging = true;
				bool isDisabled = (currentTouch.clickNotification == ClickNotification.None);

				if (justStarted)
				{
					Notify(currentTouch.dragged, "OnDragStart", null);
					Notify(currentTouch.current, "OnDragOver", currentTouch.dragged);
				}
				else if (currentTouch.last != currentTouch.current)
				{
					Notify(currentTouch.last, "OnDragOut", currentTouch.dragged);
					Notify(currentTouch.current, "OnDragOver", currentTouch.dragged);
				}

				Notify(currentTouch.dragged, "OnDrag", currentTouch.delta);

				currentTouch.last = currentTouch.current;
				isDragging = false;

				if (isDisabled)
				{
					// If the notification status has already been disabled, keep it as such
					currentTouch.clickNotification = ClickNotification.None;
				}
				else if (currentTouch.clickNotification == ClickNotification.BasedOnDelta && click < mag)
				{
					// We've dragged far enough to cancel the click
					currentTouch.clickNotification = ClickNotification.None;
				}
			}
		}

		// Send out the unpress message
		if (unpressed)
		{
			currentTouch.pressStarted = false;
			if (mTooltip != null) ShowTooltip(false);

			if (currentTouch.pressed != null)
			{
				// If there was a drag event in progress, make sure OnDragOut gets sent
				if (currentTouch.dragStarted)
				{
					Notify(currentTouch.last, "OnDragOut", currentTouch.dragged);
					Notify(currentTouch.dragged, "OnDragEnd", null);
				}

				// Send the notification of a touch ending
				Notify(currentTouch.pressed, "OnPress", false);

				// Send a hover message to the object, but don't add it to the list of hovered items
				// as it's already present. This happens when the mouse is released over the same button
				// it was pressed on, and since it already had its 'OnHover' event, it never got
				// Highlight(false), so we simply re-notify it so it can update the visible state.
				if (useMouse && currentTouch.pressed == mHover) Notify(currentTouch.pressed, "OnHover", true);

				// If the button/touch was released on the same object, consider it a click and select it
				if (currentTouch.dragged == currentTouch.current ||
					(currentTouch.clickNotification != ClickNotification.None &&
					currentTouch.totalDelta.magnitude < drag))
				{
					if (currentTouch.pressed != mCurrentSelection)
					{
						mNextSelection = null;
						mCurrentSelection = currentTouch.pressed;
						Notify(currentTouch.pressed, "OnSelect", true);
					}
					else
					{
						mNextSelection = null;
						mCurrentSelection = currentTouch.pressed;
					}

					// If the touch should consider clicks, send out an OnClick notification
					if (currentTouch.clickNotification != ClickNotification.None)
					{
						float time = RealTime.time;

						Notify(currentTouch.pressed, "OnClick", null);

						if (currentTouch.clickTime + 0.35f > time)
						{
							Notify(currentTouch.pressed, "OnDoubleClick", null);
						}
						currentTouch.clickTime = time;
					}
				}
				else if (currentTouch.dragStarted) // The button/touch was released on a different object
				{
					// Send a drop notification (for drag & drop)
					Notify(currentTouch.current, "OnDrop", currentTouch.dragged);
				}
			}
			currentTouch.dragStarted = false;
			currentTouch.pressed = null;
			currentTouch.dragged = null;
		}
	}

	/// <summary>
	/// Show or hide the tooltip.
	/// </summary>

	public void ShowTooltip (bool val)
	{
		mTooltipTime = 0f;
		Notify(mTooltip, "OnTooltip", val);
		if (!val) mTooltip = null;
	}
}
