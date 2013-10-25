//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

#if UNITY_FLASH || UNITY_WP8 || UNITY_METRO
#define USE_SIMPLE_DICTIONARY
#endif

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// UI Panel is responsible for collecting, sorting and updating widgets in addition to generating widgets' geometry.
/// </summary>

[ExecuteInEditMode]
[AddComponentMenu("NGUI/UI/Panel")]
public class UIPanel : MonoBehaviour
{
	/// <summary>
	/// List of active panels.
	/// </summary>

	static public BetterList<UIPanel> list = new BetterList<UIPanel>();

	public enum DebugInfo
	{
		None,
		Gizmos,
		Geometry,
	}

	public delegate void OnChangeDelegate ();

	/// <summary>
	/// Notification triggered when something changes within the panel.
	/// </summary>

	public OnChangeDelegate onChange;

	/// <summary>
	/// Whether this panel will show up in the panel tool (set this to 'false' for dynamically created temporary panels)
	/// </summary>

	public bool showInPanelTool = true;

	/// <summary>
	/// Whether normals and tangents will be generated for all meshes
	/// </summary>
	
	public bool generateNormals = false;

	/// <summary>
	/// Whether widgets drawn by this panel are static (won't move). This will improve performance.
	/// </summary>

	public bool widgetsAreStatic = false;

	/// <summary>
	/// Whether widgets will be culled while the panel is being dragged.
	/// Having this on improves performance, but turning it off will reduce garbage collection.
	/// </summary>

	public bool cullWhileDragging = false;

	/// <summary>
	/// Matrix that will transform the specified world coordinates to relative-to-panel coordinates.
	/// </summary>

	[HideInInspector] public Matrix4x4 worldToLocal = Matrix4x4.identity;

	// Panel's alpha (affects the alpha of all widgets)
	[HideInInspector][SerializeField] float mAlpha = 1f;

	// Clipping rectangle
	[HideInInspector][SerializeField] UIDrawCall.Clipping mClipping = UIDrawCall.Clipping.None;
	[HideInInspector][SerializeField] Vector4 mClipRange = Vector4.zero;
	[HideInInspector][SerializeField] Vector2 mClipSoftness = new Vector2(40f, 40f);
	[HideInInspector][SerializeField] int mDepth = 0;

	// Whether a full rebuild of geometry buffers is required
	static bool mFullRebuild = false;

	// Cached in order to reduce memory allocations
	static BetterList<Vector3> mVerts = new BetterList<Vector3>();
	static BetterList<Vector3> mNorms = new BetterList<Vector3>();
	static BetterList<Vector4> mTans = new BetterList<Vector4>();
	static BetterList<Vector2> mUvs = new BetterList<Vector2>();
	static BetterList<Color32> mCols = new BetterList<Color32>();

	GameObject mGo;
	Transform mTrans;
	Camera mCam;
	int mLayer = -1;
	float mCullTime = 0f;
	float mUpdateTime = 0f;
	float mMatrixTime = 0f;

	// Values used for visibility checks
	static float[] mTemp = new float[4];
	Vector2 mMin = Vector2.zero;
	Vector2 mMax = Vector2.zero;

	// Used for SetAlphaRecursive()
	UIPanel[] mChildPanels;

	/// <summary>
	/// Cached for speed. Can't simply return 'mGo' set in Awake because this function may be called on a prefab.
	/// </summary>

	public GameObject cachedGameObject { get { if (mGo == null) mGo = gameObject; return mGo; } }

	/// <summary>
	/// Cached for speed. Can't simply return 'mTrans' set in Awake because this function may be called on a prefab.
	/// </summary>

	public Transform cachedTransform { get { if (mTrans == null) mTrans = transform; return mTrans; } }

	/// <summary>
	/// Panel's alpha affects everything drawn by the panel.
	/// </summary>

