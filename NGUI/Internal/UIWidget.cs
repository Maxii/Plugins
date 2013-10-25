//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Base class for all UI components that should be derived from when creating new widget types.
/// </summary>

public abstract class UIWidget : MonoBehaviour
{
	/// <summary>
	/// List of all the active widgets currently present in the scene.
	/// </summary>

	static public BetterList<UIWidget> list = new BetterList<UIWidget>();

	public enum Pivot
	{
		TopLeft,
		Top,
		TopRight,
		Left,
		Center,
		Right,
		BottomLeft,
		Bottom,
		BottomRight,
	}

	// Cached and saved values
	[HideInInspector][SerializeField] protected Color mColor = Color.white;
	[HideInInspector][SerializeField] protected Pivot mPivot = Pivot.Center;
	[HideInInspector][SerializeField] protected int mWidth = 0;
	[HideInInspector][SerializeField] protected int mHeight = 0;
	[HideInInspector][SerializeField] protected int mDepth = 0;

	protected GameObject mGo;
	protected Transform mTrans;
	protected UIPanel mPanel;

	protected bool mChanged = true;
	protected bool mPlayMode = true;

	bool mStarted = false;
	Vector3 mDiffPos;
	Quaternion mDiffRot;
	Vector3 mDiffScale;
	Matrix4x4 mLocalToPanel;
	bool mVisibleByPanel = true;
	float mLastAlpha = 0f;

	/// <summary>
	/// Internal usage -- draw call that's drawing the widget.
	/// </summary>

	public UIDrawCall drawCall { get; set; }

	// Widget's generated geometry
	UIGeometry mGeom = new UIGeometry();
	Vector3[] mCorners = new Vector3[4];

	/// <summary>
	/// Whether the widget is visible.
	/// </summary>

	public bool isVisible { get { return mVisibleByPanel && finalAlpha > 0.001f; } }

	/// <summary>
	/// Widget's width in pixels.
	/// </summary>

	public int width
	{
		get
		{
			return mWidth;
		}
		set
		{
			int min = minWidth;
			if (value < min) value = min;

			if (mWidth != value)
			{
				mWidth = value;
				MarkAsChanged();
			}
		}
	}

	/// <summary>
	/// Widget's height in pixels.
	/// </summary>

	public int height
	{
		get
		{
			return mHeight;
		}
		set
		{
			int min = minHeight;
			if (value < min) value = min;

			if (mHeight != value)
			{
				mHeight = value;
				MarkAsChanged();
			}
		}
	}

	/// <summary>
	/// Color used by the widget.
	/// </summary>

	public Color color { get { return mColor; } set { if (!mColor.Equals(value)) { mColor = value; mChanged = true; } } }

	/// <summary>
	/// Widget's alpha -- a convenience method.
	/// </summary>

	public float alpha { get { return mColor.a; } set { Color c = mColor; c.a = value; color = c; } }

	/// <summary>
	/// Widget's final alpha, after taking the panel's alpha into account.
	/// </summary>

	public float finalAlpha { get { if (mPanel == null) CreatePanel(); return (mPanel != null) ? mColor.a * mPanel.alpha : mColor.a; } }

	/// <summary>
	/// Set or get the value that specifies where the widget's pivot point should be.
	/// </summary>

	public Pivot pivot
	{
		get
		{
			return mPivot;
		}
		set
		{
			if (mPivot != value)
			{
				Vector3 before = worldCorners[0];

				mPivot = value;
				mChanged = true;

				Vector3 after = worldCorners[0];

				Transform t = cachedTransform;
				Vector3 pos = t.position;
				float z = t.localPosition.z;
				pos.x += (before.x - after.x);
				pos.y += (before.y - after.y);
				cachedTransform.position = pos;

				pos = cachedTransform.localPosition;
				pos.x = Mathf.Round(pos.x);
				pos.y = Mathf.Round(pos.y);
				pos.z = z;
				cachedTransform.localPosition = pos;
			}
		}
	}
	
