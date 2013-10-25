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
[AddComponentMenu("NGUI/UI/Sprite")]
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
				return Mathf.RoundToInt(b.x + b.z);
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
				return Mathf.RoundToInt(b.y + b.w);
			}
			return base.minHeight;
		}
	}

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
	/// Virtual function called by the UIScreen that fills the buffers.
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

	/// <summary>
	/// Sprite's dimensions used for drawing. X = left, Y = bottom, Z = right, W = top.
	/// This function automatically adds 1 pixel on the edge if the sprite's dimensions are not even.
	/// It's used to achieve pixel-perfect sprites even when an odd dimension sprite happens to be centered.
	/// </summary>

	Vector4 drawingDimensions
	{
		get
		{
			if (mSprite == null)
			{
				return new Vector4(0f, 0f, mWidth, mHeight);
			}

			int padLeft = mSprite.paddingLeft;
			int padBottom = mSprite.paddingBottom;
			int padRight = mSprite.paddingRight;
			int padTop = mSprite.paddingTop;

			Vector2 pv = pivotOffset;

			int w = mSprite.width + mSprite.paddingLeft + mSprite.paddingRight;
			int h = mSprite.height + mSprite.paddingBottom + mSprite.paddingTop;

			if ((w & 1) == 1) ++padRight;
			if ((h & 1) == 1) ++padTop;

			float invW = 1f / w;
			float invH = 1f / h;
			Vector4 v = new Vector4(padLeft * invW, padBottom * invH, (w - padRight) * invW, (h - padTop) * invH);

			v.x -= pv.x;
			v.y -= pv.y;
			v.z -= pv.x;
			v.w -= pv.y;

			v.x *= mWidth;
			v.y *= mHeight;
			v.z *= mWidth;
			v.w *= mHeight;

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
		colF.a *= mPanel.alpha;
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

		Vector4 br = border * atlas.pixelSize;
		Vector2 po = pivotOffset;

		float fw = 1f / mWidth;
		float fh = 1f / mHeight;

		Vector2[] v = new Vector2[4];
		v[0] = new Vector2(mSprite.paddingLeft * fw, mSprite.paddingBottom * fh);
		v[3] = new Vector2(1f - mSprite.paddingRight * fw, 1f - mSprite.paddingTop * fh);

		v[1].x = v[0].x + fw * br.x;
		v[1].y = v[0].y + fh * br.y;
		v[2].x = v[3].x - fw * br.z;
		v[2].y = v[3].y - fh * br.w;

		for (int i = 0; i < 4; ++i)
		{
			v[i].x -= po.x;
			v[i].y -= po.y;
			v[i].x *= mWidth;
			v[i].y *= mHeight;
		}

		Vector2[] u = new Vector2[4];
		u[0] = new Vector2(mOuterUV.xMin, mOuterUV.yMin);
		u[1] = new Vector2(mInnerUV.xMin, mInnerUV.yMin);
		u[2] = new Vector2(mInnerUV.xMax, mInnerUV.yMax);
		u[3] = new Vector2(mOuterUV.xMax, mOuterUV.yMax);

		Color colF = color;
		colF.a *= mPanel.alpha;
		Color32 col = atlas.premultipliedAlpha ? NGUITools.ApplyPMA(colF) : colF;

		for (int x = 0; x < 3; ++x)
		{
			int x2 = x + 1;

			for (int y = 0; y < 3; ++y)
			{
				if (!mFillCenter && x == 1 && y == 1) continue;

				int y2 = y + 1;

				verts.Add(new Vector3(v[x].x, v[y].y));
				verts.Add(new Vector3(v[x].x, v[y2].y));
				verts.Add(new Vector3(v[x2].x, v[y2].y));
				verts.Add(new Vector3(v[x2].x, v[y].y));

				uvs.Add(new Vector2(u[x].x, u[y].y));
				uvs.Add(new Vector2(u[x].x, u[y2].y));
				uvs.Add(new Vector2(u[x2].x, u[y2].y));
				uvs.Add(new Vector2(u[x2].x, u[y].y));

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

		Vector2 size = new Vector2(mInnerUV.width * tex.width, mInnerUV.height * tex.height);
		size *= atlas.pixelSize;

		float width = Mathf.Abs(size.x / mWidth);
		float height = Mathf.Abs(size.y / mHeight);

		if (width * height < 0.0001f)
		{
			width = 0.01f;
			height = 0.01f;
		}

		Color colF = color;
		colF.a *= mPanel.alpha;
		Color32 col = atlas.premultipliedAlpha ? NGUITools.ApplyPMA(colF) : colF;

		Vector2 pv = pivotOffset;
		Vector2 min = new Vector2(mInnerUV.xMin, mInnerUV.yMin);
		Vector2 max = new Vector2(mInnerUV.xMax, mInnerUV.yMax);
		Vector2 clipped = max;
		float y1 = 0f;

		while (y1 < 1f)
		{
			float x1 = 0f;
			clipped.x = max.x;
			float y2 = y1 + height;

			if (y2 > 1f)
			{
				clipped.y = min.y + (max.y - min.y) * (1f - y1) / (y2 - y1);
				y2 = 1f;
			}

			while (x1 < 1f)
			{
				float x2 = x1 + width;

				if (x2 > 1f)
				{
					clipped.x = min.x + (max.x - min.x) * (1f - x1) / (x2 - x1);
					x2 = 1f;
				}

				// Convert from normalized (0-1 range) coordinates to pixels
				float fx1 = (x1 - pv.x) * mWidth;
				float fx2 = (x2 - pv.x) * mWidth;
				float fy1 = (y1 - pv.y) * mHeight;
				float fy2 = (y2 - pv.y) * mHeight;

				verts.Add(new Vector3(fx1, fy1));
				verts.Add(new Vector3(fx1, fy2));
				verts.Add(new Vector3(fx2, fy2));
				verts.Add(new Vector3(fx2, fy1));

				uvs.Add(new Vector2(min.x, min.y));
				uvs.Add(new Vector2(min.x, clipped.y));
				uvs.Add(new Vector2(clipped.x, clipped.y));
				uvs.Add(new Vector2(clipped.x, min.y));

				cols.Add(col);
				cols.Add(col);
				cols.Add(col);
				cols.Add(col);

				x1 += width;
			}
			y1 += height;
		}
	}

	/// <summary>
	/// Filled sprite fill function.
	/// </summary>

	protected void FilledFill (BetterList<Vector3> verts, BetterList<Vector2> uvs, BetterList<Color32> cols)
	{
		if (mFillAmount < 0.001f) return;

		Color colF = color;
		colF.a *= mPanel.alpha;
		Color32 col = atlas.premultipliedAlpha ? NGUITools.ApplyPMA(colF) : colF;

		Vector2[] xy = new Vector2[4];
		Vector2[] uv = new Vector2[4];

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

		xy[0] = new Vector2(v.x, v.y);
		xy[1] = new Vector2(v.x, v.w);
		xy[2] = new Vector2(v.z, v.w);
		xy[3] = new Vector2(v.z, v.y);

		uv[0] = new Vector2(tx0, ty0);
		uv[1] = new Vector2(tx0, ty1);
		uv[2] = new Vector2(tx1, ty1);
		uv[3] = new Vector2(tx1, ty0);

		if (mFillAmount < 1f)
		{
			if (mFillDirection == FillDirection.Radial90)
			{
				if (RadialCut(xy, uv, mFillAmount, mInvert, 0))
				{
					for (int i = 0; i < 4; ++i)
					{
						verts.Add(xy[i]);
						uvs.Add(uv[i]);
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

					xy[0].x = Mathf.Lerp(v.x, v.z, fx0);
					xy[1].x = xy[0].x;
					xy[2].x = Mathf.Lerp(v.x, v.z, fx1);
					xy[3].x = xy[2].x;

					xy[0].y = Mathf.Lerp(v.y, v.w, fy0);
					xy[1].y = Mathf.Lerp(v.y, v.w, fy1);
					xy[2].y = xy[1].y;
					xy[3].y = xy[0].y;

					uv[0].x = Mathf.Lerp(tx0, tx1, fx0);
					uv[1].x = uv[0].x;
					uv[2].x = Mathf.Lerp(tx0, tx1, fx1);
					uv[3].x = uv[2].x;

					uv[0].y = Mathf.Lerp(ty0, ty1, fy0);
					uv[1].y = Mathf.Lerp(ty0, ty1, fy1);
					uv[2].y = uv[1].y;
					uv[3].y = uv[0].y;

					float val = !mInvert ? fillAmount * 2f - side : mFillAmount * 2f - (1 - side);

					if (RadialCut(xy, uv, Mathf.Clamp01(val), !mInvert, NGUIMath.RepeatIndex(side + 3, 4)))
					{
						for (int i = 0; i < 4; ++i)
						{
							verts.Add(xy[i]);
							uvs.Add(uv[i]);
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

					xy[0].x = Mathf.Lerp(v.x, v.z, fx0);
					xy[1].x = xy[0].x;
					xy[2].x = Mathf.Lerp(v.x, v.z, fx1);
					xy[3].x = xy[2].x;

					xy[0].y = Mathf.Lerp(v.y, v.w, fy0);
					xy[1].y = Mathf.Lerp(v.y, v.w, fy1);
					xy[2].y = xy[1].y;
					xy[3].y = xy[0].y;

					uv[0].x = Mathf.Lerp(tx0, tx1, fx0);
					uv[1].x = uv[0].x;
					uv[2].x = Mathf.Lerp(tx0, tx1, fx1);
					uv[3].x = uv[2].x;

					uv[0].y = Mathf.Lerp(ty0, ty1, fy0);
					uv[1].y = Mathf.Lerp(ty0, ty1, fy1);
					uv[2].y = uv[1].y;
					uv[3].y = uv[0].y;

					float val = mInvert ?
						mFillAmount * 4f - NGUIMath.RepeatIndex(corner + 2, 4) :
						mFillAmount * 4f - (3 - NGUIMath.RepeatIndex(corner + 2, 4));

					if (RadialCut(xy, uv, Mathf.Clamp01(val), mInvert, NGUIMath.RepeatIndex(corner + 2, 4)))
					{
						for (int i = 0; i < 4; ++i)
						{
							verts.Add(xy[i]);
							uvs.Add(uv[i]);
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
			verts.Add(xy[i]);
			uvs.Add(uv[i]);
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
