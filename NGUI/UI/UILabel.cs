//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

#if !UNITY_3_5 && !UNITY_FLASH
#define DYNAMIC_FONT
#endif

using UnityEngine;
using System.Collections.Generic;
using System;

[ExecuteInEditMode]
[AddComponentMenu("NGUI/UI/Label")]
public class UILabel : UIWidget
{
	public enum Effect
	{
		None,
		Shadow,
		Outline,
	}

	public enum Overflow
	{
		ShrinkContent,
		ClampContent,
		ResizeFreely,
		ResizeHeight,
	}

	[HideInInspector][SerializeField] UIFont mFont;
	[HideInInspector][SerializeField] string mText = "";
	[HideInInspector][SerializeField] bool mEncoding = true;
	[HideInInspector][SerializeField] int mMaxLineCount = 0; // 0 denotes unlimited
	[HideInInspector][SerializeField] bool mPassword = false;
	[HideInInspector][SerializeField] bool mShowLastChar = false;
	[HideInInspector][SerializeField] Effect mEffectStyle = Effect.None;
	[HideInInspector][SerializeField] Color mEffectColor = Color.black;
	[HideInInspector][SerializeField] UIFont.SymbolStyle mSymbols = UIFont.SymbolStyle.Uncolored;
	[HideInInspector][SerializeField] Vector2 mEffectDistance = Vector2.one;
	[HideInInspector][SerializeField] Overflow mOverflow = Overflow.ShrinkContent;

	// Obsolete values
	[HideInInspector][SerializeField] bool mShrinkToFit = false;
	[HideInInspector][SerializeField] int mMaxLineWidth = 0;
	[HideInInspector][SerializeField] int mMaxLineHeight = 0;
	[HideInInspector][SerializeField] float mLineWidth = 0;
	[HideInInspector][SerializeField] bool mMultiline = true;

	bool mShouldBeProcessed = true;
	string mProcessedText = null;
	bool mPremultiply = false;
	Vector2 mSize = Vector2.zero;
	float mScale = 1f;
	int mLastWidth = 0;
	int mLastHeight = 0;

	/// <summary>
	/// Function used to determine if something has changed (and thus the geometry must be rebuilt)
	/// </summary>

	bool hasChanged
	{
		get
		{
			return mShouldBeProcessed;
		}
		set
		{
			if (value)
			{
				mChanged = true;
				mShouldBeProcessed = true;
			}
			else
			{
				mShouldBeProcessed = false;
			}
		}
	}

	/// <summary>
	/// Retrieve the material used by the font.
	/// </summary>

	public override Material material { get { return (mFont != null) ? mFont.material : null; } }

	/// <summary>
	/// Set the font used by this label.
	/// </summary>

	public UIFont font
	{
		get
		{
			return mFont;
		}
		set
		{
			if (mFont != value)
			{
#if DYNAMIC_FONT
				if (mFont != null && mFont.dynamicFont != null)
					mFont.dynamicFont.textureRebuildCallback -= MarkAsChanged;
#endif
				RemoveFromPanel();
				mFont = value;
				hasChanged = true;
#if DYNAMIC_FONT
				if (mFont != null && mFont.dynamicFont != null)
				{
					mFont.dynamicFont.textureRebuildCallback += MarkAsChanged;
					mFont.Request(mText);
				}
#endif
				MarkAsChanged();
			}
		}
	}

	/// <summary>
	/// Text that's being displayed by the label.
	/// </summary>

	public string text
	{
		get
		{
			return mText;
		}
		set
		{
			if (string.IsNullOrEmpty(value))
			{
				if (!string.IsNullOrEmpty(mText)) mText = "";
				hasChanged = true;
			}
			else if (mText != value)
			{
				mText = value;
				hasChanged = true;
#if DYNAMIC_FONT
				if (mFont != null) mFont.Request(value);
#endif
			}
		}
	}

	/// <summary>
	/// Whether this label will support color encoding in the format of [RRGGBB] and new line in the form of a "\\n" string.
	/// </summary>

	public bool supportEncoding
	{
		get
		{
			return mEncoding;
		}
		set
		{
			if (mEncoding != value)
			{
				mEncoding = value;
				hasChanged = true;
				if (value) mPassword = false;
			}
		}
	}

