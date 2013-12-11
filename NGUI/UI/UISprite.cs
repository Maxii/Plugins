//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Sprite is a textured element in the UI hierarchy.
/// </summary>

[ExecuteInEditMode]
[AddComponentMenu("NGUI/UI/NGUI Sprite")]
public class UISprite : UIWidget
{
	public enum Type
	{
		Simple,
		Sliced,
		Tiled,
		Filled,
	}

	public enum FillDirection
	{
		Horizontal,
		Vertical,
		Radial90,
		Radial180,
		Radial360,
	}

	// Cached and saved values
	[HideInInspector][SerializeField] UIAtlas mAtlas;
	[HideInInspector][SerializeField] string mSpriteName;
	[HideInInspector][SerializeField] bool mFillCenter = true;
	[HideInInspector][SerializeField] Type mType = Type.Simple;
	[HideInInspector][SerializeField] FillDirection mFillDirection = FillDirection.Radial360;
#if !UNITY_3_5
	[Range(0f, 1f)]
#endif
	[HideInInspector][SerializeField] float mFillAmount = 1.0f;
	[HideInInspector][SerializeField] bool mInvert = false;

	protected UISpriteData mSprite;
	protected Rect mInnerUV = new Rect();
	protected Rect mOuterUV = new Rect();
	bool mSpriteSet = false;

	/// <summary>
	/// How the sprite is drawn.
	/// </summary>

	virtual public Type type
	{
		get
		{
			return mType;
		}
		set
		{
			if (mType != value)
			{
				mType = value;
				MarkAsChanged();
			}
		}
	}

	/// <summary>
	/// Retrieve the material used by the font.
	/// </summary>

	public override Material material { get { return (mAtlas != null) ? mAtlas.spriteMaterial : null; } }

	/// <summary>
	/// Atlas used by this widget.
	/// </summary>
 
	public UIAtlas atlas
	{
		get
		{
			return mAtlas;
		}
		set
		{
			if (mAtlas != value)
			{
				RemoveFromPanel();

				mAtlas = value;
				mSpriteSet = false;
				mSprite = null;

				// Automatically choose the first sprite
				if (string.IsNullOrEmpty(mSpriteName))
				{
					if (mAtlas != null && mAtlas.spriteList.Count > 0)
					{
						SetAtlasSprite(mAtlas.spriteList[0]);
						mSpriteName = mSprite.name;
					}
				}

				// Re-link the sprite
				if (!string.IsNullOrEmpty(mSpriteName))
				{
					string sprite = mSpriteName;
					mSpriteName = "";
					spriteName = sprite;
					MarkAsChanged();
				}

				// Make sure the panel knows that the draw calls may have changed
				UIPanel.RebuildAllDrawCalls(false);
			}
		}
	}

	/// <summary>
	/// Sprite within the atlas used to draw this widget.
	/// </summary>
 
	public string spriteName
	{
		get
		{
			return mSpriteName;
		}
		set
		{
			if (string.IsNullOrEmpty(value))
			{
				// If the sprite name hasn't been set yet, no need to do anything
				if (string.IsNullOrEmpty(mSpriteName)) return;

				// Clear the sprite name and the sprite reference
				mSpriteName = "";
				mSprite = null;
				mChanged = true;
				mSpriteSet = false;
			}
			else if (mSpriteName != value)
			{
				// If the sprite name changes, the sprite reference should also be updated
				mSpriteName = value;
				mSprite = null;
				mChanged = true;
				mSpriteSet = false;
			}
		}
	}

	/// <summary>
	/// Is there a valid sprite to work with?
	/// </summary>

	public bool isValid { get { return GetAtlasSprite() != null; } }

	/// <summary>
	/// Whether the center part of the sprite will be filled or not. Turn it off if you want only to borders to show up.
	/// </summary>

	public bool fillCenter { get { return mFillCenter; } set { if (mFillCenter != value) { mFillCenter = value; MarkAsChanged(); } } }

	/// <summary>
	/// Direction of the cut procedure.
	/// </summary>

	public FillDirection fillDirection
	{
		get
		{
			return mFillDirection;
		}
		set
		{
			if (mFillDirection != value)
			{
				mFillDirection = value;
				mChanged = true;
			}
		}
	}

