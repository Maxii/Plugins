//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(UIPanel))]
public class UIPanelInspector : Editor
{
	static int s_Hash = "PanelHash".GetHashCode();

	UIPanel mPanel;
	UIWidgetInspector.Action mAction = UIWidgetInspector.Action.None;
	UIWidgetInspector.Action mActionUnderMouse = UIWidgetInspector.Action.None;
	bool mAllowSelection = true;

	Vector3 mStartPos = Vector3.zero;
	Vector4 mStartCR = Vector4.zero;
	Vector3 mStartDrag = Vector3.zero;
	Vector2 mStartMouse = Vector2.zero;
	Vector3 mStartRot = Vector3.zero;
	Vector3 mStartDir = Vector3.right;
	UIWidget.Pivot mDragPivot = UIWidget.Pivot.Center;
	GUIStyle mStyle0 = null;
	GUIStyle mStyle1 = null;

	void OnEnable () { mPanel = target as UIPanel; }

	/// <summary>
	/// Helper function that draws draggable knobs.
	/// </summary>

	void DrawKnob (Vector4 point, int id)
	{
		if (mStyle0 == null) mStyle0 = "sv_label_0";
		if (mStyle1 == null) mStyle1 = "sv_label_7";
		Vector2 screenPoint = HandleUtility.WorldToGUIPoint(point);
		Rect rect = new Rect(screenPoint.x - 7f, screenPoint.y - 7f, 14f, 14f);

		if (mPanel.clipping == UIDrawCall.Clipping.None)
		{
			mStyle0.Draw(rect, GUIContent.none, id);
		}
		else
		{
			mStyle1.Draw(rect, GUIContent.none, id);
		}
	}

	/// <summary>
	/// Handles & interaction.
	/// </summary>

