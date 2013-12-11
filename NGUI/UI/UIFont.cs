//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

// Dynamic font support contributed by the NGUI community members:
// Unisip, zh4ox, Mudwiz, Nicki, DarkMagicCK.

#if !UNITY_3_5 && !UNITY_FLASH
#define DYNAMIC_FONT
#endif

using UnityEngine;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// UIFont contains everything needed to be able to print text.
/// </summary>

[ExecuteInEditMode]
[AddComponentMenu("NGUI/UI/Font")]
public class UIFont : MonoBehaviour
{
	[HideInInspector][SerializeField] Material mMat;
	[HideInInspector][SerializeField] Rect mUVRect = new Rect(0f, 0f, 1f, 1f);
	[HideInInspector][SerializeField] BMFont mFont = new BMFont();
	[HideInInspector][SerializeField] UIAtlas mAtlas;
	[HideInInspector][SerializeField] UIFont mReplacement;
	[HideInInspector][SerializeField] float mPixelSize = 1f;

	// List of symbols, such as emoticons like ":)", ":(", etc
	[HideInInspector][SerializeField] List<BMSymbol> mSymbols = new List<BMSymbol>();

	// Used for dynamic fonts
	[HideInInspector][SerializeField] Font mDynamicFont;
	[HideInInspector][SerializeField] int mDynamicFontSize = 16;
	[HideInInspector][SerializeField] FontStyle mDynamicFontStyle = FontStyle.Normal;

	// Cached value
	UISpriteData mSprite = null;
	int mPMA = -1;
	bool mSpriteSet = false;

	// I'd use a Stack here, but then Flash export wouldn't work as it doesn't support it
	static BetterList<Color> mColors = new BetterList<Color>();

	/// <summary>
	/// Access to the BMFont class directly.
	/// </summary>

	public BMFont bmFont { get { return (mReplacement != null) ? mReplacement.bmFont : mFont; } }

	/// <summary>
	/// Original width of the font's texture in pixels.
	/// </summary>

	public int texWidth { get { return (mReplacement != null) ? mReplacement.texWidth : ((mFont != null) ? mFont.texWidth : 1); } }

	/// <summary>
	/// Original height of the font's texture in pixels.
	/// </summary>

	public int texHeight { get { return (mReplacement != null) ? mReplacement.texHeight : ((mFont != null) ? mFont.texHeight : 1); } }

	/// <summary>
	/// Whether the font has any symbols defined.
	/// </summary>

	public bool hasSymbols { get { return (mReplacement != null) ? mReplacement.hasSymbols : mSymbols.Count != 0; } }

	/// <summary>
	/// List of symbols within the font.
	/// </summary>

	public List<BMSymbol> symbols { get { return (mReplacement != null) ? mReplacement.symbols : mSymbols; } }

	/// <summary>
	/// Atlas used by the font, if any.
	/// </summary>

	public UIAtlas atlas
	{
		get
		{
			return (mReplacement != null) ? mReplacement.atlas : mAtlas;
		}
		set
		{
			if (mReplacement != null)
			{
				mReplacement.atlas = value;
			}
			else if (mAtlas != value)
			{
				if (value == null)
				{
					if (mAtlas != null) mMat = mAtlas.spriteMaterial;
					if (sprite != null) mUVRect = uvRect;
				}

				mPMA = -1;
				mAtlas = value;
				MarkAsChanged();
			}
		}
	}

	/// <summary>
	/// Get or set the material used by this font.
	/// </summary>

	public Material material
	{
		get
		{
			if (mReplacement != null) return mReplacement.material;

			if (mAtlas != null) return mAtlas.spriteMaterial;

			if (mMat != null)
			{
				if (mDynamicFont != null && mMat != mDynamicFont.material)
				{
					mMat.mainTexture = mDynamicFont.material.mainTexture;
				}
				return mMat;
			}

			if (mDynamicFont != null)
			{
				return mDynamicFont.material;
			}
			return null;
		}
		set
		{
			if (mReplacement != null)
			{
				mReplacement.material = value;
			}
			else if (mMat != value)
			{
				mPMA = -1;
				mMat = value;
				MarkAsChanged();
			}
		}
	}

