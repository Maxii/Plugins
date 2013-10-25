//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// If you don't have or don't wish to create an atlas, you can simply use this script to draw a texture.
/// Keep in mind though that this will create an extra draw call with each UITexture present, so it's
/// best to use it only for backgrounds or temporary visible widgets.
/// </summary>

[ExecuteInEditMode]
[AddComponentMenu("NGUI/UI/Texture")]
public class UITexture : UIWidget
{
	[HideInInspector][SerializeField] Rect mRect = new Rect(0f, 0f, 1f, 1f);
	[HideInInspector][SerializeField] Shader mShader;
	[HideInInspector][SerializeField] Texture mTexture;
	[HideInInspector][SerializeField] Material mMat;

	bool mCreatingMat = false;
	Material mDynamicMat = null;
	int mPMA = -1;

	/// <summary>
	/// UV rectangle used by the texture.
	/// </summary>

	public Rect uvRect
	{
		get
		{
			return mRect;
		}
		set
		{
			if (mRect != value)
			{
				mRect = value;
				MarkAsChanged();
			}
		}
	}

	/// <summary>
	/// Shader used by the texture when creating a dynamic material (when the texture was specified, but the material was not).
	/// </summary>

	public Shader shader
	{
		get
		{
			if (mShader == null)
			{
				Material mat = material;
				if (mat != null) mShader = mat.shader;
				if (mShader == null) mShader = Shader.Find("Unlit/Texture");
			}
			return mShader;
		}
		set
		{
			if (mShader != value)
			{
				mShader = value;
				Material mat = material;
				if (mat != null) mat.shader = value;
				mPMA = -1;
			}
		}
	}

	/// <summary>
	/// Whether the texture has created its material dynamically.
	/// </summary>

	public bool hasDynamicMaterial { get { return mDynamicMat != null; } }

	/// <summary>
	/// Automatically destroy the dynamically-created material.
	/// </summary>

	public override Material material
	{
		get
		{
			if (mMat != null) return mMat;
			if (mDynamicMat != null) return mDynamicMat;

			if (!mCreatingMat && mDynamicMat == null)
			{
				mCreatingMat = true;

				if (mShader == null) mShader = Shader.Find("Unlit/Transparent Colored");

				Cleanup();

				mDynamicMat = new Material(mShader);
				mDynamicMat.hideFlags = HideFlags.DontSave;
				mDynamicMat.mainTexture = mTexture;
				mPMA = 0;
				mCreatingMat = false;
			}
			return mDynamicMat;
		}
		set
		{
			if (mMat != value)
			{
				Cleanup();
				mMat = value;
				mPMA = -1;
				MarkAsChanged();
			}
		}
	}

	/// <summary>
	/// Whether the texture is using a premultiplied alpha material.
	/// </summary>

	public bool premultipliedAlpha
	{
		get
		{
			if (mPMA == -1)
			{
				Material mat = material;
				mPMA = (mat != null && mat.shader != null && mat.shader.name.Contains("Premultiplied")) ? 1 : 0;
			}
			return (mPMA == 1);
		}
	}

	/// <summary>
	/// Texture used by the UITexture. You can set it directly, without the need to specify a material.
	/// </summary>

	public override Texture mainTexture
	{
		get
		{
			if (mMat != null) return mMat.mainTexture;
			if (mTexture != null) return mTexture;
			return null;
		}
		set
		{
			RemoveFromPanel();

			Material mat = material;

			if (mat != null)
			{
				mPanel = null;
				mTexture = value;
				mat.mainTexture = value;
				MarkAsChangedLite();

				if (enabled) CreatePanel();
				if (mPanel != null) mPanel.Refresh();
			}
		}
	}

	/// <summary>
	/// Widget's dimensions used for drawing. X = left, Y = bottom, Z = right, W = top.
	/// This function automatically adds 1 pixel on the edge if the texture's dimensions are not even.
	/// It's used to achieve pixel-perfect sprites even when an odd dimension widget happens to be centered.
	/// </summary>

	Vector4 drawingDimensions
	{
		get
		{
			float left = 0f;
			float bottom = 0f;
			float right = 0f;
			float top = 0f;

			Texture tex = mainTexture;
			Rect rect = (tex != null) ? new Rect(0f, 0f, tex.width, tex.height) : new Rect(0f, 0f, mWidth, mHeight);

			Vector2 pv = pivotOffset;
			int w = Mathf.RoundToInt(rect.width);
			int h = Mathf.RoundToInt(rect.height);

			float paddedW = ((w & 1) == 0) ? w : w + 1;
			float paddedH = ((h & 1) == 0) ? h : h + 1;

			Vector4 v = new Vector4(
				left / paddedW,
				bottom / paddedH,
				(w - right) / paddedW,
				(h - top) / paddedH);

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
	/// Clean up.
	/// </summary>

	void OnDestroy () { Cleanup(); }

	void Cleanup ()
	{
		if (mDynamicMat != null)
		{
			NGUITools.Destroy(mDynamicMat);
			mDynamicMat = null;
		}
	}

	/// <summary>
	/// Adjust the scale of the widget to make it pixel-perfect.
	/// </summary>

	public override void MakePixelPerfect ()
	{
		Texture tex = mainTexture;

		if (tex != null)
		{
			int x = tex.width;
			if ((x & 1) == 1) ++x;

			int y = tex.height;
			if ((y & 1) == 1) ++y;

			width = x;
			height = y;
		}
		base.MakePixelPerfect();
	}

	/// <summary>
	/// Virtual function called by the UIScreen that fills the buffers.
	/// </summary>

	public override void OnFill (BetterList<Vector3> verts, BetterList<Vector2> uvs, BetterList<Color32> cols)
	{
		Color colF = color;
		colF.a *= mPanel.alpha;
		Color32 col = premultipliedAlpha ? NGUITools.ApplyPMA(colF) : colF;

		Vector4 v = drawingDimensions;

		verts.Add(new Vector3(v.x, v.y));
		verts.Add(new Vector3(v.x, v.w));
		verts.Add(new Vector3(v.z, v.w));
		verts.Add(new Vector3(v.z, v.y));

		uvs.Add(new Vector2(mRect.xMin, mRect.yMin));
		uvs.Add(new Vector2(mRect.xMin, mRect.yMax));
		uvs.Add(new Vector2(mRect.xMax, mRect.yMax));
		uvs.Add(new Vector2(mRect.xMax, mRect.yMin));

		cols.Add(col);
		cols.Add(col);
		cols.Add(col);
		cols.Add(col);
	}
}