	public void OnSceneGUI ()
	{
		Event e = Event.current;
		int id = GUIUtility.GetControlID(s_Hash, FocusType.Passive);
		EventType type = e.GetTypeForControl(id);
		Transform t = mPanel.cachedTransform;

		Vector3[] handles = UIWidgetInspector.GetHandles(mPanel.worldCorners);

		// Time to figure out what kind of action is underneath the mouse
		UIWidgetInspector.Action actionUnderMouse = mAction;
		bool canResize = (mPanel.clipping != UIDrawCall.Clipping.None);
		UIWidget.Pivot pivotUnderMouse = UIWidgetInspector.GetPivotUnderMouse(handles, e, canResize, ref actionUnderMouse);

		Handles.color = new Color(0.5f, 0f, 0.5f);
		Handles.DrawLine(handles[0], handles[1]);
		Handles.DrawLine(handles[1], handles[2]);
		Handles.DrawLine(handles[2], handles[3]);
		Handles.DrawLine(handles[0], handles[3]);

		switch (type)
		{
			case EventType.Repaint:
			{
				Vector3 bottomLeft = HandleUtility.WorldToGUIPoint(handles[0]);
				Vector3 topRight = HandleUtility.WorldToGUIPoint(handles[2]);
				Vector3 diff = topRight - bottomLeft;
				float mag = diff.magnitude;

				if (mag > 140f)
				{
					Handles.BeginGUI();
					for (int i = 0; i < 8; ++i) DrawKnob(handles[i], id);
					Handles.EndGUI();
				}
				else if (mag > 40f)
				{
					Handles.BeginGUI();
					for (int i = 0; i < 4; ++i) DrawKnob(handles[i], id);
					Handles.EndGUI();
				}
			}
			break;

			case EventType.MouseDown:
			{
				mStartMouse = e.mousePosition;
				mAllowSelection = true;

				if (e.button == 1)
				{
					if (e.modifiers == 0)
					{
						GUIUtility.hotControl = GUIUtility.keyboardControl = id;
						e.Use();
					}
				}
				else if (e.button == 0 && actionUnderMouse != UIWidgetInspector.Action.None &&
					UIWidgetInspector.Raycast(handles, out mStartDrag))
				{
					mStartPos = t.position;
					mStartRot = t.localRotation.eulerAngles;
					mStartDir = mStartDrag - t.position;
					mStartCR = mPanel.clipRange;
					mDragPivot = pivotUnderMouse;
					mActionUnderMouse = actionUnderMouse;
					GUIUtility.hotControl = GUIUtility.keyboardControl = id;
					e.Use();
				}
			}
			break;

			case EventType.MouseUp:
			{
				if (GUIUtility.hotControl == id)
				{
					GUIUtility.hotControl = 0;
					GUIUtility.keyboardControl = 0;

					if (e.button < 2)
					{
						bool handled = false;

						if (e.button == 1)
						{
							// Right-click: Open a context menu listing all widgets underneath
							NGUIEditorTools.ShowSpriteSelectionMenu(e.mousePosition);
							handled = true;
						}
						else if (mAction == UIWidgetInspector.Action.None)
						{
							if (mAllowSelection)
							{
								// Left-click: Select the topmost widget
								NGUIEditorTools.SelectWidget(e.mousePosition);
								handled = true;
							}
						}
						else
						{
							// Finished dragging something
							Vector3 pos = t.localPosition;
							pos.x = Mathf.Round(pos.x);
							pos.y = Mathf.Round(pos.y);
							pos.z = Mathf.Round(pos.z);
							t.localPosition = pos;
							handled = true;
						}

						if (handled) e.Use();
					}

					// Clear the actions
					mActionUnderMouse = UIWidgetInspector.Action.None;
					mAction = UIWidgetInspector.Action.None;
				}
				else if (mAllowSelection)
				{
					BetterList<UIWidget> widgets = NGUIEditorTools.SceneViewRaycast(e.mousePosition);
					if (widgets.size > 0) Selection.activeGameObject = widgets[0].gameObject;
				}
				mAllowSelection = true;
			}
			break;

			case EventType.MouseDrag:
			{
				// Prevent selection once the drag operation begins
				bool dragStarted = (e.mousePosition - mStartMouse).magnitude > 3f;
				if (dragStarted) mAllowSelection = false;

				if (GUIUtility.hotControl == id)
				{
					e.Use();

					if (mAction != UIWidgetInspector.Action.None || mActionUnderMouse != UIWidgetInspector.Action.None)
					{
						Vector3 pos;

						if (UIWidgetInspector.Raycast(handles, out pos))
						{
							if (mAction == UIWidgetInspector.Action.None && mActionUnderMouse != UIWidgetInspector.Action.None)
							{
								// Wait until the mouse moves by more than a few pixels
								if (dragStarted)
								{
									if (mActionUnderMouse == UIWidgetInspector.Action.Move)
									{
										NGUISnap.Recalculate(mPanel);
										mStartPos = t.position;
										NGUIEditorTools.RegisterUndo("Move panel", t);
									}
									else if (mActionUnderMouse == UIWidgetInspector.Action.Rotate)
									{
										mStartRot = t.localRotation.eulerAngles;
										mStartDir = mStartDrag - t.position;
										NGUIEditorTools.RegisterUndo("Rotate panel", t);
									}
									else if (mActionUnderMouse == UIWidgetInspector.Action.Scale)
									{
										mStartPos = t.localPosition;
										mStartCR = mPanel.clipRange;
										mDragPivot = pivotUnderMouse;
										NGUIEditorTools.RegisterUndo("Scale panel", t);
										NGUIEditorTools.RegisterUndo("Scale panel", mPanel);
									}
									mAction = actionUnderMouse;
								}
							}

							if (mAction != UIWidgetInspector.Action.None)
							{
								if (mAction == UIWidgetInspector.Action.Move)
								{
									t.position = mStartPos + (pos - mStartDrag);
									t.localPosition = NGUISnap.Snap(t.localPosition, mPanel.localCorners,
										e.modifiers != EventModifiers.Control);
								}
								else if (mAction == UIWidgetInspector.Action.Rotate)
								{
									Vector3 dir = pos - t.position;
									float angle = Vector3.Angle(mStartDir, dir);

									if (angle > 0f)
									{
										float dot = Vector3.Dot(Vector3.Cross(mStartDir, dir), t.forward);
										if (dot < 0f) angle = -angle;
										angle = mStartRot.z + angle;
										angle = (NGUISnap.allow && e.modifiers != EventModifiers.Control) ?
											Mathf.Round(angle / 15f) * 15f : Mathf.Round(angle);
										t.localRotation = Quaternion.Euler(mStartRot.x, mStartRot.y, angle);
									}
								}
								else if (mAction == UIWidgetInspector.Action.Scale)
								{
									// World-space delta since the drag started
									Vector3 delta = pos - mStartDrag;

									// Adjust the widget's position and scale based on the delta, restricted by the pivot
									AdjustClipping(mPanel, mStartPos, mStartCR, delta, mDragPivot);
								}
							}
						}
					}
				}
			}
			break;

			case EventType.KeyDown:
			{
				if (e.keyCode == KeyCode.UpArrow)
				{
					Vector3 pos = t.localPosition;
					pos.y += 1f;
					t.localPosition = pos;
					e.Use();
				}
				else if (e.keyCode == KeyCode.DownArrow)
				{
					Vector3 pos = t.localPosition;
					pos.y -= 1f;
					t.localPosition = pos;
					e.Use();
				}
				else if (e.keyCode == KeyCode.LeftArrow)
				{
					Vector3 pos = t.localPosition;
					pos.x -= 1f;
					t.localPosition = pos;
					e.Use();
				}
				else if (e.keyCode == KeyCode.RightArrow)
				{
					Vector3 pos = t.localPosition;
					pos.x += 1f;
					t.localPosition = pos;
					e.Use();
				}
				else if (e.keyCode == KeyCode.Escape)
				{
					if (GUIUtility.hotControl == id)
					{
						if (mAction != UIWidgetInspector.Action.None)
						{
							if (mAction == UIWidgetInspector.Action.Move)
							{
								t.position = mStartPos;
							}
							else if (mAction == UIWidgetInspector.Action.Rotate)
							{
								t.localRotation = Quaternion.Euler(mStartRot);
							}
							else if (mAction == UIWidgetInspector.Action.Scale)
							{
								t.position = mStartPos;
								mPanel.clipRange = mStartCR;
							}
						}

						GUIUtility.hotControl = 0;
						GUIUtility.keyboardControl = 0;

						mActionUnderMouse = UIWidgetInspector.Action.None;
						mAction = UIWidgetInspector.Action.None;
						e.Use();
					}
					else Selection.activeGameObject = null;
				}
			}
			break;
		}
	}

