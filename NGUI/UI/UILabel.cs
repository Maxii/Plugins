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
[AddComponentMenu("NGUI/UI/NGUI Label")]
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

	public enum Crispness
	{
		Never,
		OnDesktop,
		Always,
	}

	/// <summary>
	/// Whether the label will keep its content crisp even when shrunk.
	/// You may want to turn this off on mobile devices.
	/// </summary>

	public Crispness keepCrispWhenShrunk = Crispness.OnDesktop;

	[HideInInspector][SerializeField] Font mTrueTypeFont;
	[HideInInspector][SerializeField] UIFont mFont;
#if !UNITY_3_5
	[MultilineAttribute(6)]
#endif
	[HideInInspector][SerializeField] string mText = "";
	[HideInInspector][SerializeField] int mFontSize = 16;
	[HideInInspector][SerializeField] FontStyle mFontStyle = FontStyle.Normal;
	[HideInInspector][SerializeField] bool mEncoding = true;
	[HideInInspector][SerializeField] int mMaxLineCount = 0; // 0 denotes unlimited
	[HideInInspector][SerializeField] Color mGradientBottom = Color.grey;
	[HideInInspector][SerializeField] Effect mEffectStyle = Effect.None;
	[HideInInspector][SerializeField] Color mEffectColor = Color.black;
	[HideInInspector][SerializeField] NGUIText.SymbolStyle mSymbols = NGUIText.SymbolStyle.Uncolored;
	[HideInInspector][SerializeField] Vector2 mEffectDistance = Vector2.one;
	[HideInInspector][SerializeField] Overflow mOverflow = Overflow.ShrinkContent;
	[HideInInspector][SerializeField] Material mMaterial;
	[HideInInspector][SerializeField] bool mApplyGradient = false;
	[HideInInspector][SerializeField] Color mGradientTop = Color.white;
	[HideInInspector][SerializeField] int mSpacingX = 0;
	[HideInInspector][SerializeField] int mSpacingY = 0;

	// Obsolete values
	[HideInInspector][SerializeField] bool mShrinkToFit = false;
	[HideInInspector][SerializeField] int mMaxLineWidth = 0;
	[HideInInspector][SerializeField] int mMaxLineHeight = 0;
	[HideInInspector][SerializeField] float mLineWidth = 0;
	[HideInInspector][SerializeField] bool mMultiline = true;

#if DYNAMIC_FONT
	Font mActiveTTF = null;
	UIRoot mRoot;
#endif
	bool mShouldBeProcessed = true;
	string mProcessedText = null;
	bool mPremultiply = false;
	Vector2 mCalculatedSize = Vector2.zero;
	float mScale = 1f;
	int mLastWidth = 0;
	int mLastHeight = 0;
	int mPrintedSize = 0;
#if UNITY_EDITOR
	bool mUseDynamicFont = false;
#endif

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

	public override Material material
	{
		get
		{
			if (mMaterial != null) return mMaterial;
			if (mFont != null) return mFont.material;
			if (mTrueTypeFont != null) return mTrueTypeFont.material;
			return null;
		}
		set
		{
			if (mMaterial != value)
			{
				MarkAsChanged();
				mMaterial = value;
				MarkAsChanged();
			}
		}
	}

	[Obsolete("Use UILabel.bitmapFont instead")]
	public UIFont font { get { return bitmapFont; } set { bitmapFont = value; } }

	/// <summary>
	/// Set the font used by this label.
	/// </summary>

	public UIFont bitmapFont
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
				if (value != null && value.dynamicFont != null)
				{
					trueTypeFont = value.dynamicFont;
					return;
				}