	public float alpha
	{
		get
		{
			return mAlpha;
		}
		set
		{
			float val = Mathf.Clamp01(value);

			if (mAlpha != val)
			{
				mAlpha = val;

				for (int i = 0; i < UIDrawCall.list.size; ++i)
				{
					UIDrawCall dc = UIDrawCall.list[i];
					if (dc != null && dc.panel == this)
						dc.isDirty = true;
				}

				for (int i = 0; i < UIWidget.list.size; ++i)
				{
					UIWidget w = UIWidget.list[i];
					if (w.panel == this) w.MarkAsChangedLite();
				}
			}
		}
	}

	/// <summary>
	/// Panels can have their own depth value that will change the order with which everything they manage gets drawn.
	/// </summary>

	public int depth
	{
		get
		{
			return mDepth;
		}
		set
		{
			if (mDepth != value)
			{
				mDepth = value;
				mFullRebuild = true;

				for (int i = 0; i < UIDrawCall.list.size; ++i)
				{
					UIDrawCall dc = UIDrawCall.list[i];
					if (dc != null) dc.isDirty = true;
				}

				for (int i = 0; i < UIWidget.list.size; ++i)
					UIWidget.list[i].MarkAsChangedLite();
#if UNITY_EDITOR
				UnityEditor.EditorUtility.SetDirty(this);
#endif
				list.Sort(CompareFunc);
			}
		}
	}

	/// <summary>
	/// Function that can be used to depth-sort panels.
	/// </summary>

	static public int CompareFunc (UIPanel a, UIPanel b)
	{
		if (a != null && b != null)
		{
			if (a.mDepth < b.mDepth) return -1;
			if (a.mDepth > b.mDepth) return 1;
		}
		return 0;
	}

	/// <summary>
	/// Number of draw calls produced by this panel.
	/// </summary>

	public int drawCallCount
	{
		get
		{
			int count = 0;

			for (int i = 0; i < UIDrawCall.list.size; ++i)
			{
				UIDrawCall dc = UIDrawCall.list[i];
				if (dc.panel != this) continue;
				++count;
			}
			return count;
		}
	}

	/// <summary>
	/// Recursively set the alpha for this panel and all of its children.
	/// </summary>

	public void SetAlphaRecursive (float val, bool rebuildList)
	{
		if (rebuildList || mChildPanels == null)
			mChildPanels = GetComponentsInChildren<UIPanel>(true);
		for (int i = 0, imax = mChildPanels.Length; i < imax; ++i)
			mChildPanels[i].alpha = val;
	}

	/// <summary>
	/// Clipping method used by all draw calls.
	/// </summary>

	public UIDrawCall.Clipping clipping
	{
		get
		{
			return mClipping;
		}
		set
		{
			if (mClipping != value)
			{
				mClipping = value;
				mMatrixTime = 0f;
				UpdateDrawcalls();
			}
		}
	}

	/// <summary>
	/// Clipping position (XY) and size (ZW).
	/// </summary>

	public Vector4 clipRange
	{
		get
		{
			return mClipRange;
		}
		set
		{
			if (mClipRange != value)
			{
				mCullTime = (mCullTime == 0f) ? 0.001f : Time.realtimeSinceStartup + 0.15f;
				mClipRange = value;
				mMatrixTime = 0f;
				UpdateDrawcalls();
			}
		}
	}

	/// <summary>
	/// Clipping softness is used if the clipped style is set to "Soft".
	/// </summary>

	public Vector2 clipSoftness { get { return mClipSoftness; } set { if (mClipSoftness != value) { mClipSoftness = value; UpdateDrawcalls(); } } }

	/// <summary>
	/// Returns whether the specified rectangle is visible by the panel. The coordinates must be in world space.
	/// </summary>