	/// <summary>
	/// Amount of the sprite shown. 0-1 range with 0 being nothing shown, and 1 being the full sprite.
	/// </summary>

	public float fillAmount
	{
		get
		{
			return mFillAmount;
		}
		set
		{
			float val = Mathf.Clamp01(value);

			if (mFillAmount != val)
			{
				mFillAmount = val;
				mChanged = true;
			}
		}
	}

	/// <summary>
	/// Whether the sprite should be filled in the opposite direction.
	/// </summary>

	public bool invert
	{
		get
		{
			return mInvert;
		}
		set
		{
			if (mInvert != value)
			{
				mInvert = value;
				mChanged = true;
			}
		}
	}

	/// <summary>
	/// Sliced sprites generally have a border. X = left, Y = bottom, Z = right, W = top.
	/// </summary>

	public override Vector4 border
	{
		get
		{
			if (type == Type.Sliced)
			{
				UISpriteData sp = GetAtlasSprite();
				if (sp == null) return Vector2.zero;
				return new Vector4(sp.borderLeft, sp.borderBottom, sp.borderRight, sp.borderTop);
			}
			return base.border;
		}
	}

	/// <summary>
	/// Minimum allowed width for this widget.
	/// </summary>

	override public int minWidth
	{
		get
		{
			if (type == Type.Sliced)
			{
				Vector4 b = border;
				if (atlas != null) b *= atlas.pixelSize;
				int min = Mathf.RoundToInt(b.x + b.z);
				return Mathf.Max(base.minWidth, ((min & 1) == 1) ? min + 1 : min);
			}
			return base.minWidth;
		}
	}

	/// <summary>
	/// Minimum allowed height for this widget.
	/// </summary>

	override public int minHeight
	{
		get
		{
			if (type == Type.Sliced)
			{
				Vector4 b = border;
				if (atlas != null) b *= atlas.pixelSize;
				int min = Mathf.RoundToInt(b.y + b.w);
				return Mathf.Max(base.minHeight, ((min & 1) == 1) ? min + 1 : min);
			}
			return base.minHeight;
		}
	}

#if UNITY_EDITOR
	/// <summary>
	/// Keep sane values.
	/// </summary>

	protected override void OnValidate ()
	{
		base.OnValidate();
		mFillAmount = Mathf.Clamp01(mFillAmount);
	}
#endif

	/// <summary>
	/// Retrieve the atlas sprite referenced by the spriteName field.
	/// </summary>

	public UISpriteData GetAtlasSprite ()
	{
		if (!mSpriteSet) mSprite = null;

		if (mSprite == null && mAtlas != null)
		{
			if (!string.IsNullOrEmpty(mSpriteName))
			{
				UISpriteData sp = mAtlas.GetSprite(mSpriteName);
				if (sp == null) return null;
				SetAtlasSprite(sp);
			}

			if (mSprite == null && mAtlas.spriteList.Count > 0)
			{
				UISpriteData sp = mAtlas.spriteList[0];
				if (sp == null) return null;
				SetAtlasSprite(sp);

				if (mSprite == null)
				{
					Debug.LogError(mAtlas.name + " seems to have a null sprite!");
					return null;
				}
				mSpriteName = mSprite.name;
			}
		}
		return mSprite;
	}

	/// <summary>
	/// Set the atlas sprite directly.
	/// </summary>

	protected void SetAtlasSprite (UISpriteData sp)
	{
		mChanged = true;
		mSpriteSet = true;

		if (sp != null)
		{
			mSprite = sp;
			mSpriteName = mSprite.name;
		}
		else
		{
			mSpriteName = (mSprite != null) ? mSprite.name : "";
			mSprite = sp;
		}
	}

	/// <summary>
	/// Adjust the scale of the widget to make it pixel-perfect.
	/// </summary>