	/// <summary>
	/// Depth controls the rendering order -- lowest to highest.
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
#if UNITY_EDITOR
				UnityEditor.EditorUtility.SetDirty(this);
#endif
				UIPanel.SetDirty();
			}
		}
	}

	/// <summary>
	/// Raycast depth order on widgets takes the depth of their panel into consideration.
	/// This functionality is used to determine the "final" depth of the widget for drawing and raycasts.
	/// </summary>

	public int raycastDepth
	{
		get
		{
			return (mPanel != null) ? mDepth + mPanel.depth * 1000 : mDepth;
		}
	}

	/// <summary>
	/// Local space corners of the widget. The order is bottom-left, top-left, top-right, bottom-right.
	/// </summary>

	public virtual Vector3[] localCorners
	{
		get
		{
			Vector2 offset = pivotOffset;

			float x0 = -offset.x * mWidth;
			float y0 = -offset.y * mHeight;
			float x1 = x0 + mWidth;
			float y1 = y0 + mHeight;

			mCorners[0] = new Vector3(x0, y0, 0f);
			mCorners[1] = new Vector3(x0, y1, 0f);
			mCorners[2] = new Vector3(x1, y1, 0f);
			mCorners[3] = new Vector3(x1, y0, 0f);

			return mCorners;
		}
	}

	/// <summary>
	/// Local width and height of the widget in pixels.
	/// </summary>

	public virtual Vector2 localSize
	{
		get
		{
			Vector3[] cr = localCorners;
			return cr[2] - cr[0];
		}
	}

	/// <summary>
	/// World-space corners of the widget. The order is bottom-left, top-left, top-right, bottom-right.
	/// </summary>

	public virtual Vector3[] worldCorners
	{
		get
		{
			Vector2 offset = pivotOffset;

			float x0 = -offset.x * mWidth;
			float y0 = -offset.y * mHeight;
			float x1 = x0 + mWidth;
			float y1 = y0 + mHeight;

			Transform wt = cachedTransform;

			mCorners[0] = wt.TransformPoint(x0, y0, 0f);
			mCorners[1] = wt.TransformPoint(x0, y1, 0f);
			mCorners[2] = wt.TransformPoint(x1, y1, 0f);
			mCorners[3] = wt.TransformPoint(x1, y0, 0f);

			return mCorners;
		}
	}

	/// <summary>
	/// World-space inner rect's corners of the widget. The order is bottom-left, top-left, top-right, bottom-right.
	/// </summary>

	public Vector3[] innerWorldCorners
	{
		get
		{
			Vector2 offset = pivotOffset;

			float x0 = -offset.x * mWidth;
			float y0 = -offset.y * mHeight;
			float x1 = x0 + mWidth;
			float y1 = y0 + mHeight;

			Vector4 br = border;

			x0 += br.x;
			y0 += br.y;
			x1 -= br.z;
			y1 -= br.w;

			Transform wt = cachedTransform;

			mCorners[0] = wt.TransformPoint(x0, y0, 0f);
			mCorners[1] = wt.TransformPoint(x0, y1, 0f);
			mCorners[2] = wt.TransformPoint(x1, y1, 0f);
			mCorners[3] = wt.TransformPoint(x1, y0, 0f);

			return mCorners;
		}
	}

	/// <summary>
	/// Whether the widget has some geometry that can be drawn.
	/// </summary>

	public bool hasVertices { get { return mGeom != null && mGeom.hasVertices; } }

	/// <summary>
	/// Helper function that calculates the relative offset based on the current pivot.
	/// X = 0 (left) to 1 (right)
	/// Y = 0 (bottom) to 1 (top)
	/// </summary>

	public Vector2 pivotOffset { get { return NGUIMath.GetPivotOffset(pivot); } }

	/// <summary>
	/// Game object gets cached for speed. Can't simply return 'mGo' set in Awake because this function may be called on a prefab.
	/// </summary>

	public GameObject cachedGameObject { get { if (mGo == null) mGo = gameObject; return mGo; } }

	/// <summary>
	/// Transform gets cached for speed. Can't simply return 'mTrans' set in Awake because this function may be called on a prefab.
	/// </summary>

	public Transform cachedTransform { get { if (mTrans == null) mTrans = transform; return mTrans; } }

	/// <summary>
	/// Material used by the widget.
	/// </summary>

	public virtual Material material
	{
		get
		{
			return null;
		}
		set
		{
			throw new System.NotImplementedException(GetType() + " has no material setter");
		}
	}

	/// <summary>
	/// Texture used by the widget.
	/// </summary>

	public virtual Texture mainTexture
	{
		get
		{
			Material mat = material;
			return (mat != null) ? mat.mainTexture : null;
		}
		set
		{
			throw new System.NotImplementedException(GetType() + " has no mainTexture setter");
		}
	}

	/// <summary>
	/// Returns the UI panel responsible for this widget.
	/// </summary>

	public UIPanel panel { get { if (mPanel == null) CreatePanel(); return mPanel; } set { mPanel = value; } }

	/// <summary>
	/// Do not use this, it's obsolete.
	/// </summary>

	[System.Obsolete("There is no relative scale anymore. Widgets now have width and height instead")]
	public Vector2 relativeSize { get { return Vector2.one; } }

	// Temporary list of widgets, used in the Raycast function in order to avoid repeated allocations.
	//static BetterList<UIWidget> mTemp = new BetterList<UIWidget>();

	/// <summary>
	/// Raycast into the screen and return a list of widgets in order from closest to farthest away.
	/// This is a slow operation and will consider all active widgets.
	/// </summary>

	//static public BetterList<UIWidget> Raycast (Vector2 mousePos, Camera cam, int mask)
	//{
	//    mTemp.Clear();
		
	//    for (int i = list.size; i > 0; )
	//    {
	//        UIWidget w = list[--i];

	//        if ((mask & (1 << w.cachedGameObject.layer)) != 0 && w.mPanel != null)
	//        {
	//            Vector3[] corners = w.worldCorners;
	//            if (NGUIMath.DistanceToRectangle(corners, mousePos, cam) == 0f)
	//                mTemp.Add(w);
	//        }
	//    }
	//    return mTemp;
	//}

	/// <summary>
	/// Static widget comparison function used for depth sorting.
	/// </summary>

	static public int CompareFunc (UIWidget left, UIWidget right)
	{
		int val = UIPanel.CompareFunc(left.mPanel, right.mPanel);

		if (val == 0)
		{
			if (left.mDepth < right.mDepth) return -1;
			if (left.mDepth > right.mDepth) return 1;
		}
		return val;
	}

	/// <summary>
	/// Calculate the widget's bounds, optionally making them relative to the specified transform.
	/// </summary>

	public Bounds CalculateBounds () { return CalculateBounds(null); }

	/// <summary>
	/// Calculate the widget's bounds, optionally making them relative to the specified transform.
	/// </summary>

	public Bounds CalculateBounds (Transform relativeParent)
	{
		if (relativeParent == null)
		{
			Vector3[] corners = localCorners;
			Bounds b = new Bounds(corners[0], Vector3.zero);
			for (int j = 1; j < 4; ++j) b.Encapsulate(corners[j]);
			return b;
		}
		else
		{
			Matrix4x4 toLocal = relativeParent.worldToLocalMatrix;
			Vector3[] corners = worldCorners;
			Bounds b = new Bounds(toLocal.MultiplyPoint3x4(corners[0]), Vector3.zero);
			for (int j = 1; j < 4; ++j) b.Encapsulate(toLocal.MultiplyPoint3x4(corners[j]));
			return b;
		}
	}

	/// <summary>
	/// Mark the widget as changed so that the geometry can be rebuilt.
	/// </summary>

	void SetDirty ()
	{
		if (drawCall != null)
		{
			drawCall.isDirty = true;
		}
		else if (isVisible && hasVertices)
		{
			UIPanel.SetDirty();
		}
	}

	/// <summary>
	/// Remove this widget from the panel.
	/// </summary>

	protected void RemoveFromPanel ()
	{
		if (mPanel != null)
		{
			drawCall = null;
			mPanel = null;
			SetDirty();
		}
	}

	/// <summary>
	/// Only sets the local flag, does not notify the panel.
	/// In most cases you will want to use MarkAsChanged() instead.
	/// </summary>

	public void MarkAsChangedLite () { mChanged = true; }

	/// <summary>
	/// Tell the panel responsible for the widget that something has changed and the buffers need to be rebuilt.
	/// </summary>

	public virtual void MarkAsChanged ()
	{
		mChanged = true;
#if UNITY_EDITOR
		UnityEditor.EditorUtility.SetDirty(this);
#endif
		// If we're in the editor, update the panel right away so its geometry gets updated.
		if (mPanel != null && enabled && NGUITools.GetActive(gameObject) && !Application.isPlaying && material != null)
		{
			SetDirty();
			CheckLayer();
#if UNITY_EDITOR
			// Mark the panel as dirty so it gets updated
			UnityEditor.EditorUtility.SetDirty(mPanel.gameObject);
#endif
		}
	}

	/// <summary>
	/// Ensure we have a panel referencing this widget.
	/// </summary>

	public void CreatePanel ()
	{
		if (mPanel == null && enabled && NGUITools.GetActive(gameObject) && material != null)
		{
			mPanel = UIPanel.Find(cachedTransform, mStarted);

			if (mPanel != null)
			{
				CheckLayer();
				mChanged = true;
				UIPanel.SetDirty();
			}
		}
	}

	/// <summary>
	/// Check to ensure that the widget resides on the same layer as its panel.
	/// </summary>

	public void CheckLayer ()
	{
		if (mPanel != null && mPanel.gameObject.layer != gameObject.layer)
		{
			Debug.LogWarning("You can't place widgets on a layer different than the UIPanel that manages them.\n" +
				"If you want to move widgets to a different layer, parent them to a new panel instead.", this);
			gameObject.layer = mPanel.gameObject.layer;
		}
	}

	/// <summary>
	/// Checks to ensure that the widget is still parented to the right panel.
	/// </summary>

	public void ParentHasChanged ()
	{
		if (mPanel != null)
		{
			UIPanel p = UIPanel.Find(cachedTransform);

			if (mPanel != p)
			{
				RemoveFromPanel();
				CreatePanel();
			}
		}
	}

	/// <summary>
	/// Remember whether we're in play mode.
	/// </summary>

	protected virtual void Awake ()
	{
		mGo = gameObject;
		mPlayMode = Application.isPlaying;
	}

	/// <summary>
	/// Mark the widget and the panel as having been changed.
	/// </summary>

	protected virtual void OnEnable ()
	{
		list.Add(this);
		mChanged = true;
		mPanel = null;

		// Prior to NGUI 2.7.0 width and height was specified as transform's local scale
		if (mWidth == 0 && mHeight == 0)
		{
			UpgradeFrom265();
			cachedTransform.localScale = Vector3.one;
#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(this);
#endif
		}
	}

	/// <summary>
	/// Facilitates upgrading from NGUI 2.6.5 or earlier versions.
	/// </summary>

	protected virtual void UpgradeFrom265 ()
	{
		Vector3 scale = cachedTransform.localScale;
		mWidth = Mathf.Abs(Mathf.RoundToInt(scale.x));
		mHeight = Mathf.Abs(Mathf.RoundToInt(scale.y));
		if (GetComponent<BoxCollider>() != null)
			NGUITools.AddWidgetCollider(gameObject, true);
	}

	/// <summary>
	/// Set the depth, call the virtual start function, and sure we have a panel to work with.
	/// </summary>

	void Start ()
	{
		mStarted = true;
		OnStart();
		CreatePanel();
	}

	/// <summary>
	/// Ensure that we have a panel to work with. The reason the panel isn't added in OnEnable()
	/// is because OnEnable() is called right after Awake(), which is a problem when the widget
	/// is brought in on a prefab object as it happens before it gets parented.
	/// </summary>

	public virtual void Update ()
	{
		// Ensure we have a panel to work with by now
		if (mPanel == null) CreatePanel();
#if UNITY_EDITOR
		else if (!Application.isPlaying) ParentHasChanged();
#endif
	}

	/// <summary>
	/// Clear references.
	/// </summary>

	protected virtual void OnDisable ()
	{
		list.Remove(this);
		RemoveFromPanel();
	}

	/// <summary>
	/// Unregister this widget.
	/// </summary>

	void OnDestroy () { RemoveFromPanel(); }