	/// <summary>
	/// Draw the inspector widget.
	/// </summary>

	public override void OnInspectorGUI ()
	{
		NGUIEditorTools.SetLabelWidth(80f);
		EditorGUILayout.Space();

		float alpha = EditorGUILayout.Slider("Alpha", mPanel.alpha, 0f, 1f);

		if (alpha != mPanel.alpha)
		{
			NGUIEditorTools.RegisterUndo("Panel Alpha", mPanel);
			mPanel.alpha = alpha;
		}

		GUILayout.BeginHorizontal();
		{
			EditorGUILayout.PrefixLabel("Depth");

			int depth = mPanel.depth;
			if (GUILayout.Button("Back", GUILayout.Width(60f))) --depth;
			depth = EditorGUILayout.IntField(depth, GUILayout.MinWidth(20f));
			if (GUILayout.Button("Forward", GUILayout.Width(68f))) ++depth;

			if (mPanel.depth != depth)
			{
				NGUIEditorTools.RegisterUndo("Panel Depth", mPanel);
				mPanel.depth = depth;

				if (UIPanelTool.instance != null)
					UIPanelTool.instance.Repaint();
			}
		}
		GUILayout.EndHorizontal();

		int matchingDepths = 0;

		for (int i = 0; i < UIPanel.list.size; ++i)
		{
			UIPanel p = UIPanel.list[i];
			if (p != null && mPanel.depth == p.depth)
				++matchingDepths;
		}

		if (matchingDepths > 1)
		{
			EditorGUILayout.HelpBox(matchingDepths + " panels are sharing the depth value of " + mPanel.depth, MessageType.Warning);
		}

		UIDrawCall.Clipping clipping = (UIDrawCall.Clipping)EditorGUILayout.EnumPopup("Clipping", mPanel.clipping);

		if (mPanel.clipping != clipping)
		{
			mPanel.clipping = clipping;
			EditorUtility.SetDirty(mPanel);
		}

		if (mPanel.clipping != UIDrawCall.Clipping.None)
		{
			Vector4 range = mPanel.clipRange;

			GUILayout.BeginHorizontal();
			GUILayout.Space(80f);
			Vector2 pos = EditorGUILayout.Vector2Field("Center", new Vector2(range.x, range.y));
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Space(80f);
			Vector2 size = EditorGUILayout.Vector2Field("Size", new Vector2(range.z, range.w));
			GUILayout.EndHorizontal();

			if (size.x < 0f) size.x = 0f;
			if (size.y < 0f) size.y = 0f;

			range.x = pos.x;
			range.y = pos.y;
			range.z = size.x;
			range.w = size.y;

			if (mPanel.clipRange != range)
			{
				NGUIEditorTools.RegisterUndo("Clipping Change", mPanel);
				mPanel.clipRange = range;
				EditorUtility.SetDirty(mPanel);
			}

			if (mPanel.clipping == UIDrawCall.Clipping.SoftClip)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Space(80f);
				Vector2 soft = EditorGUILayout.Vector2Field("Softness", mPanel.clipSoftness);
				GUILayout.EndHorizontal();

				if (soft.x < 1f) soft.x = 1f;
				if (soft.y < 1f) soft.y = 1f;

				if (mPanel.clipSoftness != soft)
				{
					NGUIEditorTools.RegisterUndo("Clipping Change", mPanel);
					mPanel.clipSoftness = soft;
					EditorUtility.SetDirty(mPanel);
				}
			}
		}

