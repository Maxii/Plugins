//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// This is an internally-created script used by the UI system. You shouldn't be attaching it manually.
/// </summary>

[ExecuteInEditMode]
[AddComponentMenu("NGUI/Internal/Draw Call")]
public class UIDrawCall : MonoBehaviour
{
	/// <summary>
	/// All draw calls created by the panels.
	/// </summary>

	static public BetterList<UIDrawCall> list = new BetterList<UIDrawCall>();

	public enum Clipping : int
	{
		None = 0,
		AlphaClip = 2,	// Adjust the alpha, compatible with all devices
		SoftClip = 3,	// Alpha-based clipping with a softened edge
	}

	Transform		mTrans;			// Cached transform
	Material		mSharedMat;		// Material used by this screen
	Mesh			mMesh0;			// First generated mesh
	Mesh			mMesh1;			// Second generated mesh
	MeshFilter		mFilter;		// Mesh filter for this draw call
	MeshRenderer	mRen;			// Mesh renderer for this screen
	Clipping		mClipping;		// Clipping mode
	Vector4			mClipRange;		// Clipping, if used
	Vector2			mClipSoft;		// Clipping softness
	Material		mMat;			// Instantiated material
	int[]			mIndices;		// Cached indices

	bool mDirty = false;
	bool mReset = true;
	bool mEven = true;
	int mRenderQueue = 0;

	/// <summary>
	/// Panel managing this draw call.
	/// </summary>

	public UIPanel panel { get; set; }

	/// <summary>
	/// Whether the draw call needs to be re-created.
	/// </summary>

	public bool isDirty { get { return mDirty; } set { mDirty = value; } }

	/// <summary>
	/// Render queue used by the draw call.
	/// </summary>

	public int renderQueue
	{
		get
		{
			return mRenderQueue;
		}
		set
		{
			if (mRenderQueue != value)
			{
				mRenderQueue = value;

				if (mMat != null && mSharedMat != null)
				{
					mMat.renderQueue = mSharedMat.renderQueue + value;
#if UNITY_EDITOR
					if (mRen != null) mRen.enabled = isActive;
#endif
				}
			}
		}
	}

#if UNITY_EDITOR
	public string keyName { get { return "Draw Call " + (1 + mRenderQueue); } }

	public bool showDetails { get { return UnityEditor.EditorPrefs.GetBool(keyName, true); } }

	/// <summary>
	/// Whether the draw call is currently active.
	/// </summary>

	public bool isActive
	{
		get
		{
			return mActive;
			//return UnityEditor.EditorPrefs.GetBool(keyName, true);
		}
		set
		{
			if (mActive != value)
			{
				mActive = value;

				if (mRen != null)
				{
					mRen.enabled = value;
					UnityEditor.EditorUtility.SetDirty(gameObject);
				}
			}
			//UnityEditor.EditorPrefs.SetBool(keyName, value);
			
			//if (mRen != null)
			//{
			//    mRen.enabled = value;
			//    UnityEditor.EditorUtility.SetDirty(gameObject);
			//}
		}
	}
	bool mActive = true;
#endif

	/// <summary>
	/// Transform is cached for speed and efficiency.
	/// </summary>

	public Transform cachedTransform { get { if (mTrans == null) mTrans = transform; return mTrans; } }

	/// <summary>
	/// Material used by this screen.
	/// </summary>

	public Material material { get { return mSharedMat; } set { mSharedMat = value; } }

	/// <summary>
	/// Texture used by the material.
	/// </summary>

	public Texture mainTexture { get { return (mMat != null) ? mMat.mainTexture : null; } set { if (mMat != null) mMat.mainTexture = value; } }

	/// <summary>
	/// The number of triangles in this draw call.
	/// </summary>

	public int triangles
	{
		get
		{
			Mesh mesh = mEven ? mMesh0 : mMesh1;
			return (mesh != null) ? mesh.vertexCount >> 1 : 0;
		}
	}