	/// <summary>
	/// Style used for symbols.
	/// </summary>

	public UIFont.SymbolStyle symbolStyle
	{
		get
		{
			return mSymbols;
		}
		set
		{
			if (mSymbols != value)
			{
				mSymbols = value;
				hasChanged = true;
			}
		}
	}

	/// <summary>
	/// Overflow method controls the label's behaviour when its content doesn't fit the bounds.
	/// </summary>

	public Overflow overflowMethod
	{
		get
		{
			return mOverflow;
		}
		set
		{
			if (mOverflow != value)
			{
				mOverflow = value;
				hasChanged = true;
			}
		}
	}

#if UNITY_EDITOR
	/// <summary>
	/// Labels can't be resized manually if the overflow method is set to 'resize'.
	/// </summary>

	public override bool canResize { get { return mOverflow != Overflow.ResizeFreely; } }
#endif

	/// <summary>
	/// Maximum width of the label in pixels.
	/// </summary>

	[System.Obsolete("Use 'width' instead")]
	public int lineWidth
	{
		get { return width; }
		set { width = value; }
	}

	/// <summary>
	/// Maximum height of the label in pixels.
	/// </summary>

	[System.Obsolete("Use 'height' instead")]
	public int lineHeight
	{
		get { return height; }
		set { height = value; }
	}

	/// <summary>
	/// Whether the label supports multiple lines.
	/// </summary>
	
	public bool multiLine
	{
		get
		{
			return mMaxLineCount != 1;
		}
		set
		{
			if ((mMaxLineCount != 1) != value)
			{
				mMaxLineCount = (value ? 0 : 1);
				hasChanged = true;
				if (value) mPassword = false;
			}
		}
	}

	/// <summary>
	/// Process the label's text before returning its corners.
	/// </summary>

	public override Vector3[] localCorners
	{
		get
		{
			if (hasChanged) ProcessText();
			return base.localCorners;
		}
	}

	/// <summary>
	/// Process the label's text before returning its corners.
	/// </summary>

	public override Vector3[] worldCorners
	{
		get
		{
			if (hasChanged) ProcessText();
			return base.worldCorners;
		}
	}

	/// <summary>
	/// The max number of lines to be displayed for the label
	/// </summary>

	public int maxLineCount
	{
		get
		{
			return mMaxLineCount;
		}
		set
		{
			if (mMaxLineCount != value)
			{
				mMaxLineCount = Mathf.Max(value, 0);
				if (value != 1) mPassword = false;
				hasChanged = true;
				if (overflowMethod == Overflow.ShrinkContent) MakePixelPerfect();
			}
		}
	}

	/// <summary>
	/// Whether the label's contents should be hidden
	/// </summary>

	public bool password
	{
		get
		{
			return mPassword;
		}
		set
		{
			if (mPassword != value)
			{
				if (value)
				{
					mMaxLineCount = 1;
					mEncoding = false;
				}
				mPassword = value;
				hasChanged = true;
			}
		}
	}

	/// <summary>
	/// Whether the last character of a password field will be shown
	/// </summary>

	public bool showLastPasswordChar
	{
		get
		{
			return mShowLastChar;
		}
		set
		{
			if (mShowLastChar != value)
			{
				mShowLastChar = value;
				hasChanged = true;
			}
		}
	}

	/// <summary>
	/// What effect is used by the label.
	/// </summary>

	public Effect effectStyle
	{
		get
		{
			return mEffectStyle;
		}
		set
		{
			if (mEffectStyle != value)
			{
				mEffectStyle = value;
				hasChanged = true;
			}
		}
	}

	/// <summary>
	/// Color used by the effect, if it's enabled.
	/// </summary>

	public Color effectColor
	{
		get
		{
			return mEffectColor;
		}
		set
		{
			if (!mEffectColor.Equals(value))
			{
				mEffectColor = value;
				if (mEffectStyle != Effect.None) hasChanged = true;
			}
		}
	}

	/// <summary>
	/// Effect distance in pixels.
	/// </summary>