		if (clipping != UIDrawCall.Clipping.None && !NGUIEditorTools.IsUniform(mPanel.transform.lossyScale))
		{
			EditorGUILayout.HelpBox("Clipped panels must have a uniform scale, or clipping won't work properly!", MessageType.Error);

			if (GUILayout.Button("Auto-fix"))
			{
				NGUIEditorTools.FixUniform(mPanel.gameObject);
			}
		}

		if (NGUIEditorTools.DrawHeader("Advanced Options"))
		{
			NGUIEditorTools.BeginContents();

			GUILayout.BeginHorizontal();
			bool norms = EditorGUILayout.Toggle("Normals", mPanel.generateNormals, GUILayout.Width(100f));
			GUILayout.Label("Needed for lit shaders", GUILayout.MinWidth(20f));
			GUILayout.EndHorizontal();

			if (mPanel.generateNormals != norms)
			{
				mPanel.generateNormals = norms;
				UIPanel.RebuildAllDrawCalls(true);
				EditorUtility.SetDirty(mPanel);
			}

			GUILayout.BeginHorizontal();
			bool cull = EditorGUILayout.Toggle("Cull", mPanel.cullWhileDragging, GUILayout.Width(100f));
			GUILayout.Label("Cull widgets while dragging them", GUILayout.MinWidth(20f));
			GUILayout.EndHorizontal();

			if (mPanel.cullWhileDragging != cull)
			{
				mPanel.cullWhileDragging = cull;
				UIPanel.RebuildAllDrawCalls(true);
				EditorUtility.SetDirty(mPanel);
			}

			GUILayout.BeginHorizontal();
			bool alw = EditorGUILayout.Toggle("Visible", mPanel.alwaysOnScreen, GUILayout.Width(100f));
			GUILayout.Label("Check if widgets never go off-screen", GUILayout.MinWidth(20f));
			GUILayout.EndHorizontal();

			if (mPanel.alwaysOnScreen != alw)
			{
				mPanel.alwaysOnScreen = alw;
				UIPanel.RebuildAllDrawCalls(true);
				EditorUtility.SetDirty(mPanel);
			}

			GUILayout.BeginHorizontal();
			bool stat = EditorGUILayout.Toggle("Static", mPanel.widgetsAreStatic, GUILayout.Width(100f));
			GUILayout.Label("Check if widgets won't move", GUILayout.MinWidth(20f));
			GUILayout.EndHorizontal();

			if (mPanel.widgetsAreStatic != stat)
			{
				mPanel.widgetsAreStatic = stat;
				UIPanel.RebuildAllDrawCalls(true);
				EditorUtility.SetDirty(mPanel);
			}

			if (stat)
			{
				EditorGUILayout.HelpBox("Only mark the panel as 'static' if you know FOR CERTAIN that the widgets underneath will not move, rotate, or scale. Doing this improves performance, but moving widgets around will have no effect.", MessageType.Warning);
			}

			GUILayout.BeginHorizontal();
			bool tool = EditorGUILayout.Toggle("Panel Tool", mPanel.showInPanelTool, GUILayout.Width(100f));
			GUILayout.Label("Show in panel tool");
			GUILayout.EndHorizontal();

			if (mPanel.showInPanelTool != tool)
			{
				mPanel.showInPanelTool = !mPanel.showInPanelTool;
				EditorUtility.SetDirty(mPanel);
				EditorWindow.FocusWindowIfItsOpen<UIPanelTool>();
			}
			NGUIEditorTools.EndContents();
		}

