//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Base class for all UI components that should be derived from when creating new widget types.
/// </summary>

[ExecuteInEditMode]
[AddComponentMenu("NGUI/UI/NGUI Widget")]
public class UIWidget : MonoBehaviour
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
	[HideInInspector][SerializeField] protected int mWidth = 100;
	[HideInInspector][SerializeField] protected int mHeight = 100;
	[HideInInspector][SerializeField] protected int mDepth = 0;

	protected Vector4 mDrawRegion = new Vector4(0f, 0f, 1f, 1f);

	/// <summary>
	/// Draw region alters how the widget looks without modifying the widget's rectangle.
	/// A region is made up of 4 relative values (0-1 range). The order is Left (X), Bottom (Y), Right (Z) and Top (W).
	/// To have a widget's left edge be 30% from the left side, set X to 0.3. To have the widget's right edge be 30%
	/// from the right hand side, set Z to 0.7.
	/// </summary>

	public Vector4 drawRegion
	{
		get
		{
			return mDrawRegion;
		}
		set
		{
			if (mDrawRegion != value)
			{
				mDrawRegion = value;
				if (autoResizeBoxCollider) ResizeCollider();
				MarkAsChanged();
			}
		}
	}

	/// <summary>
	/// If set to 'true', the box collider's dimensions will be adjusted to always match the widget whenever it resizes.
	/// </summary>

	public bool autoResizeBoxCollider = false;
	
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

	[HideInInspector]
	[System.NonSerialized]
	public UIDrawCall drawCall;

	// Widget's generated geometry
	protected UIGeometry mGeom = new UIGeometry();
	protected Vector3[] mCorners = new Vector3[4];

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
				if (autoResizeBoxCollider) ResizeCollider();
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
				if (autoResizeBoxCollider) ResizeCollider();
				MarkAsChanged();
			}
		}
	}

	/// <summary>
	/// Color used by the widget.
	/// </summary>

	public Color color
	{
		get
		{
			return mColor;
		}
		set
		{
			if (mColor != value)
			{
				mColor = value;
				mChanged = true;
			}
		}
	}

	/// <summary>
	/// Widget's alpha -- a convenience method.
	/// </summary>

	public float alpha
	{
		get
		{
			return mColor.a;
		}
		set
		{
			Color c = mColor;

			if (c.a != value)
			{
				c.a = value;
				color = c;
			}
		}
	}

	/// <summary>
	/// Widget's final alpha, after taking the panel's alpha into account.
	/// </summary>

	public float finalAlpha
	{
		get
		{
			if (mPanel == null) CreatePanel();
			return (mPanel != null) ? mColor.a * mPanel.finalAlpha : mColor.a;
		}
	}

	/// <summary>
	/// Change the pivot point and do not attempt to keep the widget in the same place by adjusting its transform.
	/// </summary>

	public Pivot rawPivot
	{
		get
		{
			return mPivot;
		}
		set
		{
			if (mPivot != value)
			{
				mPivot = value;
				if (autoResizeBoxCollider) ResizeCollider();
				MarkAsChanged();
			}
		}
	}

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
				RemoveFromPanel();
				mDepth = value;
#if UNITY_EDITOR
				UnityEditor.EditorUtility.SetDirty(this);