	public Vector2 effectDistance
	{
		get
		{
			return mEffectDistance;
		}
		set
		{
			if (mEffectDistance != value)
			{
				mEffectDistance = value;
				hasChanged = true;
			}
		}
	}

	/// <summary>
	/// Whether the label will automatically shrink its size in order to fit the maximum line width.
	/// </summary>

	[System.Obsolete("Use 'overflowMethod == UILabel.Overflow.ShrinkContent' instead")]
	public bool shrinkToFit
	{
		get
		{
			return mOverflow == Overflow.ShrinkContent;
		}
		set
		{
			if (value)
			{
				overflowMethod = Overflow.ShrinkContent;
			}
		}
	}

	/// <summary>
	/// Returns the processed version of 'text', with new line characters, line wrapping, etc.
	/// </summary>

	public string processedText
	{
		get
		{
			if (mLastWidth != mWidth || mLastHeight != mHeight)
			{
				mLastWidth = mWidth;
				mLastHeight = mHeight;
				mShouldBeProcessed = true;
			}

			// Process the text if necessary
			if (hasChanged) ProcessText();
			return mProcessedText;
		}
	}

	/// <summary>
	/// Actual printed size of the text, in pixels.
	/// </summary>

	public Vector2 printedSize
	{
		get
		{
			if (hasChanged) ProcessText();
			return mSize;
		}
	}

	/// <summary>
	/// Local size of the widget, in pixels.
	/// </summary>

	public override Vector2 localSize
	{
		get
		{
			if (hasChanged) ProcessText();
			return base.localSize;
		}
	}

#if DYNAMIC_FONT
	/// <summary>
	/// Register the font texture change listener.
	/// </summary>

	protected override void OnEnable ()
	{
		if (mFont != null && mFont.dynamicFont != null)
			mFont.dynamicFont.textureRebuildCallback += MarkAsChanged;
		base.OnEnable();
	}

	/// <summary>
	/// Remove the font texture change listener.
	/// </summary>

	protected override void OnDisable ()
	{
		if (mFont != null && mFont.dynamicFont != null)
			mFont.dynamicFont.textureRebuildCallback -= MarkAsChanged;
		base.OnDisable();
	}
#endif

	/// <summary>
	/// Upgrading labels is a bit different.
	/// </summary>

	protected override void UpgradeFrom265 ()
	{
		ProcessText(true);

		if (mShrinkToFit)
		{
			overflowMethod = Overflow.ShrinkContent;
			mMaxLineCount = 0;
		}

		if (mMaxLineWidth != 0)
		{
			width = mMaxLineWidth;
			overflowMethod = mMaxLineCount > 0 ? Overflow.ResizeHeight : Overflow.ShrinkContent;
		}
		else overflowMethod = Overflow.ResizeFreely;

		if (mMaxLineHeight != 0)
			height = mMaxLineHeight;

		if (mFont != null)
		{
			int min = Mathf.RoundToInt(mFont.size * mFont.pixelSize);
			if (height < min) height = min;
		}

		mMaxLineWidth = 0;
		mMaxLineHeight = 0;
		mShrinkToFit = false;

		if (GetComponent<BoxCollider>() != null)
			NGUITools.AddWidgetCollider(gameObject, true);
	}

	/// <summary>
	/// Determine start-up values.
	/// </summary>

	protected override void OnStart ()
	{
		// Legacy support
		if (mLineWidth > 0f)
		{
			mMaxLineWidth = Mathf.RoundToInt(mLineWidth);
			mLineWidth = 0f;
		}

		if (!mMultiline)
		{
			mMaxLineCount = 1;
			mMultiline = true;
		}

		// Whether this is a premultiplied alpha shader
		mPremultiply = (font != null && font.material != null && font.material.shader.name.Contains("Premultiplied"));

#if DYNAMIC_FONT
		// Request the text within the font
		if (mFont != null) mFont.Request(mText);
#endif
	}

	/// <summary>
	/// UILabel needs additional processing when something changes.
	/// </summary>

	public override void MarkAsChanged ()
	{
		hasChanged = true;
		base.MarkAsChanged();
	}

	/// <summary>
	/// Process the raw text, called when something changes.
	/// </summary>

	void ProcessText () { ProcessText(false); }

