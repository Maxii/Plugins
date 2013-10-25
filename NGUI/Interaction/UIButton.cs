//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Similar to UIButtonColor, but adds a 'disabled' state based on whether the collider is enabled or not.
/// </summary>

[AddComponentMenu("NGUI/Interaction/Button")]
public class UIButton : UIButtonColor
{
	/// <summary>
	/// Current button that sent out the onClick event.
	/// </summary>

	static public UIButton current;

	/// <summary>
	/// Color that will be applied when the button is disabled.
	/// </summary>

	public Color disabledColor = Color.grey;

	/// <summary>
	/// Click event listener.
	/// </summary>

	public List<EventDelegate> onClick = new List<EventDelegate>();

	protected override void OnEnable ()
	{
		//Collider col = collider;
		//if (col != null) col.enabled = true;

		if (isEnabled)
		{
			if (mStarted)
			{
				if (mHighlighted) base.OnEnable();
				else UpdateColor(true, false);
			}
		}
		else UpdateColor(false, true);
	}

	protected override void OnDisable()
	{
		//Collider col = collider;
		//if (col != null) col.enabled = false;
		if (mStarted) UpdateColor(false, false);
	}

	public override void OnHover (bool isOver) { if (isEnabled) base.OnHover(isOver); }
	public override void OnPress (bool isPressed) { if (isEnabled) base.OnPress(isPressed); }

	/// <summary>
	/// Call the listener function.
	/// </summary>

	void OnClick ()
	{
		if (isEnabled)
		{
			current = this;
			EventDelegate.Execute(onClick);
			current = null;
		}
	}

	/// <summary>
	/// Whether the button should be enabled.
	/// </summary>

	public bool isEnabled
	{
		get
		{
			if (!enabled) return false;
			Collider col = collider;
			return col && col.enabled;
		}
		set
		{
			Collider col = collider;
			if (col != null) col.enabled = value;
			enabled = value;
		}
	}

	/// <summary>
	/// Update the button's color to either enabled or disabled state.
	/// </summary>

	public void UpdateColor (bool shouldBeEnabled, bool immediate)
	{
		if (tweenTarget)
		{
			if (!mStarted)
			{
				mStarted = true;
				Init();
			}

			Color c = shouldBeEnabled ? defaultColor : disabledColor;
			TweenColor tc = TweenColor.Begin(tweenTarget, 0.15f, c);

			if (immediate)
			{
				tc.color = c;
				tc.enabled = false;
			}
		}
	}
}