	public override void MakePixelPerfect ()
	{
		if (!isValid) return;
		base.MakePixelPerfect();

		UISprite.Type t = type;

		if (t == Type.Simple || t == Type.Filled)
		{
			Texture tex = mainTexture;
			UISpriteData sp = GetAtlasSprite();

			if (tex != null && sp != null)
			{
				int x = Mathf.RoundToInt(atlas.pixelSize * (sp.width + sp.paddingLeft + sp.paddingRight));
				int y = Mathf.RoundToInt(atlas.pixelSize * (sp.height + sp.paddingTop + sp.paddingBottom));
				
				if ((x & 1) == 1) ++x;
				if ((y & 1) == 1) ++y;

				width = x;
				height = y;
			}
		}
	}

	/// <summary>
	/// Update the UV coordinates.
	/// </summary>

	public override void Update ()
	{
		base.Update();

		if (mChanged || !mSpriteSet)
		{
			mSpriteSet = true;
			mSprite = null;
			mChanged = true;
		}
	}

	/// <summary>
	/// Virtual function called by the UIPanel that fills the buffers.
	/// </summary>

	public override void OnFill (BetterList<Vector3> verts, BetterList<Vector2> uvs, BetterList<Color32> cols)
	{
		Texture tex = mainTexture;

		if (tex != null)
		{
			if (mSprite == null) mSprite = atlas.GetSprite(spriteName);
			if (mSprite == null) return;

			mOuterUV.Set(mSprite.x, mSprite.y, mSprite.width, mSprite.height);
			mInnerUV.Set(mSprite.x + mSprite.borderLeft, mSprite.y + mSprite.borderTop,
				mSprite.width - mSprite.borderLeft - mSprite.borderRight,
				mSprite.height - mSprite.borderBottom - mSprite.borderTop);

			mOuterUV = NGUIMath.ConvertToTexCoords(mOuterUV, tex.width, tex.height);
			mInnerUV = NGUIMath.ConvertToTexCoords(mInnerUV, tex.width, tex.height);
		}

		switch (type)
		{
			case Type.Simple:
			SimpleFill(verts, uvs, cols);
			break;

			case Type.Sliced:
			SlicedFill(verts, uvs, cols);
			break;

			case Type.Filled:
			FilledFill(verts, uvs, cols);
			break;

			case Type.Tiled:
			TiledFill(verts, uvs, cols);
			break;
		}
	}

#region Various fill functions

	// Static variables to reduce garbage collection
	static Vector2[] mTempPos = new Vector2[4];
	static Vector2[] mTempUVs = new Vector2[4];

	/// <summary>
	/// Sprite's dimensions used for drawing. X = left, Y = bottom, Z = right, W = top.
	/// This function automatically adds 1 pixel on the edge if the sprite's dimensions are not even.
	/// It's used to achieve pixel-perfect sprites even when an odd dimension sprite happens to be centered.
	/// </summary>

	public override Vector4 drawingDimensions
	{
		get
		{
			Vector2 offset = pivotOffset;

			float x0 = -offset.x * mWidth;
			float y0 = -offset.y * mHeight;
			float x1 = x0 + mWidth;
			float y1 = y0 + mHeight;

			if (mSprite != null)
			{
				int padLeft = mSprite.paddingLeft;
				int padBottom = mSprite.paddingBottom;
				int padRight = mSprite.paddingRight;
				int padTop = mSprite.paddingTop;

				int w = mSprite.width + padLeft + padRight;
				int h = mSprite.height + padBottom + padTop;

				if (mType != Type.Sliced)
				{
					if ((w & 1) != 0) ++padRight;
					if ((h & 1) != 0) ++padTop;

					float px = (1f / w) * mWidth;
					float py = (1f / h) * mHeight;

					x0 += padLeft * px;
					x1 -= padRight * px;
					y0 += padBottom * py;
					y1 -= padTop * py;
				}
				else
				{
					x0 += padLeft;
					x1 -= padRight;
					y0 += padBottom;
					y1 -= padTop;
				}
			}

			Vector4 v = new Vector4(
				mDrawRegion.x == 0f ? x0 : Mathf.Lerp(x0, x1, mDrawRegion.x),
				mDrawRegion.y == 0f ? y0 : Mathf.Lerp(y0, y1, mDrawRegion.y),
				mDrawRegion.z == 1f ? x1 : Mathf.Lerp(x0, x1, mDrawRegion.z),
				mDrawRegion.w == 1f ? y1 : Mathf.Lerp(y0, y1, mDrawRegion.w));

			float mw = minWidth;
			float mh = minHeight;

			if (v.z - v.x < mw)
			{
				float center = (v.x + v.z) * 0.5f;
				v.x = Mathf.Round(center - mw * 0.5f);
				v.z = v.x + mw;
			}

			if (v.w - v.y < mh)
			{
				float center = (v.y + v.w) * 0.5f;
				v.y = Mathf.Round(center - mh * 0.5f);
				v.w = v.y + mw;
			}
			return v;
		}
	}