	bool IsVisible (Vector3 a, Vector3 b, Vector3 c, Vector3 d)
	{
		UpdateTransformMatrix();

		// Transform the specified points from world space to local space
		a = worldToLocal.MultiplyPoint3x4(a);
		b = worldToLocal.MultiplyPoint3x4(b);
		c = worldToLocal.MultiplyPoint3x4(c);
		d = worldToLocal.MultiplyPoint3x4(d);

		mTemp[0] = a.x;
		mTemp[1] = b.x;
		mTemp[2] = c.x;
		mTemp[3] = d.x;

		float minX = Mathf.Min(mTemp);
		float maxX = Mathf.Max(mTemp);

		mTemp[0] = a.y;
		mTemp[1] = b.y;
		mTemp[2] = c.y;
		mTemp[3] = d.y;

		float minY = Mathf.Min(mTemp);
		float maxY = Mathf.Max(mTemp);

		if (maxX < mMin.x) return false;
		if (maxY < mMin.y) return false;
		if (minX > mMax.x) return false;
		if (minY > mMax.y) return false;
		return true;
	}

	/// <summary>
	/// Returns whether the specified world position is within the panel's bounds determined by the clipping rect.
	/// </summary>

	public bool IsVisible (Vector3 worldPos)
	{
		if (mAlpha < 0.001f) return false;
		if (mClipping == UIDrawCall.Clipping.None) return true;
		UpdateTransformMatrix();

		Vector3 pos = worldToLocal.MultiplyPoint3x4(worldPos);
		if (pos.x < mMin.x) return false;
		if (pos.y < mMin.y) return false;
		if (pos.x > mMax.x) return false;
		if (pos.y > mMax.y) return false;
		return true;
	}

	/// <summary>
	/// Returns whether the specified widget is visible by the panel.
	/// </summary>

	public bool IsVisible (UIWidget w)
	{
		if (mAlpha < 0.001f) return false;
		if (!w.enabled || !NGUITools.GetActive(w.cachedGameObject) || w.alpha < 0.001f) return false;

		// No clipping? No point in checking.
		if (mClipping == UIDrawCall.Clipping.None) return true;

		Vector3[] corners = w.worldCorners;
		return IsVisible(corners[0], corners[1], corners[2], corners[3]);
	}

	/// <summary>
	/// Mark the panel's content as changed and in need of being rebuilt.
	/// </summary>

	static public void SetDirty () { mFullRebuild = true; }

	/// <summary>
	/// Get a draw call at the specified index position.
	/// </summary>

	UIDrawCall GetDrawCall (int index, Material mat)
	{
		if (index < UIDrawCall.list.size)
		{
			UIDrawCall dc = UIDrawCall.list.buffer[index];

			// If the material and texture match, keep using the same draw call
			if (dc != null && dc.panel == this && dc.material == mat && dc.mainTexture == mat.mainTexture) return dc;

			// Otherwise we need to destroy all the draw calls that follow
			for (int i = UIDrawCall.list.size; i > index; )
			{
				UIDrawCall rem = UIDrawCall.list.buffer[--i];
				DestroyDrawCall(rem, i);
			}
		}

#if UNITY_EDITOR
		// If we're in the editor, create the game object with hide flags set right away
		GameObject go = UnityEditor.EditorUtility.CreateGameObjectWithHideFlags("_UIDrawCall [" + mat.name + "]",
			//HideFlags.DontSave | HideFlags.NotEditable);
			HideFlags.HideAndDontSave);
#else
		GameObject go = new GameObject("_UIDrawCall [" + mat.name + "]");
		DontDestroyOnLoad(go);
#endif
		go.layer = cachedGameObject.layer;
		
		// Create the draw call
		UIDrawCall drawCall = go.AddComponent<UIDrawCall>();
		drawCall.material = mat;
		drawCall.renderQueue = UIDrawCall.list.size;
		drawCall.panel = this;
		//Debug.Log("Added DC " + mat.name + " as " + UIDrawCall.list.size);
		UIDrawCall.list.Add(drawCall);
		return drawCall;
	}

	/// <summary>
	/// Cache components.
	/// </summary>

	void Awake ()
	{
		mGo = gameObject;
		mTrans = transform;
	}