#endif
				UIPanel.RebuildAllDrawCalls(true);
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
			if (mPanel == null) CreatePanel();
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

			mCorners[0] = new Vector3(x0, y0);
			mCorners[1] = new Vector3(x0, y1);
			mCorners[2] = new Vector3(x1, y1);
			mCorners[3] = new Vector3(x1, y0);

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
	/// Local space region where the actual drawing will take place.
	/// X = left, Y = bottom, Z = right, W = top.
	/// </summary>

	public virtual Vector4 drawingDimensions
	{
		get
		{
			Vector2 offset = pivotOffset;

			float x0 = -offset.x * mWidth;
			float y0 = -offset.y * mHeight;
			float x1 = x0 + mWidth;
			float y1 = y0 + mHeight;

			return new Vector4(
				mDrawRegion.x == 0f ? x0 : Mathf.Lerp(x0, x1, mDrawRegion.x),
				mDrawRegion.y == 0f ? y0 : Mathf.Lerp(y0, y1, mDrawRegion.y),
				mDrawRegion.z == 1f ? x1 : Mathf.Lerp(x0, x1, mDrawRegion.z),
				mDrawRegion.w == 1f ? y1 : Mathf.Lerp(y0, y1, mDrawRegion.w));
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
	/// Shader is used to create a dynamic material if the widget has no material to work with.
	/// </summary>

	public virtual Shader shader
	{
		get
		{
			Material mat = material;
			return (mat != null) ? mat.shader : null;
		}
		set
		{
			throw new System.NotImplementedException(GetType() + " has no shader setter");
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

	/// <summary>
	/// Convenience function that returns 'true' if the widget has a box collider.
	/// </summary>

	public bool hasBoxCollider
	{
		get
		{
			BoxCollider box = collider as BoxCollider;
			return (box != null);
		}
	}

	/// <summary>
	/// Adjust the widget's collider size to match the widget's dimensions.
	/// </summary>

	public void ResizeCollider ()
	{
		if (NGUITools.IsActive(this))
		{
			BoxCollider box = collider as BoxCollider;
			if (box != null) NGUITools.UpdateWidgetCollider(box, true);
		}
	}

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

			Material leftMat = left.material;
			Material rightMat = right.material;

			if (leftMat == rightMat) return 0;
			if (leftMat != null) return -1;
			if (rightMat != null) return 1;
			return (leftMat.GetInstanceID() < rightMat.GetInstanceID()) ? -1 : 1;
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
			drawCall = UIPanel.InsertWidget(this);
		}
	}

	/// <summary>
	/// Remove this widget from the panel.
	/// </summary>

	protected void RemoveFromPanel ()
	{
		UIPanel.RemoveWidget(this);
		mPanel = null;
		list.Remove(this);
#if UNITY_EDITOR
		mOldTex = null;
		mOldShader = null;
#endif
	}

#if UNITY_EDITOR
	Texture mOldTex;
	Shader mOldShader;

	/// <summary>
	/// This callback is sent inside the editor notifying us that some property has changed.
	/// </summary>

	protected virtual void OnValidate()
	{
		mChanged = true;

		// Prior to NGUI 2.7.0 width and height was specified as transform's local scale
		if ((mWidth == 100 || mWidth == minWidth) &&
			(mHeight == 100 || mHeight == minHeight) && cachedTransform.localScale.magnitude > 8f)
		{
			UpgradeFrom265();
			cachedTransform.localScale = Vector3.one;
		}

		if (mWidth < minWidth) mWidth = minWidth;
		if (mHeight < minHeight) mHeight = minHeight;
		if (autoResizeBoxCollider) ResizeCollider();

		// If the texture is changing, we need to make sure to rebuild the draw calls
		if (mOldTex != mainTexture || mOldShader != shader)
		{
			mOldTex = mainTexture;
			mOldShader = shader;
			UIPanel.RemoveWidget(this);
			drawCall = UIPanel.InsertWidget(this);
		}
	}
#endif

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
		if (this == null) return;
		mChanged = true;
#if UNITY_EDITOR
		UnityEditor.EditorUtility.SetDirty(this);
#endif
		// If we're in the editor, update the panel right away so its geometry gets updated.
		if (mPanel != null && enabled && NGUITools.GetActive(gameObject) && !Application.isPlaying)
		{
			SetDirty();
			CheckLayer();
#if UNITY_EDITOR
			// Mark the panel as dirty so it gets updated
			if (material != null) UnityEditor.EditorUtility.SetDirty(mPanel.gameObject);
#endif
		}
	}

	/// <summary>
	/// Ensure we have a panel referencing this widget.
	/// </summary>

	public void CreatePanel ()
	{
		if (mStarted && mPanel == null && enabled && NGUITools.GetActive(gameObject))
		{
			mPanel = UIPanel.Find(cachedTransform, mStarted);

			if (mPanel != null)
			{
				int rd = raycastDepth;
				bool inserted = false;

				// Try to insert this widget at the appropriate location within the list
				for (int i = 0; i < list.size; ++i)
				{
					if (list[i].raycastDepth > rd)
					{
						list.Insert(i, this);
						inserted = true;
						break;
					}
				}

				// Add this widget to the end of the list if it's not already there
				if (!inserted) list.Add(this);

				CheckLayer();
				mChanged = true;
				drawCall = UIPanel.InsertWidget(this);
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
		mChanged = true;
		RemoveFromPanel();

		// Prior to NGUI 2.7.0 width and height was specified as transform's local scale
		if (mWidth == 100 && mHeight == 100 && cachedTransform.localScale.magnitude > 8f)
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
	/// Mark the UI as changed when returning from paused state.
	/// </summary>

	void OnApplicationPause (bool paused) { if (!paused) MarkAsChanged(); }

	/// <summary>
	/// Clear references.
	/// </summary>

	protected virtual void OnDisable () { RemoveFromPanel(); }

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

	public virtual bool canResize { get { return GetComponent<UIStretch>() == null; } }

	/// <summary>
	/// Draw some selectable gizmos.
	/// </summary>

	void OnDrawGizmos ()
	{
		if (isVisible && NGUITools.IsActive(this))
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

	public bool UpdateGeometry (bool forceVisible)
	{
		bool hasMatrix = false;
		float final = finalAlpha;
		bool visibleByAlpha = (final > 0.001f);
		bool visibleByPanel = forceVisible || mVisibleByPanel;
		bool moved = false;

		// Check to see if the widget has moved relative to the panel that manages it
		if (HasTransformChanged())
		{
#if UNITY_EDITOR
			if (!mPanel.widgetsAreStatic || !Application.isPlaying)
#else
			if (!mPanel.widgetsAreStatic)
#endif
			{
				mLocalToPanel = mPanel.worldToLocal * cachedTransform.localToWorldMatrix;
				hasMatrix = true;

				Vector2 offset = pivotOffset;

				float x0 = -offset.x * mWidth;
				float y0 = -offset.y * mHeight;
				float x1 = x0 + mWidth;
				float y1 = y0 + mHeight;

				Transform wt = cachedTransform;

				Vector3 v0 = wt.TransformPoint(x0, y0, 0f);
				Vector3 v1 = wt.TransformPoint(x1, y1, 0f);

				v0 = mPanel.worldToLocal.MultiplyPoint3x4(v0);
				v1 = mPanel.worldToLocal.MultiplyPoint3x4(v1);

				if (Vector3.SqrMagnitude(mOldV0 - v0) > 0.000001f ||
					Vector3.SqrMagnitude(mOldV1 - v1) > 0.000001f)
				{
					moved = true;
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

			if (isVisible && shader != null)
			{
				bool hadVertices = mGeom.hasVertices;
				mGeom.Clear();
				OnFill(mGeom.verts, mGeom.uvs, mGeom.cols);

				if (mGeom.hasVertices)
				{
					// Want to see what's being filled? Uncomment this line.
					//Debug.Log("Fill " + name + " (" + Time.time + ")");

					if (!hasMatrix) mLocalToPanel = mPanel.worldToLocal * cachedTransform.localToWorldMatrix;
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
		else if (moved && mGeom.hasVertices)
		{
			if (!hasMatrix) mLocalToPanel = mPanel.worldToLocal * cachedTransform.localToWorldMatrix;
			mGeom.ApplyTransform(mLocalToPanel);
			return true;
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

	virtual public int minWidth { get { return 2; } }

	/// <summary>
	/// Minimum allowed height for this widget.
	/// </summary>

	virtual public int minHeight { get { return 2; } }

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