	/// <summary>
	/// Whether the draw call is currently using a clipped shader.
	/// </summary>

	public bool isClipped { get { return mClipping != Clipping.None; } }

	/// <summary>
	/// Clipping used by the draw call
	/// </summary>

	public Clipping clipping { get { return mClipping; } set { if (mClipping != value) { mClipping = value; mReset = true; } } }

	/// <summary>
	/// Clip range set by the panel -- used with a shader that has the "_ClipRange" property.
	/// </summary>

	public Vector4 clipRange { get { return mClipRange; } set { mClipRange = value; } }

	/// <summary>
	/// Clipping softness factor, if soft clipping is used.
	/// </summary>

	public Vector2 clipSoftness { get { return mClipSoft; } set { mClipSoft = value; } }

	/// <summary>
	/// Returns a mesh for writing into. The mesh is double-buffered as it gets the best performance on iOS devices.
	/// http://forum.unity3d.com/threads/118723-Huge-performance-loss-in-Mesh.CreateVBO-for-dynamic-meshes-IOS
	/// </summary>

	Mesh GetMesh (ref bool rebuildIndices, int vertexCount)
	{
		mEven = !mEven;

		if (mEven)
		{
			if (mMesh0 == null)
			{
				mMesh0 = new Mesh();
				mMesh0.hideFlags = HideFlags.DontSave;
				mMesh0.name = "Mesh0 for " + mSharedMat.name;
#if !UNITY_3_5
				mMesh0.MarkDynamic();
#endif
				rebuildIndices = true;
			}
			else if (rebuildIndices || mMesh0.vertexCount != vertexCount)
			{
				rebuildIndices = true;
				mMesh0.Clear();
			}
			return mMesh0;
		}
		else if (mMesh1 == null)
		{
			mMesh1 = new Mesh();
			mMesh1.hideFlags = HideFlags.DontSave;
			mMesh1.name = "Mesh1 for " + mSharedMat.name;
#if !UNITY_3_5
			mMesh1.MarkDynamic();
#endif
			rebuildIndices = true;
		}
		else if (rebuildIndices || mMesh1.vertexCount != vertexCount)
		{
			rebuildIndices = true;
			mMesh1.Clear();
		}
		return mMesh1;
	}

	/// <summary>
	/// Rebuild the draw call's material.
	/// </summary>

	public void RebuildMaterial ()
	{
		NGUITools.DestroyImmediate(mMat);
		mMat = new Material(mSharedMat);
		mMat.hideFlags = HideFlags.DontSave;
		mMat.CopyPropertiesFromMaterial(mSharedMat);
		mMat.renderQueue = mSharedMat.renderQueue + mRenderQueue;
	}

	/// <summary>
	/// Update the renderer's materials.
	/// </summary>

	void UpdateMaterials ()
	{
		bool useClipping = (mClipping != Clipping.None);

		// Create a temporary material
		if (mMat == null) RebuildMaterial();

		// If clipping should be used, we need to find a replacement shader
		if (useClipping && mClipping != Clipping.None)
		{
			Shader shader = null;
			const string alpha	= " (AlphaClip)";
			const string soft	= " (SoftClip)";

			// Figure out the normal shader's name
			string shaderName = mSharedMat.shader.name;
			shaderName = shaderName.Replace(alpha, "");
			shaderName = shaderName.Replace(soft, "");

			// Try to find the new shader
			if (mClipping == Clipping.SoftClip) shader = Shader.Find(shaderName + soft);
			else shader = Shader.Find(shaderName + alpha);

			// If there is a valid shader, assign it to the custom material
			if (shader != null)
			{
				mMat.shader = shader;
			}
			else
			{
				mClipping = Clipping.None;
				Debug.LogError(shaderName + " doesn't have a clipped shader version for " + mClipping);
			}
		}

		if (mRen.sharedMaterial != mMat)
		{
			mRen.sharedMaterials = new Material[] { mMat };
		}
	}