	/// <summary>
	/// Pixel size is a multiplier applied to label dimensions when performing MakePixelPerfect() pixel correction.
	/// Most obvious use would be on retina screen displays. The resolution doubles, but with UIRoot staying the same
	/// for layout purposes, you can still get extra sharpness by switching to an HD font that has pixel size set to 0.5.
	/// </summary>

	public float pixelSize
	{
		get
		{
			if (mReplacement != null) return mReplacement.pixelSize;
			if (mAtlas != null) return mAtlas.pixelSize;
			return mPixelSize;
		}
		set
		{
			if (mReplacement != null)
			{
				mReplacement.pixelSize = value;
			}
			else if (mAtlas != null)
			{
				mAtlas.pixelSize = value;
			}
			else
			{
				float val = Mathf.Clamp(value, 0.25f, 4f);

				if (mPixelSize != val)
				{
					mPixelSize = val;
					MarkAsChanged();
				}
			}
		}
	}

	/// <summary>
	/// Whether the font is using a premultiplied alpha material.
	/// </summary>

	public bool premultipliedAlpha
	{
		get
		{
			if (mReplacement != null) return mReplacement.premultipliedAlpha;

			if (mAtlas != null) return mAtlas.premultipliedAlpha;

			if (mPMA == -1)
			{
				Material mat = material;
				mPMA = (mat != null && mat.shader != null && mat.shader.name.Contains("Premultiplied")) ? 1 : 0;
			}
			return (mPMA == 1);
		}
	}

	/// <summary>
	/// Convenience function that returns the texture used by the font.
	/// </summary>

	public Texture2D texture
	{
		get
		{
			if (mReplacement != null) return mReplacement.texture;
			Material mat = material;
			return (mat != null) ? mat.mainTexture as Texture2D : null;
		}
	}

	/// <summary>
	/// Offset and scale applied to all UV coordinates.
	/// </summary>

	public Rect uvRect
	{
		get
		{
			if (mReplacement != null) return mReplacement.uvRect;

			if (mAtlas != null && (mSprite == null && sprite != null))
			{
				Texture tex = mAtlas.texture;

				if (tex != null)
				{
					mUVRect = new Rect(
						mSprite.x - mSprite.paddingLeft,
						mSprite.y - mSprite.paddingTop,
						mSprite.width + mSprite.paddingLeft + mSprite.paddingRight,
						mSprite.height + mSprite.paddingTop + mSprite.paddingBottom);

					mUVRect = NGUIMath.ConvertToTexCoords(mUVRect, tex.width, tex.height);
#if UNITY_EDITOR
					// The font should always use the original texture size
					if (mFont != null)
					{
						float tw = (float)mFont.texWidth / tex.width;
						float th = (float)mFont.texHeight / tex.height;

						if (tw != mUVRect.width || th != mUVRect.height)
						{
							//Debug.LogWarning("Font sprite size doesn't match the expected font texture size.\n" +
							//	"Did you use the 'inner padding' setting on the Texture Packer? It must remain at '0'.", this);
							mUVRect.width = tw;
							mUVRect.height = th;
						}
					}
#endif
					// Trimmed sprite? Trim the glyphs
					if (mSprite.hasPadding) Trim();
				}
			}
			return mUVRect;
		}
		set
		{
			if (mReplacement != null)
			{
				mReplacement.uvRect = value;
			}
			else if (sprite == null && mUVRect != value)
			{
				mUVRect = value;
				MarkAsChanged();
			}
		}
	}

	/// <summary>
	/// Sprite used by the font, if any.
	/// </summary>

	public string spriteName
	{
		get
		{
			return (mReplacement != null) ? mReplacement.spriteName : mFont.spriteName;
		}
		set
		{
			if (mReplacement != null)
			{
				mReplacement.spriteName = value;
			}
			else if (mFont.spriteName != value)
			{
				mFont.spriteName = value;
				MarkAsChanged();
			}
		}
	}

	/// <summary>
	/// Whether this is a valid font.
	/// </summary>

#if DYNAMIC_FONT
	public bool isValid { get { return mDynamicFont != null || mFont.isValid; } }
#else
	public bool isValid { get { return mFont.isValid; } }
#endif