		if (GUILayout.Button("Show Draw Calls"))
		{
			NGUISettings.showAllDCs = false;

			if (UIDrawCallViewer.instance != null)
			{
				UIDrawCallViewer.instance.Focus();
				UIDrawCallViewer.instance.Repaint();
			}
			else
			{
				EditorWindow.GetWindow<UIDrawCallViewer>(false, "Draw Call Tool", true);
			}
		}
	}

	/// <summary>
	/// Adjust the panel's position and clipping rectangle.
	/// </summary>

	static void AdjustClipping (UIPanel p, Vector3 startLocalPos, Vector4 startCR, Vector3 worldDelta, UIWidget.Pivot pivot)
	{
		Transform t = p.cachedTransform;
		Transform parent = t.parent;
		Matrix4x4 parentToLocal = (parent != null) ? t.parent.worldToLocalMatrix : Matrix4x4.identity;
		Matrix4x4 worldToLocal = parentToLocal;
		Quaternion invRot = Quaternion.Inverse(t.localRotation);
		worldToLocal = worldToLocal * Matrix4x4.TRS(Vector3.zero, invRot, Vector3.one);
		Vector3 localDelta = worldToLocal.MultiplyVector(worldDelta);

		float left = 0f;
		float right = 0f;
		float top = 0f;
		float bottom = 0f;

		Vector2 dragPivot = NGUIMath.GetPivotOffset(pivot);

		if (dragPivot.x == 0f && dragPivot.y == 1f)
		{
			left = localDelta.x;
			top = localDelta.y;
		}
		else if (dragPivot.x == 0f && dragPivot.y == 0.5f)
		{
			left = localDelta.x;
		}
		else if (dragPivot.x == 0f && dragPivot.y == 0f)
		{
			left = localDelta.x;
			bottom = localDelta.y;
		}
		else if (dragPivot.x == 0.5f && dragPivot.y == 1f)
		{
			top = localDelta.y;
		}
		else if (dragPivot.x == 0.5f && dragPivot.y == 0f)
		{
			bottom = localDelta.y;
		}
		else if (dragPivot.x == 1f && dragPivot.y == 1f)
		{
			right = localDelta.x;
			top = localDelta.y;
		}
		else if (dragPivot.x == 1f && dragPivot.y == 0.5f)
		{
			right = localDelta.x;
		}
		else if (dragPivot.x == 1f && dragPivot.y == 0f)
		{
			right = localDelta.x;
			bottom = localDelta.y;
		}

		AdjustClipping(p, startCR,
			Mathf.RoundToInt(left),
			Mathf.RoundToInt(top),
			Mathf.RoundToInt(right),
			Mathf.RoundToInt(bottom));
	}

	/// <summary>
	/// Adjust the panel's clipping rectangle based on the specified modifier values.
	/// </summary>

	static void AdjustClipping (UIPanel p, Vector4 cr, int left, int top, int right, int bottom)
	{
		// Make adjustment values dividable by two since the clipping is centered
		right	= ((right >> 1) << 1);
		left	= ((left >> 1) << 1);
		bottom	= ((bottom >> 1) << 1);
		top		= ((top >> 1) << 1);

		int x = Mathf.RoundToInt(cr.x + (left + right) * 0.5f);
		int y = Mathf.RoundToInt(cr.y + (top + bottom) * 0.5f);

		int width  = Mathf.RoundToInt(cr.z + right - left);
		int height = Mathf.RoundToInt(cr.w + top - bottom);

		Vector2 soft = p.clipSoftness;
		int minx = Mathf.RoundToInt(Mathf.Max(20f, soft.x));
		int miny = Mathf.RoundToInt(Mathf.Max(20f, soft.y));

		if (width < minx) width = minx;
		if (height < miny) height = miny;

		width  = ((width  >> 1) << 1);
		height = ((height >> 1) << 1);

		p.clipRange = new Vector4(x, y, width, height);
	}
}