	/// <summary>
	/// Regular sprite fill function is quite simple.
	/// </summary>

	protected void SimpleFill (BetterList<Vector3> verts, BetterList<Vector2> uvs, BetterList<Color32> cols)
	{
		Vector2 uv0 = new Vector2(mOuterUV.xMin, mOuterUV.yMin);
		Vector2 uv1 = new Vector2(mOuterUV.xMax, mOuterUV.yMax);

		Vector4 v = drawingDimensions;

		verts.Add(new Vector3(v.x, v.y));
		verts.Add(new Vector3(v.x, v.w));
		verts.Add(new Vector3(v.z, v.w));
		verts.Add(new Vector3(v.z, v.y));

		uvs.Add(uv0);
		uvs.Add(new Vector2(uv0.x, uv1.y));
		uvs.Add(uv1);
		uvs.Add(new Vector2(uv1.x, uv0.y));

		Color colF = color;
		colF.a *= mPanel.finalAlpha;
		Color32 col = atlas.premultipliedAlpha ? NGUITools.ApplyPMA(colF) : colF;
		
		cols.Add(col);
		cols.Add(col);
		cols.Add(col);
		cols.Add(col);
	}

	/// <summary>
	/// Sliced sprite fill function is more complicated as it generates 9 quads instead of 1.
	/// </summary>

	protected void SlicedFill (BetterList<Vector3> verts, BetterList<Vector2> uvs, BetterList<Color32> cols)
	{
		if (mSprite == null) return;

		if (!mSprite.hasBorder)
		{
			SimpleFill(verts, uvs, cols);
			return;
		}

		Vector4 dr = drawingDimensions;
		Vector4 br = border * atlas.pixelSize;

		mTempPos[0].x = dr.x;
		mTempPos[0].y = dr.y;
		mTempPos[3].x = dr.z;
		mTempPos[3].y = dr.w;

		mTempPos[1].x = mTempPos[0].x + br.x;
		mTempPos[1].y = mTempPos[0].y + br.y;
		mTempPos[2].x = mTempPos[3].x - br.z;
		mTempPos[2].y = mTempPos[3].y - br.w;

		mTempUVs[0] = new Vector2(mOuterUV.xMin, mOuterUV.yMin);
		mTempUVs[1] = new Vector2(mInnerUV.xMin, mInnerUV.yMin);
		mTempUVs[2] = new Vector2(mInnerUV.xMax, mInnerUV.yMax);
		mTempUVs[3] = new Vector2(mOuterUV.xMax, mOuterUV.yMax);

		Color colF = color;
		colF.a *= mPanel.finalAlpha;
		Color32 col = atlas.premultipliedAlpha ? NGUITools.ApplyPMA(colF) : colF;

		for (int x = 0; x < 3; ++x)
		{
			int x2 = x + 1;

			for (int y = 0; y < 3; ++y)
			{
				if (!mFillCenter && x == 1 && y == 1) continue;

				int y2 = y + 1;

				verts.Add(new Vector3(mTempPos[x].x, mTempPos[y].y));
				verts.Add(new Vector3(mTempPos[x].x, mTempPos[y2].y));
				verts.Add(new Vector3(mTempPos[x2].x, mTempPos[y2].y));
				verts.Add(new Vector3(mTempPos[x2].x, mTempPos[y].y));

				uvs.Add(new Vector2(mTempUVs[x].x, mTempUVs[y].y));
				uvs.Add(new Vector2(mTempUVs[x].x, mTempUVs[y2].y));
				uvs.Add(new Vector2(mTempUVs[x2].x, mTempUVs[y2].y));
				uvs.Add(new Vector2(mTempUVs[x2].x, mTempUVs[y].y));

				cols.Add(col);
				cols.Add(col);
				cols.Add(col);
				cols.Add(col);
			}
		}
	}