#if UNITY_EDITOR
	static int mHandles = -1;

	/// <summary>
	/// Whether widgets will show handles with the Move Tool, or just the View Tool.
	/// </summary>

	static public bool showHandlesWithMoveTool
	{
		get
		{
			if (mHandles == -1)
			{
				mHandles = UnityEditor.EditorPrefs.GetInt("NGUI Handles", 1);
			}
			return (mHandles == 1);
		}
		set
		{
			int val = value ? 1 : 0;

			if (mHandles != val)
			{
				mHandles = val;
				UnityEditor.EditorPrefs.SetInt("NGUI Handles", mHandles);
			}
		}
	}

	/// <summary>
	/// Whether the widget should have some form of handles shown.
	/// </summary>

	static public bool showHandles
	{
		get
		{
			if (showHandlesWithMoveTool)
			{
				return UnityEditor.Tools.current == UnityEditor.Tool.Move;
			}
			return UnityEditor.Tools.current == UnityEditor.Tool.View;
		}
	}

	/// <summary>
	/// Whether the widget can be resized using drag handles.
	/// </summary>

	public virtual bool canResize { get { return true; } }

	/// <summary>
	/// Draw some selectable gizmos.
	/// </summary>

	void OnDrawGizmos ()
	{
		if (isVisible && hasVertices && mPanel != null)
		{
			if (UnityEditor.Selection.activeGameObject == gameObject && showHandles) return;

			Color outline = new Color(1f, 1f, 1f, 0.2f);

			Vector2 offset = pivotOffset;
			Vector3 center = new Vector3(mWidth * (0.5f - offset.x), mHeight * (0.5f - offset.y), -mDepth * 0.25f);
			Vector3 size = new Vector3(mWidth, mHeight, 1f);

			// Draw the gizmo
			Gizmos.matrix = cachedTransform.localToWorldMatrix;
			Gizmos.color = (UnityEditor.Selection.activeGameObject == cachedTransform) ? Color.white : outline;
			Gizmos.DrawWireCube(center, size);

			// Make the widget selectable
			size.z = 0.01f;
			Gizmos.color = Color.clear;
			Gizmos.DrawCube(center, size);
		}
	}
