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
[AddComponentMenu("NGUI/UI/NGUI Panel")]
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
	/// Optimization flag. Makes the assumption that the panel's geometry
	/// will always be on screen and the bounds don't need to be re-calculated.
	/// </summary>

	public bool alwaysOnScreen = false;

	/// <summary>
	/// Matrix that will transform the specified world coordinates to relative-to-panel coordinates.
	/// </summary>

	[HideInInspector] public Matrix4x4 worldToLocal = Matrix4x4.identity;

	// Panel's alpha (affects the alpha of all widgets)
	[HideInInspector][SerializeField] float mAlpha = 1f;

	// Clipping rectangle
	[HideInInspector][SerializeField] UIDrawCall.Clipping mClipping = UIDrawCall.Clipping.None;
	[HideInInspector][SerializeField] Vector4 mClipRange = new Vector4(0f, 0f, 300f, 200f);
	[HideInInspector][SerializeField] Vector2 mClipSoftness = new Vector2(4f, 4f);
	[HideInInspector][SerializeField] int mDepth = 0;

	// Whether a full rebuild of geometry buffers is required
	static bool mRebuild = false;

	// Cached in order to reduce memory allocations
	static BetterList<Vector3> mVerts = new BetterList<Vector3>();
	static BetterList<Vector3> mNorms = new BetterList<Vector3>();
	static BetterList<Vector4> mTans = new BetterList<Vector4>();
	static BetterList<Vector2> mUvs = new BetterList<Vector2>();
	static BetterList<Color32> mCols = new BetterList<Color32>();

	GameObject mGo;
	Transform mTrans;
	Camera mCam;
	UIPanel mParent;
	bool mFindParent = true;
	float mCullTime = 0f;
	float mUpdateTime = 0f;
	float mMatrixTime = 0f;
	int mLayer = -1;

	// Values used for visibility checks
	static float[] mTemp = new float[4];
	Vector2 mMin = Vector2.zero;
	Vector2 mMax = Vector2.zero;

	/// <summary>
	/// Helper property that returns the first unused depth value.
	/// </summary>

	static public int nextUnusedDepth
	{
		get
		{
			int highest = int.MinValue;
			for (int i = 0; i < list.size; ++i)
				highest = Mathf.Max(highest, list[i].depth);
			return (highest == int.MinValue) ? 0 : highest + 1;
		}
	}

	/// <summary>
	/// Cached for speed. Can't simply return 'mGo' set in Awake because this function may be called on a prefab.
	/// </summary>

	public GameObject cachedGameObject { get { if (mGo == null) mGo = gameObject; return mGo; } }

	/// <summary>
	/// Cached for speed. Can't simply return 'mTrans' set in Awake because this function may be called on a prefab.
	/// </summary>

	public Transform cachedTransform { get { if (mTrans == null) mTrans = transform; return mTrans; } }

	/// <summary>
	/// Parent panel is used to determine cumulative alpha.
	/// </summary>

	public UIPanel parent
	{
		get
		{
			if (mFindParent)
			{
				mFindParent = false;
				Transform t = cachedTransform.parent;
				mParent = (t != null) ? NGUITools.FindInParents<UIPanel>(t) : null;
			}
			return mParent;
		}
	}

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
				SetDirty();
			}
		}
	}

	/// <summary>
	/// Final alpha, taking all parent panels into consideration.
	/// </summary>

	public float finalAlpha { get { return (parent != null) ? mParent.alpha * mAlpha : mAlpha; } }

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
				mRebuild = true;

				UIDrawCall.SetDirty();

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

	public int drawCallCount { get { return UIDrawCall.Count(this); } }

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
				mCullTime = (mCullTime == 0f) ? 0.001f : RealTime.time + 0.15f;
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

	// Temporary variable to avoid GC allocation
	static Vector3[] mCorners = new Vector3[4];

	/// <summary>
	/// Local-space corners of the panel's clipping rectangle. The order is bottom-left, top-left, top-right, bottom-right.
	/// </summary>

	public Vector3[] localCorners
	{
		get
		{
			if (mClipping == UIDrawCall.Clipping.None)
			{
				Vector2 size = GetSize();

				float x0 = -0.5f * size.x;
				float y0 = -0.5f * size.y;
				float x1 = x0 + size.x;
				float y1 = y0 + size.y;

				Transform wt = (mCam != null) ? mCam.transform : null;

				if (wt != null)
				{
					mCorners[0] = wt.TransformPoint(x0, y0, 0f);
					mCorners[1] = wt.TransformPoint(x0, y1, 0f);
					mCorners[2] = wt.TransformPoint(x1, y1, 0f);
					mCorners[3] = wt.TransformPoint(x1, y0, 0f);

					wt = cachedTransform;

					for (int i = 0; i < 4; ++i)
						mCorners[i] = wt.InverseTransformPoint(mCorners[i]);
				}
				else
				{
					mCorners[0] = new Vector3(x0, y0);
					mCorners[1] = new Vector3(x0, y1);
					mCorners[2] = new Vector3(x1, y1);
					mCorners[3] = new Vector3(x1, y0);
				}
			}
			else
			{
				float x0 = mClipRange.x - 0.5f * mClipRange.z;
				float y0 = mClipRange.y - 0.5f * mClipRange.w;
				float x1 = x0 + mClipRange.z;
				float y1 = y0 + mClipRange.w;

				mCorners[0] = new Vector3(x0, y0);
				mCorners[1] = new Vector3(x0, y1);
				mCorners[2] = new Vector3(x1, y1);
				mCorners[3] = new Vector3(x1, y0);
			}
			return mCorners;
		}
	}

	/// <summary>
	/// World-space corners of the panel's clipping rectangle. The order is bottom-left, top-left, top-right, bottom-right.
	/// </summary>

	public Vector3[] worldCorners
	{
		get
		{
			if (mClipping == UIDrawCall.Clipping.None)
			{
				Vector2 size = GetSize();

				float x0 = -0.5f * size.x;
				float y0 = -0.5f * size.y;
				float x1 = x0 + size.x;
				float y1 = y0 + size.y;

				Transform wt = (mCam != null) ? mCam.transform : null;

				if (wt != null)
				{
					mCorners[0] = wt.TransformPoint(x0, y0, 0f);
					mCorners[1] = wt.TransformPoint(x0, y1, 0f);
					mCorners[2] = wt.TransformPoint(x1, y1, 0f);
					mCorners[3] = wt.TransformPoint(x1, y0, 0f);
				}
			}
			else
			{
				float x0 = mClipRange.x - 0.5f * mClipRange.z;
				float y0 = mClipRange.y - 0.5f * mClipRange.w;
				float x1 = x0 + mClipRange.z;
				float y1 = y0 + mClipRange.w;

				Transform wt = cachedTransform;

				mCorners[0] = wt.TransformPoint(x0, y0, 0f);
				mCorners[1] = wt.TransformPoint(x0, y1, 0f);
				mCorners[2] = wt.TransformPoint(x1, y1, 0f);
				mCorners[3] = wt.TransformPoint(x1, y0, 0f);
			}
			return mCorners;
		}
	}

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
	/// Causes all draw calls to be re-created on the next update.
	/// </summary>

	static public void RebuildAllDrawCalls (bool sort) { mRebuild = true; }

