using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using TechTree.Model;

public class TechTreeGraphEditor : SubEditor
{
    Vector2 scroll;
    List<System.Action> postGUI = new List<System.Action> ();
    float maxConstructionTime = 0f;
    Dictionary <string, float> maxResources = new Dictionary<string, float> ();

    bool showCharts = false;

    Blueprint _hotPre;
    Blueprint HotPre {
        set {
            if (_hotPost != null && value != null) {
                AddPrerequisite (value, _hotPost);
                _hotPost = null;
            } else {
                _hotPre = value;
                if(ActiveGroup == null) ActiveGroup = _hotPre.group;
            }
        }
    }
    
    Blueprint _hotPost;
    Blueprint HotPost {
        set {
            if (_hotPre != null && value != null) {
                AddPrerequisite (_hotPre, value);
                _hotPre = null;
            } else {
                _hotPost = value;
                if(ActiveGroup == null) ActiveGroup = _hotPost.group;
            }
        }
    }

    public TechTreeGraphEditor (BlueprintModelEditor editor) : base(editor)
    {
    }
    
    public override void Draw ()
    {
        
        var mX = 0f;
        var mY = 0f;
        maxConstructionTime = 0;
        maxResources.Clear ();


        for (var i=0; i<Component.blueprints.Count; i++) {
            var c = Component.blueprints [i];
            mX = Mathf.Max (mX, c.rect.xMax + 50);
            mY = Mathf.Max (mY, c.rect.yMax + 50);
            maxConstructionTime = Mathf.Max (maxConstructionTime, c.constructTime);
            foreach (var r in c.costs) {
                float maxR = 0;
                maxResources.TryGetValue (r.resource.ID, out maxR);
                maxR = Mathf.Max (maxR, r.qty);
                maxResources [r.resource.ID] = maxR;
            }
        }

        var maxRect = new Rect (0, 0, mX, mY);
        EditorGUI.BeginChangeCheck();
        GUILayout.BeginHorizontal("box");
        if (GUILayout.Button ("New Group", GUILayout.Width(128))) {
            var c = BlueprintModelAsset.Create<BlueprintGroup> (Component);
            Component.groups.Add (c);
            Component.SetDirty ();
        }
        showCharts = GUILayout.Toggle(showCharts, "Show Charts", "button", GUILayout.Width(128));
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        if(EditorGUI.EndChangeCheck()) {
            editor.needRepaint = true;
        }
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        var area = GUILayoutUtility.GetLastRect();
        postGUI.Clear ();
        scroll = GUI.BeginScrollView(area, scroll, maxRect);
        editor.BeginWindows ();

        for (var i=0; i<Component.blueprints.Count; i++) {
            var c = Component.blueprints [i];
            if(c.group.visible) {
                GUI.backgroundColor = c == HotBP ? styles.CREATE_COLOR : styles.NORMAL_COLOR;
                c.rect = GUILayout.Window (i, c.rect, DrawBlueprintNodeWidget, c.ID, styles.nodeWindow, GUILayout.MaxWidth(128), GUILayout.MinWidth(96));
                GUI.backgroundColor = styles.NORMAL_COLOR;
                DrawExternalWidgets (c);
            }
        }


        
        foreach (var a in postGUI) {
            a ();
        }
        var mouseRect = new Rect (Event.current.mousePosition.x, Event.current.mousePosition.y, 1, 1);
        if (_hotPre != null) {
            widget.DrawConnectionCurve (mouseRect, _hotPre.rect);
            //editor.needRepaint = true;
            editor.Repaint();
        }
        if (_hotPost != null) {
            widget.DrawConnectionCurve (_hotPost.rect, mouseRect);
            editor.Repaint();
            //editor.needRepaint = true;
        }
        var e = Event.current;
        if (e.type == EventType.MouseUp) {
            if (_hotPost != null || _hotPre != null) {
                var c = editor.CreateNewBlueprint (ActiveGroup);
                var pos = e.mousePosition;
                c.rect.x = pos.x;
                c.rect.y = pos.y;
                if (_hotPost != null) {
                    AddPrerequisite (c, _hotPost);
                    _hotPost = null;
                }
                if (_hotPre != null) {
                    AddPrerequisite (_hotPre, c);
                    _hotPre = null;
                }
                HotBP = c;
            } else {
                HotBP = null;
            }
            Event.current.Use ();
        }
        editor.EndWindows ();
        GUI.EndScrollView ();

    }

    public void AddPrerequisite (Blueprint child, Blueprint pre)
    {
        var bpr = (from i in child.prerequisites select i.blueprint).ToList ();
        if (!bpr.Contains (pre)) {
            var newPre = BlueprintModelAsset.Create<BlueprintPrerequisite>(Component);
            BlueprintModelAsset.Rename(newPre, pre.ID);
            newPre.blueprint = pre;
            child.prerequisites.Add(newPre);
        }
        
    }
    