	[System.Obsolete("Use UIFont.defaultSize instead")]
	public int size
	{
		get { return defaultSize; }
		set { defaultSize = value; }
	}

	/// <summary>
	/// Pixel-perfect size of this font.
	/// </summary>

	public int defaultSize
	{
		get
		{
			return (mReplacement != null) ? mReplacement.defaultSize : (isDynamic ? mDynamicFontSize : mFont.charSize);
		}
		set
		{
			if (mReplacement != null) mReplacement.defaultSize = value;
			else mDynamicFontSize = value;
		}
	}

	/// <summary>
	/// Retrieves the sprite used by the font, if any.
	/// </summary>

	public UISpriteData sprite
	{
		get
		{
			if (mReplacement != null) return mReplacement.sprite;

			if (!mSpriteSet) mSprite = null;

			if (mSprite == null)
			{
				if (mAtlas != null && !string.IsNullOrEmpty(mFont.spriteName))
				{
					mSprite = mAtlas.GetSprite(mFont.spriteName);

					if (mSprite == null) mSprite = mAtlas.GetSprite(name);

					mSpriteSet = true;

					if (mSprite == null) mFont.spriteName = null;
				}

				for (int i = 0, imax = mSymbols.Count; i < imax; ++i)
					symbols[i].MarkAsChanged();
			}
			return mSprite;
		}
	}

	/// <summary>
	/// Setting a replacement atlas value will cause everything using this font to use the replacement font instead.
	/// Suggested use: set up all your widgets to use a dummy font that points to the real font. Switching that font to
	/// another one (for example an eastern language one) is then a simple matter of setting this field on your dummy font.
	/// </summary>

	public UIFont replacement
	{
		get
		{
			return mReplacement;
		}
		set
		{
			UIFont rep = value;
			if (rep == this) rep = null;

			if (mReplacement != rep)
			{
				if (rep != null && rep.replacement == this) rep.replacement = null;
				if (mReplacement != null) MarkAsChanged();
				mReplacement = rep;
				MarkAsChanged();
			}
		}
	}

	/// <summary>
	/// Whether the font is dynamic.
	/// </summary>

	public bool isDynamic { get { return (mReplacement != null) ? mReplacement.isDynamic : (mDynamicFont != null); } }

	/// <summary>
	/// Get or set the dynamic font source.
	/// </summary>

	public Font dynamicFont
	{
		get
		{
			return (mReplacement != null) ? mReplacement.dynamicFont : mDynamicFont;
		}
		set
		{
			if (mReplacement != null)
			{
				mReplacement.dynamicFont = value;
			}
			else if (mDynamicFont != value)
			{
				if (mDynamicFont != null) material = null;
				mDynamicFont = value;
				MarkAsChanged();
			}
		}
	}

	/// <summary>
	/// Get or set the dynamic font's style.
	/// </summary>

	public FontStyle dynamicFontStyle
	{
		get
		{
			return (mReplacement != null) ? mReplacement.dynamicFontStyle : mDynamicFontStyle;
		}
		set
		{
			if (mReplacement != null)
			{
				mReplacement.dynamicFontStyle = value;
			}
			else if (mDynamicFontStyle != value)
			{
				mDynamicFontStyle = value;
				MarkAsChanged();
			}
		}
	}

	/// <summary>
	/// Trim the glyphs, making sure they never go past the trimmed texture bounds.
	/// </summary>

	void Trim ()
	{
		Texture tex = mAtlas.texture;

		if (tex != null && mSprite != null)
		{
			Rect full = NGUIMath.ConvertToPixels(mUVRect, texture.width, texture.height, true);
			Rect trimmed = new Rect(mSprite.x, mSprite.y, mSprite.width, mSprite.height);

			int xMin = Mathf.RoundToInt(trimmed.xMin - full.xMin);
			int yMin = Mathf.RoundToInt(trimmed.yMin - full.yMin);
			int xMax = Mathf.RoundToInt(trimmed.xMax - full.xMin);
			int yMax = Mathf.RoundToInt(trimmed.yMax - full.yMin);

			mFont.Trim(xMin, yMin, xMax, yMax);
		}
	}