	/// <summary>
	/// Tiled sprite fill function.
	/// </summary>

	protected void TiledFill (BetterList<Vector3> verts, BetterList<Vector2> uvs, BetterList<Color32> cols)
	{
		Texture tex = material.mainTexture;
		if (tex == null) return;

		Vector4 dr = drawingDimensions;
		Vector2 size = new Vector2(mInnerUV.width * tex.width, mInnerUV.height * tex.height);
		size *= atlas.pixelSize;

		Color colF = color;
		colF.a *= mPanel.finalAlpha;
		Color32 col = atlas.premultipliedAlpha ? NGUITools.ApplyPMA(colF) : colF;

		float x0 = dr.x;
		float y0 = dr.y;

		float u0 = mInnerUV.xMin;
		float v0 = mInnerUV.yMin;

		while (y0 < dr.w)
		{
			x0 = dr.x;
			float y1 = y0 + size.y;
			float v1 = mInnerUV.yMax;

			if (y1 > dr.w)
			{
				v1 = Mathf.Lerp(mInnerUV.yMin, mInnerUV.yMax, (dr.w - y0) / size.y);
				y1 = dr.w;
			}

			while (x0 < dr.z)
			{
				float x1 = x0 + size.x;
				float u1 = mInnerUV.xMax;

				if (x1 > dr.z)
				{
					u1 = Mathf.Lerp(mInnerUV.xMin, mInnerUV.xMax, (dr.z - x0) / size.x);
					x1 = dr.z;
				}

				verts.Add(new Vector3(x0, y0));
				verts.Add(new Vector3(x0, y1));
				verts.Add(new Vector3(x1, y1));
				verts.Add(new Vector3(x1, y0));

				uvs.Add(new Vector2(u0, v0));
				uvs.Add(new Vector2(u0, v1));
				uvs.Add(new Vector2(u1, v1));
				uvs.Add(new Vector2(u1, v0));

				cols.Add(col);
				cols.Add(col);
				cols.Add(col);
				cols.Add(col);

				x0 += size.x;
			}
			y0 += size.y;
		}
	}

	/// <summary>
	/// Filled sprite fill function.
	/// </summary>