	/// <summary>
	/// Layer is used to ensure that if it changes, widgets get moved as well.
	/// </summary>

	void Start ()
	{
		mLayer = mGo.layer;
		UICamera uic = UICamera.FindCameraForLayer(mLayer);
		mCam = (uic != null) ? uic.cachedCamera : NGUITools.FindCameraForLayer(mLayer);
	}

	/// <summary>
	/// Mark all widgets as having been changed so the draw calls get re-created.
	/// </summary>

	void OnEnable ()
	{
		mFullRebuild = true;
		list.Add(this);
		list.Sort(CompareFunc);
	}

	/// <summary>
	/// Destroy all draw calls we've created when this script gets disabled.
	/// </summary>

	void OnDisable ()
	{
		for (int i = UIDrawCall.list.size; i > 0; )
		{
			UIDrawCall dc = UIDrawCall.list.buffer[--i];
			if (dc != null && dc.panel == this)
				DestroyDrawCall(dc, i);
		}
		list.Remove(this);
	}

	/// <summary>
	/// Update the world-to-local transform matrix as well as clipping bounds.
	/// </summary>

	void UpdateTransformMatrix ()
	{
		if (mUpdateTime == 0f || mMatrixTime != mUpdateTime)
		{
			mMatrixTime = mUpdateTime;
			worldToLocal = cachedTransform.worldToLocalMatrix;

			if (mClipping != UIDrawCall.Clipping.None)
			{
				Vector2 size = new Vector2(mClipRange.z, mClipRange.w);

				if (size.x == 0f) size.x = (mCam == null) ? Screen.width  : mCam.pixelWidth;
				if (size.y == 0f) size.y = (mCam == null) ? Screen.height : mCam.pixelHeight;

				size *= 0.5f;

				mMin.x = mClipRange.x - size.x;
				mMin.y = mClipRange.y - size.y;
				mMax.x = mClipRange.x + size.x;
				mMax.y = mClipRange.y + size.y;
			}
		}
	}

	/// <summary>
	/// Update the clipping rect in the shaders and draw calls' positions.
	/// </summary>

	void UpdateDrawcalls ()
	{
		Vector4 range = Vector4.zero;

		if (mClipping != UIDrawCall.Clipping.None)
		{
			range = new Vector4(mClipRange.x, mClipRange.y, mClipRange.z * 0.5f, mClipRange.w * 0.5f);
		}

		if (range.z == 0f) range.z = Screen.width * 0.5f;
		if (range.w == 0f) range.w = Screen.height * 0.5f;

		RuntimePlatform platform = Application.platform;

		if (platform == RuntimePlatform.WindowsPlayer ||
			platform == RuntimePlatform.WindowsWebPlayer ||
			platform == RuntimePlatform.WindowsEditor)
		{
			range.x -= 0.5f;
			range.y += 0.5f;
		}

		Transform t = cachedTransform;
		UIDrawCall dc;
		Transform dt;

		for (int i = 0; i < UIDrawCall.list.size; )
		{
			dc = UIDrawCall.list.buffer[i];

			if (dc == null)
			{
				UIDrawCall.list.RemoveAt(i);
				continue;
			}

			if (dc.panel == this)
			{
				dc.clipping = mClipping;
				dc.clipRange = range;
				dc.clipSoftness = mClipSoftness;

				// Set the draw call's transform to match the panel's.
				// Note that parenting directly to the panel causes unity to crash as soon as you hit Play.
				dt = dc.transform;
				dt.position = t.position;
				dt.rotation = t.rotation;
				dt.localScale = t.lossyScale;
			}
			++i;
		}
	}

	/// <summary>
	/// Main update function
	/// </summary>