	/// <summary>
	/// Helper function that determines whether the font uses the specified one, taking replacements into account.
	/// </summary>

	bool References (UIFont font)
	{
		if (font == null) return false;
		if (font == this) return true;
		return (mReplacement != null) ? mReplacement.References(font) : false;
	}

	/// <summary>
	/// Helper function that determines whether the two atlases are related.
	/// </summary>

	static public bool CheckIfRelated (UIFont a, UIFont b)
	{
		if (a == null || b == null) return false;
#if DYNAMIC_FONT
		if (a.isDynamic && b.isDynamic && a.dynamicFont.fontNames[0] == b.dynamicFont.fontNames[0]) return true;
#endif
		return a == b || a.References(b) || b.References(a);
	}

	Texture dynamicTexture
	{
		get
		{
			if (mReplacement) return mReplacement.dynamicTexture;
			if (isDynamic) return mDynamicFont.material.mainTexture;
			return null;
		}
	}

	/// <summary>
	/// Refresh all labels that use this font.
	/// </summary>

	public void MarkAsChanged ()
	{
#if UNITY_EDITOR
		UnityEditor.EditorUtility.SetDirty(gameObject);
#endif
		if (mReplacement != null) mReplacement.MarkAsChanged();

		mSprite = null;
		UILabel[] labels = NGUITools.FindActive<UILabel>();

		for (int i = 0, imax = labels.Length; i < imax; ++i)
		{
			UILabel lbl = labels[i];

			if (lbl.enabled && NGUITools.GetActive(lbl.gameObject) && CheckIfRelated(this, lbl.bitmapFont))
			{
				UIFont fnt = lbl.bitmapFont;
				lbl.bitmapFont = null;
				lbl.bitmapFont = fnt;
			}
		}

		// Clear all symbols
		for (int i = 0, imax = mSymbols.Count; i < imax; ++i)
			symbols[i].MarkAsChanged();
	}

	/// <summary>
	/// Get the printed size of the specified string. The returned value is in pixels.
	/// </summary>

	public Vector2 CalculatePrintedSize (string text)
	{
		if (mReplacement != null) return mReplacement.CalculatePrintedSize(text);

#if DYNAMIC_FONT
		if (isDynamic)
		{
			NGUIText.current.size = mDynamicFontSize;
			NGUIText.current.style = mDynamicFontStyle;
			return NGUIText.CalculatePrintedSize(mDynamicFont, text);
		}
#endif
		Vector2 v = Vector2.zero;

		if (mFont != null && mFont.isValid && !string.IsNullOrEmpty(text))
		{
			if (NGUIText.current.encoding) text = NGUIText.StripSymbols(text);

			int x = 0;
			int y = 0;
			int prev = 0;
			int maxX = 0;
			int length = text.Length;
			int lineHeight = NGUIText.current.size + NGUIText.current.spacingY;
			bool useSymbols = NGUIText.current.encoding && NGUIText.current.symbolStyle != NGUIText.SymbolStyle.None && hasSymbols;

			for (int i = 0; i < length; ++i)
			{
				char c = text[i];

				// Start a new line
				if (c == '\n')
				{
					if (x > maxX) maxX = x;
					x = 0;
					y += lineHeight;
					prev = 0;
					continue;
				}

				// Skip invalid characters
				if (c < ' ') { prev = 0; continue; }

				// See if there is a symbol matching this text
				BMSymbol symbol = useSymbols ? MatchSymbol(text, i, length) : null;

				if (symbol == null)
				{
					// Get the glyph for this character
					BMGlyph glyph = mFont.GetGlyph(c);

					if (glyph != null)
					{
						x += NGUIText.current.spacingX + ((prev != 0) ? glyph.advance + glyph.GetKerning(prev) : glyph.advance);
						prev = c;
					}
				}
				else
				{
					// Symbol found -- use it
					x += NGUIText.current.spacingX + symbol.width;
					i += symbol.length - 1;
					prev = 0;
				}
			}

			// Padding is always between characters, so it's one less than the number of characters
			v.x = ((x > maxX) ? x : maxX);
			v.y = (y + NGUIText.current.size);
		}
		return v;
	}

