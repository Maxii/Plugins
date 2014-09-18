using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SpaceDUIWindow : MonoBehaviour {

	private static Dictionary<int, SpaceDUIWindow> mWindows = new Dictionary<int, SpaceDUIWindow>();

	public const int IncreaseDepthOnFocus = 100;
	public const int MaxDepthOnFocus = 5000;

	private static int mFocusIndex = 0;
	private static int mFucusedWindow = -1;
	public static int FocusedWindow { get { return mFucusedWindow; } private set { mFucusedWindow = value; } }

	public int WindowId = 0;
	private bool isFocused = false;
	private UIPanel panel;
	private bool HookedEvents = false;
	private float HookEventsRetry = 1f;
	private float HookEventsLastTry = 0f;

	void Awake()
	{
		if (mWindows.ContainsKey(this.WindowId))
		{
			Debug.LogWarning("SpaceDUIWindow: A window with ID (" + this.WindowId + ") already exists, fixing it now...");

			// Fix it, make sure we dont make a dead loop
			while (mWindows.ContainsKey(this.WindowId) && this.WindowId < 100)
				this.WindowId++;
		}

		mWindows.Add(this.WindowId, this);
	}

	void Start()
	{
		if (this.panel == null)
			this.panel = this.GetComponent<UIPanel>();

		if (this.panel && this.panel.alpha == 0f)
			this.gameObject.SetActive(false);
	}

	void Update()
	{
		if (!this.HookedEvents && Time.time >= (this.HookEventsLastTry + this.HookEventsRetry))
		{
			this.HookEventsLastTry = Time.time;

			// Hook on press for all the colliders in the window
			foreach (BoxCollider collider in this.GetComponentsInChildren<BoxCollider>())
			{
				UIEventListener listener = UIEventListener.Get(collider.gameObject);
				listener.onPress += OnPress;
			}
		}
	}

	public static SpaceDUIWindow GetWindow(int id)
	{
		if (mWindows.ContainsKey(id))
			return mWindows[id];

		return null;
	}

	public static void FocusWindow(int id)
	{
		// Focus the window
		if (GetWindow(id) != null)
			GetWindow(id).Focus();
	}

	private static void OnFocusedWindow(int id)
	{
		if (mFucusedWindow > -1 && GetWindow(mFucusedWindow))
			GetWindow(mFucusedWindow).isFocused = false;

		mFucusedWindow = id;
	}

	public static void ResetWindowsDepth()
	{
		foreach (KeyValuePair<int, SpaceDUIWindow> window in mWindows)
			window.Value.ResetDepth();
	}

	public void Focus()
	{
		if (this.isFocused)
			return;

		// Prevent focus spamm
		this.isFocused = true;

		// Call the static on focused window
		OnFocusedWindow(this.WindowId);

		// Increase the focus index
		mFocusIndex++;

		// Store the highest assigned depth
		int thisPanelHighestDepth = 0;

		// Increase the panels depth
		foreach (UIPanel p in this.GetComponentsInChildren<UIPanel>())
		{
			p.depth = this.GetOriginalDepth(p.depth) + (mFocusIndex * IncreaseDepthOnFocus);

			if (p.depth > thisPanelHighestDepth)
				thisPanelHighestDepth = p.depth;
		}

		// Check if we have reached the max depth
		if (thisPanelHighestDepth >= MaxDepthOnFocus)
		{
			ResetWindowsDepth();
			this.Focus();
		}
	}

	private int GetOriginalDepth(int depth)
	{
		while (depth > IncreaseDepthOnFocus)
			depth -= IncreaseDepthOnFocus;

		return depth;
	}

	public void ResetDepth()
	{
		// Reset the focus index
		mFocusIndex = 0;
		FocusedWindow = -1;
		this.isFocused = false;

		// Reset depths
		foreach (UIPanel p in this.GetComponentsInChildren<UIPanel>())
			p.depth = this.GetOriginalDepth(p.depth);
	}

	private BoxCollider GetWidgedCollider(UIWidget widget)
	{
		BoxCollider col0 = widget.transform.GetComponent<BoxCollider>();
		
		if (col0 != null)
			return col0;
		else
		{
			BoxCollider col1 = widget.transform.parent.GetComponent<BoxCollider>();
			
			if (col1 != null)
				return col1;
		}
		
		return null;
	}

	void OnPress(GameObject go, bool isPressed)
	{
		if (isPressed)
			this.Focus();
	}

	public void FadeIn(float duration)
	{
		if (!this.gameObject.activeSelf)
			this.gameObject.SetActive(true);

		if (this.animationCurrentMethod != FadeMethods.In && this.mFadeCoroutine != null)
			this.mFadeCoroutine.Stop();
		
		// Start the new animation
		if (this.animationCurrentMethod != FadeMethods.In)
			this.mFadeCoroutine = new UICoroutine(this, this.FadeAnimation(FadeMethods.In, duration));
	}

	public void FadeOut(float duration)
	{
		if (!this.gameObject.activeSelf)
			return;

		if (this.animationCurrentMethod != FadeMethods.Out && this.mFadeCoroutine != null)
			this.mFadeCoroutine.Stop();
		
		// Start the new animation
		if (this.animationCurrentMethod != FadeMethods.Out)
			this.mFadeCoroutine = new UICoroutine(this, this.FadeAnimation(FadeMethods.Out, duration));
	}

	public void SetAlpha(float alpha)
	{
		if (this.panel != null)
			this.panel.alpha = alpha;

		// Find child panels
		UIPanel[] panels = this.gameObject.GetComponentsInChildren<UIPanel>();

		if (panels.Length > 0)
			foreach (UIPanel p in panels)
				p.alpha = alpha;
	}

	/*
	 * Show / Hide Fade Animation
	 */
	
	private enum FadeMethods
	{
		None,
		In,
		Out
	}

	private FadeMethods animationCurrentMethod = FadeMethods.None;
	private UICoroutine mFadeCoroutine;

	// Show / Hide fade animation coroutine
	private IEnumerator FadeAnimation(FadeMethods method, float FadeDuration)
	{
		if (this.panel == null)
			yield break;

		// Check if we are trying to fade in and the window is already shown
		if (method == FadeMethods.In && this.panel.alpha == 1f)
			yield break;
		else if (method == FadeMethods.Out && this.panel.alpha == 0f)
			yield break;
		
		// Define that animation is in progress
		this.animationCurrentMethod = method;
		
		// Get the timestamp
		float startTime = Time.time;
		
		// Determine Fade in or Fade out
		if (method == FadeMethods.In)
		{
			// Calculate the time we need to fade in from the current alpha
			float internalDuration = (FadeDuration - (FadeDuration * this.panel.alpha));
			
			// Update the start time
			startTime -= (FadeDuration - internalDuration);
			
			// Fade In
			while (Time.time < (startTime + internalDuration))
			{
				float RemainingTime = (startTime + FadeDuration) - Time.time;
				float ElapsedTime = FadeDuration - RemainingTime;
				
				// Update the alpha by the percentage of the time elapsed
				this.SetAlpha(ElapsedTime / FadeDuration);
				
				yield return 0;
			}
			
			// Make sure it's 1
			this.panel.alpha = 1.0f;
		}
		else if (method == FadeMethods.Out)
		{
			// Calculate the time we need to fade in from the current alpha
			float internalDuration = (FadeDuration * this.panel.alpha);
			
			// Update the start time
			startTime -= (FadeDuration - internalDuration);
			
			// Fade Out
			while (Time.time < (startTime + internalDuration))
			{
				float RemainingTime = (startTime + FadeDuration) - Time.time;
				
				// Update the alpha by the percentage of the remaing time
				this.SetAlpha(RemainingTime / FadeDuration);
				
				yield return 0;
			}
			
			// Make sure it's 0
			this.panel.alpha = 0.0f;
			this.gameObject.SetActive(false);
		}
		
		// No longer animating
		this.animationCurrentMethod = FadeMethods.None;
	}

	public class UICoroutine : IEnumerator 
	{
		private bool stop;
		
		IEnumerator enumerator;
		MonoBehaviour behaviour;
		
		public readonly Coroutine coroutine;
		
		public UICoroutine(MonoBehaviour behaviour, IEnumerator enumerator)
		{
			this.behaviour = behaviour;
			this.enumerator = enumerator;
			this.coroutine = this.behaviour.StartCoroutine(this);
		}
		
		public object Current { get { return enumerator.Current; } }
		public bool MoveNext() { return !stop && enumerator.MoveNext(); }
		public void Reset() { enumerator.Reset(); }
		public void Stop() { stop = true; }
	}
}