	void LateUpdate ()
	{
		// Only the very first panel should be doing the update logic
		if (list[0] != this) return;

		// Update all panels
		for (int i = 0; i < list.size; ++i)
		{
			UIPanel panel = list[i];
			panel.mUpdateTime = RealTime.time;
			panel.UpdateTransformMatrix();
			panel.UpdateLayers();
			panel.UpdateWidgets();
		}

		// Fill the draw calls for all of the changed materials
		if (mFullRebuild)
		{
			UIWidget.list.Sort(UIWidget.CompareFunc);
			Fill();
		}
		else
		{
			for (int i = 0; i < UIDrawCall.list.size; )
			{
				UIDrawCall dc = UIDrawCall.list[i];

				if (dc.isDirty)
				{
					if (!Fill(dc))
					{
						DestroyDrawCall(dc, i);
						continue;
					}
				}
				++i;
			}
		}

		// Update the clipping rects
		for (int i = 0; i < list.size; ++i)
		{
			UIPanel panel = list[i];
			panel.UpdateDrawcalls();
		}
		mFullRebuild = false;
	}

	/// <summary>
	/// Destroy the specified draw call.
	/// NOTE: Future optimization can be caching these at run-time instead of destroying them.
	/// </summary>

	static void DestroyDrawCall (UIDrawCall dc, int index)
	{
		if (dc != null)
		{
			//Debug.Log("Destroyed DC " + dc.material.name + " as " + dc.renderQueue);
			UIDrawCall.list.RemoveAt(index);
			NGUITools.DestroyImmediate(dc.gameObject);
		}
	}

	/// <summary>
	/// Update the widget layers if the panel's layer has changed.
	/// </summary>

	void UpdateLayers ()
	{
		// Always move widgets to the panel's layer
		if (mLayer != cachedGameObject.layer)
		{
			mLayer = mGo.layer;
			UICamera uic = UICamera.FindCameraForLayer(mLayer);
			mCam = (uic != null) ? uic.cachedCamera : NGUITools.FindCameraForLayer(mLayer);
			SetChildLayer(cachedTransform, mLayer);

			for (int i = 0, imax = UIDrawCall.list.size; i < imax; ++i)
			{
				UIDrawCall dc = UIDrawCall.list[i];
				if (dc != null && dc.panel == this)
					dc.gameObject.layer = mLayer;
			}
		}
	}

	/// <summary>
	/// Update all of the widgets belonging to this panel.
	/// </summary>

	void UpdateWidgets()
	{
#if UNITY_EDITOR
		bool forceVisible = cullWhileDragging ? false : (clipping == UIDrawCall.Clipping.None) || (Application.isPlaying && mCullTime > mUpdateTime);
#else
		bool forceVisible = cullWhileDragging ? false : (clipping == UIDrawCall.Clipping.None) || (mCullTime > mUpdateTime);
#endif
		bool changed = false;

		// Update all widgets
		for (int i = 0, imax = UIWidget.list.size; i < imax; ++i)
		{
			UIWidget w = UIWidget.list[i];

			// If the widget is visible, update it
			if (w.enabled && w.panel == this && w.UpdateGeometry(this, forceVisible))
			{
				changed = true;
				if (mFullRebuild) continue;
				UIDrawCall dc = w.drawCall;
				if (dc != null) dc.isDirty = true;
				else mFullRebuild = true;
			}
		}

		// Inform the changed event listeners
		if (changed && onChange != null) onChange();
	}

	/// <summary>
	/// Immediately refresh the panel.
	/// </summary>

	public void Refresh ()
	{
		mFullRebuild = true;
		list[0].LateUpdate();
	}

	/// <summary>
	/// Calculate the offset needed to be constrained within the panel's bounds.
	/// </summary>