#if UNITY_EDITOR
	
	/// <summary>
	/// Context menu option to force-refresh the panel, just in case.
	/// </summary>

	[ContextMenu("Force Refresh")]
	void ForceRefresh () { mRebuild = true; }

#endif

	/// <summary>
	/// Invalidate the panel's draw calls, forcing them to be rebuilt on the next update.
	/// This call also affects all child panels.
	/// </summary>

	public void SetDirty ()
	{
		UIDrawCall.SetDirty(this);

		for (int i = 0; i < UIWidget.list.size; ++i)
		{
			UIWidget w = UIWidget.list[i];
			if (w.panel == this) w.MarkAsChangedLite();
		}

		for (int i = 0; i < list.size; ++i)
		{
			UIPanel p = list[i];
			if (p != null && p != this && p.parent == this)
				p.SetDirty();
		}
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
		// Apparently having a rigidbody helps
		if (rigidbody == null)
		{
			Rigidbody rb = gameObject.AddComponent<Rigidbody>();
			rb.isKinematic = true;
			rb.useGravity = false;
		}

		mFindParent = true;
		mRebuild = true;
		list.Add(this);
		list.Sort(CompareFunc);
	}

	/// <summary>
	/// Destroy all draw calls we've created when this script gets disabled.
	/// </summary>

	void OnDisable ()
	{
		mParent = null;
		UIDrawCall.Destroy(this);
		list.Remove(this);
		if (list.size == 0) UIDrawCall.ReleaseAll();
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
		UIDrawCall.Update(this);
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

		if (mRebuild)
		{
			Fill();
		}
		else
		{
			BetterList<UIDrawCall> dcs = UIDrawCall.activeList;

			for (int i = 0; i < dcs.size; )
			{
				UIDrawCall dc = dcs.buffer[i];

				if (dc.isDirty && !Fill(dc))
				{
					UIDrawCall.Destroy(dc);
					continue;
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
		mRebuild = false;
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
			UIDrawCall.UpdateLayer(this);
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
			if (w.panel == this && w.enabled)
			{
#if UNITY_EDITOR
				// When an object is dragged from Project view to Scene view, its Z is...
				// odd, to say the least. Force it if possible.
				if (!Application.isPlaying)
				{
					Transform t = w.cachedTransform;

					if (t.hideFlags != HideFlags.HideInHierarchy)
					{
						t = (t.parent != null && t.parent.hideFlags == HideFlags.HideInHierarchy) ?
							t.parent : null;
					}

					if (t != null)
					{
						for (; ; )
						{
							if (t.parent == null) break;
							if (t.parent.hideFlags == HideFlags.HideInHierarchy) t = t.parent;
							else break;
						}

						if (t != null)
						{
							Vector3 pos = t.localPosition;
							pos.x = Mathf.Round(pos.x);
							pos.y = Mathf.Round(pos.y);
							pos.z = 0f;

							if (Vector3.SqrMagnitude(t.localPosition - pos) > 0.0001f)
								t.localPosition = pos;
						}
					}
				}
#endif
				if (!w.UpdateGeometry(forceVisible)) continue;

				changed = true;
				
				if (!mRebuild)
				{
					if (w.drawCall != null)
					{
						w.drawCall.isDirty = true;
					}
					else
					{
						// Find an existing draw call, if possible
						w.drawCall = InsertWidget(w);
						if (w.drawCall == null) mRebuild = true;
					}
				}
			}
		}

		// Inform the changed event listeners
		if (changed && onChange != null) onChange();
	}

	/// <summary>
	/// Insert the specified widget into one of the existing draw calls if possible.
	/// If it's not possible, and a new draw call is required, 'null' is returned
	/// because draw call creation is a delayed operation.
	/// </summary>

	static public UIDrawCall InsertWidget (UIWidget w)
	{
		UIPanel p = w.panel;
		if (p == null) return null;
		Material mat = w.material;
		int depth = w.raycastDepth;
		BetterList<UIDrawCall> dcs = UIDrawCall.activeList;

		for (int i = 0; i < dcs.size; ++i)
		{
			UIDrawCall dc = dcs.buffer[i];
			if (dc.manager != p) continue;
			int dcStart = (i == 0) ? int.MinValue : dcs.buffer[i-1].depthEnd + 1;
			int dcEnd = (i + 1 == dcs.size) ? int.MaxValue : dcs.buffer[i+1].depthStart - 1;
			
			if (dcStart <= depth && dcEnd >= depth)
			{
				if (dc.baseMaterial == mat)
				{
					if (w.isVisible && w.hasVertices)
					{
						w.drawCall = dc;
						dc.isDirty = true;
						return dc;
					}
				}
				else mRebuild = true;
				return null;
			}
		}
		mRebuild = true;
		return null;
	}

	/// <summary>
	/// Remove the widget from its current draw call, invalidating everything as needed.
	/// </summary>

	static public void RemoveWidget (UIWidget w)
	{
		if (w.drawCall != null)
		{
			int depth = w.raycastDepth;
			if (depth == w.drawCall.depthStart || depth == w.drawCall.depthEnd)
				mRebuild = true;

			w.drawCall.isDirty = true;
			w.drawCall = null;
		}
	}

	/// <summary>
	/// Immediately refresh the panel.
	/// </summary>

	public void Refresh ()
	{
		mRebuild = true;
		if (list.size > 0) list[0].LateUpdate();
	}

	/// <summary>
	/// Calculate the offset needed to be constrained within the panel's bounds.
	/// </summary>

	public virtual Vector3 CalculateConstrainOffset (Vector2 min, Vector2 max)
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
		
		if (createIfMissing && panel == null)
		{
			mRebuild = true;

			UIRoot root = NGUITools.FindInParents<UIRoot>(origin.gameObject);

			if (root == null && UIRoot.list.Count > 0)
				root = UIRoot.list[0];

			if (root == null)
			{
				GameObject go = NGUITools.AddChild(null, false);
				go.name = "UI Root";
				go.layer = origin.gameObject.layer;
				root = go.AddComponent<UIRoot>();
			}

			panel = root.GetComponentInChildren<UIPanel>();

			if (panel == null)
			{
				Camera cam = NGUITools.AddChild<Camera>(root.gameObject, false);
				cam.gameObject.AddComponent<UICamera>();
				cam.orthographic = true;
				cam.orthographicSize = 1;
				cam.nearClipPlane = -10;
				cam.farClipPlane = 10;
				cam.clearFlags = (Camera.main != null) ? CameraClearFlags.Depth : CameraClearFlags.Color;
				cam.cullingMask = (1 << root.gameObject.layer);

				if (Camera.main != null)
					Camera.main.cullingMask = (Camera.main.cullingMask & (~cam.cullingMask));

				UIAnchor anch = NGUITools.AddChild<UIAnchor>(cam.gameObject, false);
				panel = NGUITools.AddChild<UIPanel>(anch.gameObject, false);
#if UNITY_EDITOR
				UnityEditor.Selection.activeGameObject = panel.gameObject;
#endif
			}

			trans.parent = panel.transform;
			trans.localScale = Vector3.one;
			trans.localPosition = Vector3.zero;
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

	static public void Fill ()
	{
		UIDrawCall.ClearAll();

		int index = 0;
		UIPanel pan = null;
		Material mat = null;
		Texture tex = null;
		Shader sdr = null;
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
				UIPanel pn = w.panel;
				Material mt = w.material;
				Texture tx = w.mainTexture;
				Shader sd = w.shader;

				if (pan != pn || mat != mt || tex != tx || sdr != sd)
				{
					if (pan != null && mVerts.size != 0)
					{
						pan.SubmitDrawCall(dc);
						dc = null;
					}

					pan = pn;
					mat = mt;
					tex = tx;
					sdr = sd;
				}

				if (pan != null && (mat != null || sdr != null || tex != null))
				{
					if (dc == null)
					{
						dc = UIDrawCall.Create(index++, pan, mat, tex, sdr);
						dc.depthStart = w.raycastDepth;
						dc.depthEnd = dc.depthStart;
						dc.panel = pan;
					}
					else
					{
						int rd = w.raycastDepth;
						if (rd < dc.depthStart) dc.depthStart = rd;
						if (rd > dc.depthEnd) dc.depthEnd = rd;
					}

					w.drawCall = dc;

					if (pan.generateNormals) w.WriteToBuffers(mVerts, mUvs, mCols, mNorms, mTans);
					else w.WriteToBuffers(mVerts, mUvs, mCols, null, null);
				}
			}
			else w.drawCall = null;
			++i;
		}
		if (mVerts.size != 0) pan.SubmitDrawCall(dc);
	}

	/// <summary>
	/// Submit the draw call using the current geometry.
	/// </summary>

	void SubmitDrawCall (UIDrawCall dc)
	{
		dc.clipping = clipping;
		dc.alwaysOnScreen = alwaysOnScreen && (clipping == UIDrawCall.Clipping.None || clipping == UIDrawCall.Clipping.ConstrainButDontClip);
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
						if (dc.manager.generateNormals) w.WriteToBuffers(mVerts, mUvs, mCols, mNorms, mTans);
						else w.WriteToBuffers(mVerts, mUvs, mCols, null, null);
					}
					else w.drawCall = null;
				}
				++i;
			}

			if (mVerts.size != 0)
			{
				dc.Set(mVerts, dc.manager.generateNormals ? mNorms : null, dc.manager.generateNormals ? mTans : null, mUvs, mCols);
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

	/// <summary>
	/// Panel's size -- which is either the clipping rect, or the screen dimensions.
	/// </summary>

	Vector2 GetSize ()
	{
		bool clip = (mClipping != UIDrawCall.Clipping.None);
#if UNITY_EDITOR
		Vector2 size = clip ? new Vector2(mClipRange.z, mClipRange.w) : new Vector2(mScreenWidth, mScreenHeight);
#else
		Vector2 size = clip ? new Vector2(mClipRange.z, mClipRange.w) : new Vector2(Screen.width, Screen.height);
#endif
		if (!clip)
		{
			UIRoot root = NGUITools.FindInParents<UIRoot>(cachedGameObject);
#if UNITY_EDITOR
			if (root != null) size *= root.GetPixelSizeAdjustment(mScreenHeight);
#else
			if (root != null) size *= root.GetPixelSizeAdjustment(Screen.height);
#endif
		}
		return size;
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
		if (mCam == null) return;

		Vector2 size = GetSize();
		GameObject go = UnityEditor.Selection.activeGameObject;
		bool selected = (go != null) && (NGUITools.FindInParents<UIPanel>(go) == this);
		bool clip = (mClipping != UIDrawCall.Clipping.None);

		Transform t = clip ? transform : (mCam != null ? mCam.transform : null);

		if (t != null)
		{
			Vector3 pos = clip ? new Vector3(mClipRange.x, mClipRange.y) : Vector3.zero;
			Gizmos.matrix = t.localToWorldMatrix;

			if (selected)
			{
				if (mClipping == UIDrawCall.Clipping.SoftClip)
				{
					if (UnityEditor.Selection.activeGameObject == gameObject)
					{
						Gizmos.color = new Color(1f, 0f, 0.5f);
						size.x -= mClipSoftness.x * 2f;
						size.y -= mClipSoftness.y * 2f;
						Gizmos.DrawWireCube(pos, size);
					}
					else
					{
						Gizmos.color = new Color(0.5f, 0f, 0.5f);
						Gizmos.DrawWireCube(pos, size);

						Gizmos.color = new Color(1f, 0f, 0.5f);
						size.x -= mClipSoftness.x * 2f;
						size.y -= mClipSoftness.y * 2f;
						Gizmos.DrawWireCube(pos, size);
					}
				}
				else
				{
					Gizmos.color = new Color(1f, 0f, 0.5f);
					Gizmos.DrawWireCube(pos, size);
				}
			}
			else
			{
				Gizmos.color = new Color(0.5f, 0f, 0.5f);
				Gizmos.DrawWireCube(pos, size);
			}
		}
	}
#endif
}