	/// <summary>
	/// Get the end of line that would fit into a field of given width.
	/// </summary>

	public string GetEndOfLineThatFits (string text)
	{
		int textLength = text.Length;
		int offset = CalculateOffsetToFit(text);
		return text.Substring(offset, textLength - offset);
	}

	/// <summary>
	/// Calculate the character index offset required to print the end of the specified text.
	/// Originally contributed by MightyM: http://www.tasharen.com/forum/index.php?topic=1049.0
	/// </summary>

	public int CalculateOffsetToFit (string text)
	{
		if (NGUIText.current.lineWidth < 1) return 0;
		if (mReplacement != null) return mReplacement.CalculateOffsetToFit(text);

#if DYNAMIC_FONT
		if (isDynamic)
		{
			NGUIText.current.size = mDynamicFontSize;
			NGUIText.current.style = mDynamicFontStyle;
			return NGUIText.CalculateOffsetToFit(mDynamicFont, text);
		}
#endif
		int textLength = text.Length;
		int remainingWidth = NGUIText.current.lineWidth;
		BMGlyph followingGlyph = null;
		int currentCharacterIndex = textLength;
		bool useSymbols = NGUIText.current.encoding && NGUIText.current.symbolStyle != NGUIText.SymbolStyle.None && hasSymbols;

		while (currentCharacterIndex > 0 && remainingWidth > 0)
		{
			char currentCharacter = text[--currentCharacterIndex];

			// See if there is a symbol matching this text
			BMSymbol symbol = useSymbols ? MatchSymbol(text, currentCharacterIndex, textLength) : null;

			// Calculate how wide this symbol or character is going to be
			int glyphWidth = NGUIText.current.spacingX;

			if (symbol != null)
			{
				glyphWidth += symbol.advance;
			}
			else
			{
				// Find the glyph for this character
				BMGlyph glyph = mFont.GetGlyph(currentCharacter);

				if (glyph != null)
				{
					glyphWidth += glyph.advance + ((followingGlyph == null) ? 0 : followingGlyph.GetKerning(currentCharacter));
					followingGlyph = glyph;
				}
				else
				{
					followingGlyph = null;
					continue;
				}
			}

			// Remaining width after this glyph gets printed
			remainingWidth -= glyphWidth;
		}
		if (remainingWidth < 0) ++currentCharacterIndex;
		return currentCharacterIndex;
	}

	/// <summary>
	/// Text wrapping functionality. Works with settings set in NGUIText.current.
	/// Returns 'true' if the specified text was able to fit into the provided dimensions, 'false' otherwise.
	/// </summary>