	/// <summary>
	/// Process the raw text, called when something changes.
	/// </summary>

	void ProcessText (bool legacyMode)
	{
		if (mFont == null) return;

		mChanged = true;
		hasChanged = false;

		float invSize = 1f / mFont.pixelSize;
		float printSize = Mathf.Abs(legacyMode ? cachedTransform.localScale.x : mFont.size);
		float lw = legacyMode ? (mMaxLineWidth != 0 ? mMaxLineWidth * invSize : 1000000) : width * invSize;
		float lh = legacyMode ? (mMaxLineHeight != 0 ? mMaxLineHeight * invSize : 1000000) : height * invSize;

		if (printSize > 0f)
		{
			for (;;)
			{
				mScale = printSize / mFont.size;

				bool fits = true;

				int pw = (mOverflow == Overflow.ResizeFreely) ? 100000 : Mathf.RoundToInt(lw / mScale);
				int ph = (mOverflow == Overflow.ResizeFreely || mOverflow == Overflow.ResizeHeight) ?
					100000 : Mathf.RoundToInt(lh / mScale);

				if (mPassword)
				{
					mProcessedText = "";

					if (mShowLastChar)
					{
						for (int i = 0, imax = mText.Length - 1; i < imax; ++i)
							mProcessedText += "*";
						if (mText.Length > 0)
							mProcessedText += mText[mText.Length - 1];
					}
					else
					{
						for (int i = 0, imax = mText.Length; i < imax; ++i)
							mProcessedText += "*";
					}
					
					fits = mFont.WrapText(mProcessedText, out mProcessedText, pw, ph, mMaxLineCount, false, UIFont.SymbolStyle.None);
				}
				else if (lw > 0f || lh > 0f)
				{
					fits = mFont.WrapText(mText, out mProcessedText, pw, ph, mMaxLineCount, mEncoding, mSymbols);
				}
				else mProcessedText = mText;

				// Remember the final printed size
				mSize = !string.IsNullOrEmpty(mProcessedText) ?
					mFont.CalculatePrintedSize(mProcessedText, mEncoding, mSymbols) : Vector2.zero;

				if (mOverflow == Overflow.ResizeFreely)
				{
					mWidth = Mathf.RoundToInt(mSize.x * mFont.pixelSize);
					mHeight = Mathf.RoundToInt(mSize.y * mFont.pixelSize);
				}
				else if (mOverflow == Overflow.ResizeHeight)
				{
					mHeight = Mathf.RoundToInt(mSize.y * mFont.pixelSize);
				}
				else if (mOverflow == Overflow.ShrinkContent && !fits)
				{
					printSize = Mathf.Round(printSize - 1f);
					if (printSize > 1f) continue;
				}

				// Upgrade to the new system
				if (legacyMode)
				{
					width = Mathf.RoundToInt(mSize.x * mFont.pixelSize);
					height = Mathf.RoundToInt(mSize.y * mFont.pixelSize);
					cachedTransform.localScale = Vector3.one;
				}
				break;
			}
		}
		else
		{
			cachedTransform.localScale = Vector3.one;
			mProcessedText = "";
			mScale = 1f;
		}
	}

	/// <summary>
	/// Text is pixel-perfect when its scale matches the size.
	/// </summary>

	public override void MakePixelPerfect ()
	{
		if (font != null)
		{
			float pixelSize = font.pixelSize;

			Vector3 pos = cachedTransform.localPosition;
			pos.x = Mathf.RoundToInt(pos.x);
			pos.y = Mathf.RoundToInt(pos.y);
			pos.z = Mathf.RoundToInt(pos.z);

			cachedTransform.localPosition = pos;
			cachedTransform.localScale = Vector3.one;

			if (mOverflow == Overflow.ResizeFreely)
			{
				AssumeNaturalSize();
			}
			else
			{
				Overflow over = mOverflow;
				mOverflow = Overflow.ShrinkContent;
				ProcessText(false);
				mOverflow = over;

				int minX = Mathf.RoundToInt(mSize.x * pixelSize);
				int minY = Mathf.RoundToInt(mSize.y * pixelSize);

				if (width < minX) width = minX;
				if (height < minY) height = minY;
			}
		}
		else base.MakePixelPerfect();
	}