#endif // UNITY_EDITOR

#if UNITY_3_5 || UNITY_4_0
	Vector3 mOldPos;
	Quaternion mOldRot;
	Vector3 mOldScale;
#endif

	/// <summary>
	/// Whether the transform has changed since the last time it was checked.
	/// </summary>

	bool HasTransformChanged ()
	{
#if UNITY_3_5 || UNITY_4_0
		Transform t = cachedTransform;
		
		if (t.position != mOldPos || t.rotation != mOldRot || t.lossyScale != mOldScale)
		{
			mOldPos = t.position;
			mOldRot = t.rotation;
			mOldScale = t.lossyScale;
			return true;
		}
#else
		if (cachedTransform.hasChanged)
		{
		    mTrans.hasChanged = false;
		    return true;
		}
#endif
		return false;
	}

	bool mForceVisible = false;
	Vector3 mOldV0;
	Vector3 mOldV1;

	/// <summary>
	/// Update the widget and fill its geometry if necessary. Returns whether something was changed.
	/// </summary>

	public bool UpdateGeometry (UIPanel p, bool forceVisible)
	{
		if (material != null && p != null)
		{
			mPanel = p;
			bool hasMatrix = false;
			float final = finalAlpha;
			bool visibleByAlpha = (final > 0.001f);
			bool visibleByPanel = forceVisible || mVisibleByPanel;

			// Has transform moved?
			if (HasTransformChanged())
			{
				// Check to see if the widget has moved relative to the panel that manages it
#if UNITY_EDITOR
				if (!mPanel.widgetsAreStatic || !Application.isPlaying)
#else
				if (!mPanel.widgetsAreStatic)
#endif
				{
					mLocalToPanel = p.worldToLocal * cachedTransform.localToWorldMatrix;
					hasMatrix = true;

					Vector2 offset = pivotOffset;

					float x0 = -offset.x * mWidth;
					float y0 = -offset.y * mHeight;
					float x1 = x0 + mWidth;
					float y1 = y0 + mHeight;

					Transform wt = cachedTransform;

					Vector3 v0 = wt.TransformPoint(x0, y0, 0f);
					Vector3 v1 = wt.TransformPoint(x1, y1, 0f);

					v0 = p.worldToLocal.MultiplyPoint3x4(v0);
					v1 = p.worldToLocal.MultiplyPoint3x4(v1);

					if (Vector3.SqrMagnitude(mOldV0 - v0) > 0.000001f ||
						Vector3.SqrMagnitude(mOldV1 - v1) > 0.000001f)
					{
						mChanged = true;
						mOldV0 = v0;
						mOldV1 = v1;
					}
				}

				// Is the widget visible by the panel?
				if (visibleByAlpha || mForceVisible != forceVisible)
				{
					mForceVisible = forceVisible;
					visibleByPanel = forceVisible || mPanel.IsVisible(this);
				}
			}
			else if (visibleByAlpha && mForceVisible != forceVisible)
			{
				mForceVisible = forceVisible;
				visibleByPanel = mPanel.IsVisible(this);
			}

			// Is the visibility changing?
			if (mVisibleByPanel != visibleByPanel)
			{
				mVisibleByPanel = visibleByPanel;
				mChanged = true;
			}

			// Has the alpha changed?
			if (mVisibleByPanel && mLastAlpha != final) mChanged = true;
			mLastAlpha = final;

			if (mChanged)
			{
				mChanged = false;

				if (isVisible)
				{
					bool hadVertices = mGeom.hasVertices;
					mGeom.Clear();
					OnFill(mGeom.verts, mGeom.uvs, mGeom.cols);

					if (mGeom.hasVertices)
					{
						// Want to see what's being filled? Uncomment this line.
						//Debug.Log("Fill " + name + " (" + Time.time + ")");

						if (!hasMatrix) mLocalToPanel = p.worldToLocal * cachedTransform.localToWorldMatrix;
						mGeom.ApplyTransform(mLocalToPanel);
						return true;
					}
					return hadVertices;
				}
				else if (mGeom.hasVertices)
				{
					mGeom.Clear();
					return true;
				}
			}
		}
		return false;
	}

	/// <summary>
	/// Append the local geometry buffers to the specified ones.
	/// </summary>

	public void WriteToBuffers (BetterList<Vector3> v, BetterList<Vector2> u, BetterList<Color32> c, BetterList<Vector3> n, BetterList<Vector4> t)
	{
		mGeom.WriteToBuffers(v, u, c, n, t);
	}

	/// <summary>
	/// Make the widget pixel-perfect.
	/// </summary>

	virtual public void MakePixelPerfect ()
	{
		Vector3 pos = cachedTransform.localPosition;
		pos.z = Mathf.Round(pos.z);
		pos.x = Mathf.Round(pos.x);
		pos.y = Mathf.Round(pos.y);
		cachedTransform.localPosition = pos;

		Vector3 ls = cachedTransform.localScale;
		cachedTransform.localScale = new Vector3(Mathf.Sign(ls.x), Mathf.Sign(ls.y), 1f);
	}

	/// <summary>
	/// Minimum allowed width for this widget.
	/// </summary>

	virtual public int minWidth { get { return 4; } }

	/// <summary>
	/// Minimum allowed height for this widget.
	/// </summary>

	virtual public int minHeight { get { return 4; } }

	/// <summary>
	/// Dimensions of the sprite's border, if any.
	/// </summary>

	virtual public Vector4 border { get { return Vector4.zero; } }

	/// <summary>
	/// Virtual Start() functionality for widgets.
	/// </summary>

	virtual protected void OnStart () { }

	/// <summary>
	/// Virtual function called by the UIPanel that fills the buffers.
	/// </summary>

	virtual public void OnFill(BetterList<Vector3> verts, BetterList<Vector2> uvs, BetterList<Color32> cols) { }
}