	public bool WrapText (string text, out string finalText)
	{
		if (mReplacement != null) return mReplacement.WrapText(text, out finalText);

#if DYNAMIC_FONT
		if (isDynamic)
		{
			NGUIText.current.size = mDynamicFontSize;
			NGUIText.current.style = mDynamicFontStyle;
			return NGUIText.WrapText(mDynamicFont, text, out finalText);
		}
#endif
		if (NGUIText.current.lineWidth < 1 || NGUIText.current.lineHeight < 1)
		{
			finalText = "";
			return false;
		}

		int height = (NGUIText.current.maxLines > 0) ?
			Mathf.Min(NGUIText.current.lineHeight, NGUIText.current.size * NGUIText.current.maxLines) : NGUIText.current.lineHeight;

		int maxLineCount = (NGUIText.current.maxLines > 0) ? NGUIText.current.maxLines : 1000000;
		int sum = NGUIText.current.size + NGUIText.current.spacingY;
		maxLineCount = (sum > 0) ? Mathf.Min(maxLineCount, height / sum) : 0;

		if (maxLineCount == 0)
		{
			finalText = "";
			return false;
		}

		StringBuilder sb = new StringBuilder();
		int textLength = text.Length;
		int remainingWidth = NGUIText.current.lineWidth;
		int previousChar = 0;
		int start = 0;
		int offset = 0;
		int lineCount = 1;
		bool lineIsEmpty = true;
		bool useSymbols = NGUIText.current.encoding && NGUIText.current.symbolStyle != NGUIText.SymbolStyle.None && hasSymbols;

		// Run through all characters
		for (; offset < textLength; ++offset)
		{
			char ch = text[offset];

			// New line character -- start a new line
			if (ch == '\n')
			{
				if (lineCount == maxLineCount) break;
				remainingWidth = NGUIText.current.lineWidth;

				// Add the previous word to the final string
				if (start < offset) sb.Append(text.Substring(start, offset - start + 1));
				else sb.Append(ch);

				lineIsEmpty = true;
				++lineCount;
				start = offset + 1;
				previousChar = 0;
				continue;
			}

			// If this marks the end of a word, add it to the final string.
			if (ch == ' ' && previousChar != ' ' && start < offset)
			{
				sb.Append(text.Substring(start, offset - start + 1));
				lineIsEmpty = false;
				start = offset + 1;
				previousChar = ch;
			}

			// When encoded symbols such as [RrGgBb] or [-] are encountered, skip past them
			if (NGUIText.ParseSymbol(text, ref offset)) { --offset; continue; }

			// See if there is a symbol matching this text
			BMSymbol symbol = useSymbols ? MatchSymbol(text, offset, textLength) : null;

			// Calculate how wide this symbol or character is going to be
			int glyphWidth = 0;

			if (symbol != null)
			{
				glyphWidth = NGUIText.current.spacingX + symbol.advance;
			}
			else
			{
				// Find the glyph for this character
				BMGlyph glyph = (symbol == null) ? mFont.GetGlyph(ch) : null;

				if (glyph != null)
				{
					glyphWidth = NGUIText.current.spacingX +
						((previousChar != 0) ? glyph.advance + glyph.GetKerning(previousChar) : glyph.advance);
				}
				else continue;
			}

			// Remaining width after this glyph gets printed
			remainingWidth -= glyphWidth;

			// Doesn't fit?
			if (remainingWidth < 0)
			{
				// Can't start a new line
				if (lineIsEmpty || lineCount == maxLineCount)
				{
					// This is the first word on the line -- add it up to the character that fits
					sb.Append(text.Substring(start, Mathf.Max(0, offset - start)));

					if (lineCount++ == maxLineCount)
					{
						start = offset;
						break;
					}
					NGUIText.EndLine(ref sb);

					// Start a brand-new line
					lineIsEmpty = true;

					if (ch == ' ')
					{
						start = offset + 1;
						remainingWidth = NGUIText.current.lineWidth;
					}
					else
					{
						start = offset;
						remainingWidth = NGUIText.current.lineWidth - glyphWidth;
					}
					previousChar = 0;
				}
				else
				{
					// Skip all spaces before the word
					while (start < textLength && text[start] == ' ') ++start;

					// Revert the position to the beginning of the word and reset the line
					lineIsEmpty = true;
					remainingWidth = NGUIText.current.lineWidth;
					offset = start - 1;
					previousChar = 0;

					if (lineCount++ == maxLineCount) break;
					NGUIText.EndLine(ref sb);
					continue;
				}
			}
			else previousChar = ch;

			// Advance the offset past the symbol
			if (symbol != null)
			{
				offset += symbol.length - 1;
				previousChar = 0;
			}
		}

		if (start < offset) sb.Append(text.Substring(start, offset - start));
		finalText = sb.ToString();
		return (offset == textLength) || (lineCount <= Mathf.Min(NGUIText.current.maxLines, maxLineCount));
	}

	static Color32 s_c0, s_c1;

	/// <summary>
	/// Print the specified text into the buffers.
	/// Note: 'lineWidth' parameter should be in pixels.
	/// </summary>