	/// <summary>
	/// Make the label assume its natural size.
	/// </summary>

	public void AssumeNaturalSize ()
	{
		if (font != null)
		{
			ProcessText(false);

			float pixelSize = font.pixelSize;
			int minX = Mathf.RoundToInt(mSize.x * pixelSize);
			int minY = Mathf.RoundToInt(mSize.y * pixelSize);

			if (width < minX) width = minX;
			if (height < minY) height = minY;
		}
	}

	/// <summary>
	/// Apply a shadow effect to the buffer.
	/// </summary>

	void ApplyShadow (BetterList<Vector3> verts, BetterList<Vector2> uvs, BetterList<Color32> cols, int start, int end, float x, float y)
	{
		Color c = mEffectColor;
		c.a *= alpha * mPanel.alpha;
		Color32 col = (font.premultipliedAlpha) ? NGUITools.ApplyPMA(c) : c;

		for (int i = start; i < end; ++i)
		{
			verts.Add(verts.buffer[i]);
			uvs.Add(uvs.buffer[i]);
			cols.Add(cols.buffer[i]);

			Vector3 v = verts.buffer[i];
			v.x += x;
			v.y += y;
			verts.buffer[i] = v;
			cols.buffer[i] = col;
		}
	}

	/// <summary>
	/// Draw the label.
	/// </summary>

	public override void OnFill (BetterList<Vector3> verts, BetterList<Vector2> uvs, BetterList<Color32> cols)
	{
		if (mFont == null) return;

		Pivot p = pivot;
		int offset = verts.size;

		Color col = color;
		col.a *= mPanel.alpha;
		if (font.premultipliedAlpha) col = NGUITools.ApplyPMA(col);

		string text = processedText;
		float scale = mScale * mFont.pixelSize;
		int w = Mathf.RoundToInt(width / scale);
		int start = verts.size;

		// Print the text into the buffers
		if (p == Pivot.Left || p == Pivot.TopLeft || p == Pivot.BottomLeft)
		{
			mFont.Print(text, col, verts, uvs, cols, mEncoding, mSymbols, UIFont.Alignment.Left, w, mPremultiply);
		}
		else if (p == Pivot.Right || p == Pivot.TopRight || p == Pivot.BottomRight)
		{
			mFont.Print(text, col, verts, uvs, cols, mEncoding, mSymbols, UIFont.Alignment.Right, w, mPremultiply);
		}
		else
		{
			mFont.Print(text, col, verts, uvs, cols, mEncoding, mSymbols, UIFont.Alignment.Center, w, mPremultiply);
		}

		Vector2 po = pivotOffset;
		float fx = Mathf.Lerp(0f, -mWidth, po.x);
		float fy = Mathf.Lerp(mHeight, 0f, po.y);

		// Center vertically
		fy += Mathf.Lerp(mSize.y * scale - mHeight, 0f, po.y);

		if (scale == 1f)
		{
			for (int i = start; i < verts.size; ++i)
			{
				verts.buffer[i].x += fx;
				verts.buffer[i].y += fy;
			}
		}
		else
		{
			for (int i = start; i < verts.size; ++i)
			{
				verts.buffer[i].x = fx + verts.buffer[i].x * scale;
				verts.buffer[i].y = fy + verts.buffer[i].y * scale;
			}
		}

		// Apply an effect if one was requested
		if (effectStyle != Effect.None)
		{
			int end = verts.size;
			float pixel = mFont.pixelSize;
			fx = pixel * mEffectDistance.x;
			fy = pixel * mEffectDistance.y;

			ApplyShadow(verts, uvs, cols, offset, end, fx, -fy);

			if (effectStyle == Effect.Outline)
			{
				offset = end;
				end = verts.size;

				ApplyShadow(verts, uvs, cols, offset, end, -fx, fy);

				offset = end;
				end = verts.size;

				ApplyShadow(verts, uvs, cols, offset, end, fx, fy);

				offset = end;
				end = verts.size;

				ApplyShadow(verts, uvs, cols, offset, end, -fx, -fy);
			}
		}
	}
}
