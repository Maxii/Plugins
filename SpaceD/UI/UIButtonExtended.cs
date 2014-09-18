using UnityEngine;
using System.Collections;

[AddComponentMenu("SpaceD UI/Extended Button")]
public class UIButtonExtended : UIButton {

	public UIWidget additionalTarget;

	public Color additionalNormal = Color.white;
	public Color additionalHover = new Color(225f / 255f, 200f / 255f, 150f / 255f, 1f);
	public Color additionalPressed = new Color(183f / 255f, 163f / 255f, 123f / 255f, 1f);
	public Color additionalDisabled = Color.grey;

	protected override void OnInit()
	{
		base.OnInit();

		if (this.additionalTarget != null)
			this.additionalNormal = this.additionalTarget.color;
	}

	/// <summary>
	/// Change the visual state.
	/// </summary>
	
	public override void SetState(State state, bool immediate)
	{
		base.SetState(state, immediate);
		
		if (this.additionalTarget != null)
		{
			TweenColor tc;
			
			switch (mState)
			{
				case State.Hover: tc = TweenColor.Begin(this.additionalTarget.gameObject, this.duration, this.additionalHover); break;
				case State.Pressed: tc = TweenColor.Begin(this.additionalTarget.gameObject, this.duration, this.additionalPressed); break;
				case State.Disabled: tc = TweenColor.Begin(this.additionalTarget.gameObject, this.duration, this.additionalDisabled); break;
				default: tc = TweenColor.Begin(this.additionalTarget.gameObject, this.duration, this.additionalNormal); break;
			}

			if (immediate && tc != null)
			{
				tc.value = tc.to;
				tc.enabled = false;
			}
		}
	}
}