    void DrawPrerequisiteConnections (Blueprint c)
    {
        var rect = new Rect (c.rect.x - 12, c.rect.y, 24, c.rect.height);
        GUILayout.BeginArea (rect);
        GUILayout.FlexibleSpace ();
        foreach (var cp in c.prerequisites.ToArray()) {
            if (widget.ControlButton (styles.DELETE_COLOR, "Clear Prerequisite")) {
                c.prerequisites.Remove (cp);
            }
            if (cp.blueprint.group.visible) {
                var buttonRect = GUILayoutUtility.GetLastRect ();
                buttonRect.x += rect.x;
                buttonRect.y += rect.y;
                var srcRect = cp.blueprint.rect;
                srcRect.xMax += 12;
                postGUI.Add (() => widget.DrawConnectionCurve (srcRect, buttonRect));
            }

        }
        if (widget.ControlButton (styles.CREATE_COLOR, "Add New Prerequisite")) {
            HotPre = c;
        }
        GUILayout.FlexibleSpace ();
        GUILayout.EndArea ();
    }
    
    void DrawPostrequisiteConnections (Blueprint c)
    {
        var rect = new Rect (c.rect.xMax, c.rect.y, 24, c.rect.height);
        GUILayout.BeginArea (rect);
        GUILayout.FlexibleSpace ();
        if (widget.ControlButton (styles.CREATE_COLOR, "Connect Prerequisite")) {
            HotPost = c;
        }
        GUILayout.FlexibleSpace ();
        GUILayout.EndArea ();
    }
    
    void DrawExternalWidgets (Blueprint c)
    {
        DrawPrerequisiteConnections (c);
        DrawPostrequisiteConnections (c);
        widget.DrawRect (c.rect, c.group.color, c.group.color, 1);
    }

    void DrawCharts (Blueprint bp)
    {

        GUILayout.Box ("", GUILayout.MinHeight (48), GUILayout.ExpandWidth (true));
        var rect = GUILayoutUtility.GetLastRect ();
        rect.width = Mathf.Min(12, rect.width / Mathf.Max (4, (1 + bp.costs.Count)));
        var bottom = rect.yMax;

        rect.yMin = bottom - (48 * (bp.constructTime / maxConstructionTime));
        widget.DrawRect (rect, styles.TIME_COLOR, styles.TIME_COLOR, -2);

        foreach (var c in bp.costs) {
            GUI.backgroundColor = c.resource.color;
            rect.x += rect.width;
            rect.yMin = bottom - (48 * (c.qty / maxResources [c.resource.ID]));
            widget.DrawRect (rect, c.resource.color, c.resource.color, -2);
        }
    }
    
    void DrawBlueprintWidgetHeader (Blueprint bp)
    {
        widget.DrawRow (() => {
            GUILayout.FlexibleSpace ();
            if (widget.ControlButton (styles.DELETE_COLOR, "Delete")) {
                if (EditorUtility.DisplayDialog ("This operation can not be undone!", "Are you sure you want to remove this Blueprint?", "Yes", "Cancel")) {
                    editor.Delete(bp);
                }
            }
        });

    }
    
    void DrawBlueprintNodeWidget (int unitIndex)
    {
        GUILayout.BeginVertical();
        GUI.contentColor = Color.black;
        var unit = Component.blueprints [unitIndex];
        if (Event.current.type == EventType.MouseUp) {
            if (HotBP != unit) {
                HotBP = unit;
                editor.focusControl = "blueprintID";
                Event.current.Use ();
            }
        }

        var background = GUI.backgroundColor;
        GUI.backgroundColor = unit.ID == null || unit.ID == "" ? styles.MISSING_COLOR : background;
        GUILayout.Space (-18);

        DrawBlueprintWidgetHeader (unit);

        if (unit.isFactory) {
            GUILayout.Label ("Factory", "box", GUILayout.ExpandWidth (true));
        }

        GUILayout.Label (string.Format ("T: {0}", unit.constructTime));
        var costs = "";
        foreach (var c in unit.costs) {
            if(c.qty > 0) {
                if(costs.Length > 0) costs += ", ";
                costs += string.Format ("{0} {1}", c.qty, c.resource.ID);
            }
        }

        GUILayout.Label ("C:" + costs);
        if (unit.prebuilt) {
            GUILayout.Label ("Available On Start", "box", GUILayout.ExpandWidth (true));
        }
        if (unit.allowMultiple) {
            GUILayout.Label ("Allow Multiple", "box", GUILayout.ExpandWidth (true));
        }
        if (unit.mutex) {
            GUILayout.Label ("Mutex", "box", GUILayout.ExpandWidth (true));
        }
        
        //unit.rect = new Rect (unit.rect.x, unit.rect.y, 128, 128);
        GUI.backgroundColor = background;
        if(showCharts) {
            DrawCharts (unit);
        }
        if (Event.current.type == EventType.Repaint) {
            var lastRect = GUILayoutUtility.GetLastRect ();
            //Hack to enable resizing, and reduce wierd rect size jittering.
            if(Mathf.Abs(lastRect.yMax - unit.rect.height) > 5)
                unit.rect.height = lastRect.yMax;
        }

        GUI.DragWindow ();
        GUILayout.EndVertical();
    }
}