	protected void FilledFill (BetterList<Vector3> verts, BetterList<Vector2> uvs, BetterList<Color32> cols)
	{
		if (mFillAmount < 0.001f) return;

		Color colF = color;
		colF.a *= mPanel.finalAlpha;
		Color32 col = atlas.premultipliedAlpha ? NGUITools.ApplyPMA(colF) : colF;
		Vector4 v = drawingDimensions;

		float tx0 = mOuterUV.xMin;
		float ty0 = mOuterUV.yMin;
		float tx1 = mOuterUV.xMax;
		float ty1 = mOuterUV.yMax;

		// Horizontal and vertical filled sprites are simple -- just end the sprite prematurely
		if (mFillDirection == FillDirection.Horizontal || mFillDirection == FillDirection.Vertical)
		{
			if (mFillDirection == FillDirection.Horizontal)
			{
				float fill = (tx1 - tx0) * mFillAmount;

				if (mInvert)
				{
					v.x = v.z - (v.z - v.x) * mFillAmount;
					tx0 = tx1 - fill;
				}
				else
				{
					v.z = v.x + (v.z - v.x) * mFillAmount;
					tx1 = tx0 + fill;
				}
			}
			else if (mFillDirection == FillDirection.Vertical)
			{
				float fill = (ty1 - ty0) * mFillAmount;

				if (mInvert)
				{
					v.y = v.w - (v.w - v.y) * mFillAmount;
					ty0 = ty1 - fill;
				}
				else
				{
					v.w = v.y + (v.w - v.y) * mFillAmount;
					ty1 = ty0 + fill;
				}
			}
		}

		mTempPos[0] = new Vector2(v.x, v.y);
		mTempPos[1] = new Vector2(v.x, v.w);
		mTempPos[2] = new Vector2(v.z, v.w);
		mTempPos[3] = new Vector2(v.z, v.y);

		mTempUVs[0] = new Vector2(tx0, ty0);
		mTempUVs[1] = new Vector2(tx0, ty1);
		mTempUVs[2] = new Vector2(tx1, ty1);
		mTempUVs[3] = new Vector2(tx1, ty0);

		if (mFillAmount < 1f)
		{
			if (mFillDirection == FillDirection.Radial90)
			{
				if (RadialCut(mTempPos, mTempUVs, mFillAmount, mInvert, 0))
				{
					for (int i = 0; i < 4; ++i)
					{
						verts.Add(mTempPos[i]);
						uvs.Add(mTempUVs[i]);
						cols.Add(col);
					}
				}
				return;
			}

			if (mFillDirection == FillDirection.Radial180)
			{
				for (int side = 0; side < 2; ++side)
				{
					float fx0, fx1, fy0, fy1;

					fy0 = 0f;
					fy1 = 1f;

					if (side == 0) { fx0 = 0f; fx1 = 0.5f; }
					else { fx0 = 0.5f; fx1 = 1f; }

					mTempPos[0].x = Mathf.Lerp(v.x, v.z, fx0);
					mTempPos[1].x = mTempPos[0].x;
					mTempPos[2].x = Mathf.Lerp(v.x, v.z, fx1);
					mTempPos[3].x = mTempPos[2].x;

					mTempPos[0].y = Mathf.Lerp(v.y, v.w, fy0);
					mTempPos[1].y = Mathf.Lerp(v.y, v.w, fy1);
					mTempPos[2].y = mTempPos[1].y;
					mTempPos[3].y = mTempPos[0].y;

					mTempUVs[0].x = Mathf.Lerp(tx0, tx1, fx0);
					mTempUVs[1].x = mTempUVs[0].x;
					mTempUVs[2].x = Mathf.Lerp(tx0, tx1, fx1);
					mTempUVs[3].x = mTempUVs[2].x;

					mTempUVs[0].y = Mathf.Lerp(ty0, ty1, fy0);
					mTempUVs[1].y = Mathf.Lerp(ty0, ty1, fy1);
					mTempUVs[2].y = mTempUVs[1].y;
					mTempUVs[3].y = mTempUVs[0].y;

					float val = !mInvert ? fillAmount * 2f - side : mFillAmount * 2f - (1 - side);

					if (RadialCut(mTempPos, mTempUVs, Mathf.Clamp01(val), !mInvert, NGUIMath.RepeatIndex(side + 3, 4)))
					{
						for (int i = 0; i < 4; ++i)
						{
							verts.Add(mTempPos[i]);
							uvs.Add(mTempUVs[i]);
							cols.Add(col);
						}
					}
				}
				return;
			}

			if (mFillDirection == FillDirection.Radial360)
			{
				for (int corner = 0; corner < 4; ++corner)
				{
					float fx0, fx1, fy0, fy1;

					if (corner < 2) { fx0 = 0f; fx1 = 0.5f; }
					else { fx0 = 0.5f; fx1 = 1f; }

					if (corner == 0 || corner == 3) { fy0 = 0f; fy1 = 0.5f; }
					else { fy0 = 0.5f; fy1 = 1f; }

					mTempPos[0].x = Mathf.Lerp(v.x, v.z, fx0);
					mTempPos[1].x = mTempPos[0].x;
					mTempPos[2].x = Mathf.Lerp(v.x, v.z, fx1);
					mTempPos[3].x = mTempPos[2].x;

					mTempPos[0].y = Mathf.Lerp(v.y, v.w, fy0);
					mTempPos[1].y = Mathf.Lerp(v.y, v.w, fy1);
					mTempPos[2].y = mTempPos[1].y;
					mTempPos[3].y = mTempPos[0].y;

					mTempUVs[0].x = Mathf.Lerp(tx0, tx1, fx0);
					mTempUVs[1].x = mTempUVs[0].x;
					mTempUVs[2].x = Mathf.Lerp(tx0, tx1, fx1);
					mTempUVs[3].x = mTempUVs[2].x;

					mTempUVs[0].y = Mathf.Lerp(ty0, ty1, fy0);
					mTempUVs[1].y = Mathf.Lerp(ty0, ty1, fy1);
					mTempUVs[2].y = mTempUVs[1].y;
					mTempUVs[3].y = mTempUVs[0].y;

					float val = mInvert ?
						mFillAmount * 4f - NGUIMath.RepeatIndex(corner + 2, 4) :
						mFillAmount * 4f - (3 - NGUIMath.RepeatIndex(corner + 2, 4));

					if (RadialCut(mTempPos, mTempUVs, Mathf.Clamp01(val), mInvert, NGUIMath.RepeatIndex(corner + 2, 4)))
					{
						for (int i = 0; i < 4; ++i)
						{
							verts.Add(mTempPos[i]);
							uvs.Add(mTempUVs[i]);
							cols.Add(col);
						}
					}
				}
				return;
			}
		}

		// Fill the buffer with the quad for the sprite
		for (int i = 0; i < 4; ++i)
		{
			verts.Add(mTempPos[i]);
			uvs.Add(mTempUVs[i]);
			cols.Add(col);
		}
	}

