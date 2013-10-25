//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using AnimationOrTween;
using System.Collections.Generic;

/// <summary>
/// Play the specified animation on click.
/// </summary>

[ExecuteInEditMode]
[AddComponentMenu("NGUI/Interaction/Play Animation")]
public class UIPlayAnimation : MonoBehaviour
{
	/// <summary>
	/// Target animation to activate.
	/// </summary>

	public Animation target;

	/// <summary>
	/// Optional clip name, if the animation has more than one clip.
	/// </summary>

	public string clipName;

	/// <summary>
	/// Which event will trigger the animation.
	/// </summary>

	public Trigger trigger = Trigger.OnClick;

	/// <summary>
	/// Which direction to animate in.
	/// </summary>

	public Direction playDirection = Direction.Forward;

	/// <summary>
	/// Whether the animation's position will be reset on play or will continue from where it left off.
	/// </summary>

	public bool resetOnPlay = false;

	/// <summary>
	/// Whether the selected object (this button) will be cleared when the animation gets activated.
	/// </summary>

	public bool clearSelection = false;

	/// <summary>
	/// What to do if the target game object is currently disabled.
	/// </summary>

	public EnableCondition ifDisabledOnPlay = EnableCondition.DoNothing;

	/// <summary>
	/// What to do with the target when the animation finishes.
	/// </summary>

	public DisableCondition disableWhenFinished = DisableCondition.DoNotDisable;

	/// <summary>
	/// Event delegates called when the animation finishes.
	/// </summary>

	public List<EventDelegate> onFinished = new List<EventDelegate>();

	// Deprecated functionality, kept for backwards compatibility
	[HideInInspector][SerializeField] GameObject eventReceiver;
	[HideInInspector][SerializeField] string callWhenFinished;

	bool mStarted = false;
	bool mHighlighted = false;
	int mActive = 0;

	void Awake ()
	{
		// Remove deprecated functionality if new one is used
		if (eventReceiver != null && EventDelegate.IsValid(onFinished))
		{
			eventReceiver = null;
			callWhenFinished = null;
#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(this);
#endif
		}
	}

	void Start ()
	{
		mStarted = true;

		if (target == null)
		{
			target = GetComponentInChildren<Animation>();
#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(this);
#endif
		}
	}

	void OnEnable ()
	{
#if UNITY_EDITOR
		if (!Application.isPlaying) return;
#endif
		if (mStarted && mHighlighted)
			OnHover(UICamera.IsHighlighted(gameObject));
	}

	void OnHover (bool isOver)
	{
		if (enabled)
		{
			if ( trigger == Trigger.OnHover ||
				(trigger == Trigger.OnHoverTrue && isOver) ||
				(trigger == Trigger.OnHoverFalse && !isOver))
			{
				Play(isOver);
			}
			mHighlighted = isOver;
		}
	}

	void OnPress (bool isPressed)
	{
		if (enabled)
		{
			if ( trigger == Trigger.OnPress ||
				(trigger == Trigger.OnPressTrue && isPressed) ||
				(trigger == Trigger.OnPressFalse && !isPressed))
			{
				Play(isPressed);
			}
		}
	}

	void OnClick ()
	{
		if (enabled && trigger == Trigger.OnClick)
		{
			Play(true);
		}
	}

	void OnDoubleClick ()
	{
		if (enabled && trigger == Trigger.OnDoubleClick)
		{
			Play(true);
		}
	}

	void OnSelect (bool isSelected)
	{
		if (enabled)
		{
			if (trigger == Trigger.OnSelect ||
				(trigger == Trigger.OnSelectTrue && isSelected) ||
				(trigger == Trigger.OnSelectFalse && !isSelected))
			{
				Play(true);
			}
		}
	}

	void OnActivate (bool isActive)
	{
		if (enabled)
		{
			if (trigger == Trigger.OnActivate ||
				(trigger == Trigger.OnActivateTrue && isActive) ||
				(trigger == Trigger.OnActivateFalse && !isActive))
			{
				Play(isActive);
			}
		}
	}

	/// <summary>
	/// Start playing the animation.
	/// </summary>

	public void Play (bool forward)
	{
		if (target != null)
		{
			mActive = 0;

			if (clearSelection && UICamera.selectedObject == gameObject)
				UICamera.selectedObject = null;

			int pd = -(int)playDirection;
			Direction dir = forward ? playDirection : ((Direction)pd);
			ActiveAnimation anim = ActiveAnimation.Play(target, clipName, dir, ifDisabledOnPlay, disableWhenFinished);

			if (anim != null)
			{
				if (resetOnPlay) anim.Reset();

				for (int i = 0; i < onFinished.Count; ++i)
				{
					++mActive;
					EventDelegate.Add(anim.onFinished, OnFinished, true);
				}
			}
		}
	}

	/// <summary>
	/// Callback triggered when each tween executed by this script finishes.
	/// </summary>

	void OnFinished ()
	{
		if (--mActive == 0)
		{
			EventDelegate.Execute(onFinished);

			// Legacy functionality
			if (eventReceiver != null && !string.IsNullOrEmpty(callWhenFinished))
				eventReceiver.SendMessage(callWhenFinished, SendMessageOptions.DontRequireReceiver);

			eventReceiver = null;
		}
	}
}