	public Vector3 CalculateConstrainOffset (Vector2 min, Vector2 max)
	{
		float offsetX = clipRange.z * 0.5f;
		float offsetY = clipRange.w * 0.5f;

		Vector2 minRect = new Vector2(min.x, min.y);
		Vector2 maxRect = new Vector2(max.x, max.y);
		Vector2 minArea = new Vector2(clipRange.x - offsetX, clipRange.y - offsetY);
		Vector2 maxArea = new Vector2(clipRange.x + offsetX, clipRange.y + offsetY);

		if (clipping == UIDrawCall.Clipping.SoftClip)
		{
			minArea.x += clipSoftness.x;
			minArea.y += clipSoftness.y;
			maxArea.x -= clipSoftness.x;
			maxArea.y -= clipSoftness.y;
		}
		return NGUIMath.ConstrainRect(minRect, maxRect, minArea, maxArea);
	}

	/// <summary>
	/// Constrain the current target position to be within panel bounds.
	/// </summary>

	public bool ConstrainTargetToBounds (Transform target, ref Bounds targetBounds, bool immediate)
	{
		Vector3 offset = CalculateConstrainOffset(targetBounds.min, targetBounds.max);

		if (offset.magnitude > 0f)
		{
			if (immediate)
			{
				target.localPosition += offset;
				targetBounds.center += offset;
				SpringPosition sp = target.GetComponent<SpringPosition>();
				if (sp != null) sp.enabled = false;
			}
			else
			{
				SpringPosition sp = SpringPosition.Begin(target.gameObject, target.localPosition + offset, 13f);
				sp.ignoreTimeScale = true;
				sp.worldSpace = false;
			}
			return true;
		}
		return false;
	}

	/// <summary>
	/// Constrain the specified target to be within the panel's bounds.
	/// </summary>

	public bool ConstrainTargetToBounds (Transform target, bool immediate)
	{
		Bounds bounds = NGUIMath.CalculateRelativeWidgetBounds(cachedTransform, target);
		return ConstrainTargetToBounds(target, ref bounds, immediate);
	}

	/// <summary>
	/// Helper function that recursively sets all children with widgets' game objects layers to the specified value, stopping when it hits another UIPanel.
	/// </summary>

	static void SetChildLayer (Transform t, int layer)
	{
		for (int i = 0; i < t.childCount; ++i)
		{
			Transform child = t.GetChild(i);

			if (child.GetComponent<UIPanel>() == null)
			{
				if (child.GetComponent<UIWidget>() != null)
				{
					child.gameObject.layer = layer;
				}					
				SetChildLayer(child, layer);
			}
		}
	}

	/// <summary>
	/// Find the UIPanel responsible for handling the specified transform.
	/// </summary>

	static public UIPanel Find (Transform trans, bool createIfMissing)
	{
		Transform origin = trans;
		UIPanel panel = null;

		while (panel == null && trans != null)
		{
			panel = trans.GetComponent<UIPanel>();
			if (panel != null) break;
			if (trans.parent == null) break;
			trans = trans.parent;
		}

		if (createIfMissing && panel == null && trans != origin)
		{
			mFullRebuild = true;
			panel = trans.gameObject.AddComponent<UIPanel>();
			SetChildLayer(panel.cachedTransform, panel.cachedGameObject.layer);
		}
		return panel;
	}

	/// <summary>
	/// Find the UIPanel responsible for handling the specified transform, creating a new one if necessary.
	/// </summary>

	static public UIPanel Find (Transform trans) { return Find(trans, true); }

	/// <summary>
	/// Fill the geometry fully, processing all widgets and re-creating all draw calls.
	/// </summary>

	static void Fill ()
	{
		for (int i = UIDrawCall.list.size; i > 0; )
			DestroyDrawCall(UIDrawCall.list[--i], i);

		int index = 0;
		UIPanel pan = null;
		Material mat = null;
		UIDrawCall dc = null;

		for (int i = 0; i < UIWidget.list.size; )
		{
			UIWidget w = UIWidget.list[i];

			if (w == null)
			{
				UIWidget.list.RemoveAt(i);
				continue;
			}

			if (w.isVisible && w.hasVertices)
			{
				if (pan != w.panel || mat != w.material)
				{
					if (pan != null && mat != null && mVerts.size != 0)
					{
						pan.SubmitDrawCall(dc);
						dc = null;
					}

					pan = w.panel;
					mat = w.material;
				}

				if (pan != null && mat != null)
				{
					if (dc == null) dc = pan.GetDrawCall(index++, mat);
					w.drawCall = dc;
					if (pan.generateNormals) w.WriteToBuffers(mVerts, mUvs, mCols, mNorms, mTans);
					else w.WriteToBuffers(mVerts, mUvs, mCols, null, null);
				}
			}
			else w.drawCall = null;
			++i;
		}

		if (mVerts.size != 0)
			pan.SubmitDrawCall(dc);
	}