	public void Print (string text, BetterList<Vector3> verts, BetterList<Vector2> uvs, BetterList<Color32> cols)
	{
		if (mReplacement != null)
		{
			mReplacement.Print(text, verts, uvs, cols);
		}
		else if (!string.IsNullOrEmpty(text))
		{
			if (!isValid)
			{
				Debug.LogError("Attempting to print using an invalid font!");
				return;
			}

#if DYNAMIC_FONT
			if (isDynamic)
			{
				// NOTE: This shouldn't be used anymore. All dynamic font printing goes directly through NGUIText instead.
				NGUIText.current.size = mDynamicFontSize;
				NGUIText.current.style = mDynamicFontStyle;
				NGUIText.Print(dynamicFont, text, verts, uvs, cols);
				return;
			}
#endif
			mColors.Add(Color.white);

			int fs = NGUIText.current.size;
			int indexOffset = verts.size;
			int maxX = 0;
			int x = 0;
			int y = 0;
			int prev = 0;
			int lineHeight = (fs + NGUIText.current.spacingY);
			Vector3 v0 = Vector3.zero, v1 = Vector3.zero;
			Vector2 u0 = Vector2.zero, u1 = Vector2.zero;
			Color gb = NGUIText.current.tint * NGUIText.current.gradientBottom;
			Color gt = NGUIText.current.tint * NGUIText.current.gradientTop;
			Color32 uc = NGUIText.current.tint;

			float invX = uvRect.width / mFont.texWidth;
			float invY = mUVRect.height / mFont.texHeight;

			int textLength = text.Length;
			bool useSymbols = NGUIText.current.encoding && NGUIText.current.symbolStyle != NGUIText.SymbolStyle.None && hasSymbols && sprite != null;

			for (int i = 0; i < textLength; ++i)
			{
				char c = text[i];

				if (c == '\n')
				{
					if (x > maxX) maxX = x;

					if (NGUIText.current.alignment != TextAlignment.Left)
					{
						NGUIText.Align(verts, indexOffset, x - NGUIText.current.spacingX);
						indexOffset = verts.size;
					}

					x = 0;
					y += lineHeight;
					prev = 0;
					continue;
				}

				if (c < ' ')
				{
					prev = 0;
					continue;
				}

				if (NGUIText.current.encoding && NGUIText.ParseSymbol(text, ref i, mColors, NGUIText.current.premultiply))
				{
					Color fc = NGUIText.current.tint * mColors[mColors.size - 1];
					uc = fc;

					if (NGUIText.current.gradient)
					{
						gb = NGUIText.current.gradientBottom * fc;
						gt = NGUIText.current.gradientTop * fc;
					}
					--i;
					continue;
				}

				// See if there is a symbol matching this text
				BMSymbol symbol = useSymbols ? MatchSymbol(text, i, textLength) : null;

				if (symbol == null)
				{
					BMGlyph glyph = mFont.GetGlyph(c);
					if (glyph == null) continue;

					if (prev != 0) x += glyph.GetKerning(prev);

					if (c == ' ')
					{
						x += NGUIText.current.spacingX + glyph.advance;
						prev = c;
						continue;
					}

					v0.x =  (x + glyph.offsetX);
					v0.y = -(y + glyph.offsetY);

					v1.x = v0.x + glyph.width;
					v1.y = v0.y - glyph.height;

					u0.x = mUVRect.xMin + invX * glyph.x;
					u0.y = mUVRect.yMax - invY * glyph.y;

					u1.x = u0.x + invX * glyph.width;
					u1.y = u0.y - invY * glyph.height;

					x += NGUIText.current.spacingX + glyph.advance;
					prev = c;

					if (NGUIText.current.gradient)
					{
						float min = NGUIText.current.size - glyph.offsetY;
						float max = min - glyph.height;

						min /= NGUIText.current.size;
						max /= NGUIText.current.size;

						s_c0 = Color.Lerp(gb, gt, min);
						s_c1 = Color.Lerp(gb, gt, max);

						cols.Add(s_c0);
						cols.Add(s_c1);
						cols.Add(s_c1);
						cols.Add(s_c0);
					}
					else for (int b = 0; b < 4; ++b) cols.Add(uc);
				}
				else
				{
					v0.x =  (x + symbol.offsetX);
					v0.y = -(y + symbol.offsetY);

					v1.x = v0.x + symbol.width;
					v1.y = v0.y - symbol.height;

					Rect uv = symbol.uvRect;

					u0.x = uv.xMin;
					u0.y = uv.yMax;
					u1.x = uv.xMax;
					u1.y = uv.yMin;

					x += NGUIText.current.spacingX + symbol.advance;
					i += symbol.length - 1;
					prev = 0;

					if (NGUIText.current.symbolStyle == NGUIText.SymbolStyle.Colored)
					{
						for (int b = 0; b < 4; ++b) cols.Add(uc);
					}
					else
					{
						Color32 col = Color.white;
						col.a = uc.a;
						for (int b = 0; b < 4; ++b) cols.Add(col);
					}
				}

				verts.Add(new Vector3(v1.x, v0.y));
				verts.Add(new Vector3(v1.x, v1.y));
				verts.Add(new Vector3(v0.x, v1.y));
				verts.Add(new Vector3(v0.x, v0.y));

				uvs.Add(new Vector2(u1.x, u0.y));
				uvs.Add(new Vector2(u1.x, u1.y));
				uvs.Add(new Vector2(u0.x, u1.y));
				uvs.Add(new Vector2(u0.x, u0.y));
			}

			if (NGUIText.current.alignment != TextAlignment.Left && indexOffset < verts.size)
			{
				NGUIText.Align(verts, indexOffset, x - NGUIText.current.spacingX);
				indexOffset = verts.size;
			}
			mColors.Clear();
		}
	}

