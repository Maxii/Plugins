//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;

/// <summary>
/// Tween the widget's size.
/// </summary>

[RequireComponent(typeof(UIWidget))]
[AddComponentMenu("NGUI/Tween/Tween Height")]
public class TweenHeight : UITweener
{
	public int from = 100;
	public int to = 100;
	public bool updateTable = false;

	UIWidget mWidget;
	UITable mTable;

	public UIWidget cachedWidget { get { if (mWidget == null) mWidget = GetComponent<UIWidget>(); return mWidget; } }

	public int height { get { return cachedWidget.height; } set { cachedWidget.height = value; } }

	protected override void OnUpdate (float factor, bool isFinished)
	{
		cachedWidget.height = Mathf.RoundToInt(from * (1f - factor) + to * factor);

		if (updateTable)
		{
			if (mTable == null)
			{
				mTable = NGUITools.FindInParents<UITable>(gameObject);
				if (mTable == null) { updateTable = false; return; }
			}
			mTable.repositionNow = true;
		}
	}

	/// <summary>
	/// Start the tweening operation.
	/// </summary>

	static public TweenHeight Begin (UIWidget widget, float duration, int height)
	{
		TweenHeight comp = UITweener.Begin<TweenHeight>(widget.gameObject, duration);
		comp.from = widget.height;
		comp.to = height;

		if (duration <= 0f)
		{
			comp.Sample(1f, true);
			comp.enabled = false;
		}
		return comp;
	}
}