	/// <summary>
	/// Set the draw call's geometry.
	/// </summary>

	public void Set (BetterList<Vector3> verts, BetterList<Vector3> norms, BetterList<Vector4> tans, BetterList<Vector2> uvs, BetterList<Color32> cols)
	{
		int count = verts.size;

		// Safety check to ensure we get valid values
		if (count > 0 && (count == uvs.size && count == cols.size) && (count % 4) == 0)
		{
			// Cache all components
			if (mFilter == null) mFilter = gameObject.GetComponent<MeshFilter>();
			if (mFilter == null) mFilter = gameObject.AddComponent<MeshFilter>();
			if (mRen == null) mRen = gameObject.GetComponent<MeshRenderer>();

			if (mRen == null)
			{
				mRen = gameObject.AddComponent<MeshRenderer>();
#if UNITY_EDITOR
				mRen.enabled = isActive;
#endif
				UpdateMaterials();
			}
			else if (mMat != null && mMat.mainTexture != mSharedMat.mainTexture)
			{
				UpdateMaterials();
			}

			if (verts.size < 65000)
			{
				int indexCount = (count >> 1) * 3;
				bool rebuildIndices = (mIndices == null || mIndices.Length != indexCount);

				// Populate the index buffer
				if (rebuildIndices)
				{
					// It takes 6 indices to draw a quad of 4 vertices
					mIndices = new int[indexCount];
					int index = 0;

					for (int i = 0; i < count; i += 4)
					{
						mIndices[index++] = i;
						mIndices[index++] = i + 1;
						mIndices[index++] = i + 2;

						mIndices[index++] = i + 2;
						mIndices[index++] = i + 3;
						mIndices[index++] = i;
					}
				}

				// Set the mesh values
				Mesh mesh = GetMesh(ref rebuildIndices, verts.size);
				mesh.vertices = verts.ToArray();
				if (norms != null) mesh.normals = norms.ToArray();
				if (tans != null) mesh.tangents = tans.ToArray();
				mesh.uv = uvs.ToArray();
				mesh.colors32 = cols.ToArray();
				if (rebuildIndices) mesh.triangles = mIndices;
				mesh.RecalculateBounds();
				mFilter.mesh = mesh;
			}
			else
			{
				if (mFilter.mesh != null) mFilter.mesh.Clear();
				Debug.LogError("Too many vertices on one panel: " + verts.size);
			}
		}
		else
		{
			if (mFilter.mesh != null) mFilter.mesh.Clear();
			Debug.LogError("UIWidgets must fill the buffer with 4 vertices per quad. Found " + count);
		}
	}

	/// <summary>
	/// This function is called when it's clear that the object will be rendered.
	/// We want to set the shader used by the material, creating a copy of the material in the process.
	/// We also want to update the material's properties before it's actually used.
	/// </summary>

	void OnWillRenderObject ()
	{
		if (mReset)
		{
			mReset = false;
			UpdateMaterials();
		}

		if (mMat != null && isClipped)
		{
			mMat.mainTextureOffset = new Vector2(-mClipRange.x / mClipRange.z, -mClipRange.y / mClipRange.w);
			mMat.mainTextureScale = new Vector2(1f / mClipRange.z, 1f / mClipRange.w);

			Vector2 sharpness = new Vector2(1000.0f, 1000.0f);
			if (mClipSoft.x > 0f) sharpness.x = mClipRange.z / mClipSoft.x;
			if (mClipSoft.y > 0f) sharpness.y = mClipRange.w / mClipSoft.y;
			mMat.SetVector("_ClipSharpness", sharpness);
		}
	}

	/// <summary>
	/// Cleanup.
	/// </summary>

	void OnDestroy ()
	{
		NGUITools.DestroyImmediate(mMesh0);
		NGUITools.DestroyImmediate(mMesh1);
		NGUITools.DestroyImmediate(mMat);
	}
}