#endif
				if (trueTypeFont != null) trueTypeFont = null;
				else RemoveFromPanel();

				mFont = value;
				MarkAsChanged();
			}
		}
	}

	/// <summary>
	/// Set the font used by this label.
	/// </summary>

	public Font trueTypeFont
	{
		get
		{
			return mTrueTypeFont;
		}
		set
		{
			if (mTrueTypeFont != value)
			{
#if DYNAMIC_FONT
				SetActiveFont(null);
				RemoveFromPanel();
				mTrueTypeFont = value;
				hasChanged = true;
				mFont = null;
				SetActiveFont(value);
				ProcessAndRequest();
				if (mActiveTTF != null)
					base.MarkAsChanged();
#else
				mTrueTypeFont = value;
#endif
			}
		}
	}

	/// <summary>
	/// Ambiguous helper function.
	/// </summary>

	public UnityEngine.Object ambigiousFont
	{
		get
		{
			return (mFont != null) ? (UnityEngine.Object)mFont : (UnityEngine.Object)mTrueTypeFont;
		}
		set
		{
			UIFont bf = value as UIFont;
			if (bf != null) bitmapFont = bf;
			else trueTypeFont = value as Font;
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
				if (!string.IsNullOrEmpty(mText))
				{
					mText = "";
					hasChanged = true;
					ProcessAndRequest();
				}
			}
			else if (mText != value)
			{
				mText = value;
				hasChanged = true;
				ProcessAndRequest();
			}
		}
	}

	/// <summary>
	/// Dynamic font size used by the label.
	/// </summary>

	public int fontSize
	{
		get
		{
			if (mFont != null) return mFont.defaultSize;
			return mFontSize;
		}
		set
		{
			value = Mathf.Clamp(value, 0, 144);

			if (mFontSize != value)
			{
				mFontSize = value;
				hasChanged = true;
				ProcessAndRequest();
			}
		}
	}

	/// <summary>
	/// Dynamic font style used by the label.
	/// </summary>

	public FontStyle fontStyle
	{
		get
		{
			return mFontStyle;
		}
		set
		{
			if (mFontStyle != value)
			{
				mFontStyle = value;
				hasChanged = true;
				ProcessAndRequest();
			}
		}
	}

	/// <summary>
	/// Whether a gradient will be applied.
	/// </summary>

	public bool applyGradient
	{
		get
		{
			return mApplyGradient;
		}
		set
		{
			if (mApplyGradient != value)
			{
				mApplyGradient = value;
				MarkAsChanged();
			}
		}
	}

	/// <summary>
	/// Top gradient color.
	/// </summary>

	public Color gradientTop
	{
		get
		{
			return mGradientTop;
		}
		set
		{
			if (mGradientTop != value)
			{
				mGradientTop = value;
				if (mApplyGradient) MarkAsChanged();
			}
		}
	}

	/// <summary>
	/// Bottom gradient color.
	/// </summary>

	public Color gradientBottom
	{
		get
		{
			return mGradientBottom;
		}
		set
		{
			if (mGradientBottom != value)
			{
				mGradientBottom = value;
				if (mApplyGradient) MarkAsChanged();
			}
		}
	}

	/// <summary>
	/// Additional horizontal spacing between characters when printing text.
	/// </summary>

	public int spacingX
	{
		get
		{
			return mSpacingX;
		}
		set
		{
			if (mSpacingX != value)
			{
				mSpacingX = value;
				MarkAsChanged();
			}
		}
	}

	/// <summary>
	/// Additional vertical spacing between lines when printing text.
	/// </summary>

	public int spacingY
	{
		get
		{
			return mSpacingY;
		}
		set
		{
			if (mSpacingY != value)
			{
				mSpacingY = value;
				MarkAsChanged();
			}
		}
	}

	/// <summary>
	/// Whether the label will use the printed size instead of font size when printing the label.
	/// It's a dynamic font feature that will ensure that the text is crisp when shrunk.
	/// </summary>

	bool usePrintedSize
	{
		get
		{
			if (trueTypeFont != null && overflowMethod == Overflow.ShrinkContent && keepCrispWhenShrunk != Crispness.Never)
			{
#if UNITY_IPHONE || UNITY_ANDROID || UNITY_WP8 || UNITY_BLACKBERRY
				return (keepCrispWhenShrunk == Crispness.Always);
#else
				return true;
#endif
			}
			return false;
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
			}
		}
	}

	/// <summary>
	/// Style used for symbols.
	/// </summary>

	public NGUIText.SymbolStyle symbolStyle
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

	public override bool canResize { get { return mOverflow != Overflow.ResizeFreely && base.canResize; } }
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
	/// Process the label's text before returning its drawing dimensions.
	/// </summary>

	public override Vector4 drawingDimensions
	{
		get
		{
			if (hasChanged) ProcessText();
			return base.drawingDimensions;
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
				hasChanged = true;
				if (overflowMethod == Overflow.ShrinkContent) MakePixelPerfect();
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
			if (mEffectColor != value)
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
			return mCalculatedSize;
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

	/// <summary>
	/// Whether the label has a valid font.
	/// </summary>

#if DYNAMIC_FONT
	bool isValid { get { return mFont != null || mTrueTypeFont != null; } }
#else
	bool isValid { get { return mFont != null; } }
#endif

	/// <summary>
	/// Label's active pixel size scale.
	/// </summary>

	float pixelSize { get { return (mFont != null) ? mFont.pixelSize : 1f; } }

#if DYNAMIC_FONT
	/// <summary>
	/// Register the font texture change listener.
	/// </summary>

	protected override void OnEnable ()
	{
		base.OnEnable();

		// Auto-upgrade from 3.0.2 and earlier
		if (mTrueTypeFont == null && mFont != null && mFont.isDynamic)
		{
			mTrueTypeFont = mFont.dynamicFont;
			mFontSize = mFont.defaultSize;
			mFontStyle = mFont.dynamicFontStyle;
			mFont = null;
		}
		mRoot = NGUITools.FindInParents<UIRoot>(gameObject);
		SetActiveFont(mTrueTypeFont);
	}

	/// <summary>
	/// Remove the font texture change listener.
	/// </summary>

	protected override void OnDisable ()
	{
		SetActiveFont(null);
		base.OnDisable();
	}

	/// <summary>
	/// Set the active font, correctly setting and clearing callbacks.
	/// </summary>

	protected void SetActiveFont (Font fnt)
	{
		if (mActiveTTF != fnt)
		{
			if (mActiveTTF != null)
				mActiveTTF.textureRebuildCallback -= MarkAsChanged;

			mActiveTTF = fnt;

			if (mActiveTTF != null)
				mActiveTTF.textureRebuildCallback += MarkAsChanged;
		}
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
			int min = Mathf.RoundToInt(mFont.defaultSize * mFont.pixelSize);
			if (height < min) height = min;
		}

		mMaxLineWidth = 0;
		mMaxLineHeight = 0;
		mShrinkToFit = false;

		if (GetComponent<BoxCollider>() != null)
			NGUITools.AddWidgetCollider(gameObject, true);
	}

	/// <summary>
	/// Request the needed characters in the texture.
	/// </summary>

	void ProcessAndRequest ()
	{
#if UNITY_EDITOR
		if (!mAllowProcessing) return;
#endif
		if (ambigiousFont != null)
		{
			ProcessText();
#if DYNAMIC_FONT
			if (mActiveTTF != null) NGUIText.RequestCharactersInTexture(mActiveTTF, mText);
#endif
		}
	}

#if UNITY_EDITOR
	// Used to ensure that we don't process font more than once inside OnValidate function below
	bool mAllowProcessing = true;

	/// <summary>
	/// Validate the properties.
	/// </summary>

	protected override void OnValidate ()
	{
		base.OnValidate();

		UIFont fnt = mFont;
		Font ttf = mTrueTypeFont;

		mFont = null;
		mTrueTypeFont = null;
		mAllowProcessing = false;

#if DYNAMIC_FONT
		SetActiveFont(null);
#endif
		if (ttf != null && (fnt == null || !mUseDynamicFont))
		{
			bitmapFont = null;
			trueTypeFont = ttf;
			mUseDynamicFont = true;
		}
		else if (fnt != null)
		{
			// Auto-upgrade from 3.0.2 and earlier
			if (fnt.isDynamic)
			{
				trueTypeFont = fnt.dynamicFont;
				mFontStyle = fnt.dynamicFontStyle;
				mUseDynamicFont = true;
			}
			else
			{
				bitmapFont = fnt;
				mUseDynamicFont = false;
			}
			mFontSize = fnt.defaultSize;
		}
		else
		{
			trueTypeFont = ttf;
			mUseDynamicFont = true;
		}

		hasChanged = true;
		mAllowProcessing = true;
		ProcessAndRequest();
		if (autoResizeBoxCollider) ResizeCollider();
	}
#endif

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
		mPremultiply = (material != null && material.shader != null && material.shader.name.Contains("Premultiplied"));

#if DYNAMIC_FONT
		// Request the text within the font
		ProcessAndRequest();
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
		if (!isValid) return;

		mChanged = true;
		hasChanged = false;

		int fs = fontSize;
		float invFS = 1f / fs;
		float ps = pixelSize;
		float invSize = 1f / ps;
		float lw = legacyMode ? (mMaxLineWidth  != 0 ? mMaxLineWidth  * invSize : 1000000) : width  * invSize;
		float lh = legacyMode ? (mMaxLineHeight != 0 ? mMaxLineHeight * invSize : 1000000) : height * invSize;

		mScale = 1f;
		mPrintedSize = Mathf.Abs(legacyMode ? Mathf.RoundToInt(cachedTransform.localScale.x) : fs);

		NGUIText.current.size = fs;
		UpdateNGUIText();

		if (mPrintedSize > 0)
		{
			for (;;)
			{
				mScale = mPrintedSize * invFS;

				bool fits = true;

				NGUIText.current.lineWidth  = (mOverflow == Overflow.ResizeFreely) ? 1000000 : Mathf.RoundToInt(lw / mScale);
				NGUIText.current.lineHeight = (mOverflow == Overflow.ResizeFreely || mOverflow == Overflow.ResizeHeight) ?
					1000000 : Mathf.RoundToInt(lh / mScale);

				if (lw > 0f || lh > 0f)
				{
					if (mFont != null) fits = mFont.WrapText(mText, out mProcessedText);
#if DYNAMIC_FONT
					else fits = NGUIText.WrapText(mTrueTypeFont, mText, out mProcessedText);
#endif
				}
				else mProcessedText = mText;

				// Remember the final printed size
				if (!string.IsNullOrEmpty(mProcessedText))
				{
					if (mFont != null) mCalculatedSize = mFont.CalculatePrintedSize(mProcessedText);
#if DYNAMIC_FONT
					else mCalculatedSize = NGUIText.CalculatePrintedSize(mTrueTypeFont, mProcessedText);
#endif
				}
				else mCalculatedSize = Vector2.zero;

				if (mOverflow == Overflow.ResizeFreely)
				{
					mWidth = Mathf.RoundToInt(mCalculatedSize.x * ps);
					mHeight = Mathf.RoundToInt(mCalculatedSize.y * ps);
				}
				else if (mOverflow == Overflow.ResizeHeight)
				{
					mHeight = Mathf.RoundToInt(mCalculatedSize.y * ps);
				}
				else if (mOverflow == Overflow.ShrinkContent && !fits)
				{
					if (--mPrintedSize > 1) continue;
				}

				// Upgrade to the new system
				if (legacyMode)
				{
					width = Mathf.RoundToInt(mCalculatedSize.x * ps);
					height = Mathf.RoundToInt(mCalculatedSize.y * ps);
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
		if (ambigiousFont != null)
		{
			float pixelSize = (bitmapFont != null) ? bitmapFont.pixelSize : 1f;

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

				int minX = Mathf.RoundToInt(mCalculatedSize.x * pixelSize);
				int minY = Mathf.RoundToInt(mCalculatedSize.y * pixelSize);

				if (bitmapFont != null)
				{
					minX = Mathf.Max(bitmapFont.defaultSize);
					minY = Mathf.Max(bitmapFont.defaultSize);
				}
				else
				{
					minX = Mathf.Max(base.minWidth);
					minY = Mathf.Max(base.minHeight);
				}

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
		if (ambigiousFont != null)
		{
			ProcessText(false);
			float pixelSize = (bitmapFont != null) ? bitmapFont.pixelSize : 1f;
			width = Mathf.RoundToInt(mCalculatedSize.x * pixelSize);
			height = Mathf.RoundToInt(mCalculatedSize.y * pixelSize);
		}
	}

	/// <summary>
	/// Apply a shadow effect to the buffer.
	/// </summary>

	void ApplyShadow (BetterList<Vector3> verts, BetterList<Vector2> uvs, BetterList<Color32> cols, int start, int end, float x, float y)
	{
		Color c = mEffectColor;
		c.a *= alpha * mPanel.finalAlpha;
		Color32 col = (bitmapFont != null && bitmapFont.premultipliedAlpha) ? NGUITools.ApplyPMA(c) : c;

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
		if (!isValid) return;

		int offset = verts.size;

		Color col = color;
		col.a *= mPanel.finalAlpha;
		if (mFont != null && mFont.premultipliedAlpha) col = NGUITools.ApplyPMA(col);

		string text = processedText;
		float pixelSize = (mFont != null) ? mFont.pixelSize : 1f;
		float scale = mScale * pixelSize;
		bool usePS = usePrintedSize;
		int start = verts.size;

		UpdateNGUIText();
		NGUIText.current.size = usePS ? mPrintedSize : fontSize;
		NGUIText.current.lineWidth = usePS ? mWidth : Mathf.RoundToInt(mWidth / scale);
		NGUIText.current.tint = col;

		if (mFont != null) mFont.Print(text, verts, uvs, cols);
#if DYNAMIC_FONT
		else NGUIText.Print(mTrueTypeFont, text, verts, uvs, cols);
#endif
		Vector2 po = pivotOffset;
		float fx = Mathf.Lerp(0f, -mWidth, po.x);
		float fy = Mathf.Lerp(mHeight, 0f, po.y);

		// Align vertically
		fy = Mathf.RoundToInt(fy + Mathf.Lerp(mCalculatedSize.y * scale - mHeight, 0f, po.y));

		if (usePS || scale == 1f)
		{
#if UNITY_FLASH
			for (int i = start; i < verts.size; ++i)
			{
				Vector3 buff = verts.buffer[i];
				buff.x += fx;
				buff.y += fy;
				verts.buffer[i] = buff;
			}
#else
			for (int i = start; i < verts.size; ++i)
			{
				verts.buffer[i].x += fx;
				verts.buffer[i].y += fy;
			}
#endif
		}
		else
		{
#if UNITY_FLASH
			for (int i = start; i < verts.size; ++i)
			{
				Vector3 buff = verts.buffer[i];
				buff.x = fx + verts.buffer[i].x * scale;
				buff.y = fy + verts.buffer[i].y * scale;
				verts.buffer[i] = buff;
			}
#else
			for (int i = start; i < verts.size; ++i)
			{
				verts.buffer[i].x = fx + verts.buffer[i].x * scale;
				verts.buffer[i].y = fy + verts.buffer[i].y * scale;
			}
#endif
		}

		// Apply an effect if one was requested
		if (effectStyle != Effect.None)
		{
			int end = verts.size;
			float pixel = pixelSize;
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

	/// <summary>
	/// Calculate the offset necessary to fit the specified text. Helper function.
	/// </summary>

	public int CalculateOffsetToFit (string text)
	{
		UpdateNGUIText();
		NGUIText.current.encoding = false;
		NGUIText.current.symbolStyle = NGUIText.SymbolStyle.None;

		if (bitmapFont != null)
		{
			return bitmapFont.CalculateOffsetToFit(text);
		}
#if DYNAMIC_FONT
		return NGUIText.CalculateOffsetToFit(trueTypeFont, text);
#else
		return 0;
#endif
	}

	/// <summary>
	/// Update NGUIText.current with all the properties from this label.
	/// </summary>

	public void UpdateNGUIText ()
	{
		NGUIText.current.size = fontSize;
		NGUIText.current.style = mFontStyle;
		NGUIText.current.lineWidth = mWidth;
		NGUIText.current.lineHeight = mHeight;
		NGUIText.current.gradient = mApplyGradient;
		NGUIText.current.gradientTop = mGradientTop;
		NGUIText.current.gradientBottom = mGradientBottom;
		NGUIText.current.encoding = mEncoding;
		NGUIText.current.premultiply = mPremultiply;
		NGUIText.current.symbolStyle = mSymbols;
		NGUIText.current.spacingX = mSpacingX;
		NGUIText.current.spacingY = mSpacingY;
#if DYNAMIC_FONT
		NGUIText.current.pixelDensity = (usePrintedSize && mRoot != null) ? 1f / mRoot.pixelSizeAdjustment : 1f;
#else
		NGUIText.current.pixelDensity = 1f;
#endif
		Pivot p = pivot;

		if (p == Pivot.Left || p == Pivot.TopLeft || p == Pivot.BottomLeft)
		{
			NGUIText.current.alignment = TextAlignment.Left;
		}
		else if (p == Pivot.Right || p == Pivot.TopRight || p == Pivot.BottomRight)
		{
			NGUIText.current.alignment = TextAlignment.Right;
		}
		else NGUIText.current.alignment = TextAlignment.Center;
	}

	/// <summary>
	/// Convenience function, in case you wanted to associate progress bar, slider or scroll bar's
	/// OnValueChanged function in inspector with a label.
	/// </summary>

	public void SetCurrentPercent ()
	{
		if (UIProgressBar.current != null)
		{
			text = Mathf.RoundToInt(UIProgressBar.current.value * 100f) + "%";
		}
	}

	/// <summary>
	/// Convenience function, in case you wanted to automatically set some label's text
	/// by selecting a value in the UIPopupList.
	/// </summary>

	public void SetCurrentSelection ()
	{
		if (UIPopupList.current != null)
		{
			text = UIPopupList.current.isLocalized ?
				Localization.Localize(UIPopupList.current.value) :
				UIPopupList.current.value;
		}
	}
}