	/// <summary>
	/// Submit the draw call using the current geometry.
	/// </summary>

	void SubmitDrawCall (UIDrawCall dc)
	{
		dc.Set(mVerts, generateNormals ? mNorms : null, generateNormals ? mTans : null, mUvs, mCols);
		mVerts.Clear();
		mNorms.Clear();
		mTans.Clear();
		mUvs.Clear();
		mCols.Clear();
	}

	/// <summary>
	/// Fill the geometry for the specified draw call.
	/// </summary>

	static bool Fill (UIDrawCall dc)
	{
		if (dc != null)
		{
			dc.isDirty = false;

			for (int i = 0; i < UIWidget.list.size; )
			{
				UIWidget w = UIWidget.list[i];

				if (w == null)
				{
					UIWidget.list.RemoveAt(i);
					continue;
				}

				if (w.drawCall == dc)
				{
					if (w.isVisible && w.hasVertices)
					{
						if (dc.panel.generateNormals) w.WriteToBuffers(mVerts, mUvs, mCols, mNorms, mTans);
						else w.WriteToBuffers(mVerts, mUvs, mCols, null, null);
					}
					else w.drawCall = null;
				}
				++i;
			}

			if (mVerts.size != 0)
			{
				dc.Set(mVerts, dc.panel.generateNormals ? mNorms : null, dc.panel.generateNormals ? mTans : null, mUvs, mCols);
				mVerts.Clear();
				mNorms.Clear();
				mTans.Clear();
				mUvs.Clear();
				mCols.Clear();
				return true;
			}
		}
		return false;
	}

#if UNITY_EDITOR

	int mScreenWidth = 1280;
	int mScreenHeight = 720;
	
	void Update ()
	{
		mScreenWidth = Screen.width;
		mScreenHeight = Screen.height;
	}

	/// <summary>
	/// Draw a visible pink outline for the clipped area.
	/// </summary>

	void OnDrawGizmos ()
	{
		if (mCam == null || !mCam.isOrthoGraphic) return;

		bool clip = (mClipping != UIDrawCall.Clipping.None);
		Vector2 size = clip ? new Vector2(mClipRange.z, mClipRange.w) : Vector2.zero;

		GameObject go = UnityEditor.Selection.activeGameObject;
		bool selected = (go != null) && (NGUITools.FindInParents<UIPanel>(go) == this);

		//if (selected || clip)
		{
			if (size.x == 0f) size.x = mScreenWidth;
			if (size.y == 0f) size.y = mScreenHeight;

			if (!clip)
			{
				UIRoot root = NGUITools.FindInParents<UIRoot>(cachedGameObject);
				if (root != null) size *= root.GetPixelSizeAdjustment(mScreenHeight);
			}

			Transform t = clip ? transform : (mCam != null ? mCam.transform : null);

			if (t != null)
			{
				Vector3 pos = clip ? new Vector3(mClipRange.x, mClipRange.y) : Vector3.zero;
				Gizmos.matrix = t.localToWorldMatrix;

				if (selected)
				{
					Gizmos.color = new Color(1f, 0f, 0.5f);
					Gizmos.DrawWireCube(pos, size);
				}
				else
				{
					Gizmos.color = new Color(0.5f, 0f, 0.5f);
					Gizmos.DrawWireCube(pos, size);
				}
			}
		}
	}
#endif
}