	/// <summary>
	/// Retrieve the specified symbol, optionally creating it if it's missing.
	/// </summary>

	BMSymbol GetSymbol (string sequence, bool createIfMissing)
	{
		for (int i = 0, imax = mSymbols.Count; i < imax; ++i)
		{
			BMSymbol sym = mSymbols[i];
			if (sym.sequence == sequence) return sym;
		}

		if (createIfMissing)
		{
			BMSymbol sym = new BMSymbol();
			sym.sequence = sequence;
			mSymbols.Add(sym);
			return sym;
		}
		return null;
	}

	/// <summary>
	/// Retrieve the symbol at the beginning of the specified sequence, if a match is found.
	/// </summary>

	BMSymbol MatchSymbol (string text, int offset, int textLength)
	{
		// No symbols present
		int count = mSymbols.Count;
		if (count == 0) return null;
		textLength -= offset;

		// Run through all symbols
		for (int i = 0; i < count; ++i)
		{
			BMSymbol sym = mSymbols[i];

			// If the symbol's length is longer, move on
			int symbolLength = sym.length;
			if (symbolLength == 0 || textLength < symbolLength) continue;

			bool match = true;

			// Match the characters
			for (int c = 0; c < symbolLength; ++c)
			{
				if (text[offset + c] != sym.sequence[c])
				{
					match = false;
					break;
				}
			}

			// Match found
			if (match && sym.Validate(atlas)) return sym;
		}
		return null;
	}

	/// <summary>
	/// Add a new symbol to the font.
	/// </summary>

	public void AddSymbol (string sequence, string spriteName)
	{
		BMSymbol symbol = GetSymbol(sequence, true);
		symbol.spriteName = spriteName;
		MarkAsChanged();
	}

	/// <summary>
	/// Remove the specified symbol from the font.
	/// </summary>

	public void RemoveSymbol (string sequence)
	{
		BMSymbol symbol = GetSymbol(sequence, false);
		if (symbol != null) symbols.Remove(symbol);
		MarkAsChanged();
	}

	/// <summary>
	/// Change an existing symbol's sequence to the specified value.
	/// </summary>

	public void RenameSymbol (string before, string after)
	{
		BMSymbol symbol = GetSymbol(before, false);
		if (symbol != null) symbol.sequence = after;
		MarkAsChanged();
	}

	/// <summary>
	/// Whether the specified sprite is being used by the font.
	/// </summary>

	public bool UsesSprite (string s)
	{
		if (!string.IsNullOrEmpty(s))
		{
			if (s.Equals(spriteName))
				return true;

			for (int i = 0, imax = symbols.Count; i < imax; ++i)
			{
				BMSymbol sym = symbols[i];
				if (s.Equals(sym.spriteName))
					return true;
			}
		}
		return false;
	}
}