	/// <summary>
	/// Adjust the specified quad, making it be radially filled instead.
	/// </summary>

	static bool RadialCut (Vector2[] xy, Vector2[] uv, float fill, bool invert, int corner)
	{
		// Nothing to fill
		if (fill < 0.001f) return false;

		// Even corners invert the fill direction
		if ((corner & 1) == 1) invert = !invert;

		// Nothing to adjust
		if (!invert && fill > 0.999f) return true;

		// Convert 0-1 value into 0 to 90 degrees angle in radians
		float angle = Mathf.Clamp01(fill);
		if (invert) angle = 1f - angle;
		angle *= 90f * Mathf.Deg2Rad;

		// Calculate the effective X and Y factors
		float cos = Mathf.Cos(angle);
		float sin = Mathf.Sin(angle);

		RadialCut(xy, cos, sin, invert, corner);
		RadialCut(uv, cos, sin, invert, corner);
		return true;
	}

	/// <summary>
	/// Adjust the specified quad, making it be radially filled instead.
	/// </summary>

	static void RadialCut (Vector2[] xy, float cos, float sin, bool invert, int corner)
	{
		int i0 = corner;
		int i1 = NGUIMath.RepeatIndex(corner + 1, 4);
		int i2 = NGUIMath.RepeatIndex(corner + 2, 4);
		int i3 = NGUIMath.RepeatIndex(corner + 3, 4);

		if ((corner & 1) == 1)
		{
			if (sin > cos)
			{
				cos /= sin;
				sin = 1f;

				if (invert)
				{
					xy[i1].x = Mathf.Lerp(xy[i0].x, xy[i2].x, cos);
					xy[i2].x = xy[i1].x;
				}
			}
			else if (cos > sin)
			{
				sin /= cos;
				cos = 1f;

				if (!invert)
				{
					xy[i2].y = Mathf.Lerp(xy[i0].y, xy[i2].y, sin);
					xy[i3].y = xy[i2].y;
				}
			}
			else
			{
				cos = 1f;
				sin = 1f;
			}

			if (!invert) xy[i3].x = Mathf.Lerp(xy[i0].x, xy[i2].x, cos);
			else xy[i1].y = Mathf.Lerp(xy[i0].y, xy[i2].y, sin);
		}
		else
		{
			if (cos > sin)
			{
				sin /= cos;
				cos = 1f;

				if (!invert)
				{
					xy[i1].y = Mathf.Lerp(xy[i0].y, xy[i2].y, sin);
					xy[i2].y = xy[i1].y;
				}
			}
			else if (sin > cos)
			{
				cos /= sin;
				sin = 1f;

				if (invert)
				{
					xy[i2].x = Mathf.Lerp(xy[i0].x, xy[i2].x, cos);
					xy[i3].x = xy[i2].x;
				}
			}
			else
			{
				cos = 1f;
				sin = 1f;
			}

			if (invert) xy[i3].y = Mathf.Lerp(xy[i0].y, xy[i2].y, sin);
			else xy[i1].x = Mathf.Lerp(xy[i0].x, xy[i2].x, cos);
		}
	}
#endregion
}
